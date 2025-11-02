using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForUpworkRestaurentManagement.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }= string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public string DeliveryAddress { get; set; }

        public string Notes { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null;
        public virtual ICollection<OrderItem> OrderItems { get; set; }= new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparing,
        ReadyForPickup,
        Delivered,
        Cancelled
    }
}
