import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../common/button/button.component';
import { CardComponent } from '../common/card/card.component';
import { InputComponent } from '../common/input/input.component';
import { AlertComponent } from '../common/alert/alert.component';
import { BrandingService } from '../../services/branding.service';
import { BrandingInfo } from '../../types/branding.interface';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { Validators } from '../../utils/validators';

@Component({
  selector: 'app-branding-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, CardComponent, InputComponent, AlertComponent],
  templateUrl: './branding-page.component.html',
  styleUrls: ['./branding-page.component.scss']
})
export class BrandingPageComponent implements OnInit {
  brandings: BrandingInfo[] = [];
  logoFile: File | null = null;
  editingId: string | null = null;
  isSaving = false;
  saveSuccess = '';
  saveError = '';

  newBrand: Partial<BrandingInfo> = {
    id: '',
    userId: '',
    logoUrl: '',
    primaryColor: '#4f46e5',
    secondaryColor: '#e0e7ff',
    companyName: '',
    contactEmail: '',
    website: '',
    reportFooterText: 'Powered by AccessLens'
  };

  constructor(
    private brandingService: BrandingService,
    private auth: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBranding();
  }

  private loadBranding(): void {
    this.brandingService.loadBranding().subscribe(b => {
      this.brandings = b;
      if (b.length > 0) {
        this.updatePreview(b[0]);
      }
    });
  }

  onBackToDashboard(): void {
    this.router.navigate(['/dashboard']);
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
      this.saveError = 'Please select a valid image file (PNG, JPG, SVG)';
      return;
    }

    this.logoFile = file;
    this.saveError = '';

