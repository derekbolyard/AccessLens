import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { BrandingInfo } from '../types/branding.interface';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private brandingSubject = new BehaviorSubject<BrandingInfo[]>([]);
  public branding$ = this.brandingSubject.asObservable();

  getBranding(): Observable<BrandingInfo[]> {
    return this.branding$;
  }

  createBranding(info: BrandingInfo): void {
    const list = [...this.brandingSubject.value, info];
    this.brandingSubject.next(list);
  }

  updateBranding(id: string, update: Partial<BrandingInfo>): void {
    const list = this.brandingSubject.value.map(b =>
      b.id === id ? { ...b, ...update } : b
    );
    this.brandingSubject.next(list);
  }

  deleteBranding(id: string): void {
    const list = this.brandingSubject.value.filter(b => b.id !== id);
    this.brandingSubject.next(list);
  }
}
