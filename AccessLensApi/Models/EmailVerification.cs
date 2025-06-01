using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Models
{
    public class EmailVerification
    {
        [Key]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        [StringLength(6)]
        public string Code { get; set; }

        public DateTime ExpiresUtc { get; set; }
    }
}
