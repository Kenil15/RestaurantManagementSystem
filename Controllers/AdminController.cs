using ForUpworkRestaurentManagement.Data;
using ForUpworkRestaurentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ForUpworkRestaurentManagement.Services;

namespace ForUpworkRestaurentManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public AdminController(ApplicationDbContext context, IEmailService email)
        {
            _context = context;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            var usersCount = await _context.Users.CountAsync();
            var itemsCount = await _context.MenuItems.CountAsync();
            var ordersCount = await _context.Orders.CountAsync();

            var today = DateTime.UtcNow.Date;
            var week = today.AddDays(-7);
            var todaySales = await _context.Orders
                .Where(o => o.OrderDate >= today && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var weekSales = await _context.Orders
                .Where(o => o.OrderDate >= week && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var popular = await _context.OrderItems
                .GroupBy(oi => oi.MenuItemId)
                .Select(g => new { MenuItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .FirstOrDefaultAsync();
            var popularItem = popular != null ? await _context.MenuItems.FindAsync(popular.MenuItemId) : null;

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.Id)
                .Take(10)
                .ToListAsync();

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            ViewBag.UsersCount = usersCount;
            ViewBag.ItemsCount = itemsCount;
            ViewBag.OrdersCount = ordersCount;
            ViewBag.TodaySales = todaySales;
            ViewBag.WeekSales = weekSales;
            ViewBag.PopularItem = popularItem?.Name;
            ViewBag.PopularQty = popular?.Qty ?? 0;
            ViewBag.RecentUsers = recentUsers;
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.TableBookings
                .Include(b => b.Restaurant)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.BookingTime)
                .ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var booking = await _context.TableBookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status = "Confirmed";
            await _context.SaveChangesAsync();
            // notify user
            var user = await _context.Users.FindAsync(booking.UserId);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var subject = "Your table booking is confirmed";
                var html = $"<p>Your booking at restaurant ID #{booking.RestaurantId} on {booking.BookingDate:yyyy-MM-dd} at {booking.BookingTime:hh\\:mm} has been <strong>confirmed</strong>.</p>";
                await _email.SendAsync(user.Email, subject, html);
            }
            TempData["Success"] = "Booking confirmed.";
            return RedirectToAction(nameof(Bookings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var booking = await _context.TableBookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status = "Rejected";
            await _context.SaveChangesAsync();
            // notify user
            var user = await _context.Users.FindAsync(booking.UserId);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                var subject = "Your table booking was rejected";
                var html = $"<p>Your booking at restaurant ID #{booking.RestaurantId} on {booking.BookingDate:yyyy-MM-dd} at {booking.BookingTime:hh\\:mm} was <strong>rejected</strong>. Please try another time slot.</p>";
                await _email.SendAsync(user.Email, subject, html);
            }
            TempData["Success"] = "Booking rejected.";
            return RedirectToAction(nameof(Bookings));
        }
    }
}


