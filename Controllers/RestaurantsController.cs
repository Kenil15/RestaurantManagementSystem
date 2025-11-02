using ForUpworkRestaurentManagement.Data;
using ForUpworkRestaurentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForUpworkRestaurentManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RestaurantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public RestaurantsController(ApplicationDbContext context) { _context = context; }

        [AllowAnonymous]
        public async Task<IActionResult> Browse()
        {
            var data = await _context.Restaurants.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.Restaurants.OrderBy(r => r.Name).ToListAsync();
            return View(data);
        }

        public IActionResult Create() => View(new Restaurant());

        [HttpPost]
        public async Task<IActionResult> Create(Restaurant model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Restaurants.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var r = await _context.Restaurants.FindAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Restaurant model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.Restaurants.FindAsync(id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var r = await _context.Restaurants.FindAsync(id);
            if (r != null)
            {
                _context.Restaurants.Remove(r);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}


