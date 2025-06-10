import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Site, Report } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { CardComponent } from '../common/card/card.component';
import { ButtonComponent } from '../common/button/button.component';
import { BadgeComponent, BadgeVariant } from '../common/badge/badge.component';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, CardComponent, ButtonComponent, BadgeComponent],
  templateUrl: './reports-list.component.html',
  styleUrls: ['./reports-list.component.scss']
})
export class ReportsListComponent implements OnInit {
  @Input() siteId: string = '';
  @Output() backToSites = new EventEmitter<void>();
  @Output() reportSelected = new EventEmitter<string>();
  
  site: Site | null = null;

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    if (this.siteId) {
      this.reportService.getSiteById(this.siteId).subscribe(site => {
        this.site = site || null;
      });
    }
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

  getAverageScoreClass(): string {
    const score = this.getAverageScore();
    if (score >= 90) return 'excellent';
    if (score >= 80) return 'good';
    if (score >= 70) return 'fair';
    return 'poor';
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
    this.backToSites.emit();
  }

  onReportClick(report: Report): void {
    if (report.status === 'completed') {
      this.reportSelected.emit(report.id);
    }
  }

  onViewReport(report: Report, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (report.status === 'completed') {
      this.reportSelected.emit(report.id);
    }
  }
}