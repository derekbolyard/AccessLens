using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Features.Auth.Models
{
    public class MagicLinkUsage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string JwtId { get; set; } = string.Empty;

        public DateTime UsedAt { get; set; }

        public DateTime ExpiresUtc { get; set; }
    }
}
