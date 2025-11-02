using ForUpworkRestaurentManagement.Services;
using ForUpworkRestaurentManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForUpworkRestaurentManagement.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IShoppingCartService _cartService;

        public CartController(IShoppingCartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            var cartItems = await _cartService.GetCartItems();
            var total = await _cartService.GetCartTotal();

            var viewModel = new CartViewModel
            {
                Items = cartItems,
                TotalAmount = total
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int menuItemId, int quantity = 1, bool includeColdDrink = false)
        {
            try
            {
                await _cartService.AddToCart(menuItemId, quantity);
                if (includeColdDrink)
                {
                    // Try to find a soft drink item to upsell
                    try
                    {
                        var softDrinkId = await _cartService.ResolveMenuItemIdByName("Soft Drink");
                        if (softDrinkId > 0)
                        {
                            await _cartService.AddToCart(softDrinkId, 1);
                        }
                    }
                    catch { /* ignore upsell errors */ }
                }
                TempData["Success"] = "Item added to cart successfully!";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Please login to add items to cart";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart(int menuItemId, int quantity)
        {
            try
            {
                await _cartService.UpdateCartItem(menuItemId, quantity);
                TempData["Success"] = "Cart updated successfully!";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Please login to modify cart";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int menuItemId)
        {
            try
            {
                await _cartService.RemoveFromCart(menuItemId);
                TempData["Success"] = "Item removed from cart!";
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Please login to modify cart";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetCartCount()
        {
            var count = _cartService.GetCartItemCount();
            return Json(new { count });
        }
    }

}
