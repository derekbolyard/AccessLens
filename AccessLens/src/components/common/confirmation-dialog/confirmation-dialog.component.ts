import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../modal/modal.component';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, ModalComponent, ButtonComponent],
  template: `
    <app-modal 
      [isOpen]="isOpen"
      [title]="title"
      size="sm"
      [showFooter]="true"
      (close)="onCancel()"
    >
      <div class="confirmation-content">
        <div class="confirmation-icon" [class]="iconClass">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path [attr.d]="iconPath"/>
          </svg>
        </div>
        <p class="confirmation-message">{{ message }}</p>
      </div>

      <div slot="footer">
        <app-button 
          variant="secondary" 
          [disabled]="isProcessing"
          (click)="onCancel()"
        >
          {{ cancelText }}
        </app-button>
        <app-button 
          [variant]="confirmVariant" 
          [loading]="isProcessing"
          [disabled]="isProcessing"
          (click)="onConfirm()"
        >
          {{ isProcessing ? processingText : confirmText }}
        </app-button>
      </div>
    </app-modal>
  `,
  styles: [`
    .confirmation-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-4);
      text-align: center;
      padding: var(--space-4) 0;
    }

    .confirmation-icon {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .confirmation-icon.warning {
      background-color: var(--warning-100);
      color: var(--warning-600);
    }

    .confirmation-icon.error {
      background-color: var(--error-100);
      color: var(--error-600);
    }

    .confirmation-icon.info {
      background-color: var(--primary-100);
      color: var(--primary-600);
    }

    .confirmation-message {
      margin: 0;
      color: var(--gray-700);
      line-height: 1.5;
    }
  `]
})
export class ConfirmationDialogComponent {
  @Input() isOpen = false;
  @Input() title = 'Confirm Action';
  @Input() message = 'Are you sure you want to proceed?';
  @Input() confirmText = 'Confirm';
  @Input() cancelText = 'Cancel';
  @Input() processingText = 'Processing...';
  @Input() confirmVariant: 'primary' | 'error' | 'warning' = 'primary';
  @Input() type: 'warning' | 'error' | 'info' = 'warning';
  @Input() isProcessing = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  get iconClass(): string {
    return this.type;
  }

  get iconPath(): string {
    const icons = {
      warning: 'M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z M12 9v4 M12 17h.01',
      error: 'M12 2L2 7v10c0 5.55 3.84 10 9 9s9-4.45 9-9V7l-10-5z M12 8v4 M12 16h.01',
      info: 'M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z M12 6v6 M12 16h.01'
    };
    return icons[this.type];
  }

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}