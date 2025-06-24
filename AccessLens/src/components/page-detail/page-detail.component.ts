import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Page, AccessibilityIssue, IssueStatus, IssueType } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { BreadcrumbComponent } from '../common/breadcrumb/breadcrumb.component';
import { PageContextComponent } from '../common/page-context/page-context.component';
import { AlertComponent } from '../common/alert/alert.component';
import { LoadingComponent } from '../common/loading/loading.component';
import { CardComponent } from '../common/card/card.component';
import { ToastService } from '../common/toast/toast.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-page-detail',
  standalone: true,
  imports: [CommonModule, BreadcrumbComponent, PageContextComponent, AlertComponent, LoadingComponent, CardComponent],
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
  isLoading = true;

  constructor(
    private reportService: ReportService,
    private route: ActivatedRoute,
    private router: Router,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.pageId = this.route.snapshot.paramMap.get('pageId') || '';
    this.siteId = this.route.snapshot.paramMap.get('siteId') || '';
    this.reportId = this.route.snapshot.paramMap.get('reportId') || '';
    
    this.loadPageData();
  }

  private loadPageData(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    
    if (this.pageId) {
      this.reportService.getPageById(this.pageId).subscribe({
        next: (page) => {
          this.page = page || null;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Failed to load page:', error);
          this.toastService.error('Failed to load page details');
          this.isLoading = false;
          this.cdr.markForCheck();
        }
      });
    }
    
    if (this.siteId) {
      this.reportService.getSiteById(this.siteId).subscribe({
        next: (site) => {
          this.siteName = site?.name || '';
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Failed to load site:', error);
        }
      });
    }
  }

  get breadcrumbItems() {
    return [
      { label: 'All Sites', icon: 'sites', action: () => this.onBackToSites() },
      { label: this.siteName, icon: 'reports', action: () => this.onBackToReports() },
      { label: 'Pages', icon: 'pages', action: () => this.onBackToPages() },
      { label: this.page?.title || 'Page Issues', icon: 'issues' }
    ];
  }

  trackByIssueId(index: number, issue: AccessibilityIssue): string {
    return issue.id;
  }

  setFilter(filter: 'all' | 'pending' | 'fixed' | 'ignored'): void {
    this.activeFilter = filter;
    this.cdr.markForCheck();
  }

  getFilteredIssues(): AccessibilityIssue[] {
    if (!this.page) return [];

    switch (this.activeFilter) {
      case 'pending':
        return this.page.issues.filter(issue => issue.status === 'open');
      case 'fixed':
        return this.page.issues.filter(issue => issue.status === 'fixed');
      case 'ignored':
        return this.page.issues.filter(issue => issue.status === 'ignored');
      default:
        return this.page.issues;
    }
  }

  getPendingIssues(): AccessibilityIssue[] {
    return this.page?.issues.filter(issue => issue.status === 'open') || [];
  }

  getTypeLabel(type: IssueType): string {
    return type.charAt(0).toUpperCase() + type.slice(1);
  }

  getStatusLabel(status: IssueStatus): string {
    switch (status) {
      case 'open': return 'Open';
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

  updateIssueStatus(issueId: string, status: IssueStatus): void {
    if (this.page && !this.updatingIssues.has(issueId)) {
      this.updatingIssues.add(issueId);
      this.updateError = '';
      this.cdr.markForCheck();
      
      try {
        this.reportService.updateIssueStatus(this.page.id, issueId, status);
        
        setTimeout(() => {
          this.updatingIssues.delete(issueId);
          this.toastService.success(`Issue status updated to ${this.getStatusLabel(status)}`);
          this.cdr.markForCheck();
        }, 500);
        
      } catch (error) {
        this.updatingIssues.delete(issueId);
        this.toastService.error('Failed to update issue status. Please try again.');
        console.error('Failed to update issue status:', error);
        this.cdr.markForCheck();
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