using ForUpworkRestaurentManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ForUpworkRestaurentManagement.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var data = await _context.TableBookings
                .Include(b => b.Restaurant)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.BookingTime)
                .ToListAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var booking = await _context.TableBookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            if (booking == null)
                return NotFound();

            if (booking.Status == "Pending")
            {
                booking.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your booking has been cancelled.";
            }
            else
            {
                TempData["Error"] = "Only pending bookings can be cancelled.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
