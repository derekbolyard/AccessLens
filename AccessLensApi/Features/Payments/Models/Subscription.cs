using AccessLensApi.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Features.Payments.Models
{
    public class Subscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public bool Active { get; set; } = true;

        public string StripeSubId { get; set; }
        public DateTime? NextBillingUtc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // New plan relationship
        public Guid? PlanId { get; set; }
        [ForeignKey("PlanId")]
        public virtual SubscriptionPlan? Plan { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Computed property for human-readable status
        [NotMapped]
        public string ReadableStatus => Active switch
        {
            true when NextBillingUtc > DateTime.UtcNow => "Active",
            true => "Past Due", 
            false => "Canceled"
        };
    }
}
