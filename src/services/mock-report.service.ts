import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { Site, Report, Page, AccessibilityIssue } from '../types/report.interface';
import { v4 as uuidv4 } from 'uuid';

@Injectable({
  providedIn: 'root'
})
export class MockReportService {
  private mockSites: Site[] = [];
  
  constructor() {
    this.generateMockData();
  }

  private generateMockData(): void {
    // Create mock sites
    const site1Id = uuidv4();
    const site2Id = uuidv4();
    
    // Create mock reports
    const report1Id = uuidv4();
    const report2Id = uuidv4();
    
    // Create mock pages
    const pages1: Page[] = [
      this.createMockPage(report1Id, 'Homepage', 'https://example.com', 95, 3, 2, 1),
      this.createMockPage(report1Id, 'About Us', 'https://example.com/about', 88, 5, 3, 1),
      this.createMockPage(report1Id, 'Contact', 'https://example.com/contact', 92, 2, 1, 0)
    ];
    
    const pages2: Page[] = [
      this.createMockPage(report2Id, 'Home', 'https://bakery.com', 85, 6, 2, 1),
      this.createMockPage(report2Id, 'Products', 'https://bakery.com/products', 78, 8, 3, 2)
    ];
    
    // Create reports with pages
    const report1: Report = {
      id: report1Id,
      siteId: site1Id,
      name: 'June 2024 Scan',
      createdDate: new Date('2024-06-01'),
      status: 'completed',
      totalPages: pages1.length,
      totalIssues: pages1.reduce((sum, page) => sum + page.totalIssues, 0),
      fixedIssues: pages1.reduce((sum, page) => sum + page.fixedIssues, 0),
      ignoredIssues: pages1.reduce((sum, page) => sum + page.ignoredIssues, 0),
      averageScore: Math.round(pages1.reduce((sum, page) => sum + (page.score || 0), 0) / pages1.length),
      pages: pages1
    };
    
    const report2: Report = {
      id: report2Id,
      siteId: site2Id,
      name: 'May 2024 Scan',
      createdDate: new Date('2024-05-15'),
      status: 'completed',
      totalPages: pages2.length,
      totalIssues: pages2.reduce((sum, page) => sum + page.totalIssues, 0),
      fixedIssues: pages2.reduce((sum, page) => sum + page.fixedIssues, 0),
      ignoredIssues: pages2.reduce((sum, page) => sum + page.ignoredIssues, 0),
      averageScore: Math.round(pages2.reduce((sum, page) => sum + (page.score || 0), 0) / pages2.length),
      pages: pages2
    };
    
    // Create sites with reports
    this.mockSites = [
      {
        id: site1Id,
        name: 'Example Coffee',
        url: 'https://example.com',
        description: 'Corporate website for Example Coffee',
        userId: 'user-1',
        createdAt: new Date('2024-01-05'),
        updatedAt: new Date(),
        totalReports: 1,
        lastScanDate: report1.createdDate,
        reports: [report1]
      },
      {
        id: site2Id,
        name: 'Sample Bakery',
        url: 'https://bakery.com',
        description: 'E-commerce site for Sample Bakery',
        userId: 'user-1',
        createdAt: new Date('2024-02-10'),
        updatedAt: new Date(),
        totalReports: 1,
        lastScanDate: report2.createdDate,
        reports: [report2]
      }
    ];
  }

  private createMockPage(reportId: string, title: string, url: string, score: number, totalIssues: number, fixedIssues: number, ignoredIssues: number): Page {
    const issues: AccessibilityIssue[] = [];
    
    // Generate mock issues
    for (let i = 0; i < totalIssues; i++) {
      const issueStatus = i < fixedIssues ? 'fixed' : (i < fixedIssues + ignoredIssues ? 'ignored' : 'open');
      const issueType = i % 3 === 0 ? 'error' : (i % 3 === 1 ? 'warning' : 'notice');
      const issueImpact = i % 4 === 0 ? 'critical' : (i % 4 === 1 ? 'serious' : (i % 4 === 2 ? 'moderate' : 'minor'));
      
      issues.push({
        id: uuidv4(),
        type: issueType as any,
        rule: this.getRandomRule(),
        description: this.getRandomDescription(issueType as any),
        element: this.getRandomElement(),
        xpath: `/html/body/div[1]/${this.getRandomElement().replace('<', '').replace('>', '')}`,
        status: issueStatus as any,
        severity: i % 3 === 0 ? 'high' : (i % 3 === 1 ? 'medium' : 'low'),
        category: this.getRandomCategory(),
        impact: issueImpact as any,
        help: this.getRandomHelp(),
        helpUrl: `https://dequeuniversity.com/rules/axe/4.4/${this.getRandomRule()}`,
        firstDetected: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000),
        lastSeen: new Date()
      });
    }
    
