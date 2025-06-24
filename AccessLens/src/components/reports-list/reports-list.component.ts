import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Site, Report } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { BreadcrumbComponent, BreadcrumbItem } from '../common/breadcrumb/breadcrumb.component';
import { PageContextComponent } from '../common/page-context/page-context.component';
import { BadgeComponent, BadgeVariant } from '../common/badge/badge.component';
import { CardComponent } from '../common/card/card.component';
import { LoadingComponent } from '../common/loading/loading.component';
import { InputComponent } from '../common/input/input.component';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, FormsModule, BreadcrumbComponent, PageContextComponent, BadgeComponent, CardComponent, LoadingComponent, InputComponent],
  templateUrl: './reports-list.component.html',
  styleUrls: ['./reports-list.component.scss']
})
export class ReportsListComponent implements OnInit {
  siteId: string = '';
  site: Site | null = null;
  breadcrumbItems: BreadcrumbItem[] = [];
  isLoading = true;
  
  // Filter and sort properties
  searchTerm = '';
  sortKey = 'date-desc';

  constructor(
    private reportService: ReportService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.siteId = this.route.snapshot.paramMap.get('siteId') || '';
    console.log('Reports list component initialized with siteId:', this.siteId);
    this.updateBreadcrumbItems();
    
    if (this.siteId) {
      this.loadSite();
    } else {
      console.error('No siteId found in route parameters');
      this.isLoading = false;
      this.cdr.markForCheck();
    }
  }

  private loadSite(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    console.log('Loading site with ID:', this.siteId);
    this.reportService.getSiteById(this.siteId).subscribe({
      next: (site) => {
        console.log('Site loaded:', site);
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
      { label: this.site?.name || 'Site', icon: 'reports' }
    ];
    this.cdr.markForCheck();
  }

  getFilteredAndSortedReports(): Report[] {
    if (!this.site) return [];
    
    let filteredReports = this.site.reports;

    // Apply search filter
    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filteredReports = filteredReports.filter(report => 
        report.name.toLowerCase().includes(searchLower)
      );
    }

    // Apply sorting
    return this.sortReports(filteredReports);
  }

  private sortReports(reports: Report[]): Report[] {
    const [field, direction] = this.sortKey.split('-');
    
    return [...reports].sort((a, b) => {
      let aValue: any;
      let bValue: any;

      switch (field) {
        case 'date':
          aValue = a.createdDate.getTime();
          bValue = b.createdDate.getTime();
          break;
        case 'score':
          aValue = a.averageScore;
          bValue = b.averageScore;
          break;
        case 'pages':
          aValue = a.totalPages;
          bValue = b.totalPages;
          break;
        default:
          return 0;
      }

      if (direction === 'asc') {
        return aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
      } else {
        return aValue > bValue ? -1 : aValue < bValue ? 1 : 0;
      }
    });
  }

  onSearchChange(searchTerm: string): void {
    this.searchTerm = searchTerm;
    this.cdr.markForCheck();
  }

  onSortChange(): void {
    // Sorting will be applied automatically through getFilteredAndSortedReports()
    this.cdr.markForCheck();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.cdr.markForCheck();
  }

  trackByReportId(index: number, report: Report): string {
    return report.id;
  }

  getTotalPending(): number {
    if (!this.site) return 0;
    return this.site.reports.reduce((total, report) => 
      total + (report.totalIssues - report.fixedIssues - report.ignoredIssues), 0);
  }

  getTotalFixed(): number {
    if (!this.site) return 0;
    return this.site.reports.reduce((total, report) => total + report.fixedIssues, 0);
  }

  getAverageScore(): number {
    if (!this.site || this.site.reports.length === 0) return 0;
    const completedReports = this.site.reports.filter(r => r.status === 'completed');
    if (completedReports.length === 0) return 0;
    return Math.round(
      completedReports.reduce((total, report) => total + report.averageScore, 0) / completedReports.length
    );
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

  onBackToSites(): void {
    console.log('Navigating back to sites');
    this.router.navigate(['/sites']).then(success => {
      if (!success) {
        console.error('Navigation to sites failed');
      }
    });
  }

  onReportClick(report: Report): void {
    if (report.status === 'completed') {
      console.log('Navigating to report pages:', this.siteId, report.id);
      this.router.navigate(['/sites', this.siteId, 'reports', report.id, 'pages']).then(success => {
        if (!success) {
          console.error('Navigation to pages failed');
        }
      });
    }
  }

  onViewReport(report: Report, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (report.status === 'completed') {
      console.log('Navigating to report pages (view button):', this.siteId, report.id);
      this.router.navigate(['/sites', this.siteId, 'reports', report.id, 'pages']).then(success => {
        if (!success) {
          console.error('Navigation to pages failed');
        }
      });
    }
  }
}