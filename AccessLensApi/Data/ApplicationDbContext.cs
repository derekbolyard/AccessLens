using AccessLensApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AccessLensApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<SnapshotPass> SnapshotPasses { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Scan> Scans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API configurations

            modelBuilder.Entity<User>()
                .HasKey(u => u.Email);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256);

            modelBuilder.Entity<EmailVerification>()
                .HasKey(ev => ev.Id);

            modelBuilder.Entity<EmailVerification>()
                .Property(ev => ev.Email)
                .HasMaxLength(256);

            modelBuilder.Entity<SnapshotPass>()
                .HasKey(sp => sp.Id);
            modelBuilder.Entity<SnapshotPass>()
                .Property(sp => sp.Email)
                .HasMaxLength(256);
            modelBuilder.Entity<SnapshotPass>()
                .HasOne<User>()
                .WithMany(u => u.SnapshotPasses)
                .HasForeignKey(sp => sp.Email)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subscription>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Subscription>()
                .Property(s => s.Email)
                .HasMaxLength(256);
            modelBuilder.Entity<Subscription>()
                .HasOne<User>()
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.Email)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Scan>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Scan>()
                .Property(s => s.Email)
                .HasMaxLength(256);
            modelBuilder.Entity<Scan>()
                .HasOne<User>()
                .WithMany(u => u.Scans)
                .HasForeignKey(s => s.Email)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: configure default values
            modelBuilder.Entity<User>()
                .Property(u => u.EmailVerified)
                .HasDefaultValue(false);
            modelBuilder.Entity<User>()
                .Property(u => u.FirstScan)
                .HasDefaultValue(true);

            modelBuilder.Entity<SnapshotPass>()
                .Property(sp => sp.CreditsLeft)
                .HasDefaultValue(1);

            modelBuilder.Entity<Subscription>()
                .Property(s => s.Active)
                .HasDefaultValue(true);

            modelBuilder.Entity<Scan>()
                .Property(s => s.NeedPayment)
                .HasDefaultValue(false);
            modelBuilder.Entity<Scan>()
                .Property(s => s.NeedVerify)
                .HasDefaultValue(false);
            modelBuilder.Entity<Scan>()
                .Property(s => s.Status)
                .HasDefaultValue("pending");
        }
    }
}
