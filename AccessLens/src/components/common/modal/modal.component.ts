import { Component, Input, Output, EventEmitter, HostListener, ChangeDetectionStrategy, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | 'full';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="modal-overlay" *ngIf="isOpen" (click)="onOverlayClick($event)" role="dialog" aria-modal="true" [attr.aria-labelledby]="title ? 'modal-title' : null">
      <div #modalContainer class="modal-container" [class]="getModalClasses()" tabindex="-1">
        <div class="modal-header" *ngIf="title || showCloseButton">
          <h3 *ngIf="title" id="modal-title" class="modal-title">{{ title }}</h3>
          <button 
            *ngIf="showCloseButton"
            class="modal-close"
            (click)="close.emit()"
            aria-label="Close modal"
            type="button"
          >
            <app-icon name="x" [size]="20"></app-icon>
          </button>
        </div>
        
        <div class="modal-body">
          <ng-content></ng-content>
        </div>
        
        <div class="modal-footer" *ngIf="showFooter">
          <ng-content select="[slot=footer]"></ng-content>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./modal.component.scss']
})
export class ModalComponent implements AfterViewInit {
  @Input() isOpen: boolean = false;
  @Input() title: string = '';
  @Input() size: ModalSize = 'md';
  @Input() showCloseButton: boolean = true;
  @Input() showFooter: boolean = false;
  @Input() closeOnOverlayClick: boolean = true;
  
  @Output() close = new EventEmitter<void>();

  @ViewChild('modalContainer') modalContainer!: ElementRef;

  private previousActiveElement: HTMLElement | null = null;

  @HostListener('document:keydown.escape', ['$event'])
  onEscapeKey(event: KeyboardEvent): void {
    if (this.isOpen) {
      this.close.emit();
    }
  }

  ngAfterViewInit(): void {
    if (this.isOpen) {
      this.handleModalOpen();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.handleModalOpen();
    } else {
      this.handleModalClose();
    }
  }

  private handleModalOpen(): void {
    // Store the currently focused element
    this.previousActiveElement = document.activeElement as HTMLElement;
    
    // Focus the modal container
    setTimeout(() => {
      if (this.modalContainer) {
        this.modalContainer.nativeElement.focus();
      }
    }, 0);

    // Prevent body scroll
    document.body.style.overflow = 'hidden';
  }

  private handleModalClose(): void {
    // Restore focus to the previously focused element
    if (this.previousActiveElement) {
      this.previousActiveElement.focus();
      this.previousActiveElement = null;
    }

    // Restore body scroll
    document.body.style.overflow = '';
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