using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class FollowController : Controller
    {
        private readonly FoodieDbContext _context;

        public FollowController(FoodieDbContext context)
        {
            _context = context;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /Follow/Toggle
        [HttpPost]
        public async Task<IActionResult> Toggle(long restaurantId)
        {
            var existing = await _context.RestaurantFollowers
                .FirstOrDefaultAsync(f => f.UserId == CurrentUserId && f.RestaurantId == restaurantId);

            bool isFollowing;
            if (existing != null)
            {
                _context.RestaurantFollowers.Remove(existing);
                await _context.SaveChangesAsync();
                isFollowing = false;
            }
            else
            {
                var restaurant = await _context.Restaurants.FindAsync(restaurantId);
                if (restaurant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy quán ăn này." });
                }

                _context.RestaurantFollowers.Add(new RestaurantFollower
                {
                    UserId = CurrentUserId,
                    RestaurantId = restaurantId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                isFollowing = true;
            }

            var followersCount = await _context.RestaurantFollowers.CountAsync(f => f.RestaurantId == restaurantId);

            return Json(new
            {
                success = true,
                isFollowing,
                followersCount,
                message = isFollowing ? "Đã theo dõi quán thành công!" : "Đã hủy theo dõi quán."
            });
        }

        // GET /Follow/Status?restaurantId=1
        [HttpGet]
        public async Task<IActionResult> Status(long restaurantId)
        {
            var isFollowing = await _context.RestaurantFollowers
                .AnyAsync(f => f.UserId == CurrentUserId && f.RestaurantId == restaurantId);
            var followersCount = await _context.RestaurantFollowers
                .CountAsync(f => f.RestaurantId == restaurantId);

            return Json(new { isFollowing, followersCount });
        }

        // GET /Follow/MyFollows - Danh sách các quán đã theo dõi
        public async Task<IActionResult> MyFollows()
        {
            var follows = await _context.RestaurantFollowers
                .Include(f => f.Restaurant)
                .Where(f => f.UserId == CurrentUserId && f.Restaurant.DeletedAt == null)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(follows);
        }
    }
}
