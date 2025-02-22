using FoodBridge.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace FoodBridge.Data
{
    public class FoodDonationContext : DbContext
    {
        public FoodDonationContext(DbContextOptions<FoodDonationContext> options)
            : base(options) { }

        public DbSet<FoodItem> FoodItems { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Store> Stores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FoodItem>().HasIndex(f => f.ExpirationDate);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Store>().HasIndex(s => new { s.Latitude, s.Longitude });

            modelBuilder.Entity<Donor>().HasIndex(d => d.Username).IsUnique();

            modelBuilder
                .Entity<Donor>()
                .HasOne(d => d.Store)
                .WithOne(s => s.Donor)
                .HasForeignKey<Store>(s => s.DonorId);

            modelBuilder
                .Entity<FoodItem>()
                .HasOne<Store>()
                .WithMany(s => s.FoodItems)
                .HasForeignKey("StoreId")
                .IsRequired();
        }
    }
}
