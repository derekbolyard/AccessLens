// src/app/core/csrf.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

const CSRF_HEADER = 'X-CSRF-TOKEN';
const STORAGE_KEY = 'X-CSRF-TOKEN';

@Injectable({ providedIn: 'root' })
export class CsrfService {
  private token: string | null = null;

  constructor(private http: HttpClient) {}

  /** Call once via APP_INITIALIZER */
  async init(): Promise<void> {
    const t = await firstValueFrom(
      this.http.get('/api/auth/csrf', {
        responseType: 'text',
        withCredentials: true
      })
    );

    // responseType:'text' returns string | null | undefined
    if (t) {
      this.token = t;
      sessionStorage.setItem(STORAGE_KEY, t);
    }
  }

  get csrf(): string | null {
    return this.token ?? sessionStorage.getItem(STORAGE_KEY);
  }
}
