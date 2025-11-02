using ForUpworkRestaurentManagement.Data;
using ForUpworkRestaurentManagement.Models;
using ForUpworkRestaurentManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace ForUpworkRestaurentManagement.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, string search, int? restaurantId)
        {
            var menuItemsQuery = _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .Where(m => m.IsAvailable);

            if (categoryId.HasValue)
            {
                menuItemsQuery = menuItemsQuery.Where(m => m.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                menuItemsQuery = menuItemsQuery.Where(m =>
                    m.Name.Contains(search) ||
                    m.Description.Contains(search) ||
                    m.Category.Name.Contains(search));
            }

            if (restaurantId.HasValue)
            {
                menuItemsQuery = menuItemsQuery.Where(m => m.RestaurantId == restaurantId.Value);
            }

            var menuItems = await menuItemsQuery
                .OrderBy(m => m.Category.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
            var restaurants = await _context.Restaurants.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

            var viewModel = new MenuViewModel
            {
                MenuItems = menuItems.Select(m => new MenuItemViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ImageUrl = m.ImageUrl,
                    CategoryId = m.CategoryId,
                    IsAvailable = m.IsAvailable,
                }).ToList(),
                Categories = categories,
            };

            ViewBag.Restaurants = new SelectList(restaurants, "Id", "Name");

            return View(viewModel);
        }

        [HttpGet("/Menu/Restaurant/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Restaurant(int id, DateTime? date = null)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            if (restaurant == null) return NotFound();

            var items = await _context.MenuItems
                .Where(m => m.RestaurantId == id && m.IsAvailable)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var reviews = await _context.RestaurantReviews
                .Where(r => r.RestaurantId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var vm = new RestaurantMenuViewModel
            {
                Restaurant = restaurant,
                MenuItems = items,
                BookingDate = date ?? DateTime.Today,
                GuestsCount = 2,
                Reviews = reviews,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0
            };

            vm.AvailableSlots = BuildAvailableSlots(restaurant, vm.BookingDate.Value);
            return View(vm);
        }

        [HttpPost("/Menu/Restaurant/{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restaurant(int id, RestaurantMenuViewModel model)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            if (restaurant == null) return NotFound();

            // Rebuild menu list for redisplay
            model.Restaurant = restaurant;
            model.MenuItems = await _context.MenuItems
                .Where(m => m.RestaurantId == id && m.IsAvailable)
                .OrderBy(m => m.Name)
                .ToListAsync();

            // Robustly parse incoming form values for date/time to avoid culture issues
            var dateStr = Request.Form["BookingDate"].ToString();
            var timeStr = Request.Form["BookingTime"].ToString();

            if (!string.IsNullOrWhiteSpace(dateStr))
            {
                DateTime parsedDate;
                var dateFormats = new[] { "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy", "dd/MM/yyyy" };
                if (DateTime.TryParseExact(dateStr, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate))
                {
                    model.BookingDate = parsedDate.Date;
                }
                else if (DateTime.TryParse(dateStr, out parsedDate))
                {
                    model.BookingDate = parsedDate.Date;
                }
                else
                {
                    Console.WriteLine($"[Booking] Failed to parse date: '{dateStr}'");
                    ModelState.AddModelError(string.Empty, "Could not parse booking date.");
                }
            }

            if (!string.IsNullOrWhiteSpace(timeStr))
            {
                TimeSpan parsedTime;
                var timeFormats = new[] { "HH:mm:ss", "hh:mm:ss", "HH:mm", "hh:mm" };
                bool parsed = false;
                foreach (var fmt in timeFormats)
                {
                    if (TimeSpan.TryParseExact(timeStr, fmt, System.Globalization.CultureInfo.InvariantCulture, out parsedTime))
                    {
                        model.BookingTime = parsedTime;
                        parsed = true;
                        break;
                    }
                }
                if (!parsed && TimeSpan.TryParse(timeStr, out parsedTime))
                {
                    model.BookingTime = parsedTime;
                    parsed = true;
                }
                if (!parsed)
                {
                    Console.WriteLine($"[Booking] Failed to parse time: '{timeStr}'");
                    ModelState.AddModelError(string.Empty, "Could not parse booking time.");
                }
            }

            if (!model.BookingDate.HasValue || !model.BookingTime.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Please select booking date and time.");
            }

            if (model.BookingTime.HasValue)
            {
                var t = model.BookingTime.Value;
                if (t < restaurant.OpeningTime || t > restaurant.ClosingTime)
                {
                    ModelState.AddModelError(string.Empty, "Selected time is outside restaurant hours.");
                }
            }

            if (ModelState.IsValid)
            {
                // Enforce capacity per slot
                var activeCount = await _context.TableBookings.CountAsync(b =>
                    b.RestaurantId == id &&
                    b.BookingDate == model.BookingDate!.Value.Date &&
                    b.BookingTime == model.BookingTime!.Value &&
                    (b.Status == "Pending" || b.Status == "Confirmed"));

                var capacity = restaurant.SlotCapacity > 0 ? restaurant.SlotCapacity : int.MaxValue;
                if (activeCount >= capacity)
                {
                    ModelState.AddModelError(string.Empty, "Selected time slot is fully booked. Please choose another time.");
                }
                else
                {
                    var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!;
                    var booking = new TableBooking
                    {
                        RestaurantId = id,
                        UserId = userId,
                        BookingDate = model.BookingDate!.Value.Date,
                        BookingTime = model.BookingTime!.Value,
                        GuestsCount = Math.Max(1, model.GuestsCount),
                        Status = "Pending"
                    };

                    _context.TableBookings.Add(booking);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[Booking] Created booking Id={booking.Id} for RestaurantId={id} on {booking.BookingDate:yyyy-MM-dd} {booking.BookingTime}");
                    TempData["Success"] = "Your table has been requested. We will confirm shortly.";
                    return RedirectToAction(nameof(Restaurant), new { id, date = model.BookingDate!.Value.ToString("yyyy-MM-dd") });
                }
            }

            model.AvailableSlots = BuildAvailableSlots(restaurant, model.BookingDate ?? DateTime.Today);
            if (!ModelState.IsValid)
            {
                // Log all model state errors server-side for quick diagnostics
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value!.Errors)
                    {
                        Console.WriteLine($"[Booking][ModelError] Key='{kv.Key}' Error='{err.ErrorMessage}'");
                    }
                }
            }
            return View(model);
        }

        [HttpPost("/Menu/Restaurant/{id}/Review")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int id, int rating, string? comment)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null || !restaurant.IsActive) return NotFound();

            rating = Math.Max(1, Math.Min(5, rating));
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!;
            var review = new RestaurantReview
            {
                RestaurantId = id,
                UserId = userId,
                Rating = rating,
                Comment = comment?.Trim()
            };
            _context.RestaurantReviews.Add(review);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thanks for your review!";
            return RedirectToAction(nameof(Restaurant), new { id });
        }

        private static List<TimeSpan> BuildAvailableSlots(Restaurant r, DateTime date)
        {
            var slots = new List<TimeSpan>();
            var start = r.OpeningTime;
            var end = r.ClosingTime;
            var step = TimeSpan.FromMinutes(30);
            for (var t = start; t <= end; t = t.Add(step))
            {
                slots.Add(t);
            }
            return slots;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View(menuItems);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Categories.ToListAsync();
            var restaurants = await _context.Restaurants.Where(r=>r.IsActive).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Restaurants = new SelectList(restaurants, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(MenuItemViewModel viewModel, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                var menuItem = new MenuItem
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Price = viewModel.Price,
                    ImageUrl = viewModel.ImageUrl,
                    CategoryId = viewModel.CategoryId,
                    RestaurantId = viewModel.RestaurantId,
                    IsAvailable = viewModel.IsAvailable
                };

                // Handle image upload if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
                    if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    menuItem.ImageUrl = $"/images/uploads/{fileName}";
                }

                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }

            var categories = await _context.Categories.ToListAsync();
            var restaurants = await _context.Restaurants.Where(r=>r.IsActive).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Restaurants = new SelectList(restaurants, "Id", "Name");
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            var viewModel = new MenuItemViewModel
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Description = menuItem.Description,
                Price = menuItem.Price,
                ImageUrl = menuItem.ImageUrl,
                CategoryId = menuItem.CategoryId,
                IsAvailable = menuItem.IsAvailable
            };

            var categories = await _context.Categories.ToListAsync();
            var restaurants = await _context.Restaurants.Where(r=>r.IsActive).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Restaurants = new SelectList(restaurants, "Id", "Name");
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, MenuItemViewModel viewModel, IFormFile? imageFile)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem == null)
                {
                    return NotFound();
                }

                menuItem.Name = viewModel.Name;
                menuItem.Description = viewModel.Description;
                menuItem.Price = viewModel.Price;
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
                    if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    menuItem.ImageUrl = $"/images/uploads/{fileName}";
                }
                else
                {
                    menuItem.ImageUrl = viewModel.ImageUrl;
                }
                menuItem.CategoryId = viewModel.CategoryId;
                menuItem.RestaurantId = viewModel.RestaurantId;
                menuItem.IsAvailable = viewModel.IsAvailable;

                _context.Update(menuItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            _context.MenuItems.Remove(menuItem);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }
    }
}
