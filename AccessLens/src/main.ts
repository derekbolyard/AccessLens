import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { APP_INITIALIZER, ErrorHandler, importProvidersFrom } from '@angular/core';
import { CsrfService } from './services/csrf.service';
import { provideRouter, withPreloading, PreloadAllModules } from '@angular/router';
import { routes } from './app/app.routes';
import { environment } from './environments/environment';
import * as Sentry from '@sentry/angular';

// Initialize Sentry if error reporting is enabled
if (environment.features.enableErrorReporting && environment.sentryDsn) {
  Sentry.init({
    dsn: environment.sentryDsn,
    integrations: [
      Sentry.browserTracingIntegration(),
    ],
    
    // Set tracesSampleRate to 1.0 to capture 100% of transactions for performance monitoring
    // We recommend adjusting this value in production
    tracesSampleRate: environment.production ? 0.2 : 1.0,
    
    // Set sampleRate to 1.0 to capture 100% of errors
    sampleRate: environment.production ? 0.5 : 1.0,
    
    // Only enable in production or when explicitly enabled
    enabled: environment.production || environment.features.enableErrorReporting,
    
    // Environment tag for better filtering in Sentry dashboard
    environment: environment.production ? 'production' : 'development',
  });
}

export function initCsrf(csrf: CsrfService) {
  console.info('BUILD MODE:', environment.production ? 'prod' : 'dev');
  // Angular calls this _before_ the first component.
  return () => csrf.init();        // returns Promise<void>
}

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    { provide: APP_INITIALIZER, useFactory: initCsrf, deps: [CsrfService], multi: true },
    provideRouter(
      routes,
      withPreloading(PreloadAllModules) // Preload all lazy-loaded modules after initial load
    ),
    // Sentry error handler
    ...(environment.features.enableErrorReporting ? [
      {
        provide: ErrorHandler,
        useValue: Sentry.createErrorHandler({
          showDialog: false, // Don't show the Sentry report dialog
        }),
      }
    ] : [])
  ]
}).catch(err => console.error(err));