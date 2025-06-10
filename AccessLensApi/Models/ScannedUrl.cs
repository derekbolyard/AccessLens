using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public ICollection<Finding> Findings { get; set; }
    }
}
