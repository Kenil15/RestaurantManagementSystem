using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.Models
{
    public class RestaurantReview
    {
        public int Id { get; set; }
        [Required]
        public int RestaurantId { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Range(1,5)]
        public int Rating { get; set; }
        [StringLength(1000)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Restaurant Restaurant { get; set; } = null!;
    }
}
