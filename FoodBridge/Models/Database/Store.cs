using System.ComponentModel.DataAnnotations;

namespace FoodBridge.Models.Database
{
    public class Store
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid DonorId { get; set; }
        public Donor Donor { get; set; }
        public ICollection<FoodItem> FoodItems { get; set; }
    }
}
