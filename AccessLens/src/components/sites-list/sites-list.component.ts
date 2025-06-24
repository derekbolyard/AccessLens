import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Site } from '../../types/report.interface';
import { ReportService } from '../../services/report.service';
import { PageContextComponent } from '../common/page-context/page-context.component';
import { CardComponent } from '../common/card/card.component';
import { ButtonComponent } from '../common/button/button.component';
import { LoadingComponent } from '../common/loading/loading.component';
import { InputComponent } from '../common/input/input.component';
import { AddSiteModalComponent } from './add-site-modal.component';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sites-list',
  standalone: true,
  imports: [CommonModule, FormsModule, PageContextComponent, CardComponent, ButtonComponent, LoadingComponent, InputComponent, AddSiteModalComponent],
  templateUrl: './sites-list.component.html',
  styleUrls: ['./sites-list.component.scss']
})
export class SitesListComponent implements OnInit {
  sites: Site[] = [];
  isLoading = true;
  showAddSiteModal = false;
  
  // Filter and sort properties
  searchTerm = '';
  sortKey = 'name-asc';

  constructor(
    private reportService: ReportService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSites();
  }

  private loadSites(): void {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.reportService.getSites().subscribe({
      next: (sites) => {
        this.sites = sites;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Failed to load sites:', error);
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  getFilteredAndSortedSites(): Site[] {
    let filteredSites = this.sites;

    // Apply search filter
    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filteredSites = filteredSites.filter(site => 
        site.name.toLowerCase().includes(searchLower) ||
        site.url.toLowerCase().includes(searchLower)
      );
    }

    // Apply sorting
    return this.sortSites(filteredSites);
  }

  private sortSites(sites: Site[]): Site[] {
    const [field, direction] = this.sortKey.split('-');
    
    return [...sites].sort((a, b) => {
      let aValue: any;
      let bValue: any;

      switch (field) {
        case 'name':
          aValue = a.name.toLowerCase();
          bValue = b.name.toLowerCase();
          break;
        case 'lastScan':
          aValue = a.lastScanDate ? a.lastScanDate.getTime() : 0;
          bValue = b.lastScanDate ? b.lastScanDate.getTime() : 0;
          break;
        case 'reports':
          aValue = a.totalReports || 0;
          bValue = b.totalReports || 0;
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
    // Sorting will be applied automatically through getFilteredAndSortedSites()
    this.cdr.markForCheck();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.cdr.markForCheck();
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

  onAddSite(): void {
    this.showAddSiteModal = true;
    this.cdr.markForCheck();
  }

  onCloseAddSiteModal(): void {
    this.showAddSiteModal = false;
    this.cdr.markForCheck();
  }

  onSiteAdded(site: Site): void {
    // Add the new site to the local array immediately for better UX
    this.sites = [...this.sites, site];
    this.showAddSiteModal = false;
    this.cdr.markForCheck();
    
    // Optionally refresh from server to ensure consistency
    // but don't show loading state since we already have the data
    this.reportService.refreshSites().subscribe({
      next: (sites) => {
        this.sites = sites;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Failed to refresh sites:', error);
        // Keep the local data if refresh fails
      }
    });
  }

  onSiteClick(site: Site): void {
    console.log('Navigating to site reports:', site.id);
    this.router.navigate(['/sites', site.id, 'reports']).then(success => {
      if (!success) {
        console.error('Navigation failed');
      }
    });
  }

  onViewSite(site: Site, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    console.log('Navigating to site reports (view button):', site.id);
    this.router.navigate(['/sites', site.id, 'reports']).then(success => {
      if (!success) {
        console.error('Navigation failed');
      }
    });
  }
}