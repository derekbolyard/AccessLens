import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export type AlertVariant = 'success' | 'warning' | 'error' | 'info';
export type AlertSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-alert',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="getAlertClasses()" *ngIf="!dismissed">
      <div class="alert-icon" *ngIf="showIcon">
        <app-icon [name]="getIconName()" [size]="getIconSize()"></app-icon>
      </div>
      
      <div class="alert-content">
        <h4 *ngIf="title" class="alert-title">{{ title }}</h4>
        <div class="alert-message">
          <ng-content></ng-content>
        </div>
      </div>
      
      <button 
        *ngIf="dismissible" 
        class="alert-dismiss"
        (click)="onDismiss()"
        aria-label="Dismiss alert"
      >
        <app-icon name="x" [size]="16"></app-icon>
      </button>
    </div>
  `,
  styleUrls: ['./alert.component.scss']
})
export class AlertComponent {
  @Input() variant: AlertVariant = 'info';
  @Input() size: AlertSize = 'md';
  @Input() title: string = '';
  @Input() dismissible: boolean = false;
  @Input() showIcon: boolean = true;
  @Input() bordered: boolean = false;
  
  @Output() dismiss = new EventEmitter<void>();

  dismissed: boolean = false;

  getAlertClasses(): string {
    const classes = ['alert'];
    
    classes.push(`alert-${this.variant}`);
    
    if (this.size !== 'md') {
      classes.push(`alert-${this.size}`);
    }
    
    if (this.bordered) {
      classes.push('alert-bordered');
    }
    
    return classes.join(' ');
  }

  getIconName(): string {
    switch (this.variant) {
      case 'success': return 'check';
      case 'warning': return 'issues';
      case 'error': return 'x';
      case 'info': return 'info';
      default: return 'info';
    }
  }

  getIconSize(): number {
    switch (this.size) {
      case 'sm': return 14;
      case 'lg': return 20;
      default: return 16;
    }
  }

  onDismiss(): void {
    this.dismissed = true;
    this.dismiss.emit();
  }
}