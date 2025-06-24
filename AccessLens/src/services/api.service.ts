import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { MockBackendService } from '../mock/mock-backend.service';
import { Site, Report, Page } from '../types/report.interface';
import { User } from './auth.service';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  error?: string;
}

export interface ScanRequest {
  url: string;
  email: string;
}

export interface AuthRequest {
  email: string;
  code?: string;
  hcaptchaToken?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;
  private useMockBackend = environment.features.useMockBackend;

  constructor(
    private http: HttpClient,
    private mockBackend: MockBackendService
  ) {}

  // Auth endpoints
  sendMagicLink(email: string): Observable<ApiResponse<{ message: string }>> {
    if (this.useMockBackend) {
      return this.mockBackend.sendMagicLink({ email });
    }
    return this.http.post<ApiResponse<{ message: string }>>(`${this.apiUrl}/auth/send-magic-link`, { email });
  }

  verifyMagicLink(email: string, code: string, hcaptchaToken?: string): Observable<ApiResponse<{ token: string; user: User }>> {
    if (this.useMockBackend) {
      return this.mockBackend.verifyMagicLink({ email, code, hcaptchaToken });
    }
    return this.http.post<ApiResponse<{ token: string; user: User }>>(`${this.apiUrl}/auth/verify`, {
      email,
      code,
      hcaptchaToken
    });
  }

  getCsrfToken(): Observable<string> {
    if (this.useMockBackend) {
      return this.mockBackend.getCsrfToken();
    }
    return this.http.get(`${this.apiUrl}/auth/csrf`, { responseType: 'text' });
  }

  // Sites endpoints
  getSites(): Observable<ApiResponse<Site[]>> {
    if (this.useMockBackend) {
      return this.mockBackend.getSites();
    }
    return this.http.get<ApiResponse<Site[]>>(`${this.apiUrl}/sites`);
  }

  getSiteById(id: string): Observable<ApiResponse<Site>> {
    if (this.useMockBackend) {
      return this.mockBackend.getSiteById(id);
    }
    return this.http.get<ApiResponse<Site>>(`${this.apiUrl}/sites/${id}`);
  }

  createSite(siteData: Partial<Site>): Observable<ApiResponse<Site>> {
    if (this.useMockBackend) {
      return this.mockBackend.createSite(siteData);
    }
    return this.http.post<ApiResponse<Site>>(`${this.apiUrl}/sites`, siteData);
  }

  updateSite(id: string, siteData: Partial<Site>): Observable<ApiResponse<Site>> {
    if (this.useMockBackend) {
      // Mock implementation would go here
      throw new Error('Mock update site not implemented');
    }
    return this.http.put<ApiResponse<Site>>(`${this.apiUrl}/sites/${id}`, siteData);
  }

  deleteSite(id: string): Observable<ApiResponse<{ message: string }>> {
    if (this.useMockBackend) {
      // Mock implementation would go here
      throw new Error('Mock delete site not implemented');
    }
    return this.http.delete<ApiResponse<{ message: string }>>(`${this.apiUrl}/sites/${id}`);
  }

  // Reports endpoints
  getReports(siteId?: string): Observable<ApiResponse<Report[]>> {
    if (this.useMockBackend) {
      return this.mockBackend.getReports(siteId);
    }
    const url = siteId ? `${this.apiUrl}/sites/${siteId}/reports` : `${this.apiUrl}/reports`;
    return this.http.get<ApiResponse<Report[]>>(url);
  }

  getReportById(id: string): Observable<ApiResponse<Report>> {
    if (this.useMockBackend) {
      return this.mockBackend.getReportById(id);
    }
    return this.http.get<ApiResponse<Report>>(`${this.apiUrl}/reports/${id}`);
  }

  // Pages endpoints
  getPages(reportId?: string): Observable<ApiResponse<Page[]>> {
    if (this.useMockBackend) {
      return this.mockBackend.getPages(reportId);
    }
    const url = reportId ? `${this.apiUrl}/reports/${reportId}/pages` : `${this.apiUrl}/pages`;
    return this.http.get<ApiResponse<Page[]>>(url);
  }

  getPageById(id: string): Observable<ApiResponse<Page>> {
    if (this.useMockBackend) {
      return this.mockBackend.getPageById(id);
    }
    return this.http.get<ApiResponse<Page>>(`${this.apiUrl}/pages/${id}`);
  }

  // Issues endpoints
  updateIssueStatus(pageId: string, issueId: string, status: string): Observable<ApiResponse<{ message: string }>> {
    if (this.useMockBackend) {
      return this.mockBackend.updateIssueStatus(pageId, issueId, status);
    }
    return this.http.patch<ApiResponse<{ message: string }>>(`${this.apiUrl}/pages/${pageId}/issues/${issueId}`, {
      status
    });
  }

  // Scan endpoints
  requestScan(url: string, email: string): Observable<ApiResponse<{ scanId: string; message: string }>> {
    if (this.useMockBackend) {
      return this.mockBackend.requestScan({ url, email });
    }
    return this.http.post<ApiResponse<{ scanId: string; message: string }>>(`${this.apiUrl}/scan/starter`, {
      url,
      email
    });
  }

  getScanStatus(scanId: string): Observable<ApiResponse<{ status: string; progress: number }>> {
    if (this.useMockBackend) {
      return this.mockBackend.getScanStatus(scanId);
    }
    return this.http.get<ApiResponse<{ status: string; progress: number }>>(`${this.apiUrl}/scans/${scanId}/status`);
  }

  // Utility methods
  setUseMockBackend(useMock: boolean): void {
    this.useMockBackend = useMock;
  }

  getUseMockBackend(): boolean {
    return this.useMockBackend;
  }
}