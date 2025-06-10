import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'ghost';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type IconPosition = 'left' | 'right';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './button.component.html',
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

  getIconSvg(): string {
    const icons: { [key: string]: string } = {
      'plus': '<path d="M3 3v18h18V3z"/><path d="M8 12h8"/><path d="M12 8v8"/>',
      'check': '<polyline points="20,6 9,17 4,12"/>',
      'x': '<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>',
      'arrow-left': '<polyline points="15,18 9,12 15,6"/>',
      'arrow-right': '<polyline points="9,6 15,12 9,18"/>',
      'external-link': '<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><polyline points="15,3 21,3 21,9"/><line x1="10" y1="14" x2="21" y2="3"/>',
      'refresh': '<polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="m3 4 5 5-5 5"/><path d="m21 20-5-5 5-5"/>',
      'download': '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7,10 12,15 17,10"/><line x1="12" y1="15" x2="12" y2="3"/>',
      'upload': '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17,8 12,3 7,8"/><line x1="12" y1="3" x2="12" y2="15"/>',
      'edit': '<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="m18.5 2.5 3 3L12 15l-4 1 1-4 9.5-9.5z"/>',
      'trash': '<polyline points="3,6 5,6 21,6"/><path d="m19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/>',
      'eye': '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>',
      'settings': '<circle cx="12" cy="12" r="3"/><path d="m12 1 1.09 3.26L16 5l-1.74 2.74L17 10l-3.26 1.09L12 14l-1.74-2.91L7 10l1.74-2.74L7 5l3.26-.74L12 1z"/>'
    };
    
    return icons[this.icon] || '';
  }

  onClick(event: Event): void {
    if (!this.disabled && !this.loading) {
      this.click.emit(event);
    }
  }
}