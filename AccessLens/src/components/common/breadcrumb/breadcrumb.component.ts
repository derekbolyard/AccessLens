import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconComponent } from '../icons/icon.component';

export interface BreadcrumbItem {
  label: string;
  route?: string;
  icon?: string;
  action?: () => void;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="breadcrumb-nav" aria-label="Breadcrumb">
      <div class="breadcrumb-container">
        <div class="breadcrumb-items">
          <ng-container *ngFor="let item of items; let i = index; let isLast = last">
            <button 
              class="breadcrumb-item"
              [class.current]="isLast"
              [disabled]="isLast"
              (click)="onItemClick(item)"
            >
              <app-icon *ngIf="item.icon" [name]="item.icon" [size]="16"></app-icon>
              {{ item.label }}
            </button>
            
            <app-icon 
              *ngIf="!isLast" 
              class="breadcrumb-separator" 
              name="arrow-right"
              [size]="16"
            ></app-icon>
          </ng-container>
        </div>
        
        <div class="breadcrumb-actions">
          <ng-content select="[slot=actions]"></ng-content>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .breadcrumb-nav {
      background: white;
      border: 4px solid var(--slate-900);
      box-shadow: var(--shadow-pro-brutal);
      margin-bottom: var(--space-6);
    }

    .breadcrumb-container {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: var(--space-4) var(--space-6);
      gap: var(--space-4);
    }

    .breadcrumb-items {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      flex-wrap: wrap;
    }

    .breadcrumb-item {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      background: var(--slate-100);
      border: 2px solid var(--slate-900);
      color: var(--slate-900);
      font-size: var(--text-sm);
      cursor: pointer;
      padding: var(--space-2) var(--space-3);
      transition: all 0.2s ease;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.025em;
      box-shadow: 2px 2px 0px 0px var(--slate-800);
    }

    .breadcrumb-item:hover:not(:disabled) {
      background-color: var(--blue-100);
      color: var(--blue-700);
      transform: translate(-1px, -1px);
      box-shadow: 3px 3px 0px 0px var(--slate-800);
    }

    .breadcrumb-item.current {
      background-color: var(--blue-600);
      color: white;
      cursor: default;
      box-shadow: inset 2px 2px 0px 0px var(--blue-800);
    }

    .breadcrumb-separator {
      color: var(--slate-400);
      margin: 0 var(--space-1);
    }

    .breadcrumb-actions {
      display: flex;
      gap: var(--space-2);
    }

    @media (max-width: 768px) {
      .breadcrumb-container {
        flex-direction: column;
        gap: var(--space-3);
        align-items: flex-start;
      }

      .breadcrumb-items {
        width: 100%;
      }

      .breadcrumb-actions {
        width: 100%;
        justify-content: flex-end;
      }
    }
  `]
})
export class BreadcrumbComponent {
  @Input() items: BreadcrumbItem[] = [];
  @Output() itemClick = new EventEmitter<BreadcrumbItem>();

  onItemClick(item: BreadcrumbItem): void {
    if (item.action) {
      item.action();
    }
    this.itemClick.emit(item);
  }
}