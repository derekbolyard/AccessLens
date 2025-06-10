import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type AlertVariant = 'success' | 'warning' | 'error' | 'info';
export type AlertSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alert.component.html',
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

  getIconSvg(): string {
    const icons: { [key: string]: string } = {
      'success': '<polyline points="20,6 9,17 4,12"/>',
      'warning': '<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>',
      'error': '<circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/>',
      'info': '<circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>'
    };
    
    return icons[this.variant] || icons['info'];
  }

  onDismiss(): void {
    this.dismissed = true;
    this.dismiss.emit();
  }
}