using System.ComponentModel.DataAnnotations;

namespace FoodBridge.Models.Database
{
    public class Donor
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public Store Store { get; set; }
    }
}
