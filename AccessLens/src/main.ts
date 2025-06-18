import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { APP_INITIALIZER } from '@angular/core';
import { CsrfService } from './services/csrf.service';
import { provideRouter } from '@angular/router';
import { routes } from './app/app.routes';

export function initCsrf(csrf: CsrfService) {
  // Angular calls this _before_ the first component.
  return () => csrf.init();        // returns Promise<void>
}

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    { provide: APP_INITIALIZER, useFactory: initCsrf, deps: [CsrfService], multi: true },
    provideRouter(routes),
  ]
}).catch(err => console.error(err));