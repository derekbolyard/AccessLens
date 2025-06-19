import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { BrandingInfo } from '../types/branding.interface';
import { v4 as uuidv4 } from 'uuid';

@Injectable({
  providedIn: 'root'
})
export class MockBrandingService {
  private brandingSubject = new BehaviorSubject<BrandingInfo[]>([]);
  public branding$ = this.brandingSubject.asObservable();

  constructor() {
    // Initialize with some mock data
    const mockBranding: BrandingInfo = {
      id: uuidv4(),
      userId: uuidv4(),
      logoUrl: 'https://via.placeholder.com/200x100?text=Company+Logo',
      primaryColor: '#4f46e5',
      secondaryColor: '#e0e7ff',
      companyName: 'Demo Company',
      contactEmail: 'contact@demo.com',
      website: 'https://demo.com',
      reportFooterText: 'Powered by AccessLens'
    };
    this.brandingSubject.next([mockBranding]);
  }

  loadBranding(): Observable<BrandingInfo[]> {
    return this.branding$;
  }

  getBranding(): Observable<BrandingInfo | null> {
    const brandings = this.brandingSubject.value;
    return of(brandings.length > 0 ? brandings[0] : null);
  }

  createBranding(formData: FormData): Observable<BrandingInfo> {
    const newBranding: BrandingInfo = {
      id: uuidv4(),
      userId: uuidv4(),
      logoUrl: 'https://via.placeholder.com/200x100?text=New+Logo',
      primaryColor: formData.get('PrimaryColor')?.toString() || '#4f46e5',
      secondaryColor: formData.get('SecondaryColor')?.toString() || '#e0e7ff',
      companyName: formData.get('CompanyName')?.toString() || '',
      contactEmail: formData.get('ContactEmail')?.toString() || '',
      website: formData.get('Website')?.toString() || '',
      reportFooterText: formData.get('ReportFooterText')?.toString() || 'Powered by AccessLens'
    };

    const currentBrandings = this.brandingSubject.value;
    this.brandingSubject.next([...currentBrandings, newBranding]);
    
    return of(newBranding);
  }

  updateBranding(id: string, formData: FormData): Observable<void> {
    const currentBrandings = this.brandingSubject.value;
    const index = currentBrandings.findIndex(b => b.id === id);
    
    if (index !== -1) {
      const updatedBranding: BrandingInfo = {
        ...currentBrandings[index],
        primaryColor: formData.get('PrimaryColor')?.toString() || currentBrandings[index].primaryColor,
        secondaryColor: formData.get('SecondaryColor')?.toString() || currentBrandings[index].secondaryColor,
        companyName: formData.get('CompanyName')?.toString() || currentBrandings[index].companyName || '',
        contactEmail: formData.get('ContactEmail')?.toString() || currentBrandings[index].contactEmail || '',
        website: formData.get('Website')?.toString() || currentBrandings[index].website || '',
        reportFooterText: formData.get('ReportFooterText')?.toString() || currentBrandings[index].reportFooterText || ''
      };
      
      const newBrandings = [...currentBrandings];
      newBrandings[index] = updatedBranding;
      this.brandingSubject.next(newBrandings);
    }
    
    return of(undefined);
  }

  deleteBranding(id: string): Observable<void> {
    const currentBrandings = this.brandingSubject.value;
    this.brandingSubject.next(currentBrandings.filter(b => b.id !== id));
    return of(undefined);
  }

  resetBranding(): Observable<boolean> {
    const currentBrandings = this.brandingSubject.value;
    if (currentBrandings.length > 0) {
      const resetBranding: BrandingInfo = {
        ...currentBrandings[0],
        logoUrl: 'https://via.placeholder.com/200x100?text=Default+Logo',
        primaryColor: '#4f46e5',
        secondaryColor: '#e0e7ff',
        companyName: 'Default Company',
        contactEmail: '',
        website: '',
        reportFooterText: 'Powered by AccessLens'
      };
      
      const newBrandings = [...currentBrandings];
      newBrandings[0] = resetBranding;
      this.brandingSubject.next(newBrandings);
    }
    
    return of(true);
  }
}