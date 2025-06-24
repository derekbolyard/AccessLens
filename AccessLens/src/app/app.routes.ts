import { Routes } from '@angular/router';
import { AuthGuard } from '../guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { 
    path: 'dashboard', 
    loadComponent: () => import('../components/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'sites', 
    loadComponent: () => import('../components/sites-list/sites-list.component').then(m => m.SitesListComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'sites/:siteId/reports', 
    loadComponent: () => import('../components/reports-list/reports-list.component').then(m => m.ReportsListComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'sites/:siteId/reports/:reportId/pages', 
    loadComponent: () => import('../components/pages-list/pages-list.component').then(m => m.PagesListComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'sites/:siteId/reports/:reportId/pages/:pageId', 
    loadComponent: () => import('../components/page-detail/page-detail.component').then(m => m.PageDetailComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'upgrade', 
    loadComponent: () => import('../components/upgrade/upgrade-page.component').then(m => m.UpgradePageComponent),
    canActivate: [AuthGuard] 
  },
  { 
    path: 'branding', 
    loadComponent: () => import('../components/branding/branding-settings.component').then(m => m.BrandingSettingsComponent),
    canActivate: [AuthGuard] 
  },
  { path: '**', redirectTo: 'dashboard' }
];