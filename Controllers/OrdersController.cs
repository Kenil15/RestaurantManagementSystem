using ForUpworkRestaurentManagement.Data;
using ForUpworkRestaurentManagement.Models;
using ForUpworkRestaurentManagement.Services;
using ForUpworkRestaurentManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ForUpworkRestaurentManagement.Hubs;

namespace ForUpworkRestaurentManagement.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShoppingCartService _cartService;
        private readonly IHubContext<DriverTrackingHub> _hubContext;

        public OrdersController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IShoppingCartService cartService,
            IHubContext<DriverTrackingHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Order> ordersQuery;

            if (User.IsInRole("Admin"))
            {
                ordersQuery = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem);
            }
            else
            {
                ordersQuery = _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem);
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var viewModel = orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                UserId = o.UserId,
                UserName = o.User?.UserName,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                DeliveryAddress = o.DeliveryAddress,
                Notes = o.Notes,
                OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
                {
                    MenuItemName = oi.MenuItem.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var cartItems = await _cartService.GetCartItems();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var viewModel = new CreateOrderViewModel
            {
                CartItems = cartItems,
                DeliveryAddress = user?.Address
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            var cartItems = await _cartService.GetCartItems();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            // Be forgiving: if payment method missing, default to CashOnDelivery
            if (string.IsNullOrWhiteSpace(viewModel.PaymentMethod))
            {
                viewModel.PaymentMethod = "CashOnDelivery";
            }

            var user = await _userManager.GetUserAsync(User);
            var deliveryAddress = string.IsNullOrWhiteSpace(viewModel.DeliveryAddress) ? user?.Address : viewModel.DeliveryAddress;
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cartItems.Sum(item => item.TotalPrice),
                Status = OrderStatus.Pending,
                DeliveryAddress = deliveryAddress ?? string.Empty,
                Notes = viewModel.Notes
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = cartItem.MenuItemId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.Price
                };
                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();
            await _cartService.ClearCart();

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if user is authorized to view this order
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && order.UserId != user.Id)
            {
                return Forbid();
            }

            var viewModel = new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                UserName = order.User?.UserName,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                DeliveryAddress = order.DeliveryAddress,
                Notes = order.Notes,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    MenuItemName = oi.MenuItem.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && order.UserId != user.Id)
            {
                return Forbid();
            }

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed)
            {
                order.Status = OrderStatus.Cancelled;
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Order cancelled.";
            }
            else
            {
                TempData["Error"] = "Order can no longer be cancelled.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order status updated successfully!";
            // notify clients in the order group
            await _hubContext.Clients.Group($"order-{id}").SendAsync("OrderStatusUpdated", status.ToString());
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SimulateDriver(int id)
        {
            // simple demo simulation: emit few points
            var route = new (double lat, double lng)[]
            {
                (23.0225,72.5714), (23.0240,72.5720), (23.0260,72.5730), (23.0280,72.5745), (23.0295,72.5755)
            };
            foreach (var p in route)
            {
                await _hubContext.Clients.Group($"order-{id}").SendAsync("DriverLocationUpdated", new { lat = p.lat, lng = p.lng });
                await Task.Delay(1200);
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
