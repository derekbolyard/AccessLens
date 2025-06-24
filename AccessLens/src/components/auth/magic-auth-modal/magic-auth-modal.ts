import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { AuthService } from '../../../services/auth.service';
import { ButtonComponent } from "../../common/button/button.component";
import { AlertComponent } from "../../common/alert/alert.component";
import { ModalComponent } from "../../common/modal/modal.component";
import { InputComponent } from "../../common/input/input.component";
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../common/toast/toast.service';

@Component({
  selector: 'app-magic-auth-modal',
  templateUrl: './magic-auth-modal.html',
  styleUrls: ['./magic-auth-modal.scss'],
  imports: [ButtonComponent, AlertComponent, ModalComponent, InputComponent, FormsModule, CommonModule],
  standalone: true
})
export class MagicAuthModalComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() authSuccess = new EventEmitter<any>();

  currentStep: 'email' | 'email-sent' = 'email';
  email = '';
  isLoading = false;
  isMockLoggingIn = false;
  authError = '';

  constructor(
    private authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // Reset form when component initializes
    this.resetForm();
  }

  ngOnChanges(): void {
    // Reset form when modal opens
    if (this.isOpen) {
      this.resetForm();
    }
  }

  get isMockBackendEnabled(): boolean {
    return environment.features.useMockBackend;
  }

  onSubmitEmail(): void {
    if (!this.email.trim() || !this.isValidEmail(this.email)) {
      this.authError = 'Please enter a valid email address';
      return;
    }

    this.isLoading = true;
    this.authError = '';

    this.authService.sendVerificationCode(this.email).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        this.currentStep = 'email-sent';
        this.toastService.success('Verification code sent to your email');
      },
      error: (error: any) => {
        this.isLoading = false;
        this.authError = error.error?.error || 'Failed to send sign-in link. Please try again.';
        this.toastService.error(this.authError);
      }
    });
  }

  onMockLogin(): void {
    if (!this.isMockBackendEnabled) {
      return;
    }

    this.isMockLoggingIn = true;
    this.authError = '';

    // Simulate the verification process with a mock code
    this.authService.verifyCode(this.email, '123456').subscribe({
      next: (response: any) => {
        this.isMockLoggingIn = false;
        this.authSuccess.emit({ email: this.email, provider: 'magic-link' });
        this.close.emit();
        this.resetForm();
      },
      error: (error: any) => {
        this.isMockLoggingIn = false;
        this.authError = error.error?.error || 'Mock login failed. Please try again.';
        this.toastService.error(this.authError);
      }
    });
  }

  onBackToEmail(): void {
    this.currentStep = 'email';
    this.authError = '';
  }

  onResendEmail(): void {
    this.onSubmitEmail();
  }

  private resetForm(): void {
    this.currentStep = 'email';
    this.email = '';
    this.isLoading = false;
    this.isMockLoggingIn = false;
    this.authError = '';
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}