import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { APP_INITIALIZER, Provider } from '@angular/core';
import { CsrfService } from './services/csrf.service';
import { MockCsrfService } from './services/mock-csrf.service';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';
import { environment } from './environments/environment';
import { BrandingService } from './services/branding.service';
import { MockBrandingService } from './services/mock-branding.service';
import { AuthService } from './services/auth.service';
import { MockAuthService } from './services/mock-auth.service';
import { ReportService } from './services/report.service';
import { MockReportService } from './services/mock-report.service';
import { SupportService } from './services/support.service';
import { MockSupportService } from './services/mock-support.service';
import { SubscriptionService } from './services/subscription.service';
import { MockSubscriptionService } from './services/mock-subscription.service';

// Factory functions for real services
export function initCsrf(csrf: CsrfService) {
  return () => csrf.init();
}

// Configure providers based on environment
const providers: Provider[] = [
  provideHttpClient(withInterceptorsFromDi()),
  provideRouter(routes),
];

// Conditionally add real or mock services based on environment
if (environment.features.useMocks) {
  // Use mock services
  providers.push(
    { provide: CsrfService, useClass: MockCsrfService },
    { provide: BrandingService, useClass: MockBrandingService },
    { provide: AuthService, useClass: MockAuthService },
    { provide: ReportService, useClass: MockReportService },
    { provide: SupportService, useClass: MockSupportService },
    { provide: SubscriptionService, useClass: MockSubscriptionService }
  );
} else {
  // Use real services
  providers.push(
    { provide: APP_INITIALIZER, useFactory: initCsrf, deps: [CsrfService], multi: true }
  );
}

bootstrapApplication(AppComponent, {
  providers
}).catch(err => console.error(err));