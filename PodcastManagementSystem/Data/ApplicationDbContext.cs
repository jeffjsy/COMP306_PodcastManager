using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Models;

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

        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Example configuration if needed:
            // builder.Entity<Podcast>().HasMany(p => p.Episodes).WithOne(e => e.Podcast);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Role)
                      .HasConversion<int>()
                      .IsRequired();
            });
        }
    }
}
