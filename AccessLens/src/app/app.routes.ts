import { Routes } from '@angular/router';
import { ReportListComponent } from './reports/report-list.component';
import { ReportDetailComponent } from './reports/report-detail.component';

export const routes: Routes = [
  { path: '', redirectTo: 'reports', pathMatch: 'full' },
  { path: 'reports', component: ReportListComponent },
  { path: 'reports/:id', component: ReportDetailComponent }
];
