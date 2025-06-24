import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Site, Report } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { CardComponent } from '../common/card/card.component';
import { BadgeComponent, BadgeVariant } from '../common/badge/badge.component';
import { LoadingComponent } from '../common/loading/loading.component';
import { Router } from '@angular/router';

interface ChartDataPoint {
  label: string;
  value: number;
  score: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CardComponent, BadgeComponent, LoadingComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  sites: Site[] = [];
  isLoading = true;

  constructor(
    private reportService: ReportService,
    public router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.isLoading = true;
    this.reportService.getSites().subscribe({
      next: (sites) => {
        this.sites = sites;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load dashboard data:', error);
        this.isLoading = false;
      }
    });
  }

  trackBySiteId(index: number, site: Site): string {
    return site.id;
  }

  trackByReportId(index: number, report: Report): string {
    return report.id;
  }

  // KPI Calculations
  getTotalFixedIssues(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => reportTotal + report.fixedIssues, 0), 0);
  }

  getTotalPendingIssues(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => 
        reportTotal + (report.totalIssues - report.fixedIssues - report.ignoredIssues), 0), 0);
  }

  getTotalIgnoredIssues(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => reportTotal + report.ignoredIssues, 0), 0);
  }

  getTotalPages(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => reportTotal + report.totalPages, 0), 0);
  }

  getAverageScore(): number {
    const allReports = this.sites.flatMap(site => site.reports.filter(r => r.status === 'completed'));
    if (allReports.length === 0) return 0;
    return Math.round(
      allReports.reduce((total, report) => total + report.averageScore, 0) / allReports.length
    );
  }

  // Mock change calculations (in a real app, these would compare with previous period)
  getFixedIssuesChange(): number {
    return Math.floor(this.getTotalFixedIssues() * 0.15); // Mock 15% increase
  }

  getPendingIssuesChange(): number {
    return Math.floor(this.getTotalPendingIssues() * -0.08); // Mock 8% decrease
  }

  getScoreChange(): number {
    return 3; // Mock 3% improvement
  }

  // Chart Data
  getScoreTrendData(): ChartDataPoint[] {
    // Mock trend data - in real app, this would come from historical data
    const currentScore = this.getAverageScore();
    return [
      { label: 'Jan', value: Math.max(0, currentScore - 15), score: Math.max(0, currentScore - 15) },
      { label: 'Feb', value: Math.max(0, currentScore - 12), score: Math.max(0, currentScore - 12) },
      { label: 'Mar', value: Math.max(0, currentScore - 8), score: Math.max(0, currentScore - 8) },
      { label: 'Apr', value: Math.max(0, currentScore - 5), score: Math.max(0, currentScore - 5) },
      { label: 'May', value: Math.max(0, currentScore - 2), score: Math.max(0, currentScore - 2) },
      { label: 'Jun', value: currentScore, score: currentScore }
    ];
  }

  getScorePolylinePoints(): string {
    const data = this.getScoreTrendData();
    return data.map((point, index) => {
      const x = (index / (data.length - 1)) * 100;
      const y = 100 - point.value; // Invert Y axis for SVG
      return `${x},${y}`;
    }).join(' ');
  }

  // Pie chart percentages
  getFixedPercentage(): number {
    const total = this.getTotalFixedIssues() + this.getTotalPendingIssues() + this.getTotalIgnoredIssues();
    return total > 0 ? Math.round((this.getTotalFixedIssues() / total) * 100) : 0;
  }

  getPendingPercentage(): number {
    const total = this.getTotalFixedIssues() + this.getTotalPendingIssues() + this.getTotalIgnoredIssues();
    return total > 0 ? Math.round((this.getTotalPendingIssues() / total) * 100) : 0;
  }

  getIgnoredPercentage(): number {
    const total = this.getTotalFixedIssues() + this.getTotalPendingIssues() + this.getTotalIgnoredIssues();
    return total > 0 ? Math.round((this.getTotalIgnoredIssues() / total) * 100) : 0;
  }

  // Recent Reports
  getRecentReports(): Report[] {
    const allReports = this.sites.flatMap(site => site.reports);
    return allReports
      .sort((a, b) => b.createdDate.getTime() - a.createdDate.getTime())
      .slice(0, 5);
  }

  getSiteName(siteId: string): string {
    const site = this.sites.find(s => s.id === siteId);
    return site?.name || 'Unknown Site';
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'completed': return 'success';
      case 'in-progress': return 'warning';
      case 'failed': return 'error';
      default: return 'secondary';
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

  // Site Performance
  getSiteAverageScore(site: Site): number {
    const completedReports = site.reports.filter(r => r.status === 'completed');
    if (completedReports.length === 0) return 0;
    return Math.round(
      completedReports.reduce((total, report) => total + report.averageScore, 0) / completedReports.length
    );
  }

  getSiteTotalPages(site: Site): number {
    return site.reports.reduce((total, report) => total + report.totalPages, 0);
  }

  getSitePendingIssues(site: Site): number {
    return site.reports.reduce((total, report) => 
      total + (report.totalIssues - report.fixedIssues - report.ignoredIssues), 0);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  onSiteClick(site: Site): void {
    console.log('Dashboard: Navigating to site reports:', site.id);
    this.router.navigate(['/sites', site.id, 'reports']).then(success => {
      if (!success) {
        console.error('Dashboard: Navigation to site reports failed');
      }
    });
  }

  onReportClick(report: Report): void {
    console.log('Dashboard: Navigating to report:', report.siteId, report.id);
    this.router.navigate(['/sites', report.siteId, 'reports']).then(success => {
      if (!success) {
        console.error('Dashboard: Navigation to report failed');
      }
    });
  }
}