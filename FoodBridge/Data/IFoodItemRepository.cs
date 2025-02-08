using FoodBridge.DatabaseModels;
using System.ComponentModel.DataAnnotations;

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
        Task<IEnumerable<FoodItem>> GetAvailableFoodItemsAsync(double latitude, double longitude);
        Task<FoodItem> AddFoodItemAsync(FoodItem foodItem);
        Task<FoodItem> GetFoodItemByIdAsync(Guid id);
        Task<FoodItem> ClaimFoodItemAsync(Guid id, string claimerName);
    }
}
