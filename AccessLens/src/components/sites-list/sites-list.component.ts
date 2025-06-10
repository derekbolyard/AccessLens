import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Site } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { CardComponent } from '../common/card/card.component';
import { ButtonComponent } from '../common/button/button.component';

@Component({
  selector: 'app-sites-list',
  standalone: true,
  imports: [CommonModule, CardComponent, ButtonComponent],
  templateUrl: './sites-list.component.html',
  styleUrls: ['./sites-list.component.scss']
})
export class SitesListComponent implements OnInit {
  @Output() siteSelected = new EventEmitter<string>();
  
  sites: Site[] = [];

  constructor(private reportService: ReportService) {}

  ngOnInit(): void {
    this.reportService.getSites().subscribe(sites => {
      this.sites = sites;
    });
  }

  trackBySiteId(index: number, site: Site): string {
    return site.id;
  }

  getTotalFixed(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => reportTotal + report.fixedIssues, 0), 0);
  }

  getTotalPending(): number {
    return this.sites.reduce((total, site) => 
      total + site.reports.reduce((reportTotal, report) => 
        reportTotal + (report.totalIssues - report.fixedIssues - report.ignoredIssues), 0), 0);
  }

  getTotalPagesForSite(site: Site): number {
    return site.reports.reduce((total, report) => total + report.totalPages, 0);
  }

  getTotalPendingForSite(site: Site): number {
    return site.reports.reduce((total, report) => 
      total + (report.totalIssues - report.fixedIssues - report.ignoredIssues), 0);
  }

  getTotalFixedForSite(site: Site): number {
    return site.reports.reduce((total, report) => total + report.fixedIssues, 0);
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  }

  onSiteClick(site: Site): void {
    this.siteSelected.emit(site.id);
  }

  onViewSite(site: Site, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    this.siteSelected.emit(site.id);
  }
}