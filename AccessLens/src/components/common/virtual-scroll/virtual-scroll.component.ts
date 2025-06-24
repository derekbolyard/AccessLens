import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectionStrategy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, fromEvent } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';

export interface VirtualScrollItem {
  id: string | number;
  data: any;
}

@Component({
  selector: 'app-virtual-scroll',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div 
      #scrollContainer 
      class="virtual-scroll-container"
      [style.height.px]="containerHeight"
      (scroll)="onScroll($event)"
    >
      <div 
        class="virtual-scroll-spacer"
        [style.height.px]="totalHeight"
      >
        <div 
          class="virtual-scroll-content"
          [style.transform]="'translateY(' + offsetY + 'px)'"
        >
          <ng-container *ngFor="let item of visibleItems; trackBy: trackByFn">
            <ng-content [ngTemplateOutlet]="itemTemplate" [ngTemplateOutletContext]="{ $implicit: item, index: item.index }"></ng-content>
          </ng-container>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .virtual-scroll-container {
      overflow-y: auto;
      position: relative;
    }

    .virtual-scroll-spacer {
      position: relative;
    }

    .virtual-scroll-content {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
    }
  `]
})
export class VirtualScrollComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() items: VirtualScrollItem[] = [];
  @Input() itemHeight: number = 50;
  @Input() containerHeight: number = 400;
  @Input() buffer: number = 3;
  @Input() itemTemplate: any;
  
  @Output() scrolledToBottom = new EventEmitter<void>();
  @Output() scrolledToTop = new EventEmitter<void>();
  
  @ViewChild('scrollContainer') scrollContainer!: ElementRef;
  
  visibleItems: (VirtualScrollItem & { index: number })[] = [];
  offsetY: number = 0;
  totalHeight: number = 0;
  
  private destroy$ = new Subject<void>();
  private lastScrollTop = 0;
  private isScrollingDown = true;
  
  ngOnInit(): void {
    this.updateVisibleItems();
  }
  
  ngAfterViewInit(): void {
    // Listen for window resize events
    fromEvent(window, 'resize')
      .pipe(
        debounceTime(100),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.updateVisibleItems();
      });
  }
  
  ngOnChanges(): void {
    this.totalHeight = this.items.length * this.itemHeight;
    this.updateVisibleItems();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  onScroll(event: Event): void {
    const target = event.target as HTMLElement;
    const scrollTop = target.scrollTop;
    
    // Determine scroll direction
    this.isScrollingDown = scrollTop > this.lastScrollTop;
    this.lastScrollTop = scrollTop;
    
    // Check if scrolled to bottom
    if (scrollTop + target.clientHeight >= target.scrollHeight - 20) {
      this.scrolledToBottom.emit();
    }
    
    // Check if scrolled to top
    if (scrollTop <= 20) {
      this.scrolledToTop.emit();
    }
    
    this.updateVisibleItems();
  }
  
  private updateVisibleItems(): void {
    if (!this.scrollContainer) return;
    
    const scrollTop = this.scrollContainer.nativeElement.scrollTop;
    const viewportHeight = this.scrollContainer.nativeElement.clientHeight;
    
    // Calculate the range of visible items
    const startIndex = Math.max(0, Math.floor(scrollTop / this.itemHeight) - this.buffer);
    const endIndex = Math.min(
      this.items.length - 1,
      Math.floor((scrollTop + viewportHeight) / this.itemHeight) + this.buffer
    );
    
    // Update the visible items
    this.visibleItems = this.items
      .slice(startIndex, endIndex + 1)
      .map((item, i) => ({ ...item, index: startIndex + i }));
    
    // Update the offset
    this.offsetY = startIndex * this.itemHeight;
  }
  
  trackByFn(index: number, item: VirtualScrollItem): string | number {
    return item.id;
  }
}