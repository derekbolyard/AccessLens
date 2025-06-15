import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { environment } from '../environments/environment';

export interface MagicAuthUser {
  email: string;
  emailVerified: boolean;
  provider: 'magic-link';
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
export class MagicAuthService {
  private apiUrl = environment.apiUrl;
  private userSubject = new BehaviorSubject<MagicAuthUser | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromToken();
  }

  private loadUserFromToken(): void {
    const token = localStorage.getItem('auth_token');
    if (token) {
      try {
        // Decode JWT to get user info (you might want to add proper JWT decoding)
        const payload = JSON.parse(atob(token.split('.')[1]));
        const user: MagicAuthUser = {
          email: payload.email,
          emailVerified: true,
          provider: 'magic-link'
        };
        this.userSubject.next(user);
      } catch (error) {
        console.error('Failed to parse auth token:', error);
        localStorage.removeItem('auth_token');
      }
    }
  }

  sendVerificationCode(email: string): Observable<SendCodeResponse> {
    return this.http.post<SendCodeResponse>(`${this.apiUrl}/auth/send-code`, { email });
  }

  verifyCode(email: string, code: string, hcaptchaToken?: string): Observable<VerifyCodeResponse> {
    return this.http.post<VerifyCodeResponse>(`${this.apiUrl}/auth/verify`, {
      email,
      code,
      hcaptchaToken
    });
  }

  handleAuthCallback(token: string): void {
    localStorage.setItem('auth_token', token);
    this.loadUserFromToken();
  }

  signOut(): Observable<boolean> {
    localStorage.removeItem('auth_token');
    this.userSubject.next(null);
    return of(true);
  }

  get currentUser(): MagicAuthUser | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }
}