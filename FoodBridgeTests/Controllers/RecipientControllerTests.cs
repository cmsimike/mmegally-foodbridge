using System.Text.Json;
using FoodBridge.Controllers;
using FoodBridge.Data;
using FoodBridge.Models.Database;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FoodBridgeTests.Controllers
{
    public class RecipientControllerTests
    {
        private readonly Mock<IFoodItemRepository> _mockRepo;
        private readonly RecipientController _controller;
        private readonly Guid _testFoodItemId = Guid.NewGuid();

        public RecipientControllerTests()
        {
            _mockRepo = new Mock<IFoodItemRepository>();
            _controller = new RecipientController(_mockRepo.Object);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        [Fact]
        public async Task ClaimFood_WithValidRequest_ReturnsOkWithClaimCode()
        {
            var request = new ClaimFoodRequest { ClaimerName = "John Doe" };
            var foodItem = new FoodItem
            {
                Id = _testFoodItemId,
                Name = "Test Food",
                IsClaimed = false,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            var claimedFoodItem = new FoodItem
            {
                Id = _testFoodItemId,
                Name = "Test Food",
                IsClaimed = true,
                ClaimCode = "ABC123",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(_testFoodItemId))
                .ReturnsAsync(foodItem);

            _mockRepo
                .Setup(repo => repo.ClaimFoodItemAsync(_testFoodItemId, request.ClaimerName))
                .ReturnsAsync(claimedFoodItem);

            var actionResult = await _controller.ClaimFood(_testFoodItemId, request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            // Serialize and deserialize to check the actual structure
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("id", out var idElement));
            Assert.True(root.TryGetProperty("claimCode", out var claimCodeElement));
            Assert.Equal(_testFoodItemId.ToString(), idElement.GetString());
            Assert.Equal("ABC123", claimCodeElement.GetString());
        }

        [Fact]
        public async Task ClaimFood_WithNonexistentId_ReturnsNotFound()
        {
            var request = new ClaimFoodRequest { ClaimerName = "John Doe" };

            var actionResult = await _controller.ClaimFood(_testFoodItemId, request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);

            var jsonString = JsonSerializer.Serialize(notFoundResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Food item not found", messageElement.GetString());
        }

        [Fact]
        public async Task ClaimFood_WithAlreadyClaimedItem_ReturnsConflict()
        {
            var request = new ClaimFoodRequest { ClaimerName = "John Doe" };
            var foodItem = new FoodItem
            {
                Id = _testFoodItemId,
                Name = "Test Food",
                IsClaimed = true,
                ClaimCode = "XYZ789",
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(_testFoodItemId))
                .ReturnsAsync(foodItem);

            var actionResult = await _controller.ClaimFood(_testFoodItemId, request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(409, conflictResult.StatusCode);

            var jsonString = JsonSerializer.Serialize(conflictResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Food item is already claimed", messageElement.GetString());
        }

        [Fact]
        public async Task GetAvailableFood_ReturnsOkWithItems()
        {
            var latitude = 40.7128;
            var longitude = -74.0060;
            var foodItems = new[]
            {
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Food 1",
                    IsClaimed = false,
                    ExpirationDate = DateTime.UtcNow.AddDays(1),
                },
                new FoodItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Food 2",
                    IsClaimed = false,
                    ExpirationDate = DateTime.UtcNow.AddDays(2),
                },
            };

            _mockRepo
                .Setup(repo => repo.GetAvailableFoodItemsAsync(latitude, longitude))
                .ReturnsAsync(foodItems);

            var actionResult = await _controller.GetAvailableFood(latitude, longitude);

            var result = Assert.IsType<ActionResult<IEnumerable<FoodItem>>>(actionResult);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);

            var returnedFoodItems = Assert.IsAssignableFrom<IEnumerable<FoodItem>>(okResult.Value);
            Assert.Equal(2, returnedFoodItems.Count());
        }

        [Fact]
        public async Task ClaimFood_WithInvalidModel_ReturnsBadRequest()
        {
            var request = new ClaimFoodRequest { ClaimerName = "" }; // Invalid empty name
            _controller.ModelState.AddModelError(
                "ClaimerName",
                "The ClaimerName field is required."
            );

            var actionResult = await _controller.ClaimFood(_testFoodItemId, request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var badRequestResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task ClaimFood_WhenRepositoryThrowsException_ReturnsConflict()
        {
            var request = new ClaimFoodRequest { ClaimerName = "John Doe" };
            var foodItem = new FoodItem
            {
                Id = _testFoodItemId,
                Name = "Test Food",
                IsClaimed = false,
                ExpirationDate = DateTime.UtcNow.AddDays(1),
            };

            _mockRepo
                .Setup(repo => repo.GetFoodItemByIdAsync(_testFoodItemId))
                .ReturnsAsync(foodItem);

            _mockRepo
                .Setup(repo => repo.ClaimFoodItemAsync(_testFoodItemId, request.ClaimerName))
                .ThrowsAsync(new InvalidOperationException("Unexpected error during claim"));

            var actionResult = await _controller.ClaimFood(_testFoodItemId, request);

            var result = Assert.IsType<ActionResult<object>>(actionResult);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(409, conflictResult.StatusCode);

            var jsonString = JsonSerializer.Serialize(conflictResult.Value);
            var responseDoc = JsonDocument.Parse(jsonString);
            var root = responseDoc.RootElement;

            Assert.True(root.TryGetProperty("message", out var messageElement));
            Assert.Equal("Unexpected error during claim", messageElement.GetString());
        }
    }
}
