using System.ComponentModel.DataAnnotations;
using FoodBridge.Models.Database;

namespace FoodBridge.Data
{
    public class ClaimFoodRequest
    {
        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string ClaimerName { get; set; }
    }

    public interface IFoodItemRepository
    {
        // Recipient
        Task<IEnumerable<FoodItem>> GetAvailableFoodItemsAsync(double latitude, double longitude);
        Task<FoodItem> AddFoodItemAsync(FoodItem foodItem);
        Task<FoodItem> GetFoodItemByIdAsync(Guid id);
        Task<FoodItem> ClaimFoodItemAsync(Guid id, string claimerName);

        // Donor
        Task<bool> DonorExistsAsync(string username);
        Task<Donor> AddDonorAsync(Donor donor);
        Task<Donor> GetDonorByUsernameAsync(string username);
        Task<Store> AddStoreAsync(Store store);
        Task<Store> GetStoreAsync(Guid storeId);
        Task<Store> GetStoreByDonorIdAsync(Guid donorId);
    }
}
