import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../common/modal/modal.component';
import { ButtonComponent } from '../common/button/button.component';
import { AlertComponent } from '../common/alert/alert.component';
import { AuthService, User } from '../../services/auth.service';

@Component({
  selector: 'app-auth-modal',
  standalone: true,
  imports: [CommonModule, ModalComponent, ButtonComponent, AlertComponent],
  templateUrl: './auth-modal.component.html',
  styleUrls: ['./auth-modal.component.scss']
})
export class AuthModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() authSuccess = new EventEmitter<User>();

  isSigningIn: 'google' | 'github' | null = null;
  authError = '';

  constructor(private authService: AuthService) {}

  signInWithGoogle(): void {
    this.isSigningIn = 'google';
    this.authError = '';

    this.authService.signInWithGoogle().subscribe({
      next: (user) => {
        this.authService.setUser(user);
        this.isSigningIn = null;
        this.authSuccess.emit(user);
        this.close.emit();
      },
      error: (error) => {
        this.isSigningIn = null;
        this.authError = 'Failed to sign in with Google. Please try again.';
        console.error('Google sign-in failed:', error);
      }
    });
  }

  signInWithGitHub(): void {
    this.isSigningIn = 'github';
    this.authError = '';

    this.authService.signInWithGitHub().subscribe({
      next: (user) => {
        this.authService.setUser(user);
        this.isSigningIn = null;
        this.authSuccess.emit(user);
        this.close.emit();
      },
      error: (error) => {
        this.isSigningIn = null;
        this.authError = 'Failed to sign in with GitHub. Please try again.';
        console.error('GitHub sign-in failed:', error);
      }
    });
  }
}