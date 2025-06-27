using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Features.Payments.Models
{
    public class SubscriptionPlan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string StripeProductId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public string Interval { get; set; } = string.Empty; // "month" | "year"

        public int ScanLimit { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Features { get; set; } = "[]"; // JSON string

        public bool IsPopular { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
