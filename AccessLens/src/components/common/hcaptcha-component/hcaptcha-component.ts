import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ElementRef, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

declare global {
  interface Window {
    hcaptcha: any;
  }
}

@Component({
  selector: 'app-hcaptcha',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div #hcaptchaContainer></div>`,
  styles: [`
    :host {
      display: block;
    }

    /* Style the hCaptcha container */
    ::ng-deep .h-captcha {
      margin: 0 auto;
    }

    /* Ensure proper spacing in forms */
    :host {
      margin: var(--space-4) 0;
    }
  `]
})
export class HCaptchaComponent implements OnInit, OnDestroy {
  @Input() siteKey!: string;
  @Input() theme: 'light' | 'dark' = 'light';
  @Input() size: 'normal' | 'compact' = 'normal';
  
  @Output() success = new EventEmitter<string>();
  @Output() error = new EventEmitter<void>();
  @Output() expired = new EventEmitter<void>();

  @ViewChild('hcaptchaContainer', { static: true }) hcaptchaContainer!: ElementRef;

  private widgetId: string | null = null;
  private isScriptLoaded = false;
  private scriptElement: HTMLScriptElement | null = null;

  ngOnInit(): void {
    this.loadHCaptchaScript().then(() => {
      this.renderCaptcha();
    });
  }

  ngOnDestroy(): void {
    if (this.widgetId && window.hcaptcha) {
      window.hcaptcha.remove(this.widgetId);
    }
    
    // Clean up script element if it exists
    if (this.scriptElement && document.head.contains(this.scriptElement)) {
      document.head.removeChild(this.scriptElement);
    }
  }

  private async loadHCaptchaScript(): Promise<void> {
    if (this.isScriptLoaded || window.hcaptcha) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      this.scriptElement = document.createElement('script');
      this.scriptElement.src = 'https://js.hcaptcha.com/1/api.js';
      this.scriptElement.async = true;
      this.scriptElement.defer = true;
      
      this.scriptElement.onload = () => {
        this.isScriptLoaded = true;
        resolve();
      };
      
      this.scriptElement.onerror = () => {
        reject(new Error('Failed to load hCaptcha script'));
      };

      document.head.appendChild(this.scriptElement);
    });
  }

  private renderCaptcha(): void {
    if (!window.hcaptcha || !this.siteKey) {
      return;
    }

    this.widgetId = window.hcaptcha.render(this.hcaptchaContainer.nativeElement, {
      sitekey: this.siteKey,
      theme: this.theme,
      size: this.size,
      callback: (token: string) => {
        this.success.emit(token);
      },
      'error-callback': () => {
        this.error.emit();
      },
      'expired-callback': () => {
        this.expired.emit();
      }
    });
  }

  reset(): void {
    if (this.widgetId && window.hcaptcha) {
      window.hcaptcha.reset(this.widgetId);
    }
  }

  getResponse(): string | null {
    if (this.widgetId && window.hcaptcha) {
      return window.hcaptcha.getResponse(this.widgetId);
    }
    return null;
  }
}