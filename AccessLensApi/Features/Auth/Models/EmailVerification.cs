using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Features.Auth.Models
{
    public class EmailVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresUtc { get; set; }
    }
}
