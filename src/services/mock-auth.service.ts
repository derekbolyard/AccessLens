import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { User } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class MockAuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor() {
    // Check if we have a mock user in localStorage
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    const userData = localStorage.getItem('mock_user');
    if (userData) {
      try {
        const user = JSON.parse(userData);
        this.userSubject.next(user);
      } catch (error) {
        console.error('Failed to parse mock user data from storage:', error);
        localStorage.removeItem('mock_user');
      }
    }
  }

  signInWithGoogle(): Observable<User> {
    const mockUser: User = {
      id: 'google_123',
      name: 'John Doe',
      email: 'john.doe@gmail.com',
      avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=40&h=40&fit=crop&crop=face',
      provider: 'google'
    };

    return of(mockUser).pipe(delay(1000));
  }

  signInWithGitHub(): Observable<User> {
    const mockUser: User = {
      id: 'github_456',
      name: 'Jane Smith',
      email: 'jane.smith@github.com',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=40&h=40&fit=crop&crop=face',
      provider: 'github'
    };

    return of(mockUser).pipe(delay(1000));
  }

  sendVerificationCode(email: string): Observable<{ message: string }> {
    return of({ message: 'Verification code sent successfully' }).pipe(delay(1000));
  }

  verifyCode(email: string, code: string, hcaptchaToken?: string): Observable<{ message: string }> {
    // Simulate successful verification if code is '123456'
    if (code === '123456') {
      const mockUser: User = {
        id: email,
        name: email.split('@')[0],
        email: email,
        avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(email)}&background=0078d7&color=fff`,
        provider: 'magic-link'
      };
      
      this.setUser(mockUser);
      return of({ message: 'Verification successful' }).pipe(delay(1000));
    }
    
    // Simulate failed verification
    return new Observable(observer => {
      setTimeout(() => {
        observer.error({ error: { error: 'Invalid verification code' } });
      }, 1000);
    });
  }

  setUser(user: User): void {
    this.userSubject.next(user);
    localStorage.setItem('mock_user', JSON.stringify(user));
  }

  signOut(): Observable<boolean> {
    this.userSubject.next(null);
    localStorage.removeItem('mock_user');
    return of(true).pipe(delay(500));
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  get isMagicLinkAuthEnabled(): boolean {
    return true;
  }
}