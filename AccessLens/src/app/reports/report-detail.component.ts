import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { ReportService } from './report.service';
import { Report } from './report';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  templateUrl: './report-detail.component.html',
  styleUrls: ['./report-detail.component.scss'],
  imports: [CommonModule, RouterLink, MatCardModule, MatTableModule, MatButtonModule]
})
export class ReportDetailComponent implements OnInit {
  report?: Report;
  displayedColumns = ['url', 'issue', 'rule', 'severity'];

  constructor(private route: ActivatedRoute, private service: ReportService) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getReport(id).subscribe(r => (this.report = r));
  }
}
