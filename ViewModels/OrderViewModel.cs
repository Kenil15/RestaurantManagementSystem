using System.ComponentModel.DataAnnotations;

namespace ForUpworkRestaurentManagement.ViewModels
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }= string.Empty;
        public string UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }= string.Empty;
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }=new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

}
