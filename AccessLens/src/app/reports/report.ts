export interface Finding {
  url: string;
  issue: string;
  rule: string;
  severity: string;
}

export interface Report {
  reportId: number;
  siteName: string;
  scanDate: string;
  pageCount: number;
  rulesPassed: number;
  rulesFailed: number;
  totalRulesTested: number;
  status: string;
  findings: Finding[];
}
