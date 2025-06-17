import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputComponent } from '../common/input/input.component';
import { ButtonComponent } from '../common/button/button.component';
import { CardComponent } from '../common/card/card.component';
import { BrandingService } from '../../services/branding.service';
import { BrandingInfo } from '../../types/branding.interface';
import { v4 as uuidv4 } from 'uuid';

@Component({
  selector: 'app-branding-page',
  standalone: true,
  imports: [CommonModule, FormsModule, InputComponent, ButtonComponent, CardComponent],
  templateUrl: './branding-page.component.html',
  styleUrls: ['./branding-page.component.scss']
})
export class BrandingPageComponent implements OnInit {
  brandings: BrandingInfo[] = [];

  newBrand: BrandingInfo = {
    id: '',
    userId: 'user_123',
    logoUrl: '',
    primaryColor: '#4f46e5',
    secondaryColor: '#e0e7ff'
  };

  constructor(private brandingService: BrandingService) {}

  ngOnInit(): void {
    this.brandingService.getBranding().subscribe(b => this.brandings = b);
  }

  createBrand(): void {
    const brand = { ...this.newBrand, id: uuidv4() };
    this.brandingService.createBranding(brand);
    this.resetForm();
  }

  updateBrand(brand: BrandingInfo): void {
    this.brandingService.updateBranding(brand.id, brand);
  }

  deleteBrand(id: string): void {
    this.brandingService.deleteBranding(id);
  }

  private resetForm(): void {
    this.newBrand = {
      id: '',
      userId: 'user_123',
      logoUrl: '',
      primaryColor: '#4f46e5',
      secondaryColor: '#e0e7ff'
    };
  }
}
