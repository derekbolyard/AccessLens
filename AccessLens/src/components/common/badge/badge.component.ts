import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export type BadgeVariant = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info';
export type BadgeSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="getBadgeClasses()">
      <app-icon *ngIf="icon" [name]="icon" [size]="getIconSize()"></app-icon>
      <ng-content></ng-content>
      <button *ngIf="dismissible" class="badge-dismiss" (click)="onDismiss($event)">
        <app-icon name="x" [size]="12"></app-icon>
      </button>
    </span>
  `,
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

  getIconSize(): number {
    switch (this.size) {
      case 'sm': return 10;
      case 'lg': return 14;
      default: return 12;
    }
  }

  onDismiss(event: Event): void {
    event.stopPropagation();
    this.dismiss.emit(event);
  }
}