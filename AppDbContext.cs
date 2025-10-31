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
        }

    }
}
//public void onmodelCreating(ModelBuilder modelBuilder)
//{
//    base.OnModelCreating(modelBuilder);
//    // Customize the ASP.NET Identity model and override the defaults if needed.
//    // For example, you can rename the ASP.NET Identity table names and more.
//    // Add your customizations after calling base.OnModelCreating(builder);
//}
/*

 protected override void OnModelCreating(ModelBuilder modelBuilder)
{
base.OnModelCreating(modelBuilder);

modelBuilder.Entity<Claim>()
    .HasOne(c => c.Post)
    .WithMany(p => p.Claims)
    .HasForeignKey(c => c.PostId)
    .OnDelete(DeleteBehavior.Restrict);   // 🚫 SQL will prevent deleting Post if Claims exist
}



 */