    // Show preview
    const reader = new FileReader();
    reader.onload = (e) => {
      const previewImg = document.getElementById('logo-preview') as HTMLImageElement;
      if (previewImg) {
        previewImg.src = e.target?.result as string;
        previewImg.style.display = 'block';
      }
    };
    reader.readAsDataURL(file);
  }

  removeLogo(): void {
    this.logoFile = null;
    const previewImg = document.getElementById('logo-preview') as HTMLImageElement;
    if (previewImg) {
      previewImg.style.display = 'none';
    }
    const fileInput = document.getElementById('logo-upload') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }

  onColorChange(colorType: 'primaryColor' | 'secondaryColor', value: string): void {
    if (colorType === 'primaryColor') {
      this.newBrand.primaryColor = value;
    } else {
      this.newBrand.secondaryColor = value;
    }
    this.updateLivePreview();
  }

  onEditColorChange(brand: BrandingInfo, colorType: 'primaryColor' | 'secondaryColor', value: string): void {
    if (colorType === 'primaryColor') {
      brand.primaryColor = value;
    } else {
      brand.secondaryColor = value;
    }
    this.updatePreview(brand);
  }

  updateLivePreview(): void {
    const root = document.documentElement;
    root.style.setProperty('--primary-600', this.newBrand.primaryColor || '#4f46e5');
    root.style.setProperty('--primary-500', this.newBrand.primaryColor || '#4f46e5');
    root.style.setProperty('--primary-700', this.adjustColorBrightness(this.newBrand.primaryColor || '#4f46e5', -20));
  }

  updatePreview(brand: BrandingInfo): void {
    const root = document.documentElement;
    root.style.setProperty('--primary-600', brand.primaryColor);
    root.style.setProperty('--primary-500', brand.primaryColor);
    root.style.setProperty('--primary-700', this.adjustColorBrightness(brand.primaryColor, -20));
  }

  private adjustColorBrightness(hex: string, percent: number): string {
    const num = parseInt(hex.replace("#", ""), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;
    return "#" + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
      (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
      (B < 255 ? B < 1 ? 0 : B : 255)).toString(16).slice(1);
  }

  getContrastColor(hexColor: string): string {
    // Convert hex to RGB
    const r = parseInt(hexColor.slice(1, 3), 16);
    const g = parseInt(hexColor.slice(3, 5), 16);
    const b = parseInt(hexColor.slice(5, 7), 16);
    
    // Calculate luminance
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
    
    // Return black or white based on luminance
    return luminance > 0.5 ? '#000000' : '#ffffff';
  }

  isFormValid(): boolean {
    return !!(
      this.newBrand.companyName?.trim() &&
      this.newBrand.primaryColor &&
      this.newBrand.secondaryColor &&
      this.isValidColor(this.newBrand.primaryColor) &&
      this.isValidColor(this.newBrand.secondaryColor) &&
      (!this.newBrand.contactEmail || Validators.isValidEmail(this.newBrand.contactEmail)) &&
      (!this.newBrand.website || Validators.isValidUrl(this.newBrand.website))
    );
  }

  private isValidColor(color: string): boolean {
    return /^#[0-9A-F]{6}$/i.test(color);
  }

  createBrand(): void {
    if (!this.isFormValid()) {
      this.saveError = 'Please fill in all required fields correctly';
      return;
    }

    const userId = this.auth.currentUser?.id;
    if (!userId) {
      this.saveError = 'You must be signed in to create branding';
      return;
    }

    this.isSaving = true;
    this.saveError = '';

    const form = new FormData();
    form.append('PrimaryColor', this.newBrand.primaryColor!);
    form.append('SecondaryColor', this.newBrand.secondaryColor!);
    form.append('CompanyName', this.newBrand.companyName || '');
    form.append('ContactEmail', this.newBrand.contactEmail || '');
    form.append('Website', this.newBrand.website || '');
    form.append('ReportFooterText', this.newBrand.reportFooterText || '');
    
    if (this.logoFile) {
      form.append('Logo', this.logoFile);
    }

    this.brandingService.createBranding(form).subscribe({
      next: () => {
        this.isSaving = false;
        this.saveSuccess = 'Branding created successfully!';
        this.resetForm();
        this.loadBranding();
        setTimeout(() => {
          this.saveSuccess = '';
        }, 3000);
      },
      error: (error) => {
        this.isSaving = false;
        this.saveError = 'Failed to create branding. Please try again.';
        console.error('Failed to create branding:', error);
      }
    });
  }

  startEdit(id: string): void {
    this.editingId = id;
    this.saveError = '';
    this.saveSuccess = '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.logoFile = null;
    this.loadBranding(); // Reload to reset any changes
  }

  saveBrand(brand: BrandingInfo): void {
    if (!this.editingId) return;

    if (!this.isValidColor(brand.primaryColor) || !this.isValidColor(brand.secondaryColor)) {
      this.saveError = 'Please enter valid color codes';
      return;
    }

    this.isSaving = true;
    this.saveError = '';

    const form = new FormData();
    form.append('PrimaryColor', brand.primaryColor);
    form.append('SecondaryColor', brand.secondaryColor);
    form.append('CompanyName', brand.companyName || '');
    form.append('ContactEmail', brand.contactEmail || '');
    form.append('Website', brand.website || '');
    form.append('ReportFooterText', brand.reportFooterText || '');
    
    if (this.logoFile) {
      form.append('Logo', this.logoFile);
    }

    this.brandingService.updateBranding(this.editingId, form).subscribe({
      next: () => {
        this.isSaving = false;
        this.editingId = null;
        this.logoFile = null;
        this.saveSuccess = 'Branding updated successfully!';
        this.loadBranding();
        setTimeout(() => {
          this.saveSuccess = '';
        }, 3000);
      },
      error: (error) => {
        this.isSaving = false;
        this.saveError = 'Failed to update branding. Please try again.';
        console.error('Failed to update branding:', error);
      }
    });
  }

  deleteBrand(id: string): void {
    if (!confirm('Are you sure you want to delete this branding? This action cannot be undone.')) {
      return;
    }

    this.brandingService.deleteBranding(id).subscribe({
      next: () => {
        this.saveSuccess = 'Branding deleted successfully!';
        this.loadBranding();
        setTimeout(() => {
          this.saveSuccess = '';
        }, 3000);
      },
      error: (error) => {
        this.saveError = 'Failed to delete branding. Please try again.';
        console.error('Failed to delete branding:', error);
      }
    });
  }

  private resetForm(): void {
    this.newBrand = {
      id: '',
      userId: '',
      logoUrl: '',
      primaryColor: '#4f46e5',
      secondaryColor: '#e0e7ff',
      companyName: '',
      contactEmail: '',
      website: '',
      reportFooterText: 'Powered by AccessLens'
    };
    this.logoFile = null;
    
    // Reset file input
    const fileInput = document.getElementById('logo-upload') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
    
    // Hide preview
    const previewImg = document.getElementById('logo-preview') as HTMLImageElement;
    if (previewImg) {
      previewImg.style.display = 'none';
    }
  }
}