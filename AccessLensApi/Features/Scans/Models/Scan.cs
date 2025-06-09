using System;
using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Models
{
    public class Scan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public string Url { get; set; }

        public int? Score { get; set; }
        public string PdfKey { get; set; }
        public string TeaserKey { get; set; }

        public bool NeedPayment { get; set; } = false;
        public bool NeedVerify { get; set; } = false;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";  // “pending” | “success” | “failed”

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
