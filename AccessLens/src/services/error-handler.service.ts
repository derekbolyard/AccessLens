import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

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
  
  handleError(error: any, context?: string): void {
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

    // Send to error reporting service in production
    if (environment.production && environment.features.enableErrorReporting) {
      this.sendErrorReport(errorReport);
    }
  }

  private sendErrorReport(errorReport: ErrorReport): void {
    // In a real app, this would send to an error reporting service like Sentry
    console.log('Error report would be sent:', errorReport);
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
}