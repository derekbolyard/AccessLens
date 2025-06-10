import { Component } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { SitesListComponent } from './components/sites-list/sites-list.component';
import { ReportsListComponent } from './components/reports-list/reports-list.component';
import { PagesListComponent } from './components/pages-list/pages-list.component';
import { PageDetailComponent } from './components/page-detail/page-detail.component';
import { UpgradePageComponent } from './components/upgrade/upgrade-page.component';
import { ToastContainerComponent } from './components/common/toast/toast-container.component';
import { ReportService } from './services/report.service';
import { AuthService } from './services/auth.service';
import { SupportService } from './services/support.service';
import { SubscriptionService } from './services/subscription.service';
import { ErrorHandlerService } from './services/error-handler.service';
import { AnalyticsService } from './services/analytics.service';
import { CacheService } from './services/cache.service';
import { ToastService } from './components/common/toast/toast.service';

type ViewState = 'dashboard' | 'sites' | 'reports' | 'pages' | 'page-detail' | 'upgrade';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HeaderComponent,
    FooterComponent,
    DashboardComponent,
    SitesListComponent, 
    ReportsListComponent,
    PagesListComponent, 
    PageDetailComponent,
    UpgradePageComponent,
    ToastContainerComponent
  ],
  template: `
    <div class="app">
      <app-header 
        (dashboardClick)="onDashboardClick()"
        (sitesClick)="onSitesClick()"
        (upgradeClick)="onUpgradeClick()"
      ></app-header>
      
      <main class="main-content">
        <app-dashboard 
          *ngIf="currentView === 'dashboard'"
          (siteSelected)="onSiteSelected($event)"
          (reportSelected)="onReportSelected($event)"
        ></app-dashboard>
        
        <app-sites-list 
          *ngIf="currentView === 'sites'"
          (siteSelected)="onSiteSelected($event)"
        ></app-sites-list>
        
        <app-reports-list 
          *ngIf="currentView === 'reports'"
          [siteId]="selectedSiteId"
          (backToSites)="onBackToSites()"
          (reportSelected)="onReportSelected($event)"
        ></app-reports-list>
        
        <app-pages-list 
          *ngIf="currentView === 'pages'"
          [siteId]="selectedSiteId"
          (backToSites)="onBackToSites()"
          (backToReports)="onBackToReports()"
          (pageSelected)="onPageSelected($event)"
        ></app-pages-list>
        
        <app-page-detail 
          *ngIf="currentView === 'page-detail'"
          [pageId]="selectedPageId"
          [siteName]="selectedSiteName"
          (backToSites)="onBackToSites()"
          (backToReports)="onBackToReports()"
          (backToPages)="onBackToPages()"
        ></app-page-detail>

        <app-upgrade-page
          *ngIf="currentView === 'upgrade'"
          (backToDashboard)="onDashboardClick()"
        ></app-upgrade-page>
      </main>

      <app-footer></app-footer>
      <app-toast-container></app-toast-container>
    </div>
  `,
  styles: [`
    .app {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
    }
  `]
})
export class App {
  currentView: ViewState = 'dashboard';
  selectedSiteId: string = '';
  selectedReportId: string = '';
  selectedPageId: string = '';
  selectedSiteName: string = '';
  selectedReportName: string = '';

  constructor(
    private reportService: ReportService,
    private authService: AuthService,
    private supportService: SupportService,
    private subscriptionService: SubscriptionService,
    private errorHandler: ErrorHandlerService,
    private analytics: AnalyticsService,
    private cacheService: CacheService,
    private toastService: ToastService
  ) {
    // Set up global error handler
    window.addEventListener('error', (event) => {
      this.errorHandler.handleError(event.error, 'Global Error Handler');
    });

    window.addEventListener('unhandledrejection', (event) => {
      this.errorHandler.handleError(event.reason, 'Unhandled Promise Rejection');
    });
  }

  onDashboardClick(): void {
    this.currentView = 'dashboard';
    this.selectedSiteId = '';
    this.selectedReportId = '';
    this.selectedPageId = '';
    this.selectedSiteName = '';
    this.selectedReportName = '';
    this.analytics.trackPageView('dashboard');
  }

  onSitesClick(): void {
    this.currentView = 'sites';
    this.selectedSiteId = '';
    this.selectedReportId = '';
    this.selectedPageId = '';
    this.selectedSiteName = '';
    this.selectedReportName = '';
    this.analytics.trackPageView('sites');
  }

  onUpgradeClick(): void {
    this.currentView = 'upgrade';
    this.analytics.trackPageView('upgrade');
  }

  onSiteSelected(siteId: string): void {
    this.selectedSiteId = siteId;
    this.currentView = 'reports';
    
    // Get site name for breadcrumb
    this.reportService.getSiteById(siteId).subscribe(site => {
      this.selectedSiteName = site?.name || '';
    });
    
    this.analytics.trackPageView('reports');
  }

  onReportSelected(reportId: string): void {
    this.selectedReportId = reportId;
    this.currentView = 'pages';
    
    // Get report name for breadcrumb
    this.reportService.getReportById(reportId).subscribe(report => {
      this.selectedReportName = report?.name || '';
    });
    
    this.analytics.trackPageView('pages');
  }

  onPageSelected(pageId: string): void {
    this.selectedPageId = pageId;
    this.currentView = 'page-detail';
    this.analytics.trackPageView('page-detail');
  }

  onBackToSites(): void {
    this.currentView = 'sites';
    this.selectedSiteId = '';
    this.selectedReportId = '';
    this.selectedPageId = '';
    this.selectedSiteName = '';
    this.selectedReportName = '';
  }

  onBackToReports(): void {
    this.currentView = 'reports';
    this.selectedReportId = '';
    this.selectedPageId = '';
    this.selectedReportName = '';
  }

  onBackToPages(): void {
    this.currentView = 'pages';
    this.selectedPageId = '';
  }
}

bootstrapApplication(App, {
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    // Add other providers here if needed
  ]
}).catch(err => console.error(err));