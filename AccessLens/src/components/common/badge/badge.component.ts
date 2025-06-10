import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
export type BadgeSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './badge.component.html',
  styleUrls: ['./badge.component.scss']
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'primary';
  @Input() size: BadgeSize = 'md';
  @Input() outline: boolean = false;
  @Input() dot: boolean = false;
  @Input() dismissible: boolean = false;
  @Input() icon: string = '';
  
  @Output() dismiss = new EventEmitter<Event>();

  getBadgeClasses(): string {
    const classes = ['badge'];
    
    classes.push(`badge-${this.variant}`);
    
    if (this.size !== 'md') {
      classes.push(`badge-${this.size}`);
    }
    
    if (this.outline) {
      classes.push('badge-outline');
    }
    
    if (this.dot) {
      classes.push('badge-dot');
    }
    
    return classes.join(' ');
  }

  getIconSvg(): string {
    const icons: { [key: string]: string } = {
      'check': '<polyline points="20,6 9,17 4,12"/>',
      'x': '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>',
      'alert': '<circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>',
      'info': '<circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>',
      'star': '<polygon points="12,2 15.09,8.26 22,9.27 17,14.14 18.18,21.02 12,17.77 5.82,21.02 7,14.14 2,9.27 8.91,8.26"/>',
      'heart': '<path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>'
    };
    
    return icons[this.icon] || '';
  }

  onDismiss(event: Event): void {
    event.stopPropagation();
    this.dismiss.emit(event);
  }
}