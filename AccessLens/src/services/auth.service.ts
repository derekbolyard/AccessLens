import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { delay, map, catchError } from 'rxjs/operators';
import { environment } from '../environments/environment';

export interface User {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  provider: 'magic-link' | 'google' | 'github';
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    // Check if user is stored in localStorage on service init
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        this.userSubject.next(JSON.parse(storedUser));
      } catch {
        localStorage.removeItem('user');
      }
    }
  }

  sendMagicLink(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/api/auth/send-magic-link`, { email })
      .pipe(
        catchError(error => {
          console.error('Magic link request failed:', error);
          throw error;
        })
      );
  }

  // Keep existing OAuth methods for future use
  signInWithGoogle(): Observable<User> {
    // Simulate Google OAuth flow
    const mockUser: User = {
      id: 'google_123',
      name: 'John Doe',
      email: 'john.doe@gmail.com',
      avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=40&h=40&fit=crop&crop=face',
      provider: 'google'
    };

    return of(mockUser).pipe(
      delay(1500)
    );
  }

  signInWithGitHub(): Observable<User> {
    // Simulate GitHub OAuth flow
    const mockUser: User = {
      id: 'github_456',
      name: 'Jane Smith',
      email: 'jane.smith@github.com',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=40&h=40&fit=crop&crop=face',
      provider: 'github'
    };

    return of(mockUser).pipe(
      delay(1500)
    );
  }

  signOut(): Observable<boolean> {
    return of(true).pipe(
      delay(500)
    );
  }

  setUser(user: User): void {
    this.userSubject.next(user);
    localStorage.setItem('user', JSON.stringify(user));
  }

  setUserFromToken(token: string): void {
    // Decode JWT to get user info (you might want to add jwt-decode library)
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const user: User = {
        id: payload.jti || 'unknown',
        name: payload.email?.split('@')[0] || 'User',
        email: payload.email || '',
        provider: 'magic-link'
      };
      this.setUser(user);
      localStorage.setItem('access_token', token);
    } catch (error) {
      console.error('Failed to decode token:', error);
    }
  }

  clearUser(): void {
    this.userSubject.next(null);
    localStorage.removeItem('user');
    localStorage.removeItem('access_token');
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    const token = localStorage.getItem('access_token');
    const user = this.currentUser;
    return !!(token && user);
  }

  getAuthToken(): string | null {
    return localStorage.getItem('access_token');
  }
}