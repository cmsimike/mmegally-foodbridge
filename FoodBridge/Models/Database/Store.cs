using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodBridge.Models.Database
{
    public class Store
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        public required double Latitude { get; set; }

        [Required]
        public required double Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid DonorId { get; set; }
        public Donor? Donor { get; set; }
        public ICollection<FoodItem>? FoodItems { get; set; }
    }
}
