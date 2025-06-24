import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { environment } from '../environments/environment';
import { debounce } from '../utils/performance';
import { filter } from 'rxjs/operators';

export interface AnalyticsEvent {
  action: string;
  category: string;
  label?: string;
  value?: number;
}

declare global {
  interface Window {
    gtag: (...args: any[]) => void;
    dataLayer: any[];
  }
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private readonly DEBOUNCE_TIME = 300; // ms
  private isInitialized = false;
  
  // Debounced tracking methods to prevent excessive calls
  private debouncedTrackEvent = debounce(this.trackEventImpl.bind(this), this.DEBOUNCE_TIME);
  private debouncedTrackPageView = debounce(this.trackPageViewImpl.bind(this), this.DEBOUNCE_TIME);
  
  constructor(
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    if (isPlatformBrowser(this.platformId) && environment.features.enableAnalytics) {
      this.initializeGoogleAnalytics();
      
      // Track route changes
      this.router.events.pipe(
        filter(event => event instanceof NavigationEnd)
      ).subscribe((event: any) => {
        this.trackPageView(event.urlAfterRedirects);
      });
    }
  }
  
  private initializeGoogleAnalytics(): void {
    if (!environment.googleAnalyticsMeasurementId || this.isInitialized) {
      return;
    }
    
    // Load the Google Analytics script
    const gaScript = document.createElement('script');
    gaScript.async = true;
    gaScript.src = `https://www.googletagmanager.com/gtag/js?id=${environment.googleAnalyticsMeasurementId}`;
    document.head.appendChild(gaScript);
    
    // Configure Google Analytics
    window.gtag = window.gtag || function(){(window.dataLayer = window.dataLayer || []).push(arguments);};
    window.gtag('js', new Date());
    window.gtag('config', environment.googleAnalyticsMeasurementId, {
      send_page_view: false, // We'll handle page views manually
      anonymize_ip: true,
      cookie_flags: 'SameSite=None;Secure'
    });
    
    this.isInitialized = true;
  }
  
  trackEvent(event: AnalyticsEvent): void {
    if (!environment.features.enableAnalytics) {
      return;
    }
    
    this.debouncedTrackEvent(event);
  }

  trackPageView(page: string): void {
    if (!environment.features.enableAnalytics) {
      return;
    }
    
    this.debouncedTrackPageView(page);
  }

  trackScanRequest(siteUrl: string): void {
    this.trackEvent({
      action: 'scan_requested',
      category: 'accessibility',
      label: siteUrl
    });
  }

  trackIssueStatusChange(from: string, to: string): void {
    this.trackEvent({
      action: 'issue_status_changed',
      category: 'accessibility',
      label: `${from}_to_${to}`
    });
  }

  trackUpgrade(planId: string): void {
    this.trackEvent({
      action: 'plan_upgraded',
      category: 'subscription',
      label: planId
    });
  }
  
  // Private implementation methods
  private trackEventImpl(event: AnalyticsEvent): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    if (environment.features.enableAnalytics && window.gtag) {
      window.gtag('event', event.action, {
        event_category: event.category,
        event_label: event.label,
        value: event.value
      });
    }
    
    // Log in development
    if (!environment.production) {
      console.log('Analytics event:', event);
    }
  }
  
  private trackPageViewImpl(page: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    
    if (environment.features.enableAnalytics && window.gtag) {
      window.gtag('event', 'page_view', {
        page_path: page,
        page_title: document.title
      });
    }
    
    // Log in development
    if (!environment.production) {
      console.log('Page view:', page);
    }
  }
}