import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { AuthService } from 'src/services/auth.service';
import { CacheService } from 'src/services/cache.service';
import { ErrorHandlerService } from 'src/services/error-handler.service';
import { ToastService } from '../common/toast/toast.service';
import { BrandingSettings, DEFAULT_BRANDING } from './branding.interface';

@Injectable({
  providedIn: 'root'
})
export class BrandingService {
  private brandingSubject = new BehaviorSubject<BrandingSettings | null>(null);
  public branding$ = this.brandingSubject.asObservable();

  constructor(
    private authService: AuthService,
    private cacheService: CacheService,
    private errorHandler: ErrorHandlerService,
    private toastService: ToastService
  ) {
    this.loadUserBranding();
    
    // Subscribe to auth changes to reload branding when user changes
    this.authService.user$.subscribe(user => {
      if (user) {
        this.loadUserBranding();
      } else {
        this.brandingSubject.next(null);
        this.removeBrandingFromDocument();
      }
    });
  }

  private loadUserBranding(): void {
    const currentUser = this.authService.currentUser;
    if (!currentUser) return;

    // Load from localStorage with user-specific key
    const storageKey = `user_branding_${currentUser.id}`;
    const savedBranding = localStorage.getItem(storageKey);
    
    if (savedBranding) {
      try {
        const branding = JSON.parse(savedBranding);
        this.brandingSubject.next(branding);
        this.applyBrandingToDocument(branding);
      } catch (error) {
        this.errorHandler.handleError(error, 'BrandingService.loadUserBranding');
        this.toastService.error('Failed to load branding settings');
      }
    }
  }

  getBranding(): Observable<BrandingSettings | null> {
    return this.branding$;
  }

  updateBranding(branding: Partial<BrandingSettings>): Observable<boolean> {
    const currentUser = this.authService.currentUser;
    if (!currentUser) {
      this.toastService.error('You must be logged in to save branding settings');
      return of(false);
    }

    return of(true).pipe(
      delay(800), // Simulate API call
      map(() => {
        try {
          const currentBranding = this.brandingSubject.value;
          const updatedBranding: BrandingSettings = {
            id: currentBranding?.id || this.generateId(),
            userId: currentUser.id,
            ...DEFAULT_BRANDING,
            ...currentBranding,
            ...branding,
            updatedDate: new Date(),
            createdDate: currentBranding?.createdDate || new Date()
          } as BrandingSettings;

          // Save to localStorage with user-specific key
          const storageKey = `user_branding_${currentUser.id}`;
          localStorage.setItem(storageKey, JSON.stringify(updatedBranding));
          
          // Update state and apply to document
          this.brandingSubject.next(updatedBranding);
          this.applyBrandingToDocument(updatedBranding);
          
          // Clear cache
          this.cacheService.delete('branding');
          
          this.toastService.success('Branding settings saved successfully!');
          return true;
        } catch (error) {
          this.errorHandler.handleError(error, 'BrandingService.updateBranding');
          this.toastService.error('Failed to save branding settings');
          return false;
        }
      })
    );
  }

  resetBranding(): Observable<boolean> {
    const currentUser = this.authService.currentUser;
    if (!currentUser) {
      this.toastService.error('You must be logged in to reset branding settings');
      return of(false);
    }

    return of(true).pipe(
      delay(500),
      map(() => {
        try {
          // Remove from localStorage
          const storageKey = `user_branding_${currentUser.id}`;
          localStorage.removeItem(storageKey);
          
          // Reset state and remove from document
          this.brandingSubject.next(null);
          this.removeBrandingFromDocument();
          
          this.toastService.success('Branding reset to default settings');
          return true;
        } catch (error) {
          this.errorHandler.handleError(error, 'BrandingService.resetBranding');
          this.toastService.error('Failed to reset branding settings');
          return false;
        }
      })
    );
  }

  private applyBrandingToDocument(branding: BrandingSettings): void {
    const root = document.documentElement;
    
    // Apply custom colors
    root.style.setProperty('--brand-primary', branding.primaryColor);
    root.style.setProperty('--brand-secondary', branding.secondaryColor);
    root.style.setProperty('--brand-accent', branding.accentColor);
    
    // Override default primary colors with brand colors
    root.style.setProperty('--primary-600', branding.primaryColor);
    root.style.setProperty('--primary-700', this.darkenColor(branding.primaryColor, 10));
    root.style.setProperty('--primary-500', this.lightenColor(branding.primaryColor, 10));
    root.style.setProperty('--blue-600', branding.primaryColor);
    root.style.setProperty('--blue-700', this.darkenColor(branding.primaryColor, 10));
    root.style.setProperty('--blue-500', this.lightenColor(branding.primaryColor, 10));
    
    // Apply font family if specified
    if (branding.fontFamily && branding.fontFamily !== 'Inter') {
      root.style.setProperty('--brand-font', branding.fontFamily);
      document.body.style.fontFamily = `${branding.fontFamily}, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`;
    }
  }

  private removeBrandingFromDocument(): void {
    const root = document.documentElement;
    
    // Remove custom properties
    root.style.removeProperty('--brand-primary');
    root.style.removeProperty('--brand-secondary');
    root.style.removeProperty('--brand-accent');
    root.style.removeProperty('--brand-font');
    
    // Reset to default colors
    root.style.removeProperty('--primary-600');
    root.style.removeProperty('--primary-700');
    root.style.removeProperty('--primary-500');
    root.style.removeProperty('--blue-600');
    root.style.removeProperty('--blue-700');
    root.style.removeProperty('--blue-500');
    
    // Reset font
    document.body.style.fontFamily = '';
  }

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  private darkenColor(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = Math.max(0, Math.min(255, (num >> 16) - amt));
    const G = Math.max(0, Math.min(255, (num >> 8 & 0x00FF) - amt));
    const B = Math.max(0, Math.min(255, (num & 0x0000FF) - amt));
    return '#' + (0x1000000 + R * 0x10000 + G * 0x100 + B).toString(16).slice(1);
  }

  private lightenColor(color: string, percent: number): string {
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = Math.max(0, Math.min(255, (num >> 16) + amt));
    const G = Math.max(0, Math.min(255, (num >> 8 & 0x00FF) + amt));
    const B = Math.max(0, Math.min(255, (num & 0x0000FF) + amt));
    return '#' + (0x1000000 + R * 0x10000 + G * 0x100 + B).toString(16).slice(1);
  }

  // Method to refresh branding when user changes
  refreshBrandingForUser(): void {
    this.loadUserBranding();
  }
}