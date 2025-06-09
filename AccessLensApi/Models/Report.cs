using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Models
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
        public string Status { get; set; } = "Completed"; // Completed, In Progress, Failed

        public ICollection<ScannedUrl> ScannedUrls { get; set; }
        public ICollection<Finding> Findings { get; set; }
    }
}
