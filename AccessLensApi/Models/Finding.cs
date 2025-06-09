using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Models
{
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

        public string Severity { get; set; }

        public string Status { get; set; } = "Open"; // Open, Fixed, Ignored
        public DateTime FirstDetected { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
