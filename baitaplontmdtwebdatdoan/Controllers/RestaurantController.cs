using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly FoodieDbContext _context;

        public RestaurantController(FoodieDbContext context)
        {
            _context = context;
        }

        // GET /Restaurant  -> danh sách tất cả nhà hàng
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .Where(r => r.Status == "open" && r.DeletedAt == null)
                .OrderByDescending(r => r.AvgRating)
                .ToListAsync();

            return View(restaurants);
        }

        // GET /Restaurant/Details/1 -> chi tiết 1 quán + menu của quán đó + đánh giá
        public async Task<IActionResult> Details(long id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.RestaurantOperatingHours)
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (restaurant == null)
                return NotFound();

            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.RestaurantId == id && m.IsAvailable == true && m.DeletedAt == null)
                .OrderBy(m => m.Category!.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var categories = await _context.MenuCategories
                .Where(c => c.RestaurantId == id)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.RestaurantId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            var isFavorite = false;
            var isFollowing = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(userIdStr, out var uId))
                {
                    isFavorite = await _context.Favorites.AnyAsync(f => f.RestaurantId == id && f.UserId == uId);
                    isFollowing = await _context.RestaurantFollowers.AnyAsync(f => f.RestaurantId == id && f.UserId == uId);
                }
            }
            var followersCount = await _context.RestaurantFollowers.CountAsync(f => f.RestaurantId == id);

            ViewBag.IsFavorite = isFavorite;
            ViewBag.IsFollowing = isFollowing;
            ViewBag.FollowersCount = followersCount;

            var vm = new RestaurantDetailsViewModel
            {
                Restaurant = restaurant,
                MenuItems = menuItems,
                Categories = categories,
                Reviews = reviews
            };

            return View(vm);
        }
    }
}