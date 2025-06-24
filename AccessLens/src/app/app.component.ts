import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { HeaderComponent } from '../components/header/header.component';
import { FooterComponent } from '../components/footer/footer.component';
import { ToastContainerComponent } from '../components/common/toast/toast-container.component';
import { AuthModalComponent } from '../components/auth/auth-modal.component';
import { ErrorHandlerService } from '../services/error-handler.service';
import { AuthService, User } from '../services/auth.service';
import { filter } from 'rxjs/operators';
import { AnalyticsService } from '../services/analytics.service';
import * as Sentry from '@sentry/angular';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    FooterComponent,
    ToastContainerComponent,
    AuthModalComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app">
      <!-- Show auth modal if user is not authenticated -->
      <app-auth-modal
        [isOpen]="!currentUser && !isCheckingAuth"
        (close)="onAuthModalClose()"
        (authSuccess)="onAuthSuccess($event)"
      ></app-auth-modal>

      <!-- Show main app if user is authenticated -->
      <div *ngIf="currentUser" class="app-content">
        <app-header></app-header>
        
        <main id="main-content" class="main-content" role="main" tabindex="-1">
          <router-outlet></router-outlet>
        </main>

        <app-footer></app-footer>
      </div>

      <!-- Show loading state while checking auth -->
      <div *ngIf="isCheckingAuth" class="auth-loading" role="status" aria-live="polite" aria-label="Loading application">
        <div class="loading-container">
          <div class="loading-spinner" aria-hidden="true"></div>
          <p>Loading...</p>
          <span class="sr-only">Application is loading, please wait</span>
        </div>
      </div>

      <app-toast-container></app-toast-container>
    </div>
  `,
  styles: [`
    .app {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .app-content {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
    }

    .main-content:focus {
      outline: none;
    }

    .auth-loading {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: var(--slate-50);
    }

    .loading-container {
      text-align: center;
      padding: var(--space-8);
      background: white;
      border: 4px solid var(--slate-900);
      box-shadow: var(--shadow-pro-brutal);
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 4px solid var(--slate-200);
      border-top: 4px solid var(--blue-600);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto var(--space-4);
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-container p {
      color: var(--slate-600);
      font-size: var(--text-lg);
      font-weight: 600;
      margin: 0;
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }
  `]
})
export class AppComponent implements OnInit {
  currentUser: User | null = null;
  isCheckingAuth = true;
  private hasNavigatedAfterAuth = false;

  constructor(
    private errorHandler: ErrorHandlerService,
    private authService: AuthService,
    private router: Router,
    private analyticsService: AnalyticsService,
    private cdr: ChangeDetectorRef
  ) {
    // Set up global error handler
    window.addEventListener('error', (event) => {
      this.errorHandler.handleError(event.error, 'Global Error Handler');
    });

    window.addEventListener('unhandledrejection', (event) => {
      this.errorHandler.handleError(event.reason, 'Unhandled Promise Rejection');
    });
    
    // Track page views
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.analyticsService.trackPageView(event.urlAfterRedirects);
      
      // Focus main content after navigation for screen readers
      setTimeout(() => {
        const mainContent = document.getElementById('main-content');
        if (mainContent) {
          mainContent.focus();
        }
      }, 100);
    });
  }

  ngOnInit(): void {
    // Subscribe to auth state changes
    this.authService.user$.subscribe(user => {
      const wasAuthenticated = !!this.currentUser;
      this.currentUser = user;
      this.isCheckingAuth = false;
      this.cdr.markForCheck();

      // If user just authenticated (first login), navigate to dashboard
      if (user && !wasAuthenticated && !this.hasNavigatedAfterAuth) {
        this.hasNavigatedAfterAuth = true;
        // Use setTimeout to ensure the navigation happens after the current change detection cycle
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 0);
      }

      // Reset navigation flag when user logs out
      if (!user) {
        this.hasNavigatedAfterAuth = false;
      }
    });
  }

  onAuthModalClose(): void {
    // Don't allow closing the auth modal if not authenticated
    // The modal will stay open until user successfully authenticates
  }

  onAuthSuccess(user: User): void {
    // The auth service will handle setting the user
    // Navigation will be handled by the user$ subscription above
  }
}