using FoodBridge.Models.Database;
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

        public async Task<IEnumerable<FoodItem>> GetAvailableFoodItemsAsync(
            double latitude,
            double longitude
        )
        {
            return await _context
                .FoodItems.Where(f => f.ExpirationDate > DateTime.UtcNow && f.ClaimCode == null)
                .ToListAsync();
        }

        public async Task<FoodItem> AddFoodItemAsync(FoodItem foodItem)
        {
            foodItem.Id = Guid.NewGuid();

            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            return foodItem;
        }

        public async Task<FoodItem?> GetFoodItemByIdAsync(Guid id)
        {
            return await _context.FoodItems.FindAsync(id);
        }

        public async Task<FoodItem?> ClaimFoodItemAsync(Guid id, string claimerName)
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

            foodItem.ClaimCode = GenerateClaimCode();

            await _context.SaveChangesAsync();
            return foodItem;
        }

        private string GenerateClaimCode()
        {
            // Generate a 6-character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(
                Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray()
            );
        }

        public async Task<bool> DonorExistsAsync(string username)
        {
            return await _context.Donors.AnyAsync(d => d.Username == username);
        }

        public async Task<Donor> AddDonorAsync(Donor donor)
        {
            _context.Donors.Add(donor);
            await _context.SaveChangesAsync();
            return donor;
        }

        public async Task<Donor?> GetDonorByUsernameAsync(string username)
        {
            return await _context.Donors.FirstOrDefaultAsync(d => d.Username == username);
        }

        public async Task<Store> AddStoreAsync(Store store)
        {
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();
            return store;
        }

        public async Task<Store?> GetStoreAsync(Guid storeId)
        {
            return await _context
                .Stores.Include(s => s.FoodItems)
                .FirstOrDefaultAsync(s => s.Id == storeId);
        }

        public async Task<Store?> GetStoreByDonorIdAsync(Guid donorId)
        {
            return await _context
                .Stores.Include(s => s.FoodItems)
                .FirstOrDefaultAsync(s => s.DonorId == donorId);
        }

        public async Task UpdateFoodItemAsync(FoodItem foodItem)
        {
            _context.FoodItems.Update(foodItem);
            await _context.SaveChangesAsync();
        }
    }
}
