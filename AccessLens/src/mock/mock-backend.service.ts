import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay, map } from 'rxjs/operators';
import { Site, Report, Page, AccessibilityIssue } from '../types/report.interface';
import { User } from '../services/auth.service';

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
export class MockBackendService {
  private mockSites: Site[] = [
    {
      id: 'site-1',
      name: 'Corporate Website',
      url: 'https://example.com',
      description: 'Main corporate website',
      userId: 'user-1',
      createdAt: new Date('2024-01-05'),
      updatedAt: new Date(),
      totalReports: 3,
      lastScanDate: new Date('2024-01-15'),
      reports: []
    },
    {
      id: 'site-2',
      name: 'E-commerce Store',
      url: 'https://shop.example.com',
      description: 'Online store for products',
      userId: 'user-1',
      createdAt: new Date('2024-01-10'),
      updatedAt: new Date(),
      totalReports: 2,
      lastScanDate: new Date('2024-01-12'),
      reports: []
    },
    {
      id: 'site-3',
      name: 'Blog Platform',
      url: 'https://blog.example.com',
      description: 'Company blog and news',
      userId: 'user-1',
      createdAt: new Date('2024-01-08'),
      updatedAt: new Date(),
      totalReports: 1,
      lastScanDate: new Date('2024-01-10'),
      reports: []
    },
    // Add sites for different users to test filtering
    {
      id: 'site-4',
      name: 'Other User Site',
      url: 'https://other.example.com',
      description: 'Site belonging to another user',
      userId: 'user-2',
      createdAt: new Date('2024-01-01'),
      updatedAt: new Date(),
      totalReports: 1,
      lastScanDate: new Date('2024-01-05'),
      reports: []
    }
  ];

  private mockReports: Report[] = [
    {
      id: 'report-1',
      siteId: 'site-1',
      name: 'Accessibility Audit - January 2024',
      createdDate: new Date('2024-01-15'),
      status: 'completed',
      totalPages: 5,
      totalIssues: 23,
      fixedIssues: 15,
      ignoredIssues: 3,
      averageScore: 87,
      pages: []
    },
    {
      id: 'report-2',
      siteId: 'site-1',
      name: 'Accessibility Audit - December 2023',
      createdDate: new Date('2023-12-20'),
      status: 'completed',
      totalPages: 4,
      totalIssues: 18,
      fixedIssues: 12,
      ignoredIssues: 2,
      averageScore: 82,
      pages: []
    },
    {
      id: 'report-3',
      siteId: 'site-1',
      name: 'Accessibility Audit - November 2023',
      createdDate: new Date('2023-11-25'),
      status: 'completed',
      totalPages: 3,
      totalIssues: 15,
      fixedIssues: 10,
      ignoredIssues: 1,
      averageScore: 79,
      pages: []
    },
    {
      id: 'report-4',
      siteId: 'site-2',
      name: 'E-commerce Accessibility Review',
      createdDate: new Date('2024-01-12'),
      status: 'completed',
      totalPages: 8,
      totalIssues: 31,
      fixedIssues: 20,
      ignoredIssues: 5,
      averageScore: 75,
      pages: []
    },
    {
      id: 'report-5',
      siteId: 'site-2',
      name: 'Holiday Season Audit',
      createdDate: new Date('2023-12-15'),
      status: 'completed',
      totalPages: 6,
      totalIssues: 22,
      fixedIssues: 18,
      ignoredIssues: 2,
      averageScore: 85,
      pages: []
    },
    {
      id: 'report-6',
      siteId: 'site-3',
      name: 'Blog Accessibility Check',
      createdDate: new Date('2024-01-10'),
      status: 'in-progress',
      totalPages: 0,
      totalIssues: 0,
      fixedIssues: 0,
      ignoredIssues: 0,
      averageScore: 0,
      pages: []
    },
    // Report for other user's site
    {
      id: 'report-7',
      siteId: 'site-4',
      name: 'Other User Report',
      createdDate: new Date('2024-01-05'),
      status: 'completed',
      totalPages: 2,
      totalIssues: 5,
      fixedIssues: 3,
      ignoredIssues: 1,
      averageScore: 90,
      pages: []
    }
  ];

