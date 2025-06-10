import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type InputSize = 'sm' | 'md' | 'lg';
export type InputType = 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './input.component.html',
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

  getIconSvg(icon: string): string {
    const icons: { [key: string]: string } = {
      'search': '<circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>',
      'email': '<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/>',
      'lock': '<rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><circle cx="12" cy="16" r="1"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>',
      'user': '<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>',
      'phone': '<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>',
      'globe': '<circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>'
    };
    
    return icons[icon] || '';
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