using AccessLensApi.Features.Sites.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AccessLensApi.Features.Reports.Models
{
    public class Report
    {
        [Key]
        public Guid ReportId { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public string SiteName { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        public int PageCount { get; set; }
        public int RulesPassed { get; set; }
        public int RulesFailed { get; set; }
        public int TotalRulesTested { get; set; }
        public int? Score { get; set; } // Overall accessibility score (0-100)
        public string? PdfKey { get; set; } // Storage key for generated PDF report
        public string Status { get; set; } = "Completed"; // Completed, In Progress, Failed

        public Guid? SiteId { get; set; }
        [ForeignKey("SiteId")]
        public virtual Site? Site { get; set; }

        // Calculated properties - not mapped to database
        [NotMapped]
        public string Name => $"{Site?.Name ?? SiteName} - {ScanDate:MMM dd, yyyy}";

        [NotMapped]
        public int TotalPages => ScannedUrls?.Count ?? PageCount;

        [NotMapped]
        public int TotalIssues => Findings?.Count ?? RulesFailed;

        [NotMapped]
        public int FixedIssues => Findings?.Count(f => f.Status == "Fixed") ?? 0;

        [NotMapped]
        public int IgnoredIssues => Findings?.Count(f => f.Status == "Ignored") ?? 0;

        [NotMapped]
        public decimal? AverageScore
        {
            get
            {
                if (ScannedUrls == null || !ScannedUrls.Any())
                    return null;

                var urlsWithScores = ScannedUrls.Where(u => u.Score.HasValue);
                return urlsWithScores.Any() ? urlsWithScores.Average(u => u.Score!.Value) : null;
            }
        }

        [NotMapped]
        public string? PdfUrl
        {
            get
            {
                if (string.IsNullOrEmpty(PdfKey))
                    return null;
                
                // Note: In a real implementation, you'd inject IStorageService to generate the URL
                // For now, this returns the key - the controller should generate the actual URL
                return PdfKey;
            }
        }

        public ICollection<ScannedUrl> ScannedUrls { get; set; }
        public ICollection<Finding> Findings { get; set; }
    }
}
