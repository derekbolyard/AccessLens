export interface Site {
  id: string;
  name: string;
  url: string;
  createdDate: Date;
  lastScanDate: Date;
  totalReports: number;
  reports: Report[];
}

export interface Report {
  id: string;
  siteId: string;
  name: string; // Calculated by backend
  createdDate: Date;
  status: 'completed' | 'in-progress' | 'failed';
  totalPages: number;
  totalIssues: number;
  fixedIssues: number;
  ignoredIssues: number;
  averageScore: number;
  pages: Page[];
}

export interface Page {
  id: string;
  reportId: string;
  title: string;
  url: string;
  scanDate: Date;
  status: 'completed' | 'in-progress' | 'failed';
  totalIssues: number;
  fixedIssues: number;
  ignoredIssues: number;
  score: number;
  issues: AccessibilityIssue[];
}

export interface AccessibilityIssue {
  id: string;
  type: 'error' | 'warning' | 'notice';
  rule: string;
  description: string;
  element: string;
  xpath: string;
  status: 'not-fixed' | 'fixed' | 'ignored';
  impact: 'critical' | 'serious' | 'moderate' | 'minor';
  help: string;
  helpUrl: string;
}

export type IssueStatus = 'not-fixed' | 'fixed' | 'ignored';
export type IssueType = 'error' | 'warning' | 'notice';
export type IssueImpact = 'critical' | 'serious' | 'moderate' | 'minor';
export type PageStatus = 'completed' | 'in-progress' | 'failed';
export type ReportStatus = 'completed' | 'in-progress' | 'failed';