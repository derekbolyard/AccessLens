import { Injectable } from '@angular/core';

const STORAGE_KEY = 'X-CSRF-TOKEN';

@Injectable({ providedIn: 'root' })
export class MockCsrfService {
  private token: string | null = null;

  constructor() {
    // Generate a mock CSRF token
    this.token = this.generateMockToken();
    sessionStorage.setItem(STORAGE_KEY, this.token);
  }

  async init(): Promise<void> {
    // No need to fetch from server, we already set the token in constructor
    return Promise.resolve();
  }

  get csrf(): string | null {
    return this.token ?? sessionStorage.getItem(STORAGE_KEY);
  }

  private generateMockToken(): string {
    // Generate a random string to simulate a CSRF token
    return Array.from(
      { length: 32 },
      () => Math.floor(Math.random() * 16).toString(16)
    ).join('');
  }
}