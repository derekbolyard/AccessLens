import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Site, Page } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { BreadcrumbComponent, BreadcrumbItem } from '../common/breadcrumb/breadcrumb.component';
import { PageContextComponent } from '../common/page-context/page-context.component';
import { BadgeComponent, BadgeVariant } from '../common/badge/badge.component';
import { CardComponent } from '../common/card/card.component';
import { LoadingComponent } from '../common/loading/loading.component';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-pages-list',
  standalone: true,
  imports: [CommonModule, BreadcrumbComponent, PageContextComponent, BadgeComponent, CardComponent, LoadingComponent],
  templateUrl: './pages-list.component.html',
  styleUrls: ['./pages-list.component.scss']
})
export class PagesListComponent implements OnInit {
  siteId: string = '';
  reportId: string = '';
  site: Site | null = null;
  breadcrumbItems: BreadcrumbItem[] = [];
  isLoading = true;

  constructor(
    private reportService: ReportService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.siteId = this.route.snapshot.paramMap.get('siteId') || '';
    this.reportId = this.route.snapshot.paramMap.get('reportId') || '';
    
    this.updateBreadcrumbItems();
    
    if (this.siteId) {
      this.loadSite();
    }
  }

  private loadSite(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.reportService.getSiteById(this.siteId).subscribe({
      next: (site) => {
        this.site = site || null;
        this.updateBreadcrumbItems();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Failed to load site:', error);
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private updateBreadcrumbItems(): void {
    this.breadcrumbItems = [
      { label: 'All Sites', icon: 'sites', action: () => this.onBackToSites() },
      { label: this.site?.name || 'Site', icon: 'reports', action: () => this.onBackToReports() },
      { label: 'Pages', icon: 'pages' }
    ];
    this.cdr.markForCheck();
  }

  allPagesForSite(): Page[] {
    if (!this.site || !this.site.reports) return [];
    return this.site.reports.flatMap(report => report.pages || []);
  }

  trackByPageId(index: number, page: Page): string {
    return page.id;
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'completed': return 'success';
      case 'in-progress': return 'warning';
      case 'failed': return 'error';
      default: return 'secondary';
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'completed': return 'check';
      case 'in-progress': return '';
      case 'failed': return 'x';
      default: return '';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'completed': return 'Completed';
      case 'in-progress': return 'In Progress';
      case 'failed': return 'Failed';
      default: return status;
    }
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  getSiteTotalPages(): number {
    return this.allPagesForSite().length;
  }

  getSiteTotalPendingIssues(): number {
    const pages = this.allPagesForSite();
    return pages.reduce((total, page) => {
      return total + (page.totalIssues - page.fixedIssues - page.ignoredIssues);
    }, 0);
  }

  getSiteTotalFixedIssues(): number {
    const pages = this.allPagesForSite();
    return pages.reduce((total, page) => {
      return total + page.fixedIssues;
    }, 0);
  }

  getSiteAverageScore(): number {
    const pages = this.allPagesForSite();
    if (pages.length === 0) return 0;
    const completedPages = pages.filter(page => page.status === 'completed');
    if (completedPages.length === 0) return 0;
    
    const totalScore = completedPages.reduce((sum, page) => sum + (page.score || 0), 0);
    return Math.round(totalScore / completedPages.length);
  }

  onBackToSites(): void {
    this.router.navigate(['/sites']);
  }

  onBackToReports(): void {
    this.router.navigate(['/sites', this.siteId, 'reports']);
  }

  onPageClick(page: Page): void {
    if (page.status === 'completed') {
      this.router.navigate(['/sites', this.siteId, 'reports', this.reportId, 'pages', page.id]);
    }
  }

  onViewPage(page: Page, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (page.status === 'completed') {
      this.router.navigate(['/sites', this.siteId, 'reports', this.reportId, 'pages', page.id]);
    }
  }
}