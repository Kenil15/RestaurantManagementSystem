using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.ViewModels
{
    public class CreateOrderViewModel
    {
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }
        public List<CartItemViewModel> CartItems { get; set; }
        [Required]
        public string PaymentMethod { get; set; } = "CashOnDelivery"; // CashOnDelivery, Card, UPI
    }
}
