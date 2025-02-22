using System.ComponentModel.DataAnnotations;

namespace FoodBridge.Models.Request
{
    public class LoginDonorRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
