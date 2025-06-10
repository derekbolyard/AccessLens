import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type CardVariant = 'default' | 'primary' | 'success' | 'warning' | 'error';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './card.component.html',
  styleUrls: ['./card.component.scss']
})
export class CardComponent {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() variant: CardVariant = 'default';
  @Input() hover: boolean = false;
  @Input() clickable: boolean = false;
  @Input() elevated: boolean = false;
  @Input() bordered: boolean = false;
  @Input() noPadding: boolean = false;
  @Input() showFooter: boolean = false;
  @Input() headerAction: boolean = false;
  
  @Output() cardClick = new EventEmitter<Event>();

  getCardClasses(): string {
    const classes = ['card'];
    
    if (this.variant !== 'default') {
      classes.push(`card-${this.variant}`);
    }
    
    if (this.hover) {
      classes.push('card-hover');
    }
    
    if (this.clickable) {
      classes.push('card-clickable');
    }
    
    if (this.elevated) {
      classes.push('card-elevated');
    }
    
    if (this.bordered) {
      classes.push('card-bordered');
    }
    
    return classes.join(' ');
  }

  onClick(event: Event): void {
    if (this.clickable) {
      this.cardClick.emit(event);
    }
  }
}