import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ToastService, Toast } from './toast.service';
import { AlertComponent } from '../alert/alert.component';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, AlertComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toast-container">
      <div 
        *ngFor="let toast of toasts; trackBy: trackByToastId"
        class="toast-item"
      >
        <app-alert
          [variant]="toast.type"
          [title]="toast.title || ''"
          [dismissible]="toast.dismissible ?? false"
          (dismiss)="dismissToast(toast.id)"
        >
          {{ toast.message }}
        </app-alert>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: var(--space-4);
      right: var(--space-4);
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      max-width: 400px;
      pointer-events: none;
    }

    .toast-item {
      pointer-events: auto;
    }

    @media (max-width: 768px) {
      .toast-container {
        left: var(--space-4);
        right: var(--space-4);
        max-width: none;
      }
    }
  `]
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  private subscription?: Subscription;

  constructor(
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    if (this.toastService?.toasts$) {
      this.subscription = this.toastService.toasts$.subscribe(toasts => {
        this.toasts = toasts || [];
        this.cdr.markForCheck();
      });
    } else {
      this.toasts = [];
    }
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  trackByToastId(index: number, toast: Toast): string {
    return toast.id;
  }

  dismissToast(id: string): void {
    if (this.toastService?.dismiss) {
      this.toastService.dismiss(id);
    }
  }
}