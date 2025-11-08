using Waster.Models;
using Waster.Models.DbModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Waster
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Post> Posts { get; set; }
        public DbSet<ClaimPost> ClaimPosts { get; set; }
        public DbSet<VolunteerAssignment> VolunteerAssignments { get; set; }
        public DbSet<ImpactRecord> ImpactRecords { get; set; }
        public DbSet<DashboardStats> dashboardStatus { get; set; }
        public DbSet<RefreshTokens> RefreshTokens { get; set; }
        public DbSet<BookMark> BookMarks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.RefreshTokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // Post → ClaimPost
            modelBuilder.Entity<ClaimPost>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);  // ❌ stop cascade here

            // Post → ImpactRecord
            modelBuilder.Entity<ImpactRecord>()
                .HasOne(ir => ir.Post)
                .WithMany(p => p.ImpactRecords)
                .HasForeignKey(ir => ir.PostId)
                .OnDelete(DeleteBehavior.Restrict);  // ❌ also restrict here

            // ClaimPost → VolunteerAssignment
            modelBuilder.Entity<VolunteerAssignment>()
                .HasOne(v => v.Claim)
                .WithOne(c => c.VolunteerAssignment)
                .HasForeignKey<VolunteerAssignment>(v => v.ClaimID)
                .OnDelete(DeleteBehavior.Restrict);  // ❌ avoid cycles



            modelBuilder.Entity<DashboardStats>()
                .HasKey(ds => ds.Id); // Define primary key for DashboardStats

            modelBuilder.Entity<Post>().HasIndex(p => new { p.UserId, p.Status ,p.IsDeleted})
                .HasDatabaseName("IX_Post_UserId_Status_IsDeleted");

            modelBuilder.Entity<Post>().HasIndex(p =>  p.Status )
                .HasDatabaseName("IX_Post_Status");

            modelBuilder.Entity<Post>().HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Post_IsDeleted");
            modelBuilder.Entity<ClaimPost>()
                  .HasIndex(c => new { c.RecipientId, c.Status })
                  .HasDatabaseName("IX_ClaimPost_RecipientId_Status");

            // Index for finding user's posts (most common query)
            modelBuilder.Entity<Post>()
                .HasIndex(p => new { p.UserId, p.IsDeleted, p.Status })
                .HasDatabaseName("IX_Post_UserId_IsDeleted_Status");

            // Index for searching posts by status
            modelBuilder.Entity<Post>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_Post_Status");

            // Index for finding expiring posts
            modelBuilder.Entity<Post>()
                .HasIndex(p => p.ExpiresOn)
                .HasDatabaseName("IX_Post_ExpiresOn");

            // Index for category filtering
            modelBuilder.Entity<Post>()
                .HasIndex(p => p.Category)
                .HasDatabaseName("IX_Post_Category");

            // Index for finding user's claims
            modelBuilder.Entity<ClaimPost>()
                .HasIndex(c => new { c.RecipientId, c.Status })
                .HasDatabaseName("IX_ClaimPost_RecipientId_Status");

            // Index for finding post's claims
            modelBuilder.Entity<ClaimPost>()
                .HasIndex(c => new { c.PostId, c.Status })
                .HasDatabaseName("IX_ClaimPost_PostId_Status");

            // Index for refresh tokens lookup
            modelBuilder.Entity<RefreshTokens>()
                .HasIndex(r => r.Token)
                .HasDatabaseName("IX_RefreshTokens_Token");

            // Index for finding active refresh tokens
            modelBuilder.Entity<RefreshTokens>()
                .HasIndex(r => new { r.UserId, r.ExpiresOn })
                .HasDatabaseName("IX_RefreshTokens_UserId_ExpiresOn");
            // BookMark relationships
            modelBuilder.Entity<BookMark>()
                .HasKey(b => b.Id);

            modelBuilder.Entity<BookMark>()
                .HasOne(b => b.User)
                .WithMany(u => u.BookMark)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookMark>()
                .HasOne(b => b.Post)
                .WithMany(p => p.BookMarks)
                .HasForeignKey(b => b.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            //Prevent duplicate bookmarks
            modelBuilder.Entity<BookMark>()
                .HasIndex(b => new { b.UserId, b.PostId })
                .IsUnique()
                .HasDatabaseName("IX_BookMark_UserId_PostId_Unique");
        }
    }
}
