import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../components/header/header.component';
import { FooterComponent } from '../components/footer/footer.component';
import { ToastContainerComponent } from '../components/common/toast/toast-container.component';
import { ErrorHandlerService } from '../services/error-handler.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    FooterComponent,
    ToastContainerComponent
  ],
  template: `
    <div class="app">
      <app-header></app-header>
      
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <app-footer></app-footer>
      <app-toast-container></app-toast-container>
    </div>
  `,
  styles: [`
    .app {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
    }
  `]
})
export class AppComponent {
  constructor(private errorHandler: ErrorHandlerService) {
    // Set up global error handler
    window.addEventListener('error', (event) => {
      this.errorHandler.handleError(event.error, 'Global Error Handler');
    });

    window.addEventListener('unhandledrejection', (event) => {
      this.errorHandler.handleError(event.reason, 'Unhandled Promise Rejection');
    });
  }
}
