using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly FoodieDbContext _context;

        public FavoriteController(FoodieDbContext context)
        {
            _context = context;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /Favorite
        public async Task<IActionResult> Index()
        {
            var favorites = await _context.Favorites
                .Include(f => f.Restaurant)
                .Where(f => f.UserId == CurrentUserId && f.Restaurant.DeletedAt == null)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites);
        }

        // POST /Favorite/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle(long restaurantId)
        {
            var existing = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == CurrentUserId && f.RestaurantId == restaurantId);

            bool isFavorite;
            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                await _context.SaveChangesAsync();
                isFavorite = false;
            }
            else
            {
                var restaurant = await _context.Restaurants.FindAsync(restaurantId);
                if (restaurant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy quán ăn này." });
                }

                _context.Favorites.Add(new Favorite
                {
                    UserId = CurrentUserId,
                    RestaurantId = restaurantId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                isFavorite = true;
            }

            var totalFavorites = await _context.Favorites.CountAsync(f => f.UserId == CurrentUserId);

            return Json(new
            {
                success = true,
                isFavorite,
                totalFavorites,
                message = isFavorite ? "Đã thêm vào danh sách yêu thích!" : "Đã xoá khỏi danh sách yêu thích."
            });
        }

        // GET /Favorite/GetCount
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var count = await _context.Favorites.CountAsync(f => f.UserId == CurrentUserId);
            return Json(new { count });
        }

        // GET /Favorite/Check?restaurantId=1
        [HttpGet]
        public async Task<IActionResult> Check(long restaurantId)
        {
            var isFav = await _context.Favorites.AnyAsync(f => f.UserId == CurrentUserId && f.RestaurantId == restaurantId);
            return Json(new { isFavorite = isFav });
        }
    }
}
