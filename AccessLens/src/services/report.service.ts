import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { delay, map, catchError } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Site, Report, Page, AccessibilityIssue, IssueStatus } from '../types/report.interface';
import { CacheService } from './cache.service';
import { ErrorHandlerService } from './error-handler.service';
import { AnalyticsService } from './analytics.service';
import { ToastService } from '../components/common/toast/toast.service';
import { Validators } from '../utils/validators';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private sitesSubject = new BehaviorSubject<Site[]>([]);
  public sites$ = this.sitesSubject.asObservable();
  private apiUrl = environment.apiUrl;

  private mockReports: Report[] = [
    {
      id: 'report-1',
      siteId: 'site-1',
      name: 'Accessibility Audit - January 2024',
      createdDate: new Date('2024-01-10'),
      status: 'completed',
      totalPages: 3,
      totalIssues: 18,
      fixedIssues: 12,
      ignoredIssues: 3,
      averageScore: 87,
      pages: [
        {
          id: 'page-1',
          reportId: 'report-1',
          title: 'Homepage',
          url: 'https://example.com',
          scanDate: new Date('2024-01-10'),
          status: 'completed',
          totalIssues: 12,
          fixedIssues: 8,
          ignoredIssues: 2,
          score: 85,
          issues: [
            {
              id: 'issue-1',
              type: 'error',
              rule: 'color-contrast',
              description: 'Elements must have sufficient color contrast',
              element: '<button>',
              xpath: '/html/body/div[1]/button',
              status: 'open',
              severity: 'high',
              category: 'Color',
              impact: 'critical',
              help: 'Elements must have sufficient color contrast',
              helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/color-contrast',
              firstDetected: new Date(),
              lastSeen: new Date()
            },
            {
              id: 'issue-2',
              type: 'warning',
              rule: 'alt-text',
              description: 'Images must have alternate text',
              element: '<img>',
              xpath: '/html/body/div[1]/img',
              status: 'fixed',
              severity: 'medium',
              category: 'Images',
              impact: 'critical',
              help: 'Images must have alternate text',
              helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/image-alt',
              firstDetected: new Date(),
              lastSeen: new Date()
            },
            {
              id: 'issue-3',
              type: 'warning',
              rule: 'heading-order',
              description: 'Heading levels should increase by one',
              element: '<h3>',
              xpath: '/html/body/div[1]/h3',
              status: 'ignored',
              severity: 'low',
              category: 'Structure',
              impact: 'moderate',
              help: 'Heading levels should increase by one',
              helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/heading-order',
              firstDetected: new Date(),
              lastSeen: new Date()
            }
          ]
        },
        {
          id: 'page-2',
          reportId: 'report-1',
          title: 'About Us',
          url: 'https://example.com/about',
          scanDate: new Date('2024-01-09'),
          status: 'completed',
          totalIssues: 6,
          fixedIssues: 4,
          ignoredIssues: 1,
          score: 92,
          issues: []
        },
        {
          id: 'page-3',
          reportId: 'report-1',
          title: 'Contact',
          url: 'https://example.com/contact',
          scanDate: new Date('2024-01-08'),
          status: 'completed',
          totalIssues: 0,
          fixedIssues: 0,
          ignoredIssues: 0,
          score: 100,
          issues: []
        }
      ]
    }
  ];

  private mockSites: Site[] = [
    {
      id: 'site-1',
      name: 'Corporate Website',
      url: 'https://example.com',
      description: 'Main corporate website',
      userId: 'user-1',
      createdAt: new Date('2024-01-05'),
      updatedAt: new Date(),
      reports: []
    }
  ];

  constructor(
    private http: HttpClient,
    private cacheService: CacheService,
    private errorHandler: ErrorHandlerService,
    private analytics: AnalyticsService,
    private toastService: ToastService
  ) {
    this.loadMockData();
  }

  private loadMockData(): void {
    this.sitesSubject.next(this.mockSites);
    this.cacheService.set('sites', this.mockSites, 10 * 60 * 1000); // Cache for 10 minutes
  }

  getSites(): Observable<Site[]> {
    const cacheKey = 'sites';
    
    return this.cacheService.getOrSet(cacheKey, () => {
      return this.sites$.pipe(
        catchError(error => {
          this.errorHandler.handleError(error, 'ReportService.getSites');
          this.toastService.error('Failed to load sites');
          return throwError(() => error);
        })
      );
    });
  }

  getSiteById(id: string): Observable<Site | undefined> {
    return this.getSites().pipe(
      map(sites => sites.find(site => site.id === id)),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getSiteById');
        return of(undefined);
      })
    );
  }

  getReportById(reportId: string): Observable<Report | undefined> {
    return this.getSites().pipe(
      map(sites => {
        const allReports = sites.flatMap(site => site.reports);
        return allReports.find(report => report.id === reportId);
      }),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getReportById');
        return of(undefined);
      })
    );
  }

  getPageById(pageId: string): Observable<Page | undefined> {
    return this.getSites().pipe(
      map(sites => {
        const allPages = sites
          .flatMap(site => site.reports)
          .flatMap(report => report.pages);
        return allPages.find(page => page.id === pageId);
      }),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.getPageById');
        return of(undefined);
      })
    );
  }

  updateIssueStatus(pageId: string, issueId: string, status: IssueStatus): void {
    try {
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
                const oldStatus = page.issues[issueIndex].status;
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
                  report.pages.reduce((total, p) => total + (p.score ?? 0) , 0) / report.pages.length
                );
                
                site.reports[reportIndex] = report;
                
                const newSites = [...sites];
                newSites[siteIndex] = site;
                
                this.sitesSubject.next(newSites);
                this.cacheService.delete('sites'); // Invalidate cache
                
                // Track analytics
                this.analytics.trackIssueStatusChange(oldStatus, status);
                
                return;
              }
            }
          }
        }
      }
    } catch (error) {
      this.errorHandler.handleError(error, 'ReportService.updateIssueStatus');
      throw error;
    }
  }

  requestNewScan(url: string): Observable<boolean> {
    // Validate URL
    if (!Validators.isValidUrl(url)) {
      return throwError(() => new Error('Invalid URL provided'));
    }

    // Use real API call instead of mock
    return this.http.post<{success: boolean}>(`${this.apiUrl}/scan/starter`, {
      url: url,
      email: 'user@example.com' // This should come from auth service
    }).pipe(
      map(response => response.success),
      catchError(error => {
        this.errorHandler.handleError(error, 'ReportService.requestNewScan');
        return throwError(() => error);
      })
    );
  }

  private calculateAverageScore(report: Report): number {
    if (!report.pages || report.pages.length === 0) return 0;
    const validScores = report.pages.filter(p => p.score !== undefined && p.score !== null);
    if (validScores.length === 0) return 0;
    return Math.round(validScores.reduce((total, p) => total + (p.score || 0), 0) / validScores.length);
  }
}