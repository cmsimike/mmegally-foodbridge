using System.ComponentModel.DataAnnotations;

namespace FoodBridge.Models.Request
{
    public class RegisterDonorRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public required string Username { get; set; }
    }
}