  private mockPages: Page[] = [
    {
      id: 'page-1',
      reportId: 'report-1',
      title: 'Homepage',
      url: 'https://example.com',
      scanDate: new Date('2024-01-15'),
      status: 'completed',
      totalIssues: 8,
      fixedIssues: 5,
      ignoredIssues: 1,
      score: 85,
      issues: []
    },
    {
      id: 'page-2',
      reportId: 'report-1',
      title: 'About Us',
      url: 'https://example.com/about',
      scanDate: new Date('2024-01-15'),
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
      scanDate: new Date('2024-01-15'),
      status: 'completed',
      totalIssues: 4,
      fixedIssues: 3,
      ignoredIssues: 0,
      score: 88,
      issues: []
    },
    {
      id: 'page-4',
      reportId: 'report-1',
      title: 'Services',
      url: 'https://example.com/services',
      scanDate: new Date('2024-01-15'),
      status: 'completed',
      totalIssues: 3,
      fixedIssues: 2,
      ignoredIssues: 1,
      score: 90,
      issues: []
    },
    {
      id: 'page-5',
      reportId: 'report-1',
      title: 'Blog',
      url: 'https://example.com/blog',
      scanDate: new Date('2024-01-15'),
      status: 'completed',
      totalIssues: 2,
      fixedIssues: 1,
      ignoredIssues: 0,
      score: 95,
      issues: []
    }
  ];

