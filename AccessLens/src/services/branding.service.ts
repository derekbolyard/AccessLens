import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { BrandingInfo } from '../types/branding.interface';

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
    this.brandingSubject.next(list);
  }

  deleteBranding(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
    const list = this.brandingSubject.value.filter(b => b.id !== id);
    this.brandingSubject.next(list);
      })
    );
  }
}
