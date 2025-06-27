using AccessLensApi.Features.Auth.Models;
using AccessLensApi.Features.Reports.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Features.Sites.Models
{
    public class Site
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Url { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }  // Changed from UserId to Email

        [ForeignKey("Email")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Calculated properties (computed in services)
        [NotMapped]
        public int? TotalReports { get; set; }

        [NotMapped]
        public DateTime? LastScanDate { get; set; }
    }
}
