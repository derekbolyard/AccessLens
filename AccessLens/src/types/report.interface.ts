export interface Site {
  id: string;
  name: string;
  url: string;
  description?: string;
  userId: string;
  createdAt: Date;
  updatedAt: Date;
  totalReports?: number; // calculated
  lastScanDate?: Date; // calculated
  reports: Report[];
}

export interface Report {
  id: string;
  siteId?: string;
  name: string; // calculated by backend
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
  title?: string;
  url: string;
  scanDate: Date;
  status: 'completed' | 'in-progress' | 'failed';
  totalIssues: number;
  fixedIssues: number;
  ignoredIssues: number;
  score?: number; // calculated
  issues: AccessibilityIssue[];
}

export interface AccessibilityIssue {
  id: string;
  type: 'error' | 'warning' | 'notice';
  rule: string;
  description: string;
  element: string;
  xpath: string;
  status: 'open' | 'fixed' | 'ignored';
  severity: 'low' | 'medium' | 'high' | 'critical';
  category: string;
  impact: 'critical' | 'serious' | 'moderate' | 'minor';
  help: string;
  helpUrl: string;
  userNotes?: string;
  statusUpdatedAt?: Date;
  statusUpdatedBy?: string;
  firstDetected: Date;
  lastSeen: Date;
}

export type IssueStatus = 'open' | 'fixed' | 'ignored';
export type IssueType = 'error' | 'warning' | 'notice';
export type IssueImpact = 'critical' | 'serious' | 'moderate' | 'minor';
export type PageStatus = 'completed' | 'in-progress' | 'failed';
export type ReportStatus = 'completed' | 'in-progress' | 'failed';