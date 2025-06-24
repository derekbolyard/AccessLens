import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export interface PageContextInfo {
  title: string;
  subtitle?: string;
  icon?: string;
  stats?: Array<{
    label: string;
    value: string | number;
    variant?: 'default' | 'success' | 'warning' | 'error';
  }>;
}

@Component({
  selector: 'app-page-context',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-context">
      <div class="context-header">
        <div class="context-info">
          <div class="context-title-section">
            <div class="context-icon" *ngIf="context.icon">
              <app-icon [name]="context.icon" [size]="24"></app-icon>
            </div>
            <div>
              <h1 class="context-title">{{ context.title }}</h1>
              <p *ngIf="context.subtitle" class="context-subtitle">{{ context.subtitle }}</p>
            </div>
          </div>
        </div>
        
        <div class="context-stats" *ngIf="context.stats && context.stats.length > 0">
          <div 
            *ngFor="let stat of context.stats" 
            class="stat-item"
            [class]="'stat-' + (stat.variant || 'default')"
          >
            <span class="stat-value">{{ stat.value }}</span>
            <span class="stat-label">{{ stat.label }}</span>
          </div>
        </div>
      </div>
      
      <div class="context-actions" *ngIf="hasActions">
        <ng-content select="[slot=actions]"></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .page-context {
      background: white;
      padding: var(--space-8);
      border: 6px solid var(--slate-900);
      box-shadow: var(--shadow-pro-brutal-lg);
      margin-bottom: var(--space-8);
      position: relative;
      overflow: hidden;
    }

    .page-context::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 8px;
      background: linear-gradient(90deg, var(--blue-600), var(--success-600), var(--warning-600));
      z-index: 1;
    }

    .context-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: var(--space-8);
      position: relative;
      z-index: 2;
    }

    .context-title-section {
      display: flex;
      align-items: flex-start;
      gap: var(--space-4);
    }

    .context-icon {
      width: 60px;
      height: 60px;
      background: var(--blue-100);
      border: 4px solid var(--blue-600);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--blue-700);
      box-shadow: 2px 2px 0px 0px var(--blue-600);
      flex-shrink: 0;
    }

    .context-title {
      margin: 0 0 var(--space-2) 0;
      color: var(--slate-900);
      font-weight: 900;
      font-size: var(--text-4xl);
      text-transform: uppercase;
      letter-spacing: -0.02em;
      line-height: 1.1;
    }

    .context-subtitle {
      margin: 0;
      color: var(--slate-600);
      font-size: var(--text-lg);
      font-weight: 600;
    }

    .context-stats {
      display: flex;
      gap: var(--space-4);
      align-items: center;
    }

    .stat-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: var(--space-3);
      border: 3px solid var(--slate-900);
      background: var(--slate-100);
      box-shadow: 2px 2px 0px 0px var(--slate-800);
      min-width: 80px;
      transition: all 0.2s ease;
    }

    .stat-item:hover {
      transform: translate(-1px, -1px);
      box-shadow: 3px 3px 0px 0px var(--slate-800);
    }

    .stat-item.stat-success {
      background: var(--success-100);
      border-color: var(--success-600);
    }

    .stat-item.stat-warning {
      background: var(--warning-100);
      border-color: var(--warning-600);
    }

    .stat-item.stat-error {
      background: var(--error-100);
      border-color: var(--error-600);
    }

    .stat-value {
      font-size: var(--text-2xl);
      font-weight: 900;
      color: var(--slate-900);
      line-height: 1;
      text-transform: uppercase;
    }

    .stat-label {
      font-size: var(--text-xs);
      color: var(--slate-600);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-top: var(--space-1);
      font-weight: 700;
    }

    .context-actions {
      margin-top: var(--space-6);
      padding-top: var(--space-6);
      border-top: 3px solid var(--slate-900);
      position: relative;
      z-index: 2;
    }

    @media (max-width: 768px) {
      .context-header {
        flex-direction: column;
        gap: var(--space-4);
        align-items: flex-start;
      }

      .context-title-section {
        flex-direction: column;
        gap: var(--space-3);
        align-items: flex-start;
      }

      .context-icon {
        width: 50px;
        height: 50px;
      }

      .context-title {
        font-size: var(--text-2xl);
      }

      .context-stats {
        flex-wrap: wrap;
        gap: var(--space-3);
        justify-content: flex-start;
      }

      .stat-item {
        min-width: 70px;
        padding: var(--space-2);
      }

      .page-context {
        padding: var(--space-4);
      }
    }
  `]
})
export class PageContextComponent {
  @Input() context!: PageContextInfo;
  @Input() hasActions = false;
}