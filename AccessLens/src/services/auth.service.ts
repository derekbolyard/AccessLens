import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

export interface User {
  id: string;
  name: string;
  email: string;
  avatar: string;
  provider: 'google' | 'github';
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor() {
    // Check for existing session
    this.loadUserFromStorage();
  }

  private loadUserFromStorage(): void {
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
      delay(1500),
      // Simulate potential auth failure
      // map(user => {
      //   if (Math.random() > 0.8) {
      //     throw new Error('Authentication failed');
      //   }
      //   return user;
      // })
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

  clearUser(): void {
    this.userSubject.next(null);
    localStorage.removeItem('user');
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }
}