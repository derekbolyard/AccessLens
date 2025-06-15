import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';

declare global {
  interface Window {
    hcaptcha: any;
  }
}

@Component({
  selector: 'app-hcaptcha',
  templateUrl: './hcaptcha-component.html',
  styleUrls: ['./hcaptcha-component.scss']
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

  ngOnInit(): void {
    this.loadHCaptchaScript().then(() => {
      this.renderCaptcha();
    });
  }

  ngOnDestroy(): void {
    if (this.widgetId && window.hcaptcha) {
      window.hcaptcha.remove(this.widgetId);
    }
  }

  private async loadHCaptchaScript(): Promise<void> {
    if (this.isScriptLoaded || window.hcaptcha) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://js.hcaptcha.com/1/api.js';
      script.async = true;
      script.defer = true;
      
      script.onload = () => {
        this.isScriptLoaded = true;
        resolve();
      };
      
      script.onerror = () => {
        reject(new Error('Failed to load hCaptcha script'));
      };

      document.head.appendChild(script);
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