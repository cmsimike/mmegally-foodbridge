using FoodBridge.Data;
using FoodBridge.DatabaseModels;
using FoodBridge.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace FoodBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipientController : ControllerBase
    {
        private readonly IFoodItemRepository _repository;

        public RecipientController(IFoodItemRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("available-food")]
        public async Task<ActionResult<IEnumerable<FoodItem>>> GetAvailableFood(
            [FromQuery] double latitude,
            [FromQuery] double longitude)
        {
            var foodItems = await _repository.GetAvailableFoodItemsAsync(latitude, longitude);
            return Ok(foodItems);
        }

        [HttpPost("claim/{id}")]
        public async Task<ActionResult<object>> ClaimFood(Guid id, [FromBody] ClaimFoodRequest request)
        {
            try
            {
                var foodItem = await _repository.GetFoodItemByIdAsync(id);
                if (foodItem == null)
                {
                    return NotFound(new { message = "Food item not found" });
                }

                if (foodItem.IsClaimed)
                {
                    return Conflict(new { message = "Food item is already claimed" });
                }

                var claimedItem = await _repository.ClaimFoodItemAsync(id, request.ClaimerName);

                return Ok(new
                {
                    id = claimedItem.Id,
                    claimCode = claimedItem.ClaimCode
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }

}
