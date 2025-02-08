using FoodBridge.DatabaseModels;

namespace FoodBridge.Data
{
    public static class SeedData
    {
        public static void Initialize(FoodDonationContext context)
        {
            if (context.FoodItems.Any())
            {
                return;
            }

            context.FoodItems.AddRange(
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Canned Soup",
                    Description = "Vegetable soup, unopened",
                    ExpirationDate = DateTime.UtcNow.AddDays(60),
                    DonorName = "Local Food Bank",
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    CreatedAt = DateTime.UtcNow
                },
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Fresh Bread",
                    Description = "Whole wheat bread, baked today",
                    ExpirationDate = DateTime.UtcNow.AddDays(5),
                    DonorName = "Community Bakery",
                    Latitude = 40.7589,
                    Longitude = -73.9851,
                    CreatedAt = DateTime.UtcNow
                }
            );

            context.SaveChanges();
        }
    }
}
