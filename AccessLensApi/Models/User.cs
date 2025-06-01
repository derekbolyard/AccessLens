using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccessLensApi.Models
{
    public class User
    {
        [Key]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        public bool EmailVerified { get; set; } = false;
        public bool FirstScan { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SnapshotPass> SnapshotPasses { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<Scan> Scans { get; set; }
    }
}
