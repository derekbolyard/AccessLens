import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { AuthService } from 'src/services/auth.service';
import { CacheService } from 'src/services/cache.service';
import { ErrorHandlerService } from 'src/services/error-handler.service';
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
    private errorHandler: ErrorHandlerService
  ) {
    this.loadUserBranding();
  }

  private loadUserBranding(): void {
    // Load from localStorage or create default
    const savedBranding = localStorage.getItem('user_branding');
    if (savedBranding) {
      try {
        const branding = JSON.parse(savedBranding);
        this.brandingSubject.next(branding);
        this.applyBrandingToDocument(branding);
      } catch (error) {
        this.errorHandler.handleError(error, 'BrandingService.loadUserBranding');
      }
    }
  }

  getBranding(): Observable<BrandingSettings | null> {
    return this.branding$;
  }

  updateBranding(branding: Partial<BrandingSettings>): Observable<boolean> {
    const currentUser = this.authService.currentUser;
    if (!currentUser) {
      return of(false);
    }

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

    return of(true).pipe(
      delay(500), // Simulate API call
      map(() => {
        this.brandingSubject.next(updatedBranding);
        localStorage.setItem('user_branding', JSON.stringify(updatedBranding));
        this.applyBrandingToDocument(updatedBranding);
        this.cacheService.delete('branding'); // Invalidate cache
        return true;
      })
    );
  }

  resetBranding(): Observable<boolean> {
    return of(true).pipe(
      delay(300),
      map(() => {
        this.brandingSubject.next(null);
        localStorage.removeItem('user_branding');
        this.removeBrandingFromDocument();
        return true;
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
    
    // Reset font
    document.body.style.fontFamily = '';
  }

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  private darkenColor(color: string, percent: number): string {
    // Simple color darkening - in production, use a proper color library
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) - amt;
    const G = (num >> 8 & 0x00FF) - amt;
    const B = (num & 0x0000FF) - amt;
    return '#' + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
      (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
      (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
  }

  private lightenColor(color: string, percent: number): string {
    // Simple color lightening - in production, use a proper color library
    const num = parseInt(color.replace('#', ''), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    return '#' + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
      (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
      (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
  }
}