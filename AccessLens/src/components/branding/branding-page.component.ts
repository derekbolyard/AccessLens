import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputComponent } from '../common/input/input.component';
import { ButtonComponent } from '../common/button/button.component';
import { CardComponent } from '../common/card/card.component';
import { BrandingService } from '../../services/branding.service';
import { BrandingInfo } from '../../types/branding.interface';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-branding-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, ButtonComponent, CardComponent],
  templateUrl: './branding-page.component.html',
  styleUrls: ['./branding-page.component.scss']
})
export class BrandingPageComponent implements OnInit {
  brandings: BrandingInfo[] = [];
  logoFile: File | null = null;
  editingId: string | null = null;

  newBrand: BrandingInfo = {
    id: '',
    userId: '',
    logoUrl: '',
    primaryColor: '#4f46e5',
    secondaryColor: '#e0e7ff'
  };

  constructor(
    private brandingService: BrandingService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.brandingService.loadBranding().subscribe(b => (this.brandings = b));
  }

  createBrand(): void {
    const userId = this.auth.currentUser?.id;
    if (!userId) return;

    const form = new FormData();
    form.append('PrimaryColor', this.newBrand.primaryColor);
    form.append('SecondaryColor', this.newBrand.secondaryColor);
    if (this.logoFile) {
      form.append('Logo', this.logoFile);
    }

    this.brandingService.createBranding(form).subscribe(() => {
      this.resetForm();
    });
  }

  startEdit(id: string): void {
    this.editingId = id;
  }

  saveBrand(brand: BrandingInfo): void {
    if (!this.editingId) return;
    const form = new FormData();
    form.append('PrimaryColor', brand.primaryColor);
    form.append('SecondaryColor', brand.secondaryColor);
    if (this.logoFile) {
      form.append('Logo', this.logoFile);
    }
    this.brandingService.updateBranding(this.editingId, form).subscribe(() => {
      this.editingId = null;
      this.logoFile = null;
    });
  }

  deleteBrand(id: string): void {
    this.brandingService.deleteBranding(id).subscribe();
  }

  private resetForm(): void {
    this.newBrand = {
      id: '',
      userId: '',
      logoUrl: '',
      primaryColor: '#4f46e5',
      secondaryColor: '#e0e7ff'
    };
    this.logoFile = null;
  }
}
