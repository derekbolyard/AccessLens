import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IconService } from './icon.service';

@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg 
      [attr.width]="size" 
      [attr.height]="size" 
      [attr.viewBox]="iconDef?.viewBox || '0 0 24 24'"
      fill="none" 
      stroke="currentColor" 
      [attr.stroke-width]="strokeWidth"
      stroke-linecap="round" 
      stroke-linejoin="round"
      [class]="cssClass"
    >
      <path [attr.d]="iconDef?.path" *ngIf="iconDef?.path"></path>
    </svg>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
    
    svg {
      flex-shrink: 0;
    }
  `]
})
export class IconComponent {
  @Input() name: string = '';
  @Input() size: number = 16;
  @Input() strokeWidth: number = 2;
  @Input() cssClass: string = '';

  constructor(private iconService: IconService) {}

  get iconDef() {
    return this.iconService.getIcon(this.name);
  }
}