using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForUpworkRestaurentManagement.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Foreign key
        public int CategoryId { get; set; }
        public int? RestaurantId { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; } = null;
        public virtual Restaurant? Restaurant { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }= new List<OrderItem>();
    }
}
