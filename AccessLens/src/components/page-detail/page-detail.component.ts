import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Page, AccessibilityIssue, IssueStatus, IssueType } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { AlertComponent } from '../common/alert/alert.component';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-page-detail',
  standalone: true,
  imports: [CommonModule, AlertComponent],
  templateUrl: './page-detail.component.html',
  styleUrls: ['./page-detail.component.scss']
})
export class PageDetailComponent implements OnInit {
  pageId: string = '';
  siteId: string = '';
  reportId: string = '';
  siteName: string = '';
  
  page: Page | null = null;
  activeFilter: 'all' | 'pending' | 'fixed' | 'ignored' = 'all';
  updatingIssues: Set<string> = new Set();
  updateSuccess: string = '';
  updateError: string = '';

  constructor(
    private reportService: ReportService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.pageId = this.route.snapshot.paramMap.get('pageId') || '';
    this.siteId = this.route.snapshot.paramMap.get('siteId') || '';
    this.reportId = this.route.snapshot.paramMap.get('reportId') || '';
    
    if (this.pageId) {
      this.reportService.getPageById(this.pageId).subscribe(page => {
        this.page = page || null;
      });
    }
    
    if (this.siteId) {
      this.reportService.getSiteById(this.siteId).subscribe(site => {
        this.siteName = site?.name || '';
      });
    }
  }

  trackByIssueId(index: number, issue: AccessibilityIssue): string {
    return issue.id;
  }

  setFilter(filter: 'all' | 'pending' | 'fixed' | 'ignored'): void {
    this.activeFilter = filter;
  }

  getFilteredIssues(): AccessibilityIssue[] {
    if (!this.page) return [];

    switch (this.activeFilter) {
      case 'pending':
        return this.page.issues.filter(issue => issue.status === 'not-fixed');
      case 'fixed':
        return this.page.issues.filter(issue => issue.status === 'fixed');
      case 'ignored':
        return this.page.issues.filter(issue => issue.status === 'ignored');
      default:
        return this.page.issues;
    }
  }

  getPendingIssues(): AccessibilityIssue[] {
    return this.page?.issues.filter(issue => issue.status === 'not-fixed') || [];
  }

  getScoreClass(score: number): string {
    if (score >= 90) return 'excellent';
    if (score >= 80) return 'good';
    if (score >= 70) return 'fair';
    return 'poor';
  }

  getTypeLabel(type: IssueType): string {
    return type.charAt(0).toUpperCase() + type.slice(1);
  }

  getStatusLabel(status: IssueStatus): string {
    switch (status) {
      case 'not-fixed': return 'Not Fixed';
      case 'fixed': return 'Fixed';
      case 'ignored': return 'Ignored';
      default: return status;
    }
  }

  getStatusColor(status: IssueStatus): string {
    switch (status) {
      case 'fixed': return 'fixed';
      case 'ignored': return 'ignored';
      default: return 'not-fixed';
    }
  }

  getImpactColor(impact: string): string {
    switch (impact) {
      case 'critical': return 'critical';
      case 'serious': return 'serious';
      case 'moderate': return 'moderate';
      case 'minor': return 'minor';
      default: return 'moderate';
    }
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(date);
  }

  updateIssueStatus(issueId: string, status: IssueStatus): void {
    if (this.page && !this.updatingIssues.has(issueId)) {
      this.updatingIssues.add(issueId);
      this.updateError = '';
      
      try {
        this.reportService.updateIssueStatus(this.page.id, issueId, status);
        
        // Simulate API delay
        setTimeout(() => {
          this.updatingIssues.delete(issueId);
          this.updateSuccess = `Issue status updated to ${this.getStatusLabel(status)}`;
          
          // Clear success message after 3 seconds
          setTimeout(() => {
            this.updateSuccess = '';
          }, 3000);
        }, 500);
        
      } catch (error) {
        this.updatingIssues.delete(issueId);
        this.updateError = 'Failed to update issue status. Please try again.';
        console.error('Failed to update issue status:', error);
      }
    }
  }

  isUpdating(issueId: string): boolean {
    return this.updatingIssues.has(issueId);
  }

  onBackToSites(): void {
    this.router.navigate(['/sites']);
  }

  onBackToReports(): void {
    this.router.navigate(['/sites', this.siteId, 'reports']);
  }

  onBackToPages(): void {
    this.router.navigate(['/sites', this.siteId, 'reports', this.reportId, 'pages']);
  }
}