using System.ComponentModel.DataAnnotations;

namespace FoodBridge.Models.Request
{
    public class LoginDonorRequest
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
