import { Component, Input, Output, EventEmitter } from '@angular/core';
import { AuthService } from '../../../services/auth.service'; // Change this!
import { ButtonComponent } from "../../common/button/button.component";
import { HCaptchaComponent } from "../../common/hcaptcha-component/hcaptcha-component";
import { AlertComponent } from "../../common/alert/alert.component";
import { ModalComponent } from "../../common/modal/modal.component";
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-magic-auth-modal',
  templateUrl: './magic-auth-modal.html',
  styleUrls: ['./magic-auth-modal.scss'],
  imports: [ButtonComponent, HCaptchaComponent, AlertComponent, ModalComponent, FormsModule, CommonModule]
})
export class MagicAuthModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() authSuccess = new EventEmitter<any>();

  currentStep: 'email' | 'verification' = 'email';
  email = '';
  verificationCode = '';
  isLoading = false;
  authError = '';
  requiresCaptcha = false;
  hcaptchaToken = '';
  hcaptchaSiteKey = 'your-hcaptcha-site-key';

  constructor(private authService: AuthService) {} // Change this!

  onSubmitEmail(): void {
    if (!this.email.trim() || !this.isValidEmail(this.email)) {
      this.authError = 'Please enter a valid email address';
      return;
    }

    this.isLoading = true;
    this.authError = '';

    this.authService.sendVerificationCode(this.email).subscribe({ // Change this!
      next: (response: any) => {
        this.isLoading = false;
        this.currentStep = 'verification';
      },
      error: (error: any) => {
        this.isLoading = false;
        this.authError = error.error?.error || 'Failed to send verification code. Please try again.';
      }
    });
  }

  onSubmitVerification(): void {
    if (!this.verificationCode.trim() || this.verificationCode.length !== 6) {
      this.authError = 'Please enter a valid 6-digit verification code';
      return;
    }

    if (this.requiresCaptcha && !this.hcaptchaToken) {
      this.authError = 'Please complete the captcha verification';
      return;
    }

    this.isLoading = true;
    this.authError = '';

    this.authService.verifyCode(this.email, this.verificationCode, this.hcaptchaToken).subscribe({ // Change this!
      next: (response: any) => {
        this.isLoading = false;
        this.authSuccess.emit({ email: this.email, provider: 'magic-link' });
        this.close.emit();
        this.resetForm();
      },
      error: (error: any) => {
        this.isLoading = false;
        if (error.error?.error === 'hCaptcha required.') {
          this.requiresCaptcha = true;
          this.authError = 'Please complete the captcha verification';
        } else {
          this.authError = error.error?.error || 'Invalid verification code. Please try again.';
        }
      }
    });
  }

  onBackToEmail(): void {
    this.currentStep = 'email';
    this.verificationCode = '';
    this.authError = '';
    this.requiresCaptcha = false;
    this.hcaptchaToken = '';
  }

  onResendCode(): void {
    this.onSubmitEmail();
  }

  onHCaptchaSuccess(token: string): void {
    this.hcaptchaToken = token;
    this.authError = '';
  }

  private resetForm(): void {
    this.currentStep = 'email';
    this.email = '';
    this.verificationCode = '';
    this.isLoading = false;
    this.authError = '';
    this.requiresCaptcha = false;
    this.hcaptchaToken = '';
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}