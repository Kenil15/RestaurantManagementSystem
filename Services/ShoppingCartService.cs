using ForUpworkRestaurentManagement.Data;
using ForUpworkRestaurentManagement.ViewModels;
using System.Text.Json;

namespace ForUpworkRestaurentManagement.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public ShoppingCartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private string GetCartSessionKey()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return $"Cart_{user.Identity.Name}";
            }
            else
            {
                // For anonymous users, use session ID
                var session = _httpContextAccessor.HttpContext?.Session;
                var sessionId = session?.Id ?? "Anonymous";
                return $"Cart_Anonymous_{sessionId}";
            }
        }

        public async Task AddToCart(int menuItemId, int quantity = 1)
        {
            // Check if user is authenticated for cart operations
            if (!IsUserAuthenticatedForCart())
            {
                throw new UnauthorizedAccessException("Please login to add items to cart");
            }

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
                throw new ArgumentException("Menu item not found or unavailable");

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuItemId = menuItemId,
                    Name = menuItem.Name,
                    Price = menuItem.Price,
                    Quantity = quantity,
                    ImageUrl = menuItem.ImageUrl ?? ""
                });
            }

            SaveCart(cart);
        }

        public async Task RemoveFromCart(int menuItemId)
        {
            if (!IsUserAuthenticatedForCart())
            {
                throw new UnauthorizedAccessException("Please login to modify cart");
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            await Task.CompletedTask;
        }

        public async Task UpdateCartItem(int menuItemId, int quantity)
        {
            if (!IsUserAuthenticatedForCart())
            {
                throw new UnauthorizedAccessException("Please login to modify cart");
            }

            if (quantity <= 0)
            {
                await RemoveFromCart(menuItemId);
                return;
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }
            await Task.CompletedTask;
        }

        public Task<List<CartItemViewModel>> GetCartItems()
        {
            return Task.FromResult(GetCart());
        }

        public Task<decimal> GetCartTotal()
        {
            var cart = GetCart();
            return Task.FromResult(cart.Sum(item => item.TotalPrice));
        }

        public Task ClearCart()
        {
            SaveCart(new List<CartItemViewModel>());
            return Task.CompletedTask;
        }

        public int GetCartItemCount()
        {
            return GetCart().Sum(item => item.Quantity);
        }

        public async Task<int> ResolveMenuItemIdByName(string name)
        {
            var item = _context.MenuItems.FirstOrDefault(m => m.Name == name && m.IsAvailable);
            return await Task.FromResult(item?.Id ?? 0);
        }

        // Helper method to check if user can perform cart operations
        private bool IsUserAuthenticatedForCart()
        {
            // Allow both authenticated and anonymous users to view cart
            // But require authentication for modifications if you prefer
            return true; // Change this based on your requirements
        }

        private List<CartItemViewModel> GetCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return new List<CartItemViewModel>();

            var cartJson = session.GetString(GetCartSessionKey());
            return string.IsNullOrEmpty(cartJson)
                ? new List<CartItemViewModel>()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString(GetCartSessionKey(), JsonSerializer.Serialize(cart));
            }
        }

        // Method to transfer cart when user logs in
        public async Task TransferCart(string fromSessionKey, string toSessionKey)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            var oldCartJson = session.GetString(fromSessionKey);
            if (!string.IsNullOrEmpty(oldCartJson))
            {
                session.SetString(toSessionKey, oldCartJson);
                session.Remove(fromSessionKey);
            }
            await Task.CompletedTask;
        }
    }
}
