namespace AccessLensApi.Features.Reports.Models
{
    public class AccessibilityReport
    {
        public bool WhiteLabel { get; set; }
        public string ClientName { get; set; }
        public string ClientLogoUrl { get; set; }
        public string SiteUrl { get; set; }
        public string ScanDate { get; set; }
        public string Score { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string FooterText { get; set; }
        public string ContactEmail { get; set; }
        public string ClientWebsite { get; set; }
        public string TopIssues { get; set; }
        public string LegalRisk { get; set; }
        public string CommonViolations { get; set; }
        public string ConsultationLink { get; set; }
        public List<ReportImage> Screenshots { get; set; }
        public List<PageResult> Pages { get; set; }
    }

    public class PageResult
    {
        public string Url { get; set; }
        public string PageScore { get; set; }
        public string PageChartUrl { get; set; }
        public int CriticalCount { get; set; }
        public int SeriousCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }
        public List<Issue> Issues { get; set; }
    }

    public class Issue
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Fix { get; set; }
        public string RuleId { get; set; }
        public string Target { get; set; }
        public string Severity { get; set; }
    }

    public class ReportImage
    {
        public string Src { get; set; }
        public string Alt { get; set; }
    }
}