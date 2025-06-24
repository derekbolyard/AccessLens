import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../environments/environment';
import { ToastService } from '../components/common/toast/toast.service';
import * as Sentry from '@sentry/angular';

export interface ErrorReport {
  message: string;
  stack?: string;
  url?: string;
  timestamp: Date;
  userAgent: string;
  userId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  private readonly MAX_ERROR_LOGS = 50;
  private errorLogs: ErrorReport[] = [];
  
  constructor(
    private toastService: ToastService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}
  
  handleError(error: any, context?: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    const errorReport: ErrorReport = {
      message: error.message || 'Unknown error',
      stack: error.stack,
      url: window.location.href,
      timestamp: new Date(),
      userAgent: navigator.userAgent
    };

    // Log to console in development
    if (!environment.production) {
      console.error(`Error in ${context || 'Application'}:`, error);
    }

    // Store error in memory log (limited size)
    this.logError(errorReport);

    // Send to error reporting service in production
    if (environment.production && environment.features.enableErrorReporting) {
      this.sendErrorReport(error, context);
    }
  }

  private logError(errorReport: ErrorReport): void {
    // Add to in-memory log with size limit
    this.errorLogs.push(errorReport);
    if (this.errorLogs.length > this.MAX_ERROR_LOGS) {
      this.errorLogs.shift(); // Remove oldest error
    }
  }

  private sendErrorReport(error: any, context?: string): void {
    if (environment.features.enableErrorReporting) {
      // Add context to the error
      Sentry.captureException(error, {
        tags: {
          component: context || 'unknown'
        },
        level: 'error'
      });
    }
  }

  handleApiError(error: any): string {
    if (error.status === 0) {
      return 'Unable to connect to the server. Please check your internet connection.';
    }
    
    if (error.status >= 400 && error.status < 500) {
      return error.error?.message || 'Invalid request. Please check your input and try again.';
    }
    
    if (error.status >= 500) {
      return 'Server error. Please try again later or contact support if the problem persists.';
    }
    
    return 'An unexpected error occurred. Please try again.';
  }
  
  // Get error logs for debugging
  getErrorLogs(): ErrorReport[] {
    return [...this.errorLogs];
  }
  
  // Clear error logs
  clearErrorLogs(): void {
    this.errorLogs = [];
  }
  
  // Set user context for error reporting
  setUserContext(userId: string, email?: string): void {
    if (environment.features.enableErrorReporting) {
      Sentry.setUser({
        id: userId,
        email: email
      });
    }
  }
  
  // Clear user context
  clearUserContext(): void {
    if (environment.features.enableErrorReporting) {
      Sentry.setUser(null);
    }
  }
}