using FoodBridge.Data;
using FoodBridge.Models.Database;
using FoodBridge.Models.Request;
using FoodBridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DonorController : ControllerBase
{
    private readonly IFoodItemRepository _repository;
    private readonly IAuthService _authService;
    private readonly string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly Random _random = new Random();

    public DonorController(IFoodItemRepository repository, IAuthService authService)
    {
        _repository = repository;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<object>> RegisterDonor([FromBody] RegisterDonorRequest request)
    {
        if (await _repository.DonorExistsAsync(request.Username))
        {
            return Conflict(new { message = "Username already exists" });
        }

        string password = request.Password;
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
        };

        await _repository.AddDonorAsync(donor);
        var token = _authService.GenerateToken(donor.Id);
        return Ok(new { username = donor.Username, token = token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<object>> Login([FromBody] LoginDonorRequest request)
    {
        var donor = await _repository.GetDonorByUsernameAsync(request.Username);
        if (donor == null || !BCrypt.Net.BCrypt.Verify(request.Password, donor.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var token = _authService.GenerateToken(donor.Id);
        return Ok(new { token, id = donor.Id });
    }

    [HttpPost("store")]
    [Authorize]
    public async Task<ActionResult<Store>> RegisterStore([FromBody] RegisterStoreRequest request)
    {
        var donorId = Guid.Parse(User.Identity!.Name!);
        var existingStore = await _repository.GetStoreByDonorIdAsync(donorId);
        if (existingStore != null)
        {
            return Conflict(new { message = "Donor already has a registered store" });
        }

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            DonorId = donorId,
            CreatedAt = DateTime.UtcNow,
        };

        await _repository.AddStoreAsync(store);
        return Ok(store);
    }

    [HttpPost("food")]
    [Authorize]
    public async Task<ActionResult<FoodItem>> CreateFoodItem([FromBody] FoodItem foodItem)
    {
        var donorId = Guid.Parse(User.Identity!.Name!);
        var store = await _repository.GetStoreByDonorIdAsync(donorId);
        if (store == null)
        {
            return BadRequest(
                new { message = "Donor must register a store before adding food items" }
            );
        }

        foodItem.Id = Guid.NewGuid();
        foodItem.CreatedAt = DateTime.UtcNow;
        foodItem.StoreId = store.Id;
        foodItem.IsClaimed = false;
        foodItem.ClaimCode = null;

        await _repository.AddFoodItemAsync(foodItem);
        return Ok(foodItem);
    }

    [HttpGet("store")]
    [Authorize]
    public async Task<ActionResult<Store>> GetStore()
    {
        var donorId = Guid.Parse(User.Identity!.Name!);
        var store = await _repository.GetStoreByDonorIdAsync(donorId);
        if (store == null)
        {
            return NotFound(new { message = "No store found for this donor" });
        }

        return Ok(store);
    }

    [HttpPut("food/{id}/pickup")]
    [Authorize]
    public async Task<ActionResult<FoodItem>> MarkFoodItemAsPickedUp(Guid id)
    {
        var donorId = Guid.Parse(User.Identity!.Name!);

        // Get the donor's store
        var store = await _repository.GetStoreByDonorIdAsync(donorId);
        if (store == null)
        {
            return NotFound(new { message = "No store found for this donor" });
        }

        // Get the food item
        var foodItem = await _repository.GetFoodItemByIdAsync(id);
        if (foodItem == null)
        {
            return NotFound(new { message = "Food item not found" });
        }

        // Check if the food item belongs to the donor's store
        if (foodItem.StoreId != store.Id)
        {
            return Forbid();
        }

        // Check if the item is claimed but not yet picked up
        // Item should have ClaimCode but not be marked as picked up (IsClaimed = false)
        if (foodItem.IsClaimed)
        {
            return BadRequest(new { message = "Food item is already picked up" });
        }

        if (string.IsNullOrEmpty(foodItem.ClaimCode))
        {
            return BadRequest(new { message = "Food item is not claimed" });
        }

        // Mark the item as picked up
        foodItem.IsClaimed = true;
        await _repository.UpdateFoodItemAsync(foodItem);

        return Ok(foodItem);
    }
}