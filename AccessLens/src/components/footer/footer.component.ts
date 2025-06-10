import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../common/button/button.component';
import { FeedbackModalComponent } from '../support/feedback-modal.component';
import { SupportService } from '../../services/support.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, ButtonComponent, FeedbackModalComponent],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss']
})
export class FooterComponent {
  showFeedbackModal = false;
  supportEmail: string;

  constructor(private supportService: SupportService) {
    this.supportEmail = this.supportService.getSupportEmail();
  }

  onContactSupport(): void {
    this.supportService.openEmailClient('Support Request');
  }

  onSendFeedback(): void {
    this.showFeedbackModal = true;
  }
}