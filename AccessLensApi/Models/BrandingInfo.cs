using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Models
{
    public class BrandingInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public string LogoUrl { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = "#4f46e5";
        public string SecondaryColor { get; set; } = "#e0e7ff";
    }
}
