using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.ViewModels
{
    public class MenuItemViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }=string.Empty;

        public string Description { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Restaurant")]
        public int? RestaurantId { get; set; }

        public bool IsAvailable { get; set; }= true;
    }
}
