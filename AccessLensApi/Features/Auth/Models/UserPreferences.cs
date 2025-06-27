using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Features.Auth.Models
{
    public class UserPreferences
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Column(TypeName = "nvarchar(max)")]
        public string? DashboardLayout { get; set; } // JSON string

        [Column(TypeName = "nvarchar(max)")]
        public string? NotificationSettings { get; set; } // JSON string

        [Column(TypeName = "nvarchar(max)")]
        public string? DefaultScanSettings { get; set; } // JSON string
    }
}
