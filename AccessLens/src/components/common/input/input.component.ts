import { Component, Input, Output, EventEmitter, forwardRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { IconComponent } from '../icons/icon.component';

export type InputSize = 'sm' | 'md' | 'lg';
export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="input-group" [class.has-error]="hasError">
      <label *ngIf="label" [for]="inputId" class="input-label">
        {{ label }}
        <span *ngIf="required" class="required-indicator" aria-label="required">*</span>
      </label>
      
      <div class="input-wrapper" [class]="getWrapperClasses()">
        <div *ngIf="prefixIcon" class="input-icon input-prefix" aria-hidden="true">
          <app-icon [name]="prefixIcon" [size]="16"></app-icon>
        </div>
        
        <input
          [id]="inputId"
          [type]="type"
          [placeholder]="placeholder"
          [value]="value"
          [disabled]="disabled"
          [readonly]="readonly"
          [required]="required"
          [attr.aria-describedby]="getAriaDescribedBy()"
          [attr.aria-invalid]="hasError"
          [class]="getInputClasses()"
          (input)="onInput($event)"
          (blur)="onBlur($event)"
          (focus)="onFocus($event)"
        />
        
        <div *ngIf="suffixIcon" class="input-icon input-suffix" aria-hidden="true">
          <app-icon [name]="suffixIcon" [size]="16"></app-icon>
        </div>
      </div>
      
      <div *ngIf="helpText && !hasError" [id]="inputId + '-help'" class="input-help">
        {{ helpText }}
      </div>
      
      <div *ngIf="hasError && errorMessage" [id]="inputId + '-error'" class="input-error" role="alert">
        {{ errorMessage }}
      </div>
    </div>
  `,
  styleUrls: ['./input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ]
})
export class InputComponent implements ControlValueAccessor {
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() type: InputType = 'text';
  @Input() size: InputSize = 'md';
  @Input() disabled: boolean = false;
  @Input() readonly: boolean = false;
  @Input() required: boolean = false;
  @Input() helpText: string = '';
  @Input() errorMessage: string = '';
  @Input() hasError: boolean = false;
  @Input() prefixIcon: string = '';
  @Input() suffixIcon: string = '';
  @Input() inputId: string = `input-${Math.random().toString(36).substr(2, 9)}`;
  @Input() value: string = '';
  
  @Output() inputChange = new EventEmitter<string>();
  @Output() blur = new EventEmitter<Event>();
  @Output() focus = new EventEmitter<Event>();
  
  private onChange = (value: string) => {};
  private onTouched = () => {};

  getInputClasses(): string {
    const classes = ['input'];
    
    if (this.size !== 'md') {
      classes.push(`input-${this.size}`);
    }
    
    if (this.prefixIcon) {
      classes.push('input-with-prefix');
    }
    
    if (this.suffixIcon) {
      classes.push('input-with-suffix');
    }
    
    return classes.join(' ');
  }

  getWrapperClasses(): string {
    return '';
  }

  getAriaDescribedBy(): string {
    const describedBy = [];
    if (this.helpText && !this.hasError) {
      describedBy.push(`${this.inputId}-help`);
    }
    if (this.hasError && this.errorMessage) {
      describedBy.push(`${this.inputId}-error`);
    }
    return describedBy.join(' ') || '';
  }

  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value = target.value;
    this.onChange(this.value);
    this.inputChange.emit(this.value);
  }

  onBlur(event: Event): void {
    this.onTouched();
    this.blur.emit(event);
  }

  onFocus(event: Event): void {
    this.focus.emit(event);
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    this.value = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}