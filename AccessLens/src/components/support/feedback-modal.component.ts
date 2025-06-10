import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../common/modal/modal.component';
import { ButtonComponent } from '../common/button/button.component';
import { InputComponent } from '../common/input/input.component';
import { AlertComponent } from '../common/alert/alert.component';
import { SupportService, FeedbackSubmission } from '../../services/support.service';

@Component({
  selector: 'app-feedback-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent, InputComponent, AlertComponent],
  templateUrl: './feedback-modal.component.html',
  styleUrls: ['./feedback-modal.component.scss']
})
export class FeedbackModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();

  feedback: FeedbackSubmission = {
    type: 'bug',
    subject: '',
    message: '',
    email: '',
    priority: 'medium'
  };

  isSubmitting = false;
  submitSuccess = false;
  submitError = '';

  constructor(private supportService: SupportService) {}

  ngOnInit(): void {
    // Reset form when modal opens
    if (this.isOpen) {
      this.resetForm();
    }
  }

  ngOnChanges(): void {
    if (this.isOpen) {
      this.resetForm();
    }
  }

  isFormValid(): boolean {
    return this.feedback.subject.trim().length > 0 && 
           this.feedback.message.trim().length > 0;
  }

  submitFeedback(): void {
    if (!this.isFormValid()) return;

    this.isSubmitting = true;
    this.submitError = '';

    this.supportService.submitFeedback(this.feedback).subscribe({
      next: (success) => {
        this.isSubmitting = false;
        if (success) {
          this.submitSuccess = true;
          setTimeout(() => {
            this.close.emit();
          }, 3000);
        }
      },
      error: (error) => {
        this.isSubmitting = false;
        this.submitError = 'Failed to submit feedback. Please try again or contact support directly.';
        console.error('Feedback submission failed:', error);
      }
    });
  }

  private resetForm(): void {
    this.feedback = {
      type: 'bug',
      subject: '',
      message: '',
      email: '',
      priority: 'medium'
    };
    this.isSubmitting = false;
    this.submitSuccess = false;
    this.submitError = '';
  }
}