using FoodBridge.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace FoodBridge.Data
{
    public class FoodItemRepository : IFoodItemRepository
    {
        private readonly FoodDonationContext _context;

        public FoodItemRepository(FoodDonationContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FoodItem>> GetAvailableFoodItemsAsync(double latitude, double longitude)
        {
            // In a real application, you would:
            // 1. Calculate distances using SQL geography types or similar
            // 2. Filter by expiration date
            // 3. Add pagination
            return await _context.FoodItems
                .Where(f => f.ExpirationDate > DateTime.UtcNow && !f.IsClaimed)
                .ToListAsync();
        }

        public async Task<FoodItem> AddFoodItemAsync(FoodItem foodItem)
        {
            foodItem.Id = Guid.NewGuid();
            foodItem.CreatedAt = DateTime.UtcNow;

            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            return foodItem;
        }

        public async Task<FoodItem> GetFoodItemByIdAsync(Guid id)
        {
            return await _context.FoodItems.FindAsync(id);
        }

        public async Task<FoodItem> ClaimFoodItemAsync(Guid id, string claimerName)
        {
            var foodItem = await _context.FoodItems.FindAsync(id);

            if (foodItem == null)
            {
                return null;
            }

            if (foodItem.IsClaimed)
            {
                throw new InvalidOperationException("Food item is already claimed");
            }

            foodItem.IsClaimed = true;
            foodItem.ClaimedAt = DateTime.UtcNow;
            foodItem.ClaimedByName = claimerName;
            foodItem.ClaimCode = GenerateClaimCode();
            foodItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return foodItem;
        }

        private string GenerateClaimCode()
        {
            // Generate a 6-character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
