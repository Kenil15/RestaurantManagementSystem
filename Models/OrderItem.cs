using System.ComponentModel.DataAnnotations.Schema;

namespace ForUpworkRestaurentManagement.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int MenuItemId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; }= null;
        public virtual MenuItem MenuItem { get; set; } = null;
    }
}
