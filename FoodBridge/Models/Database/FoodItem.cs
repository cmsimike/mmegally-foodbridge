using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodBridge.Models.Database
{
    [Table("FoodItems")]
    public class FoodItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsClaimed { get; set; }

        [MaxLength(10)]
        public string? ClaimCode { get; set; }

        [Required]
        public Guid StoreId { get; set; }
        public Store? Store { get; set; }
    }
}
