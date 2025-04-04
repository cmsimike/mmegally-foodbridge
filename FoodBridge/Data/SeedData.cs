﻿using FoodBridge.Models.Database;

namespace FoodBridge.Data
{
    public static class SeedData
    {
        public static void Initialize(FoodDonationContext context)
        {
            if (context.Donors.Any())
            {
                return;
            }

            var donor1 = new Donor
            {
                Id = Guid.NewGuid(),
                Username = "lmustore",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                CreatedAt = DateTime.UtcNow,
            };

            var store1 = new Store
            {
                Id = Guid.NewGuid(),
                Name = "Store at LMU",
                Latitude = 33.96848344360838,
                Longitude = -118.41593724437132,
                DonorId = donor1.Id,
                CreatedAt = DateTime.UtcNow,
            };

            donor1.Store = store1;

            context.Donors.Add(donor1);
            context.Stores.Add(store1);

            var foodItems = new[]
            {
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Canned Soup",
                    Description = "Vegetable soup, unopened",
                    ExpirationDate = DateTime.UtcNow.AddDays(60),
                    StoreId = store1.Id,
                    CreatedAt = DateTime.UtcNow,
                },
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Fresh Bread",
                    Description = "Whole wheat bread, baked today",
                    ExpirationDate = DateTime.UtcNow.AddDays(5),
                    StoreId = store1.Id,
                    CreatedAt = DateTime.UtcNow,
                },
            };

            context.FoodItems.AddRange(foodItems);
            context.SaveChanges();
        }
    }
}
