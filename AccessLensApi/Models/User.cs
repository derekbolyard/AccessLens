using AccessLensApi.Features.Payments.Models;
using AccessLensApi.Features.Scans.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessLensApi.Models
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Key]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        public bool EmailVerified { get; set; } = false;
        public bool FirstScan { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // New scan tracking fields
        public int ScansUsed { get; set; } = 0;
        public int ScanLimit { get; set; } = 0;

        public ICollection<SnapshotPass> SnapshotPasses { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<Scan> Scans { get; set; }
        
        // New relationships
        public virtual ICollection<Site> Sites { get; set; } = new List<Site>();

        // Calculated property
        [NotMapped]
        public int ScansRemaining => Math.Max(0, ScanLimit - ScansUsed);
    }
}
