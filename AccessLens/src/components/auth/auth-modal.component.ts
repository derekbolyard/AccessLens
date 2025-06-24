import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { User } from '../../services/auth.service';
import { environment } from '../../environments/environment';
import { ButtonComponent } from "../common/button/button.component";
import { ModalComponent } from "../common/modal/modal.component";
import { AlertComponent } from "../common/alert/alert.component";
import { CommonModule } from '@angular/common';
import { MagicAuthModalComponent } from "./magic-auth-modal/magic-auth-modal";
import { ToastService } from '../common/toast/toast.service';

@Component({
  selector: 'app-auth-modal',
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss'],
  imports: [ButtonComponent, ModalComponent, AlertComponent, CommonModule, MagicAuthModalComponent],
  standalone: true
})
export class AuthModalComponent implements OnInit {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() authSuccess = new EventEmitter<User>();

  isSigningIn: 'google' | 'github' | null = null;
  authError = '';

  constructor(
    private authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // Reset error state when modal opens
    if (this.isOpen) {
      this.authError = '';
    }
  }

  get isMagicLinkAuthEnabled(): boolean {
    return environment.features.useMagicLinkAuth;
  }

  signInWithGoogle(): void {
    if (this.isMagicLinkAuthEnabled) {
      this.authError = 'OAuth sign-in is currently disabled. Please use email verification.';
      return;
    }

    this.isSigningIn = 'google';
    this.authError = '';

    this.authService.signInWithGoogle().subscribe({
      next: (user) => {
        this.authService.setUser(user);
        this.isSigningIn = null;
        this.authSuccess.emit(user);
        this.close.emit();
        this.toastService.success(`Welcome, ${user.name}!`);
      },
      error: (error) => {
        this.isSigningIn = null;
        this.authError = 'Failed to sign in with Google. Please try again.';
        console.error('Google sign-in failed:', error);
      }
    });
  }

  signInWithGitHub(): void {
    if (this.isMagicLinkAuthEnabled) {
      this.authError = 'OAuth sign-in is currently disabled. Please use email verification.';
      return;
    }

    this.isSigningIn = 'github';
    this.authError = '';

    this.authService.signInWithGitHub().subscribe({
      next: (user) => {
        this.authService.setUser(user);
        this.isSigningIn = null;
        this.authSuccess.emit(user);
        this.close.emit();
        this.toastService.success(`Welcome, ${user.name}!`);
      },
      error: (error) => {
        this.isSigningIn = null;
        this.authError = 'Failed to sign in with GitHub. Please try again.';
        console.error('GitHub sign-in failed:', error);
      }
    });
  }

  onMagicAuthSuccess(user: any): void {
    this.authSuccess.emit(user);
    this.toastService.success(`Welcome, ${user.name || user.email}!`);
  }
}