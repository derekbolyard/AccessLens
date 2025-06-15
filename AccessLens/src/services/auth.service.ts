import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../environments/environment';

export interface User {
  id: string;
  name: string;
  email: string;
  avatar: string;
  provider: 'google' | 'github' | 'magic-link';
}

export interface SendCodeResponse {
  message: string;
}

export interface VerifyCodeResponse {
  message: string;
  requiresCaptcha?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
    if (environment.features.useMagicLinkAuth) {
      // Load from JWT token
      const token = localStorage.getItem('auth_token');
      if (token) {
        try {
          const payload = JSON.parse(atob(token.split('.')[1]));
          const user: User = {
            id: payload.email,
            name: payload.email.split('@')[0],
            email: payload.email,
            avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(payload.email)}&background=0078d7&color=fff`,
            provider: 'magic-link'
          };
          this.userSubject.next(user);
        } catch (error) {
          console.error('Failed to parse auth token:', error);
          localStorage.removeItem('auth_token');
        }
      }
    } else {
      // Load from old OAuth storage
      const userData = localStorage.getItem('user');
      if (userData) {
        try {
          const user = JSON.parse(userData);
          this.userSubject.next(user);
        } catch (error) {
          console.error('Failed to parse user data from storage:', error);
          localStorage.removeItem('user');
        }
      }
    }
  }

  // OAuth Methods (old flow)
  signInWithGoogle(): Observable<User> {
    if (environment.features.useMagicLinkAuth) {
      throw new Error('OAuth sign-in is disabled. Please use magic link authentication.');
    }

    const mockUser: User = {
      id: 'google_123',
      name: 'John Doe',
      email: 'john.doe@gmail.com',
      avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=40&h=40&fit=crop&crop=face',
      provider: 'google'
    };

    return of(mockUser).pipe(delay(1500));
  }

  signInWithGitHub(): Observable<User> {
    if (environment.features.useMagicLinkAuth) {
      throw new Error('OAuth sign-in is disabled. Please use magic link authentication.');
    }

    const mockUser: User = {
      id: 'github_456',
      name: 'Jane Smith',
      email: 'jane.smith@github.com',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=40&h=40&fit=crop&crop=face',
      provider: 'github'
    };

    return of(mockUser).pipe(delay(1500));
  }

  // Magic Link Methods (new flow)
  sendVerificationCode(email: string): Observable<SendCodeResponse> {
    if (!environment.features.useMagicLinkAuth) {
      throw new Error('Magic link authentication is disabled.');
    }
    return this.http.post<SendCodeResponse>(`${this.apiUrl}/auth/send-code`, { email });
  }

  verifyCode(email: string, code: string, hcaptchaToken?: string): Observable<VerifyCodeResponse> {
    if (!environment.features.useMagicLinkAuth) {
      throw new Error('Magic link authentication is disabled.');
    }
    return this.http.post<VerifyCodeResponse>(`${this.apiUrl}/auth/verify`, {
      email,
      code,
      hcaptchaToken
    });
  }

  handleAuthCallback(token: string): void {
    if (!environment.features.useMagicLinkAuth) {
      return;
    }
    localStorage.setItem('auth_token', token);
    this.loadUserFromStorage();
  }

  // Common methods
  setUser(user: User): void {
    this.userSubject.next(user);
    if (environment.features.useMagicLinkAuth) {
      // For magic link, token is already stored
    } else {
      localStorage.setItem('user', JSON.stringify(user));
    }
  }

  signOut(): Observable<boolean> {
    this.userSubject.next(null);
    if (environment.features.useMagicLinkAuth) {
      localStorage.removeItem('auth_token');
    } else {
      localStorage.removeItem('user');
    }
    return of(true).pipe(delay(500));
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  get isMagicLinkAuthEnabled(): boolean {
    return environment.features.useMagicLinkAuth;
  }
}