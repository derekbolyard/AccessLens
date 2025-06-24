import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, map, catchError, tap } from 'rxjs/operators';
import { Site, Report, Page, AccessibilityIssue, IssueStatus } from '../types/report.interface';
import { CacheService } from './cache.service';
import { ErrorHandlerService } from './error-handler.service';
import { AnalyticsService } from './analytics.service';
import { ToastService } from '../components/common/toast/toast.service';
import { Validators } from '../utils/validators';
import { ApiService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private sitesSubject = new BehaviorSubject<Site[]>([]);
  public sites$ = this.sitesSubject.asObservable();

  constructor(
    private apiService: ApiService,
    private cacheService: CacheService,
    private errorHandler: ErrorHandlerService,
    private analytics: AnalyticsService,
    private toastService: ToastService
  ) {
    this.loadSites();
  }

  private loadSites(): void {
    this.getSites().subscribe({
      next: (sites) => {
        this.sitesSubject.next(sites);
      },
      error: (error) => {
        this.errorHandler.handleError(error, 'ReportService.loadSites');
        this.toastService.error('Failed to load sites');
      }
    });
  }

  getSites(): Observable<Site[]> {
    const cacheKey = 'sites';
    
    return this.cacheService.getOrSet(cacheKey, () => {
      return this.apiService.getSites().pipe(
        map(response => response.data),
        catchError(error => {
          this.errorHandler.handleError(error, 'ReportService.getSites');
          this.toastService.error('Failed to load sites');
          return throwError(() => error);
        })
      );
    });
  }

  getSiteById(id: string): Observable<Site | undefined> {
    return this.apiService.getSiteById(id).pipe(
      map(response => response.data),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getSiteById');
        this.toastService.error('Failed to load site details');
        return of(undefined);
      })
    );
  }

  getReportById(reportId: string): Observable<Report | undefined> {
    return this.apiService.getReportById(reportId).pipe(
      map(response => response.data),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getReportById');
        this.toastService.error('Failed to load report details');
        return of(undefined);
      })
    );
  }

  getPageById(pageId: string): Observable<Page | undefined> {
    return this.apiService.getPageById(pageId).pipe(
      map(response => response.data),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getPageById');
        this.toastService.error('Failed to load page details');
        return of(undefined);
      })
    );
  }

  updateIssueStatus(pageId: string, issueId: string, status: IssueStatus): void {
    this.apiService.updateIssueStatus(pageId, issueId, status).subscribe({
      next: (response) => {
        // Update local data
        this.updateLocalIssueStatus(pageId, issueId, status);
        
        // Track analytics
        this.analytics.trackIssueStatusChange('unknown', status);
        
        this.toastService.success(`Issue status updated to ${this.getStatusLabel(status)}`);
      },
      error: (error) => {
        this.errorHandler.handleError(error, 'ReportService.updateIssueStatus');
        this.toastService.error('Failed to update issue status');
        throw error;
      }
    });
  }

  private getStatusLabel(status: IssueStatus): string {
    switch (status) {
      case 'open': return 'Open';
      case 'fixed': return 'Fixed';
      case 'ignored': return 'Ignored';
      default: return status;
    }
  }

  private updateLocalIssueStatus(pageId: string, issueId: string, status: IssueStatus): void {
    const sites = this.sitesSubject.value;
    
    for (let siteIndex = 0; siteIndex < sites.length; siteIndex++) {
      const site = { ...sites[siteIndex] };
      
      for (let reportIndex = 0; reportIndex < site.reports.length; reportIndex++) {
        const report = { ...site.reports[reportIndex] };
        
        for (let pageIndex = 0; pageIndex < report.pages.length; pageIndex++) {
          const page = { ...report.pages[pageIndex] };
          
          if (page.id === pageId) {
            const issueIndex = page.issues.findIndex(issue => issue.id === issueId);
            
            if (issueIndex !== -1) {
              page.issues[issueIndex] = { ...page.issues[issueIndex], status };
              
              // Recalculate page counters
              page.fixedIssues = page.issues.filter(i => i.status === 'fixed').length;
              page.ignoredIssues = page.issues.filter(i => i.status === 'ignored').length;
              
              report.pages[pageIndex] = page;
              
              // Recalculate report counters
              report.fixedIssues = report.pages.reduce((total, p) => total + p.fixedIssues, 0);
              report.ignoredIssues = report.pages.reduce((total, p) => total + p.ignoredIssues, 0);
              report.totalIssues = report.pages.reduce((total, p) => total + p.totalIssues, 0);
              report.averageScore = Math.round(
                report.pages.reduce((total, p) => total + (p.score ?? 0), 0) / report.pages.length
              );
              
              site.reports[reportIndex] = report;
              
              const newSites = [...sites];
              newSites[siteIndex] = site;
              
              this.sitesSubject.next(newSites);
              this.cacheService.delete('sites'); // Invalidate cache
              
              return;
            }
          }
        }
      }
    }
  }

  requestNewScan(url: string): Observable<boolean> {
    // Validate URL
    if (!Validators.isValidUrl(url)) {
      this.toastService.error('Invalid URL provided');
      return throwError(() => new Error('Invalid URL provided'));
    }

    const userEmail = 'user@example.com'; // This should come from auth service

    return this.apiService.requestScan(url, userEmail).pipe(
      map(response => response.success),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.requestNewScan');
        this.toastService.error('Failed to request scan. Please try again.');
        return throwError(() => error);
      })
    );
  }

  createSite(siteData: Partial<Site>): Observable<Site> {
    return this.apiService.createSite(siteData).pipe(
      map(response => {
        // Don't update local sites list here - let the component handle it
        // to avoid duplicate entries
        this.cacheService.delete('sites'); // Invalidate cache
        
        this.toastService.success('Site created successfully');
        return response.data;
      }),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.createSite');
        this.toastService.error('Failed to create site');
        return throwError(() => error);
      })
    );
  }

  refreshSites(): Observable<Site[]> {
    this.cacheService.delete('sites');
    return this.getSites().pipe(
      tap(sites => {
        this.sitesSubject.next(sites);
      })
    );
  }
}