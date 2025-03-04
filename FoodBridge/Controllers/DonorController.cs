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

        string password = new string(
            Enumerable.Repeat(_chars, 8).Select(s => s[_random.Next(s.Length)]).ToArray()
        );

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
        };

        await _repository.AddDonorAsync(donor);

        return Ok(new { username = donor.Username, password });
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
}
