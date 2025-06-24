import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

export type LoadingType = 'spinner' | 'dots' | 'pulse' | 'skeleton';
export type LoadingSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="getLoadingClasses()" [attr.aria-label]="getAriaLabel()" role="status">
      <div *ngIf="type === 'spinner'" class="loading-spinner" aria-hidden="true"></div>
      <div *ngIf="type === 'dots'" class="loading-dots" aria-hidden="true">
        <div class="dot"></div>
        <div class="dot"></div>
        <div class="dot"></div>
      </div>
      <div *ngIf="type === 'pulse'" class="loading-pulse" aria-hidden="true"></div>
      <div *ngIf="type === 'skeleton'" class="loading-skeleton" aria-hidden="true">
        <div class="skeleton-line" *ngFor="let line of skeletonLinesArray"></div>
      </div>
      
      <p *ngIf="text" class="loading-text">{{ text }}</p>
      <span *ngIf="!text" class="sr-only">Loading...</span>
    </div>
  `,
  styleUrls: ['./loading.component.scss']
})
export class LoadingComponent {
  @Input() type: LoadingType = 'spinner';
  @Input() size: LoadingSize = 'md';
  @Input() text: string = '';
  @Input() center: boolean = false;
  @Input() overlay: boolean = false;
  @Input() skeletonLines: number = 3;

  getLoadingClasses(): string {
    const classes = ['loading'];
    
    if (this.size !== 'md') {
      classes.push(`loading-${this.size}`);
    }
    
    if (this.center) {
      classes.push('loading-center');
    }
    
    if (this.overlay) {
      classes.push('loading-overlay');
    }
    
    return classes.join(' ');
  }

  getAriaLabel(): string {
    return this.text || 'Loading content';
  }

  get skeletonLinesArray(): number[] {
    return Array(this.skeletonLines).fill(0);
  }
}