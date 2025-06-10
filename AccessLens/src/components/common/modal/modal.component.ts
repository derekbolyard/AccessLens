import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | 'full';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() size: ModalSize = 'md';
  @Input() showCloseButton: boolean = true;
  @Input() showFooter: boolean = false;
  @Input() closeOnOverlayClick: boolean = true;
  
  @Output() close = new EventEmitter<void>();

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isOpen) {
      this.close.emit();
    }
  }

  getModalClasses(): string {
    return `modal-${this.size}`;
  }

  onOverlayClick(event: Event): void {
    if (this.closeOnOverlayClick && event.target === event.currentTarget) {
      this.close.emit();
    }
  }
}