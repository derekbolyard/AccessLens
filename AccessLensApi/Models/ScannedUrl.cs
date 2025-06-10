using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AccessLensApi.Models
{
    public class ScannedUrl
    {
        [Key]
        public Guid UrlId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReportId { get; set; }

        [ForeignKey("ReportId")]
        public Report Report { get; set; }

        [Required]
        public string Url { get; set; }

        public string ScanStatus { get; set; } = "Success"; // Success, Failed, Timeout
        public int? ResponseTime { get; set; }
        public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;

        // New fields for frontend requirements
        public string? Title { get; set; }

        [NotMapped]
        public decimal? Score 
        { 
            get
            {
                if (Findings == null || !Findings.Any())
                    return 100; // Perfect score if no issues found

                // Only count issues that are still "Open" (not fixed or ignored)
                var openFindings = Findings.Where(f => f.Status == "Open");
                
                if (!openFindings.Any())
                    return 100; // Perfect score if all issues are fixed/ignored

                var criticalIssues = openFindings.Count(f => f.Severity == "Critical");
                var highIssues = openFindings.Count(f => f.Severity == "High");
                var mediumIssues = openFindings.Count(f => f.Severity == "Medium");
                var lowIssues = openFindings.Count(f => f.Severity == "Low");

                // Weighted penalty system
                var penalty = (criticalIssues * 15) + (highIssues * 8) + (mediumIssues * 3) + (lowIssues * 1);
                var score = Math.Max(0, 100 - penalty);
                
                return (decimal)score;
            }
        }

        public ICollection<Finding> Findings { get; set; }
    }
}
