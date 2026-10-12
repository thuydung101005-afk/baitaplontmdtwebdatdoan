using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Data;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly FoodieDbContext _context;

        public AdminController(FoodieDbContext context)
        {
            _context = context;
        }

        // GET /Admin - danh sách nhà hàng chờ duyệt
        public async Task<IActionResult> Index()
        {
            var pendingRestaurants = await _context.Restaurants
                .Where(r => r.Status == "pending_approval")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var approvedRestaurants = await _context.Restaurants
                .Where(r => r.Status != "pending_approval")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.PendingRestaurants = pendingRestaurants;
            ViewBag.ApprovedRestaurants = approvedRestaurants;

            return View();
        }

        // POST /Admin/ApproveRestaurant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurant(long id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            restaurant.Status = "open";
            restaurant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt nhà hàng \"{restaurant.Name}\".";
            return RedirectToAction("Index");
        }

        // POST /Admin/RejectRestaurant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRestaurant(long id, string reason)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null) return NotFound();

            restaurant.Status = "closed";
            restaurant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã từ chối nhà hàng \"{restaurant.Name}\".";
            return RedirectToAction("Index");
        }
    }
}