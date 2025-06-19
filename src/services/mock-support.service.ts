import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { FeedbackSubmission } from './support.service';

@Injectable({
  providedIn: 'root'
})
export class MockSupportService {
  private readonly supportEmail = 'support@accessibilityreports.com';

  submitFeedback(feedback: FeedbackSubmission): Observable<boolean> {
    console.log('Mock feedback submission:', feedback);
    return of(true).pipe(delay(1500));
  }

  getSupportEmail(): string {
    return this.supportEmail;
  }

  openEmailClient(subject?: string): void {
    const emailSubject = subject || 'Accessibility Reports Support Request';
    console.log(`Mock email client opened with subject: ${emailSubject}`);
    alert(`In a real environment, this would open your email client with:\nTo: ${this.supportEmail}\nSubject: ${emailSubject}`);
  }
}