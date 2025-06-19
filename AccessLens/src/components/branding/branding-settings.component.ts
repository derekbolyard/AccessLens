import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardComponent } from '../common/card/card.component';
import { ButtonComponent } from '../common/button/button.component';
import { InputComponent } from '../common/input/input.component';
import { AlertComponent } from '../common/alert/alert.component';
import { Validators } from '../../utils/validators';
import { BrandingSettings, DEFAULT_BRANDING } from './branding.interface';
import { BrandingService } from './branding.service';

@Component({
  selector: 'app-branding-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, CardComponent, ButtonComponent, InputComponent, AlertComponent],
  templateUrl: './branding-settings.component.html',
  styleUrls: ['./branding-settings.component.scss']
})
export class BrandingSettingsComponent implements OnInit {
  @Output() backToDashboard = new EventEmitter<void>();

  brandingForm: Partial<BrandingSettings> = {
    companyName: '',
    primaryColor: '#2563eb',
    secondaryColor: '#64748b',
    accentColor: '#10b981',
    fontFamily: 'Inter',
    reportFooterText: 'Powered by AccessibilityReports'
  };

  isSaving = false;
  saveSuccess = '';
  saveError = '';

  constructor(private brandingService: BrandingService) {}

  ngOnInit(): void {
    this.loadCurrentBranding();
  }

  private loadCurrentBranding(): void {
    this.brandingService.getBranding().subscribe(branding => {
      if (branding) {
        this.brandingForm = { ...branding };
      } else {
        this.brandingForm = { ...DEFAULT_BRANDING };
      }
    });
  }

  onLogoUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    
    if (!file) return;

    // Validate file size (2MB limit)
    if (file.size > 2 * 1024 * 1024) {
      this.saveError = 'Logo file size must be less than 2MB';
      return;
    }

    // Validate file type
    if (!file.type.startsWith('image/')) {
      this.saveError = 'Please select a valid image file';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e) => {
      this.brandingForm.logo = e.target?.result as string;
      this.updatePreview();
    };
    reader.readAsDataURL(file);
  }

  removeLogo(): void {
    this.brandingForm.logo = undefined;
    this.updatePreview();
  }

  onColorInputChange(colorType: 'primaryColor' | 'secondaryColor' | 'accentColor', event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;
    
    switch (colorType) {
      case 'primaryColor':
        this.brandingForm.primaryColor = value;
        break;
      case 'secondaryColor':
        this.brandingForm.secondaryColor = value;
        break;
      case 'accentColor':
        this.brandingForm.accentColor = value;
        break;
    }
    
    this.updatePreview();
  }

  updatePreview(): void {
    // Apply changes to the current page for live preview
    const root = document.documentElement;
    root.style.setProperty('--brand-primary', this.brandingForm.primaryColor || '#2563eb');
    root.style.setProperty('--primary-600', this.brandingForm.primaryColor || '#2563eb');
  }

  isFormValid(): boolean {
    return !!(
      this.brandingForm.companyName?.trim() &&
      this.brandingForm.primaryColor &&
      this.brandingForm.secondaryColor &&
      this.brandingForm.accentColor &&
      this.isValidColor(this.brandingForm.primaryColor) &&
      this.isValidColor(this.brandingForm.secondaryColor) &&
      this.isValidColor(this.brandingForm.accentColor) &&
      (!this.brandingForm.contactEmail || Validators.isValidEmail(this.brandingForm.contactEmail)) &&
      (!this.brandingForm.website || Validators.isValidUrl(this.brandingForm.website))
    );
  }

  private isValidColor(color: string): boolean {
    return /^#[0-9A-F]{6}$/i.test(color);
  }

  saveBranding(): void {
    if (!this.isFormValid()) {
      this.saveError = 'Please fill in all required fields correctly';
      return;
    }

    this.isSaving = true;
    this.saveError = '';

    this.brandingService.updateBranding(this.brandingForm).subscribe({
      next: (success: any) => {
        this.isSaving = false;
        if (success) {
          this.saveSuccess = 'Branding settings saved successfully!';
          setTimeout(() => {
            this.saveSuccess = '';
          }, 3000);
        }
      },
      error: (error: any) => {
        this.isSaving = false;
        this.saveError = 'Failed to save branding settings. Please try again.';
        console.error('Failed to save branding:', error);
      }
    });
  }

  resetBranding(): void {
    this.brandingService.resetBranding().subscribe({
      next: (success: any) => {
        if (success) {
          this.brandingForm = { ...DEFAULT_BRANDING };
          this.saveSuccess = 'Branding reset to default settings';
          setTimeout(() => {
            this.saveSuccess = '';
          }, 3000);
        }
      },
      error: (error: any) => {
        this.saveError = 'Failed to reset branding settings';
        console.error('Failed to reset branding:', error);
      }
    });
  }

  onBackToDashboard(): void {
    this.backToDashboard.emit();
  }
}