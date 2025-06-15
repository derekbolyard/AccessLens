using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Features.Auth.Models
{
    public class SendMagicLinkRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
