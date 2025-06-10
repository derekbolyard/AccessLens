import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Report } from './report';

@Injectable({ providedIn: 'root' })
export class ReportService {
  constructor(private http: HttpClient) {}

  getReports(): Observable<Report[]> {
    return this.http.get<Report[]>('/reports.json');
  }

  getReport(id: number): Observable<Report | undefined> {
    return this.getReports().pipe(map(rs => rs.find(r => r.reportId === id)));
  }
}
