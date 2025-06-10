import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

export interface AnalyticsEvent {
  action: string;
  category: string;
  label?: string;
  value?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  
  trackEvent(event: AnalyticsEvent): void {
    if (!environment.features.enableAnalytics) {
      return;
    }

    // In a real app, this would integrate with Google Analytics, Mixpanel, etc.
    console.log('Analytics event:', event);
  }

  trackPageView(page: string): void {
    this.trackEvent({
      action: 'page_view',
      category: 'navigation',
      label: page
    });
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
}