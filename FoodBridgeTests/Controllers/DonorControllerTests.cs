using FoodBridge.Data;
using FoodBridge.Models.Database;
using FoodBridge.Models.Request;
using FoodBridge.Services;

namespace FoodBridge.Tests.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using System.Security.Claims;
    using System.Text.Json;
    using Xunit;

    public class DonorControllerTests
    {
        private readonly Mock<IFoodItemRepository> _mockRepo;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly DonorController _controller;
        private readonly Guid _testDonorId = Guid.NewGuid();
        private readonly string _testToken = "test-token-123";

        public DonorControllerTests()
        {
            _mockRepo = new Mock<IFoodItemRepository>();
            _mockAuthService = new Mock<IAuthService>();
            _controller = new DonorController(_mockRepo.Object, _mockAuthService.Object);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        private void SetupAuthentication()
        {
            var userClaims = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, _testDonorId.ToString()) },
                "Bearer" // Add authentication type
            );
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(userClaims) },
            };
        }

        [Fact]
        public async Task RegisterDonor_WithNewUsername_ReturnsOkWithPassword()
        {
            var request = new RegisterDonorRequest
            {
                Username = "newdonor",
                Password = "asd123455",
            };
            _mockRepo
                .Setup(repo => repo.DonorExistsAsync(request.Username))
                .ReturnsAsync(false);
            _mockAuthService
                .Setup(auth => auth.GenerateToken(It.IsAny<Guid>()))
                .Returns(_testToken);

            var actionResult = await _controller.RegisterDonor(request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("username", out var usernameElement));
            Assert.True(root.TryGetProperty("token", out var tokenElement));
            Assert.Equal("newdonor", usernameElement.GetString());
            Assert.Equal(_testToken, tokenElement.GetString());
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            var request = new LoginDonorRequest
            {
                Username = "testdonor",
                Password = "password123",
            };
            var donor = new Donor
            {
                Id = _testDonorId,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            };

            _mockRepo
                .Setup(repo => repo.GetDonorByUsernameAsync(request.Username))
                .ReturnsAsync(donor);
            _mockAuthService
                .Setup(auth => auth.GenerateToken(_testDonorId))
                .Returns(_testToken);

            var actionResult = await _controller.Login(request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("token", out var tokenElement));
            Assert.True(root.TryGetProperty("id", out var idElement));
            Assert.Equal(_testToken, tokenElement.GetString());
            Assert.Equal(_testDonorId.ToString(), idElement.GetString());
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            var request = new LoginDonorRequest
            {
                Username = "testdonor",
                Password = "wrongpassword",
            };
            var donor = new Donor
            {
                Id = _testDonorId,
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            };

            _mockRepo
                .Setup(repo => repo.GetDonorByUsernameAsync(request.Username))
                .ReturnsAsync(donor);

            var actionResult = await _controller.Login(request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterStore_WithValidRequest_ReturnsOkWithStore()
        {
            var request = new RegisterStoreRequest
            {
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();

            var actionResult = await _controller.RegisterStore(request);

            var result = Assert.IsType<ActionResult<Store>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var store = Assert.IsType<Store>(okResult.Value);

            Assert.Equal(request.Name, store.Name);
            Assert.Equal(request.Latitude, store.Latitude);
            Assert.Equal(request.Longitude, store.Longitude);
            Assert.Equal(_testDonorId, store.DonorId);
        }

        [Fact]
        public async Task RegisterStore_WithExistingStore_ReturnsConflict()
        {
            var request = new RegisterStoreRequest
            {
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            var existingStore = new Store
            {
                Id = Guid.NewGuid(),
                DonorId = _testDonorId,
                Name = "Existing Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(existingStore);

            var actionResult = await _controller.RegisterStore(request);

            var result = Assert.IsType<ActionResult<Store>>(actionResult);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(conflictResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Donor already has a registered store", messageElement.GetString());
        }

        [Fact]
        public async Task CreateFoodItem_WithValidRequest_ReturnsOkWithFoodItem()
        {
            var foodItem = new FoodItem
            {
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            var store = new Store
            {
                Id = Guid.NewGuid(),
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);

            var actionResult = await _controller.CreateFoodItem(foodItem);

            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFoodItem = Assert.IsType<FoodItem>(okResult.Value);

            Assert.Equal(foodItem.Name, returnedFoodItem.Name);
            Assert.Equal(foodItem.Description, returnedFoodItem.Description);
            Assert.Equal(foodItem.ExpirationDate, returnedFoodItem.ExpirationDate);
            Assert.Equal(store.Id, returnedFoodItem.StoreId);
        }

        [Fact]
        public async Task CreateFoodItem_WithNoStore_ReturnsBadRequest()
        {
            var foodItem = new FoodItem
            {
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            SetupAuthentication();

            var actionResult = await _controller.CreateFoodItem(foodItem);

            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(badRequestResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal(
                "Donor must register a store before adding food items",
                messageElement.GetString()
            );
        }

        [Fact]
        public async Task GetStore_WithExistingStore_ReturnsStore()
        {
            var store = new Store
            {
                Id = Guid.NewGuid(),
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);

            var actionResult = await _controller.GetStore();

            var result = Assert.IsType<ActionResult<Store>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedStore = Assert.IsType<Store>(okResult.Value);

            Assert.Equal(store.Id, returnedStore.Id);
            Assert.Equal(store.Name, returnedStore.Name);
            Assert.Equal(store.Latitude, returnedStore.Latitude);
            Assert.Equal(store.Longitude, returnedStore.Longitude);
        }

        [Fact]
        public async Task GetStore_WithNoStore_ReturnsNotFound()
        {
            SetupAuthentication();

            var actionResult = await _controller.GetStore();

            var result = Assert.IsType<ActionResult<Store>>(actionResult);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(notFoundResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("No store found for this donor", messageElement.GetString());
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithValidRequest_ReturnsOkWithUpdatedFoodItem()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var claimCode = "ABC123";

            var foodItem = new FoodItem
            {
                Id = foodItemId,
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsClaimed = false, // Not picked up yet
                ClaimCode = claimCode, // But has claim code (claimed)
                StoreId = storeId
            };

            var store = new Store
            {
                Id = storeId,
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);
            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(foodItemId))
                .ReturnsAsync(foodItem);
            _mockRepo
                .Setup(repo => repo.UpdateFoodItemAsync(It.IsAny<FoodItem>()))
                .Returns(Task.CompletedTask)
                .Callback<FoodItem>(item =>
                {
                    // The callback captures the updated food item for verification
                    Assert.True(item.IsClaimed); // IsClaimed should be set to true
                    Assert.Equal(claimCode, item.ClaimCode); // Claim code should remain unchanged
                });

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFoodItem = Assert.IsType<FoodItem>(okResult.Value);

            Assert.Equal(foodItemId, returnedFoodItem.Id);
            Assert.Equal(claimCode, returnedFoodItem.ClaimCode); // Claim code should remain unchanged
            Assert.True(returnedFoodItem.IsClaimed); // IsClaimed should be set to true
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithNoStore_ReturnsNotFound()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync((Store)null);

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(notFoundResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("No store found for this donor", messageElement.GetString());
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithNonExistentFoodItem_ReturnsNotFound()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var store = new Store
            {
                Id = storeId,
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);
            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(foodItemId))
                .ReturnsAsync((FoodItem)null);

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(notFoundResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Food item not found", messageElement.GetString());
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithFoodItemFromDifferentStore_ReturnsForbid()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var differentStoreId = Guid.NewGuid();

            var foodItem = new FoodItem
            {
                Id = foodItemId,
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsClaimed = false,
                ClaimCode = "ABC123",
                StoreId = differentStoreId // Different from the donor's store
            };

            var store = new Store
            {
                Id = storeId,
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);
            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(foodItemId))
                .ReturnsAsync(foodItem);

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithUnclaimedFoodItem_ReturnsBadRequest()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var foodItem = new FoodItem
            {
                Id = foodItemId,
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsClaimed = false, // Not picked up
                ClaimCode = null, // And not claimed (no claim code)
                StoreId = storeId
            };

            var store = new Store
            {
                Id = storeId,
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);
            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(foodItemId))
                .ReturnsAsync(foodItem);

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(badRequestResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Food item is not claimed", messageElement.GetString());
        }

        [Fact]
        public async Task MarkFoodItemAsPickedUp_WithAlreadyPickedUpFoodItem_ReturnsBadRequest()
        {
            // Arrange
            var foodItemId = Guid.NewGuid();
            var storeId = Guid.NewGuid();

            var foodItem = new FoodItem
            {
                Id = foodItemId,
                Name = "Test Food",
                Description = "Test Description",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsClaimed = true, // Already picked up
                ClaimCode = "ABC123",
                StoreId = storeId
            };

            var store = new Store
            {
                Id = storeId,
                DonorId = _testDonorId,
                Name = "Test Store",
                Latitude = 40.7128,
                Longitude = -74.0060,
            };

            SetupAuthentication();
            _mockRepo
                .Setup(repo => repo.GetStoreByDonorIdAsync(_testDonorId))
                .ReturnsAsync(store);
            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(foodItemId))
                .ReturnsAsync(foodItem);

            // Act
            var actionResult = await _controller.MarkFoodItemAsPickedUp(foodItemId);

            // Assert
            var result = Assert.IsType<ActionResult<FoodItem>>(actionResult);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var jsonString = JsonSerializer.Serialize(badRequestResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Food item is already picked up", messageElement.GetString());
        }
    }

}
