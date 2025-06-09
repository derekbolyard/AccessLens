import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReportService } from './report.service';
import { Report } from './report';

@Component({
  selector: 'app-report-list',
  standalone: true,
  templateUrl: './report-list.component.html',
  styleUrls: ['./report-list.component.scss'],
  imports: [CommonModule, FormsModule, RouterLink]
})
export class ReportListComponent implements OnInit {
  reports: Report[] = [];
  filter = '';

  constructor(private service: ReportService) {}

  ngOnInit(): void {
    this.service.getReports().subscribe(r => (this.reports = r));
  }

  get filteredReports(): Report[] {
    return this.reports.filter(r =>
      r.siteName.toLowerCase().includes(this.filter.toLowerCase())
    );
  }
}
