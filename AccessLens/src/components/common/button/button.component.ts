import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export type ButtonVariant = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'ghost';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type IconPosition = 'left' | 'right';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button 
      [class]="getButtonClasses()"
      [disabled]="disabled"
      [type]="type"
      (click)="onClick($event)"
    >
      <app-icon 
        *ngIf="icon && iconPosition === 'left'" 
        [name]="icon"
        [size]="getIconSize()"
      ></app-icon>
      <span *ngIf="loading" class="loading-spinner"></span>
      <ng-content></ng-content>
      <app-icon 
        *ngIf="icon && iconPosition === 'right'" 
        [name]="icon"
        [size]="getIconSize()"
      ></app-icon>
    </button>
  `,
  styleUrls: ['./button.component.scss']
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() disabled: boolean = false;
  @Input() loading: boolean = false;
  @Input() fullWidth: boolean = false;
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() icon: string = '';
  @Input() iconPosition: IconPosition = 'left';
  
  @Output() click = new EventEmitter<Event>();

  getButtonClasses(): string {
    const classes = ['btn'];
    
    classes.push(`btn-${this.variant}`);
    
    if (this.size !== 'md') {
      classes.push(`btn-${this.size}`);
    }
    
    if (this.fullWidth) {
      classes.push('btn-full');
    }
    
    return classes.join(' ');
  }

  getIconSize(): number {
    switch (this.size) {
      case 'xs': return 12;
      case 'sm': return 14;
      case 'lg': return 20;
      case 'xl': return 24;
      default: return 16;
    }
  }

  onClick(event: Event): void {
    if (!this.disabled && !this.loading) {
      this.click.emit(event);
    }
  }
}