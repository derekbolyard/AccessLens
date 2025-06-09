import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReportService } from './report.service';
import { Report } from './report';

@Component({
  selector: 'app-report-detail',
  standalone: true,
  templateUrl: './report-detail.component.html',
  styleUrls: ['./report-detail.component.scss'],
  imports: [CommonModule, RouterLink]
})
export class ReportDetailComponent implements OnInit {
  report?: Report;

  constructor(private route: ActivatedRoute, private service: ReportService) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getReport(id).subscribe(r => (this.report = r));
  }
}
