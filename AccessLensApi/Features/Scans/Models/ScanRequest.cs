using System.ComponentModel.DataAnnotations;
namespace AccessLensApi.Features.Scans.Models
{
    public class ScanRequest
    {
        [Required]
        [MaxLength(500)]
        public string Url { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        public string? HcaptchaToken { get; set; }
    }
}
