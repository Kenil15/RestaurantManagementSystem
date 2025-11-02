using ForUpworkRestaurentManagement.ViewModels;

namespace ForUpworkRestaurentManagement.Services
{
    public interface IShoppingCartService
    {
        Task AddToCart(int menuItemId, int quantity = 1);
        Task RemoveFromCart(int menuItemId);
        Task UpdateCartItem(int menuItemId, int quantity);
        Task<List<CartItemViewModel>> GetCartItems();
        Task<decimal> GetCartTotal();
        Task ClearCart();
        int GetCartItemCount();
        Task<int> ResolveMenuItemIdByName(string name);
    }
}
