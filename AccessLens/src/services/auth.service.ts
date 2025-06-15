import { Injectable, Injector } from '@angular/core';
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

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();
  private magicAuthService: any = null;

  constructor(private injector: Injector) {
    // Check for existing session
    this.loadUserFromStorage();
    
    // Lazy load magic auth service to avoid circular dependency
    if (environment.features.useMagicLinkAuth) {
      this.initializeMagicAuth();
    }
  }

  private async initializeMagicAuth(): Promise<void> {
    try {
      // Dynamic import to avoid circular dependency
      const { MagicAuthService } = await import('./magic-auth.service');
      this.magicAuthService = this.injector.get(MagicAuthService);
      
      // Subscribe to magic auth user changes
      this.magicAuthService.user$.subscribe((magicUser: any) => {
        if (magicUser) {
          const user: User = {
            id: magicUser.email,
            name: magicUser.email.split('@')[0],
            email: magicUser.email,
            avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(magicUser.email)}&background=0078d7&color=fff`,
            provider: 'magic-link'
          };
          this.userSubject.next(user);
        } else {
          this.userSubject.next(null);
        }
      });
    } catch (error) {
      console.error('Failed to initialize magic auth service:', error);
    }
  }

  private loadUserFromStorage(): void {
    // Only load from localStorage if not using magic link auth
    if (!environment.features.useMagicLinkAuth) {
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

  signInWithGoogle(): Observable<User> {
    if (environment.features.useMagicLinkAuth) {
      throw new Error('OAuth sign-in is disabled. Please use magic link authentication.');
    }

    // Simulate Google OAuth flow
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

    // Simulate GitHub OAuth flow
    const mockUser: User = {
      id: 'github_456',
      name: 'Jane Smith',
      email: 'jane.smith@github.com',
      avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?w=40&h=40&fit=crop&crop=face',
      provider: 'github'
    };

    return of(mockUser).pipe(delay(1500));
  }
  signOut(): Observable<boolean> {
    if (environment.features.useMagicLinkAuth && this.magicAuthService) {
      return this.magicAuthService.signOut();
    } else {
      this.clearUser();
      return of(true).pipe(delay(500));
    }
  }

  setUser(user: User): void {
    if (!environment.features.useMagicLinkAuth) {
      this.userSubject.next(user);
      localStorage.setItem('user', JSON.stringify(user));
    }
    // For magic link auth, user is managed by MagicAuthService
  }

  clearUser(): void {
    if (!environment.features.useMagicLinkAuth) {
      this.userSubject.next(null);
      localStorage.removeItem('user');
    }
    // For magic link auth, user is managed by MagicAuthService
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