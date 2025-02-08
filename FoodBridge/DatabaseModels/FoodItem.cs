using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodBridge.DatabaseModels
{
    [Table("FoodItems")]
    public class FoodItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string DonorName { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsClaimed { get; set; }

        public DateTime? ClaimedAt { get; set; }

        [MaxLength(10)]
        public string? ClaimCode { get; set; }

        [MaxLength(100)]
        public string? ClaimedByName { get; set; }
    }
}
