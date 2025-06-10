import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay, map } from 'rxjs/operators';

export interface FeedbackSubmission {
  type: 'bug' | 'feature' | 'improvement' | 'other';
  subject: string;
  message: string;
  email?: string;
  priority: 'low' | 'medium' | 'high';
}

@Injectable({
  providedIn: 'root'
})
export class SupportService {
  private readonly supportEmail = 'support@accessibilityreports.com';

  submitFeedback(feedback: FeedbackSubmission): Observable<boolean> {
    // Simulate API call to submit feedback
    return of(true).pipe(
      delay(1500),
      map(success => {
        if (Math.random() > 0.9) { // 10% failure rate for demo
          throw new Error('Failed to submit feedback');
        }
        return success;
      })
    );
  }

  getSupportEmail(): string {
    return this.supportEmail;
  }

  openEmailClient(subject?: string): void {
    const emailSubject = subject || 'Accessibility Reports Support Request';
    const mailtoUrl = `mailto:${this.supportEmail}?subject=${encodeURIComponent(emailSubject)}`;
    window.open(mailtoUrl, '_blank');
  }
}