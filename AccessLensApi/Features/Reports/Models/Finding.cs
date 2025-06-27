using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AccessLensApi.Features.Core.Models;

namespace AccessLensApi.Features.Reports.Models
{
    public static class FindingCategories
    {
        public const string Images = "Images";
        public const string Forms = "Forms";
        public const string Navigation = "Navigation";
        public const string Content = "Content";
        public const string Multimedia = "Multimedia";
        public const string Structure = "Structure";
        public const string Color = "Color";
        public const string Keyboard = "Keyboard";
        public const string Focus = "Focus";
        public const string Timing = "Timing";
        public const string Other = "Other";
    }

    public class Finding
    {
        [Key]
        public Guid FindingId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReportId { get; set; }

        [ForeignKey("ReportId")]
        public Report Report { get; set; }

        [Required]
        public Guid UrlId { get; set; }

        [ForeignKey("UrlId")]
        public ScannedUrl ScannedUrl { get; set; }

        [Required]
        public string Issue { get; set; }

        [Required]
        public string Rule { get; set; }

        public string Severity { get; set; } = "Medium"; // "Low", "Medium", "High", "Critical"

        public string Status { get; set; } = "Open"; // "Open", "Fixed", "Ignored"

        public string Category { get; set; } = FindingCategories.Other;

        public string? UserNotes { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public string? StatusUpdatedBy { get; set; }
        
        public DateTime FirstDetected { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Convert this database Finding to a unified AccessibilityIssue
        /// </summary>
        public AccessibilityIssue ToAccessibilityIssue()
        {
            return new AccessibilityIssue
            {
                Code = Rule,
                Title = Rule,
                Message = Issue,
                Severity = Severity.ToUpperInvariant(),
                Status = Status,
                Category = Category,
                InstanceCount = 1
            };
        }
    }
}