    return {
      id: uuidv4(),
      reportId: reportId,
      title: title,
      url: url,
      scanDate: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000),
      status: 'completed',
      totalIssues: totalIssues,
      fixedIssues: fixedIssues,
      ignoredIssues: ignoredIssues,
      score: score,
      issues: issues
    };
  }

  private getRandomRule(): string {
    const rules = ['color-contrast', 'img-alt', 'form-label', 'aria-hidden-focus', 'heading-order', 'link-name'];
    return rules[Math.floor(Math.random() * rules.length)];
  }

  private getRandomDescription(type: 'error' | 'warning' | 'notice'): string {
    const descriptions = {
      error: [
        'Elements must have sufficient color contrast',
        'Images must have alternate text',
        'Form elements must have labels',
        'Focusable elements should not have aria-hidden=true'
      ],
      warning: [
        'Heading levels should increase by one',
        'Links should have discernible text',
        'Page should have a title',
        'Lists should be structured correctly'
      ],
      notice: [
        'Consider using landmarks to improve navigation',
        'Consider adding skip links for keyboard users',
        'Consider adding ARIA labels to improve screen reader experience',
        'Consider improving keyboard focus indicators'
      ]
    };
    
    const typeDescriptions = descriptions[type];
    return typeDescriptions[Math.floor(Math.random() * typeDescriptions.length)];
  }

  private getRandomElement(): string {
    const elements = ['<button>', '<img>', '<a>', '<div>', '<input>', '<h3>'];
    return elements[Math.floor(Math.random() * elements.length)];
  }

  private getRandomCategory(): string {
    const categories = ['Color', 'Images', 'Forms', 'Structure', 'Navigation', 'ARIA'];
    return categories[Math.floor(Math.random() * categories.length)];
  }

  private getRandomHelp(): string {
    const helps = [
      'Ensure elements have a contrast ratio of at least 4.5:1 for normal text and 3:1 for large text',
      'Add alt text that conveys the purpose or content of the image',
      'Associate labels with form controls using the for attribute or nesting',
      'Remove aria-hidden from focusable elements or make them non-focusable',
      'Ensure heading levels increase by only one level at a time',
      'Provide text content for links that describes their purpose'
    ];
    return helps[Math.floor(Math.random() * helps.length)];
  }

  getSites(): Observable<Site[]> {
    return of(this.mockSites).pipe(delay(500));
  }

  getSiteById(id: string): Observable<Site | undefined> {
    const site = this.mockSites.find(s => s.id === id);
    return of(site).pipe(delay(300));
  }

  getReportById(reportId: string): Observable<Report | undefined> {
    for (const site of this.mockSites) {
      const report = site.reports.find(r => r.id === reportId);
      if (report) {
        return of(report).pipe(delay(300));
      }
    }
    return of(undefined).pipe(delay(300));
  }

  getPageById(pageId: string): Observable<Page | undefined> {
    for (const site of this.mockSites) {
      for (const report of site.reports) {
        const page = report.pages.find(p => p.id === pageId);
        if (page) {
          return of(page).pipe(delay(300));
        }
      }
    }
    return of(undefined).pipe(delay(300));
  }

  updateIssueStatus(pageId: string, issueId: string, status: 'open' | 'fixed' | 'ignored'): void {
    for (const site of this.mockSites) {
      for (const report of site.reports) {
        const pageIndex = report.pages.findIndex(p => p.id === pageId);
        if (pageIndex !== -1) {
          const page = { ...report.pages[pageIndex] };
          const issueIndex = page.issues.findIndex(i => i.id === issueId);
          
          if (issueIndex !== -1) {
            // Update issue status
            const oldStatus = page.issues[issueIndex].status;
            page.issues[issueIndex] = { ...page.issues[issueIndex], status };
            
            // Update counters
            if (oldStatus === 'fixed') page.fixedIssues--;
            if (oldStatus === 'ignored') page.ignoredIssues--;
            if (status === 'fixed') page.fixedIssues++;
            if (status === 'ignored') page.ignoredIssues++;
            
            report.pages[pageIndex] = page;
            
            // Update report counters
            report.fixedIssues = report.pages.reduce((sum, p) => sum + p.fixedIssues, 0);
            report.ignoredIssues = report.pages.reduce((sum, p) => sum + p.ignoredIssues, 0);
            
            break;
          }
        }
      }
    }
  }

  requestNewScan(url: string): Observable<boolean> {
    if (!url || !url.startsWith('http')) {
      return throwError(() => new Error('Invalid URL'));
    }
    
    // Simulate successful scan request
    return of(true).pipe(delay(1500));
  }
}