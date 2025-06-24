import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../modal/modal.component';
import { ButtonComponent } from '../button/button.component';
import { IconComponent } from '../icons/icon.component';

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, ModalComponent, ButtonComponent, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
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
          <app-icon [name]="getIconName()" [size]="24"></app-icon>
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

  getIconName(): string {
    switch (this.type) {
      case 'warning': return 'issues';
      case 'error': return 'x';
      case 'info': return 'info';
      default: return 'info';
    }
  }

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}