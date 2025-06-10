import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';

interface CacheItem<T> {
  data: T;
  timestamp: number;
  ttl: number;
}

@Injectable({
  providedIn: 'root'
})
export class CacheService {
  private cache = new Map<string, CacheItem<any>>();
  private readonly DEFAULT_TTL = 5 * 60 * 1000; // 5 minutes

  set<T>(key: string, data: T, ttl: number = this.DEFAULT_TTL): void {
    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl
    });
  }

  get<T>(key: string): T | null {
    const item = this.cache.get(key);
    
    if (!item) {
      return null;
    }

    if (Date.now() - item.timestamp > item.ttl) {
      this.cache.delete(key);
      return null;
    }

    return item.data;
  }

  has(key: string): boolean {
    return this.get(key) !== null;
  }

  delete(key: string): void {
    this.cache.delete(key);
  }

  clear(): void {
    this.cache.clear();
  }

  getOrSet<T>(key: string, factory: () => Observable<T>, ttl?: number): Observable<T> {
    const cached = this.get<T>(key);
    
    if (cached !== null) {
      return of(cached);
    }

    return new Observable(observer => {
      factory().subscribe({
        next: (data) => {
          this.set(key, data, ttl);
          observer.next(data);
          observer.complete();
        },
        error: (error) => observer.error(error)
      });
    });
  }
}