import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { User } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class MockAuthService {
  
  sendMagicLink(email: string): Observable<{ message: string }> {
    console.log('Mock: Sending magic link to', email);
    return of({ message: 'Verification code sent successfully' }).pipe(delay(1000));
  }

  verifyMagicLink(email: string, code: string, hcaptchaToken?: string): Observable<{ token: string; user: User }> {
    console.log('Mock: Verifying code', code, 'for', email);
    
    // Simulate validation - use "123456" as the correct code
    if (code !== '123456') {
      return throwError(() => ({
        error: { error: 'Invalid verification code' }
      })).pipe(delay(500));
    }

    const user: User = {
      id: 'mock-user-1',
      name: email.split('@')[0],
      email: email,
      avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(email)}&background=0078d7&color=fff`,
      provider: 'magic-link'
    };

    return of({
      token: 'mock-jwt-token-' + Date.now(),
      user
    }).pipe(delay(1500));
  }

  signInWithGoogle(): Observable<User> {
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
    const mockUser: User = {
      id: 'github_456',
      name: 'Jane Smith',
      email: 'jane.smith@github.com',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=40&h=40&fit=crop&crop=face',
      provider: 'github'
    };

    return of(mockUser).pipe(delay(1500));
  }

  getCsrfToken(): Observable<string> {
    return of('mock-csrf-token-' + Date.now()).pipe(delay(100));
  }
}