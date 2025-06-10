import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type LoadingType = 'spinner' | 'dots' | 'pulse' | 'skeleton';
export type LoadingSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loading.component.html',
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

  get skeletonLinesArray(): number[] {
    return Array(this.skeletonLines).fill(0);
  }
}