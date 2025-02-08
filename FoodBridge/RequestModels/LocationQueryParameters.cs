using System.ComponentModel.DataAnnotations;

namespace FoodBridge.RequestModels
{
    public class LocationQueryParameters
    {
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
    }

}
