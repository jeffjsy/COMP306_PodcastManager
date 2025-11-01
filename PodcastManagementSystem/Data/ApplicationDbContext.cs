using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Models;
using System;

namespace PodcastManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Podcast> Podcasts { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ----------------------------------------------------
            // Configure ApplicationUser.Role to store as string
            // ----------------------------------------------------
            builder.Entity<ApplicationUser>()
                .Property(u => u.Role)
                .HasConversion(
                    v => v.ToString(), // Enum -> string
                    v => (UserRole)Enum.Parse(typeof(UserRole), v) // string -> Enum
                )
                .HasMaxLength(50);

            // ----------------------------------------------------
            // Podcast - Episode (1:M)
            // ----------------------------------------------------
            builder.Entity<Podcast>()
                .HasMany(p => p.Episodes)
                .WithOne(e => e.Podcast)
                .HasForeignKey(e => e.PodcastID)
                .OnDelete(DeleteBehavior.Cascade);

            // ----------------------------------------------------
            // Podcast - Subscription (1:M)
            // ----------------------------------------------------
            builder.Entity<Podcast>()
                .HasMany(p => p.Subscriptions)
                .WithOne(s => s.Podcast)
                .HasForeignKey(s => s.PodcastID)
                .OnDelete(DeleteBehavior.Cascade);

            // ----------------------------------------------------
            // ApplicationUser - Subscription (1:M)
            // ----------------------------------------------------
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Subscriptions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}




//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore;
//using PodcastManagementSystem.Models;

//namespace PodcastManagementSystem.Data
//{
//    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
//    {
//        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
//            : base(options)
//        {
//        }


//        public DbSet<Podcast> Podcasts { get; set; }
//        public DbSet<Episode> Episodes { get; set; }
//        public DbSet<Subscription> Subscriptions { get; set; }


//        protected override void OnModelCreating(ModelBuilder builder)
//        {
//            base.OnModelCreating(builder);
//            // Example configuration if needed:
//            // builder.Entity<Podcast>().HasMany(p => p.Episodes).WithOne(e => e.Podcast);

//            builder.Entity<ApplicationUser>(entity =>
//            {
//                entity.Property(e => e.Role)
//                      .HasConversion<int>()
//                      .IsRequired();
//            });

//            builder.Entity<ApplicationUser>()
//       .Property(u => u.Role)
//       .HasConversion(
//           v => v.ToString(),      // To database
//           v => (UserRole)Enum.Parse(typeof(UserRole), v)); // From database
//        }
//    }
//}
