import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../common/modal/modal.component';
import { ButtonComponent } from '../common/button/button.component';
import { InputComponent } from '../common/input/input.component';
import { AlertComponent } from '../common/alert/alert.component';
import { ReportService } from '../../services/report.service';
import { Site } from '../../types/report.interface';
import { Validators } from '../../utils/validators';

@Component({
  selector: 'app-add-site-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent, InputComponent, AlertComponent],
  templateUrl: './add-site-modal.component.html',
  styleUrls: ['./add-site-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddSiteModalComponent {
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() siteAdded = new EventEmitter<Site>();

  siteData = {
    name: '',
    url: '',
    description: ''
  };

  isSubmitting = false;
  submitSuccess = false;
  submitError = '';

  constructor(private reportService: ReportService) {}

  ngOnChanges(): void {
    if (this.isOpen) {
      this.resetForm();
    }
  }

  isFormValid(): boolean {
    const normalizedUrl = this.normalizeUrl(this.siteData.url);
    return this.siteData.name.trim().length > 0 && 
           this.siteData.url.trim().length > 0 &&
           Validators.isValidUrl(normalizedUrl);
  }

  getPreviewUrl(): string {
    return this.normalizeUrl(this.siteData.url);
  }

  private normalizeUrl(url: string): string {
    if (!url || typeof url !== 'string') return '';
    
    const trimmedUrl = url.trim();
    if (!trimmedUrl) return '';
    
    // If URL doesn't start with http:// or https://, add https://
    if (!trimmedUrl.match(/^https?:\/\//i)) {
      return `https://${trimmedUrl}`;
    }
    
    return trimmedUrl;
  }

  submitSite(): void {
    if (!this.isFormValid()) {
      this.submitError = 'Please fill in all required fields with valid information';
      return;
    }

    // Prevent double submission
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.submitError = '';

    // Normalize the URL before submitting
    const normalizedSiteData = {
      ...this.siteData,
      url: this.normalizeUrl(this.siteData.url)
    };

    this.reportService.createSite(normalizedSiteData).subscribe({
      next: (site) => {
        this.isSubmitting = false;
        this.submitSuccess = true;
        this.siteAdded.emit(site);
        setTimeout(() => {
          this.onComplete();
        }, 2000);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.submitError = 'Failed to add site. Please try again.';
        console.error('Site creation failed:', error);
      }
    });
  }

  onComplete(): void {
    this.close.emit();
  }

  private resetForm(): void {
    this.siteData = {
      name: '',
      url: '',
      description: ''
    };
    this.isSubmitting = false;
    this.submitSuccess = false;
    this.submitError = '';
  }
}