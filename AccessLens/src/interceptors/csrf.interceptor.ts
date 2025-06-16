// src/app/core/csrf.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { CsrfService } from '../services/csrf.service';

@Injectable()
export class CsrfInterceptor implements HttpInterceptor {
  constructor(private csrf: CsrfService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Always send cookies
    let clone = req.clone({ withCredentials: true });

    const token = this.csrf.csrf;
    if (token) {
      clone = clone.clone({ setHeaders: { 'X-CSRF-TOKEN': token } });
    }
    return next.handle(clone);
  }
}
