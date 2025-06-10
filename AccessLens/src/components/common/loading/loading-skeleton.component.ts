import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="skeleton-container">
      <div 
        *ngFor="let line of lines" 
        class="skeleton-line"
        [style.width]="line.width"
        [style.height]="line.height"
      ></div>
    </div>
  `,
  styles: [`
    .skeleton-container {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .skeleton-line {
      background: linear-gradient(90deg, var(--gray-200) 25%, var(--gray-100) 50%, var(--gray-200) 75%);
      background-size: 200% 100%;
      border-radius: var(--radius-sm);
      animation: shimmer 1.5s infinite;
    }

    @keyframes shimmer {
      0% {
        background-position: -200% 0;
      }
      100% {
        background-position: 200% 0;
      }
    }
  `]
})
export class LoadingSkeletonComponent {
  @Input() type: 'card' | 'list' | 'table' | 'custom' = 'card';
  @Input() count: number = 3;
  @Input() customLines: Array<{width: string, height: string}> = [];

  get lines(): Array<{width: string, height: string}> {
    if (this.type === 'custom') {
      return this.customLines;
    }

    const patterns: Record<'card' | 'list' | 'table', Array<{width: string, height: string}>> = {
      card: [
        { width: '100%', height: '200px' },
        { width: '80%', height: '16px' },
        { width: '60%', height: '14px' }
      ],
      list: [
        { width: '100%', height: '16px' },
        { width: '80%', height: '14px' },
        { width: '90%', height: '14px' }
      ],
      table: [
        { width: '100%', height: '40px' },
        { width: '100%', height: '32px' },
        { width: '100%', height: '32px' }
      ]
    };

    return Array(this.count).fill(0).flatMap(() => patterns[this.type as keyof typeof patterns]);
  }
}