import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, tap, map } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { BrandingInfo, DEFAULT_BRANDING } from '../types/branding.interface';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private brandingSubject = new BehaviorSubject<BrandingInfo[]>([]);
  public branding$ = this.brandingSubject.asObservable();
  private apiUrl = `${environment.apiUrl}/branding`;

  constructor(private http: HttpClient) {}

  loadBranding(): Observable<BrandingInfo[]> {
    return this.http.get<BrandingInfo[]>(this.apiUrl).pipe(
      tap(list => this.brandingSubject.next(list))
    );
  }

  getBranding(): Observable<BrandingInfo | null> {
    if (this.brandingSubject.value.length > 0) {
      return of(this.brandingSubject.value[0]);
    }
    
    return this.loadBranding().pipe(
      map(list => list.length > 0 ? list[0] : null)
    );
  }

  createBranding(data: FormData): Observable<BrandingInfo> {
    return this.http.post<BrandingInfo>(this.apiUrl, data).pipe(
      tap(b => this.brandingSubject.next([...this.brandingSubject.value, b]))
    );
  }

  updateBranding(id: string, data: FormData): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data).pipe(
      tap(() => {
        this.loadBranding().subscribe();
      })
    );
  }

  deleteBranding(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        const list = this.brandingSubject.value.filter(b => b.id !== id);
        this.brandingSubject.next(list);
      })
    );
  }

  resetBranding(): Observable<boolean> {
    // In a real implementation, this would call an API endpoint
    // For now, we'll just simulate success
    return of(true).pipe(
      tap(() => {
        if (this.brandingSubject.value.length > 0) {
          const defaultBranding = { ...DEFAULT_BRANDING, id: this.brandingSubject.value[0].id };
          const updatedList = [defaultBranding, ...this.brandingSubject.value.slice(1)];
          this.brandingSubject.next(updatedList);
        }
      })
    );
  }
}