import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';

const CSRF_HEADER = 'X-CSRF-TOKEN';
const STORAGE_KEY = 'X-CSRF-TOKEN';

@Injectable({ providedIn: 'root' })
export class CsrfService {
  private token: string | null = null;

  constructor(private apiService: ApiService) {}

  /** Call once via APP_INITIALIZER */
  async init(): Promise<void> {
    try {
      const token = await firstValueFrom(this.apiService.getCsrfToken());
      
      if (token) {
        this.token = token;
        sessionStorage.setItem(STORAGE_KEY, token);
      }
    } catch (error) {
      console.warn('Failed to get CSRF token:', error);
      // Continue without CSRF token in development/mock mode
    }
  }

  get csrf(): string | null {
    return this.token ?? sessionStorage.getItem(STORAGE_KEY);
  }
}