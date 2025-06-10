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
    const mockSites: Site[] = [
      {
        id: '1',
        name: 'Main Website',
        url: 'https://example.com',
        createdDate: new Date('2024-01-01'),
        lastScanDate: new Date('2024-01-15'),
        totalReports: 2,
        reports: [
          {
            id: '1-1',
            siteId: '1',
            name: 'January 2024 Audit',
            createdDate: new Date('2024-01-15'),
            status: 'completed',
            totalPages: 3,
            totalIssues: 18,
            fixedIssues: 12,
            ignoredIssues: 3,
            averageScore: 87,
            pages: [
              {
                id: '1-1-1',
                reportId: '1-1',
                title: 'Homepage',
                url: 'https://example.com',
                scanDate: new Date('2024-01-15'),
                status: 'completed',
                totalIssues: 12,
                fixedIssues: 8,
                ignoredIssues: 2,
                score: 85,
                issues: [
                  {
                    id: '1-1-1-1',
                    type: 'error',
                    rule: 'color-contrast',
                    description: 'Text has insufficient color contrast',
                    element: '<button class="submit-btn">Submit</button>',
                    xpath: '/html/body/main/form/button',
                    status: 'not-fixed',
                    impact: 'serious',
                    help: 'Ensure text has sufficient color contrast against its background. The contrast ratio should be at least 4.5:1 for normal text.',
                    helpUrl: 'https://dequeuniversity.com/rules/axe/4.6/color-contrast'
                  },
                  {
                    id: '1-1-1-2',
                    type: 'error',
                    rule: 'label',
                    description: 'Form elements must have labels',
                    element: '<input type="email" placeholder="Enter email">',
                    xpath: '/html/body/main/form/input[1]',
                    status: 'fixed',
                    impact: 'critical',
                    help: 'Every form control must have a label that describes its purpose.',
                    helpUrl: 'https://dequeuniversity.com/rules/axe/4.6/label'
                  },
                  {
                    id: '1-1-1-3',
                    type: 'warning',
                    rule: 'heading-order',
                    description: 'Heading levels should only increase by one',
                    element: '<h3>Section Title</h3>',
                    xpath: '/html/body/main/section/h3',
                    status: 'ignored',
                    impact: 'moderate',
                    help: 'Headings should be in logical order to help screen reader users navigate content.',
                    helpUrl: 'https://dequeuniversity.com/rules/axe/4.6/heading-order'
                  }
                ]
              },
              {
                id: '1-1-2',
                reportId: '1-1',
                title: 'Product Catalog',
                url: 'https://example.com/products',
                scanDate: new Date('2024-01-10'),
                status: 'completed',
                totalIssues: 6,
                fixedIssues: 4,
                ignoredIssues: 1,
                score: 92,
                issues: [
                  {
                    id: '1-1-2-1',
                    type: 'error',
                    rule: 'image-alt',
                    description: 'Images must have alternate text',
                    element: '<img src="product-1.jpg" class="product-image">',
                    xpath: '/html/body/main/div/img',
                    status: 'not-fixed',
                    impact: 'critical',
                    help: 'All images must have alt text that describes the image content.',
                    helpUrl: 'https://dequeuniversity.com/rules/axe/4.6/image-alt'
                  }
                ]
              },
              {
                id: '1-1-3',
                reportId: '1-1',
                title: 'Contact Form',
                url: 'https://example.com/contact',
                scanDate: new Date('2024-01-12'),
                status: 'completed',
                totalIssues: 0,
                fixedIssues: 0,
                ignoredIssues: 0,
                score: 100,
                issues: []
              }
            ]
          },
          {
            id: '1-2',
            siteId: '1',
            name: 'December 2023 Audit',
            createdDate: new Date('2023-12-15'),
            status: 'completed',
            totalPages: 2,
            totalIssues: 15,
            fixedIssues: 15,
            ignoredIssues: 0,
            averageScore: 95,
            pages: []
          }
        ]
      },
      {
        id: '2',
        name: 'E-commerce Store',
        url: 'https://shop.example.com',
        createdDate: new Date('2024-01-05'),
        lastScanDate: new Date('2024-01-14'),
        totalReports: 1,
        reports: [
          {
            id: '2-1',
            siteId: '2',
            name: 'Initial Audit',
            createdDate: new Date('2024-01-14'),
            status: 'completed',
            totalPages: 2,
            totalIssues: 8,
            fixedIssues: 6,
            ignoredIssues: 1,
            averageScore: 91,
            pages: [
              {
                id: '2-1-1',
                reportId: '2-1',
                title: 'Shop Homepage',
                url: 'https://shop.example.com',
                scanDate: new Date('2024-01-14'),
                status: 'completed',
                totalIssues: 5,
                fixedIssues: 4,
                ignoredIssues: 1,
                score: 88,
                issues: []
              },
              {
                id: '2-1-2',
                reportId: '2-1',
                title: 'Checkout Page',
                url: 'https://shop.example.com/checkout',
                scanDate: new Date('2024-01-13'),
                status: 'completed',
                totalIssues: 3,
                fixedIssues: 2,
                ignoredIssues: 0,
                score: 94,
                issues: []
              }
            ]
          }
        ]
      }
    ];

    this.sitesSubject.next(mockSites);
    this.cacheService.set('sites', mockSites, 10 * 60 * 1000); // Cache for 10 minutes
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
                  report.pages.reduce((total, p) => total + p.score, 0) / report.pages.length
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
}