  private mockIssues: AccessibilityIssue[] = [
    {
      id: 'issue-1',
      type: 'error',
      rule: 'color-contrast',
      description: 'Elements must have sufficient color contrast',
      element: '<button class="btn-primary">Submit</button>',
      xpath: '/html/body/div[1]/form/button',
      status: 'open',
      severity: 'high',
      category: 'Color',
      impact: 'critical',
      help: 'Ensure all text elements have a contrast ratio of at least 4.5:1',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/color-contrast',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-2',
      type: 'error',
      rule: 'image-alt',
      description: 'Images must have alternate text',
      element: '<img src="hero.jpg">',
      xpath: '/html/body/div[1]/img',
      status: 'fixed',
      severity: 'high',
      category: 'Images',
      impact: 'critical',
      help: 'All img elements must have an alt attribute',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/image-alt',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-3',
      type: 'warning',
      rule: 'heading-order',
      description: 'Heading levels should increase by one',
      element: '<h3>Section Title</h3>',
      xpath: '/html/body/div[2]/h3',
      status: 'ignored',
      severity: 'medium',
      category: 'Structure',
      impact: 'moderate',
      help: 'Ensure headings are in a logical order',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/heading-order',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-4',
      type: 'error',
      rule: 'label',
      description: 'Form elements must have labels',
      element: '<input type="email" placeholder="Email">',
      xpath: '/html/body/div[1]/form/input[1]',
      status: 'open',
      severity: 'high',
      category: 'Forms',
      impact: 'critical',
      help: 'All form inputs must have associated labels',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/label',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-5',
      type: 'warning',
      rule: 'link-name',
      description: 'Links must have discernible text',
      element: '<a href="/more">Read more</a>',
      xpath: '/html/body/div[3]/a',
      status: 'fixed',
      severity: 'medium',
      category: 'Links',
      impact: 'serious',
      help: 'Links must have text that describes their purpose',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/link-name',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-6',
      type: 'notice',
      rule: 'landmark-one-main',
      description: 'Page should contain one main landmark',
      element: '<div class="content">',
      xpath: '/html/body/div[2]',
      status: 'fixed',
      severity: 'low',
      category: 'Structure',
      impact: 'moderate',
      help: 'Ensure page has exactly one main landmark',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/landmark-one-main',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-7',
      type: 'error',
      rule: 'button-name',
      description: 'Buttons must have discernible text',
      element: '<button><i class="icon-close"></i></button>',
      xpath: '/html/body/div[1]/button',
      status: 'fixed',
      severity: 'high',
      category: 'Forms',
      impact: 'critical',
      help: 'Buttons must have accessible names',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/button-name',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    },
    {
      id: 'issue-8',
      type: 'warning',
      rule: 'region',
      description: 'Page must have one main landmark',
      element: '<body>',
      xpath: '/html/body',
      status: 'fixed',
      severity: 'medium',
      category: 'Structure',
      impact: 'moderate',
      help: 'All page content should be contained by landmarks',
      helpUrl: 'https://dequeuniversity.com/rules/axe/4.4/region',
      firstDetected: new Date('2024-01-15'),
      lastSeen: new Date('2024-01-15')
    }
  ];

  // Store current user ID for filtering
  private currentUserId: string | null = null;

  constructor() {
    this.initializeMockData();
  }

  // Method to set current user ID (called by auth service)
  setCurrentUserId(userId: string | null): void {
    this.currentUserId = userId;
  }

  private getCurrentUserId(): string {
    return this.currentUserId || 'user-1'; // Default to user-1 if not set
  }

  private initializeMockData(): void {
    // Assign issues to pages
    this.mockPages[0].issues = this.mockIssues.slice(0, 4); // Homepage gets 4 issues
    this.mockPages[1].issues = this.mockIssues.slice(4, 6); // About gets 2 issues
    this.mockPages[2].issues = this.mockIssues.slice(6, 8); // Contact gets 2 issues
    this.mockPages[3].issues = []; // Services has no issues
    this.mockPages[4].issues = []; // Blog has no issues

    // Assign pages to reports
    this.mockReports[0].pages = this.mockPages.filter(p => p.reportId === 'report-1');
    
    // Assign reports to sites
    this.mockSites[0].reports = this.mockReports.filter(r => r.siteId === 'site-1');
    this.mockSites[1].reports = this.mockReports.filter(r => r.siteId === 'site-2');
    this.mockSites[2].reports = this.mockReports.filter(r => r.siteId === 'site-3');
    this.mockSites[3].reports = this.mockReports.filter(r => r.siteId === 'site-4');
  }

  // Auth endpoints
  sendMagicLink(request: AuthRequest): Observable<ApiResponse<{ message: string }>> {
    return of({
      success: true,
      data: { message: 'Verification code sent successfully' }
    }).pipe(delay(1000));
  }

  verifyMagicLink(request: AuthRequest): Observable<ApiResponse<{ token: string; user: User }>> {
    // Simulate validation
    if (request.code !== '123456') {
      return throwError(() => ({
        error: { error: 'Invalid verification code' }
      })).pipe(delay(500));
    }

    // Generate user ID based on email for consistency
    const userId = this.generateUserIdFromEmail(request.email || '');
    
    const user: User = {
      id: userId,
      name: request.email?.split('@')[0] || 'User',
      email: request.email || '',
      avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(request.email || '')}&background=0078d7&color=fff`,
      provider: 'magic-link'
    };

    // Set current user ID for filtering
    this.setCurrentUserId(userId);

    return of({
      success: true,
      data: {
        token: 'mock-jwt-token',
        user
      }
    }).pipe(delay(1500));
  }

  private generateUserIdFromEmail(email: string): string {
    // Generate consistent user ID based on email
    // This ensures the same email always gets the same user ID
    if (email.includes('test') || email.includes('demo')) {
      return 'user-2'; // Different user for testing
    }
    return 'user-1'; // Default user
  }

  getCsrfToken(): Observable<string> {
    return of('mock-csrf-token').pipe(delay(100));
  }

  // Sites endpoints - filtered by current user
  getSites(): Observable<ApiResponse<Site[]>> {
    const currentUserId = this.getCurrentUserId();
    const userSites = this.mockSites.filter(site => site.userId === currentUserId);
    
    return of({
      success: true,
      data: userSites
    }).pipe(delay(500));
  }

  getSiteById(id: string): Observable<ApiResponse<Site>> {
    const currentUserId = this.getCurrentUserId();
    const site = this.mockSites.find(s => s.id === id && s.userId === currentUserId);
    
    if (!site) {
      return throwError(() => ({ error: 'Site not found or access denied' }));
    }

    return of({
      success: true,
      data: site
    }).pipe(delay(300));
  }

  createSite(siteData: Partial<Site>): Observable<ApiResponse<Site>> {
    const currentUserId = this.getCurrentUserId();
    
    const newSite: Site = {
      id: `site-${Date.now()}`,
      name: siteData.name || '',
      url: siteData.url || '',
      description: siteData.description,
      userId: currentUserId, // Assign to current user
      createdAt: new Date(),
      updatedAt: new Date(),
      totalReports: 0,
      reports: []
    };

    this.mockSites.push(newSite);

    return of({
      success: true,
      data: newSite
    }).pipe(delay(800));
  }

  // Reports endpoints - filtered by user's sites
  getReports(siteId?: string): Observable<ApiResponse<Report[]>> {
    const currentUserId = this.getCurrentUserId();
    let reports = this.mockReports;
    
    if (siteId) {
      // Verify user owns the site
      const site = this.mockSites.find(s => s.id === siteId && s.userId === currentUserId);
      if (!site) {
        return throwError(() => ({ error: 'Site not found or access denied' }));
      }
      reports = reports.filter(r => r.siteId === siteId);
    } else {
      // Filter reports by user's sites
      const userSiteIds = this.mockSites
        .filter(s => s.userId === currentUserId)
        .map(s => s.id);
      reports = reports.filter(r => r.siteId && userSiteIds.includes(r.siteId));
    }

    return of({
      success: true,
      data: reports
    }).pipe(delay(400));
  }

  getReportById(id: string): Observable<ApiResponse<Report>> {
    const currentUserId = this.getCurrentUserId();
    const report = this.mockReports.find(r => r.id === id);
    
    if (!report) {
      return throwError(() => ({ error: 'Report not found' }));
    }

    // Verify user owns the site this report belongs to
    const site = this.mockSites.find(s => s.id === report.siteId && s.userId === currentUserId);
    if (!site) {
      return throwError(() => ({ error: 'Report not found or access denied' }));
    }

    return of({
      success: true,
      data: report
    }).pipe(delay(300));
  }

  // Pages endpoints - filtered by user's reports
  getPages(reportId?: string): Observable<ApiResponse<Page[]>> {
    const currentUserId = this.getCurrentUserId();
    let pages = this.mockPages;
    
    if (reportId) {
      // Verify user owns the report
      const report = this.mockReports.find(r => r.id === reportId);
      if (!report) {
        return throwError(() => ({ error: 'Report not found' }));
      }
      
      const site = this.mockSites.find(s => s.id === report.siteId && s.userId === currentUserId);
      if (!site) {
        return throwError(() => ({ error: 'Report not found or access denied' }));
      }
      
      pages = pages.filter(p => p.reportId === reportId);
    } else {
      // Filter pages by user's reports
      const userReportIds = this.mockReports
        .filter(r => {
          const site = this.mockSites.find(s => s.id === r.siteId);
          return site && site.userId === currentUserId;
        })
        .map(r => r.id);
      pages = pages.filter(p => userReportIds.includes(p.reportId));
    }

    return of({
      success: true,
      data: pages
    }).pipe(delay(400));
  }

  getPageById(id: string): Observable<ApiResponse<Page>> {
    const currentUserId = this.getCurrentUserId();
    const page = this.mockPages.find(p => p.id === id);
    
    if (!page) {
      return throwError(() => ({ error: 'Page not found' }));
    }

    // Verify user owns the report this page belongs to
    const report = this.mockReports.find(r => r.id === page.reportId);
    if (!report) {
      return throwError(() => ({ error: 'Page not found' }));
    }
    
    const site = this.mockSites.find(s => s.id === report.siteId && s.userId === currentUserId);
    if (!site) {
      return throwError(() => ({ error: 'Page not found or access denied' }));
    }

    return of({
      success: true,
      data: page
    }).pipe(delay(300));
  }

  // Issues endpoints - filtered by user's pages
  updateIssueStatus(pageId: string, issueId: string, status: string): Observable<ApiResponse<{ message: string }>> {
    const currentUserId = this.getCurrentUserId();
    const page = this.mockPages.find(p => p.id === pageId);
    
    if (!page) {
      return throwError(() => ({ error: 'Page not found' }));
    }

    // Verify user owns the page
    const report = this.mockReports.find(r => r.id === page.reportId);
    if (!report) {
      return throwError(() => ({ error: 'Page not found' }));
    }
    
    const site = this.mockSites.find(s => s.id === report.siteId && s.userId === currentUserId);
    if (!site) {
      return throwError(() => ({ error: 'Page not found or access denied' }));
    }

    const issue = page.issues.find(i => i.id === issueId);
    if (!issue) {
      return throwError(() => ({ error: 'Issue not found' }));
    }

    issue.status = status as any;
    issue.statusUpdatedAt = new Date();

    // Recalculate page counters
    page.fixedIssues = page.issues.filter(i => i.status === 'fixed').length;
    page.ignoredIssues = page.issues.filter(i => i.status === 'ignored').length;

    return of({
      success: true,
      data: { message: 'Issue status updated successfully' }
    }).pipe(delay(500));
  }

  // Scan endpoints
  requestScan(request: ScanRequest): Observable<ApiResponse<{ scanId: string; message: string }>> {
    // Simulate scan request
    const scanId = `scan-${Date.now()}`;
    
    return of({
      success: true,
      data: {
        scanId,
        message: 'Scan request submitted successfully'
      }
    }).pipe(delay(1200));
  }

  getScanStatus(scanId: string): Observable<ApiResponse<{ status: string; progress: number }>> {
    return of({
      success: true,
      data: {
        status: 'in-progress',
        progress: 45
      }
    }).pipe(delay(300));
  }

  // Utility method to simulate network errors
  simulateError(errorRate: number = 0.1): boolean {
    return Math.random() < errorRate;
  }

  // Method to add delay for more realistic API simulation
  private addRealisticDelay(): number {
    return Math.random() * 1000 + 300; // 300-1300ms delay
  }
}