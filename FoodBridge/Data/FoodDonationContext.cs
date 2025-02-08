using FoodBridge.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace FoodBridge.Data
{
    public class FoodDonationContext : DbContext
    {
        public FoodDonationContext(DbContextOptions<FoodDonationContext> options)
            : base(options)
        {
        }

        public DbSet<FoodItem> FoodItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FoodItem>()
                .HasIndex(f => f.ExpirationDate);

            modelBuilder.Entity<FoodItem>()
                .HasIndex(f => new { f.Latitude, f.Longitude });
        }
    }
}

