using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccessLensApi.Features.Core.Models
{
    /// <summary>
    /// Unified model for accessibility issues across all contexts (scanning, reporting, storage)
    /// </summary>
    public class AccessibilityIssue
    {
        // Core issue identification
        public string Code { get; set; } = string.Empty;           // Rule ID (e.g., "color-contrast")
        public string Title { get; set; } = string.Empty;          // Human-readable title
        public string Message { get; set; } = string.Empty;        // Issue description
        public string Severity { get; set; } = string.Empty;       // CRITICAL, SERIOUS, MODERATE, MINOR
        
        // Context and location
        public string Target { get; set; } = string.Empty;         // CSS selector or element description
        public string ContextHtml { get; set; } = string.Empty;    // HTML snippet for context
        
        // Remediation
        public string Fix { get; set; } = string.Empty;            // How to fix the issue
        public string Category { get; set; } = "Other";            // Issue category for grouping
        
        // Metadata
        public int InstanceCount { get; set; } = 1;                // Number of instances found
        public string Status { get; set; } = "Open";               // Open, Fixed, Ignored (for tracking)
        
        // JSON serialization support for API responses
        [JsonPropertyName("type")]
        public string Type => Severity.ToLowerInvariant();         // For backward compatibility
        
        [JsonPropertyName("code")]
        public string RuleId => Code;                              // Alternative property name
        
        [JsonPropertyName("message")]
        public string Description => Message;                      // Alternative property name
    }

    /// <summary>
    /// Unified model for page scan results
    /// </summary>
    public class AccessibilityPageResult
    {
        public string Url { get; set; } = string.Empty;
        public string PageUrl => Url;                              // Alternative property name
        
        // Issues found on this page
        public List<AccessibilityIssue> Issues { get; set; } = new();
        
        // Calculated severity counts
        public int CriticalCount => Issues.Count(i => i.Severity.Equals("CRITICAL", StringComparison.OrdinalIgnoreCase));
        public int SeriousCount => Issues.Count(i => i.Severity.Equals("SERIOUS", StringComparison.OrdinalIgnoreCase));
        public int ModerateCount => Issues.Count(i => i.Severity.Equals("MODERATE", StringComparison.OrdinalIgnoreCase));
        public int MinorCount => Issues.Count(i => i.Severity.Equals("MINOR", StringComparison.OrdinalIgnoreCase));
        
        // Calculated score (can be customized)
        public int Score => Math.Max(0, 100 - (CriticalCount * 5 + SeriousCount * 3 + ModerateCount));
        public string PageScore => Score.ToString();
        
        // Report generation support
        public string PageChartUrl { get; set; } = string.Empty;   // Generated chart URL
    }

    /// <summary>
    /// Unified model for top issues (most frequent/severe across all pages)
    /// </summary>
    public class AccessibilityTopIssue
    {
        public string Code { get; set; } = string.Empty;           // Rule ID
        public string Title { get; set; } = string.Empty;          // Human-readable title  
        public string Text => Title;                               // Alternative property name
        public string Severity { get; set; } = string.Empty;       // CRITICAL, SERIOUS, etc.
        public int InstanceCount { get; set; } = 0;                // Total instances across all pages
        public List<string> AffectedPages { get; set; } = new();   // URLs where this issue appears
        
        // Example details
        public string ExampleMessage { get; set; } = string.Empty; // Example description
        public string ExampleFix { get; set; } = string.Empty;     // Example fix
    }

    /// <summary>
    /// Unified model for complete accessibility scan results
    /// </summary>
    public class AccessibilityScanResult
    {
        // Scan metadata
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
        public string SiteUrl { get; set; } = string.Empty;
        public string DiscoveryMethod { get; set; } = "manual";
        
        // Results
        public List<AccessibilityPageResult> Pages { get; set; } = new();
        public List<AccessibilityTopIssue> TopIssues { get; set; } = new();
        
        // Overall statistics
        public int TotalPages => Pages.Count;
        public int TotalIssues => Pages.Sum(p => p.Issues.Count);
        public int CriticalCount => Pages.Sum(p => p.CriticalCount);
        public int SeriousCount => Pages.Sum(p => p.SeriousCount);
        public int ModerateCount => Pages.Sum(p => p.ModerateCount);
        public int MinorCount => Pages.Sum(p => p.MinorCount);
        
        // Overall score calculation
        public int OverallScore => Math.Max(0, 100 - (CriticalCount * 5 + SeriousCount * 3 + ModerateCount));
        public string Score => OverallScore.ToString();
        
        // Visual assets
        public string? TeaserImageUrl { get; set; }
        public List<AccessibilityScreenshot> Screenshots { get; set; } = new();
    }

    /// <summary>
    /// Model for report screenshots/visual highlights
    /// </summary>
    public class AccessibilityScreenshot
    {
        public string Src { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complete accessibility report model for template generation
    /// </summary>
    public class AccessibilityReport
    {
        // Branding and presentation
        public bool WhiteLabel { get; set; } = false;
        public string ClientName { get; set; } = string.Empty;
        public string ClientLogoUrl { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = "#2563eb";
        public string SecondaryColor { get; set; } = "#16a34a";
        
        // Scan data (embedded unified models)
        public AccessibilityScanResult ScanResult { get; set; } = new();
        
        // Report metadata
        public string FooterText { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ClientWebsite { get; set; } = string.Empty;
        public string ConsultationLink { get; set; } = string.Empty;
        
        // Narrative content
        public string LegalRisk { get; set; } = "Medium";
        public string CommonViolations { get; set; } = string.Empty;
        
        // Template-friendly properties (delegated to ScanResult or allow override)
        public string SiteUrl { get; set; } = string.Empty;
        public string ScanDate { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        
        // Collections that can be overridden from scan result
        public List<AccessibilityPageResult> Pages { get; set; } = new();
        public List<AccessibilityTopIssue> TopIssues { get; set; } = new();
        public List<AccessibilityScreenshot> Screenshots { get; set; } = new();
        
        // Severity counts that can be overridden
        public int CriticalCount { get; set; } = 0;
        public int SeriousCount { get; set; } = 0;
        public int ModerateCount { get; set; } = 0;
        public int MinorCount { get; set; } = 0;
        
        // Auto-populate from ScanResult if not explicitly set
        public void PopulateFromScanResult()
        {
            if (string.IsNullOrEmpty(SiteUrl))
                SiteUrl = ScanResult.SiteUrl;
            
            if (string.IsNullOrEmpty(ScanDate))
                ScanDate = ScanResult.ScannedAt.ToString("yyyy-MM-dd");
                
            if (string.IsNullOrEmpty(Score))
                Score = ScanResult.Score;
                
            if (!Pages.Any())
                Pages = ScanResult.Pages;
                
            if (!TopIssues.Any())
                TopIssues = ScanResult.TopIssues;
                
            if (!Screenshots.Any())
                Screenshots = ScanResult.Screenshots;
                
            if (CriticalCount == 0 && SeriousCount == 0 && ModerateCount == 0 && MinorCount == 0)
            {
                CriticalCount = ScanResult.CriticalCount;
                SeriousCount = ScanResult.SeriousCount;
                ModerateCount = ScanResult.ModerateCount;
                MinorCount = ScanResult.MinorCount;
            }
        }
    }
}
