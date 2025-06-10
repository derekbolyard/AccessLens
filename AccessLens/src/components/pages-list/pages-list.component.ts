import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Site, Page } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-pages-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pages-list.component.html',
  styleUrls: ['./pages-list.component.scss']
})
export class PagesListComponent implements OnInit {
  siteId: string = '';
  reportId: string = '';
  
  site: Site | null = null;

  constructor(
    private reportService: ReportService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.siteId = this.route.snapshot.paramMap.get('siteId') || '';
    this.reportId = this.route.snapshot.paramMap.get('reportId') || '';
    
    if (this.siteId) {
      this.reportService.getSiteById(this.siteId).subscribe(site => {
        this.site = site || null;
      });
    }
  }

  allPagesForSite(): Page[] {
    if (!this.site || !this.site.reports) return [];
    return this.site.reports.flatMap(report => report.pages || []);
  }

  trackByPageId(index: number, page: Page): string {
    return page.id;
  }

  getScoreClass(score: number): string {
    if (score >= 90) return 'excellent';
    if (score >= 80) return 'good';
    if (score >= 70) return 'fair';
    return 'poor';
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
    
    const totalScore = completedPages.reduce((sum, page) => sum + page.score, 0);
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