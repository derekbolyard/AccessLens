using AccessLensApi.Features.Auth.Models;
using AccessLensApi.Features.Core.Models;
using AccessLensApi.Features.Payments.Models;
using AccessLensApi.Features.Reports.Models;
using AccessLensApi.Features.Scans.Models;
using AccessLensApi.Features.Sites.Models;
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
        public DbSet<MagicLinkUsage> MagicLinkUsages { get; set; }
        public DbSet<SnapshotPass> SnapshotPasses { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Scan> Scans { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ScannedUrl> ScannedUrls { get; set; }
        public DbSet<Finding> Findings { get; set; }
        
        // New DbSets
        public DbSet<Site> Sites { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<BrandingInfo> BrandingInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API configurations

            modelBuilder.Entity<User>()
                .HasKey(u => u.Email);

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(256);


            modelBuilder.Entity<MagicLinkUsage>()
                .HasKey(mlu => mlu.Id);

            modelBuilder.Entity<MagicLinkUsage>()
                .Property(mlu => mlu.Email)
                .HasMaxLength(256);

            modelBuilder.Entity<MagicLinkUsage>()
                .Property(mlu => mlu.JwtId)
                .HasMaxLength(50);

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

            modelBuilder.Entity<Report>()
                .HasKey(r => r.ReportId);
            modelBuilder.Entity<Report>()
                .Property(r => r.Email)
                .HasMaxLength(256);
            modelBuilder.Entity<Report>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.Email)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScannedUrl>()
                .HasKey(u => u.UrlId);
            modelBuilder.Entity<ScannedUrl>()
                .HasOne(u => u.Report)
                .WithMany(r => r.ScannedUrls)
                .HasForeignKey(u => u.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Finding>()
                .HasKey(f => f.FindingId);
            modelBuilder.Entity<Finding>()
                .HasOne(f => f.Report)
                .WithMany(r => r.Findings)
                .HasForeignKey(f => f.ReportId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Finding>()
                .HasOne(f => f.ScannedUrl)
                .WithMany(u => u.Findings)
                .HasForeignKey(f => f.UrlId)
                .OnDelete(DeleteBehavior.Cascade);

            // New entity configurations
            modelBuilder.Entity<Site>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Site>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sites)
                .HasForeignKey(s => s.Email)  // Use Email instead of UserId
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubscriptionPlan>()
                .HasKey(sp => sp.Id);
            
            // Configure SubscriptionPlan Features as TEXT for SQLite
            modelBuilder.Entity<SubscriptionPlan>()
                .Property(sp => sp.Features)
                .HasColumnType("TEXT");

            // Site relationship with User using Email (not UserId)
            modelBuilder.Entity<Site>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sites)
                .HasForeignKey(s => s.Email)
                .OnDelete(DeleteBehavior.Cascade);

            // Update Report to include Site relationship
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Site)
                .WithMany(s => s.Reports)
                .HasForeignKey(r => r.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Update Subscription to include Plan relationship
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BrandingInfo>()
                .HasKey(b => b.Id);
            modelBuilder.Entity<BrandingInfo>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .HasPrincipalKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
