import { Routes } from '@angular/router';
import { DashboardComponent } from '../components/dashboard/dashboard.component';
import { SitesListComponent } from '../components/sites-list/sites-list.component';
import { ReportsListComponent } from '../components/reports-list/reports-list.component';
import { PagesListComponent } from '../components/pages-list/pages-list.component';
import { PageDetailComponent } from '../components/page-detail/page-detail.component';
import { UpgradePageComponent } from '../components/upgrade/upgrade-page.component';
import { BrandingPageComponent } from '../components/branding/branding-page.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'sites', component: SitesListComponent },
  { path: 'sites/:siteId/reports', component: ReportsListComponent },
  { path: 'sites/:siteId/reports/:reportId/pages', component: PagesListComponent },
  { path: 'sites/:siteId/reports/:reportId/pages/:pageId', component: PageDetailComponent },
  { path: 'upgrade', component: UpgradePageComponent },
  { path: 'branding', component: BrandingPageComponent },
  { path: '**', redirectTo: '/dashboard' } // Fallback route
];
