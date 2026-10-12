using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class MenuController : Controller
    {
        private readonly FoodieDbContext _context;

        public MenuController(FoodieDbContext context)
        {
            _context = context;
        }

        // GET /Menu?category=Món chính
        public async Task<IActionResult> Index(string? category)
        {
            // Lấy toàn bộ tên danh mục (không trùng) để hiện thanh lọc
            var allCategories = await _context.MenuCategories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            // Lấy món ăn đang bán, thuộc nhà hàng đang mở cửa
            var query = _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .Where(m => m.IsAvailable == true && m.DeletedAt == null && m.Restaurant.Status == "open" && m.Restaurant.DeletedAt == null);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(m => m.Category != null && m.Category.Name == category);
            }

            var items = await query
                .OrderBy(m => m.Category!.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var vm = new MenuIndexViewModel
            {
                AllCategories = allCategories,
                SelectedCategory = category,
                Items = items
            };

            return View(vm);
        }

        // GET /Menu/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .Include(m => m.ItemOptionGroups)
                    .ThenInclude(g => g.ItemOptions)
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (item == null) return NotFound();

            var relatedItems = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .Where(m => m.RestaurantId == item.RestaurantId && m.Id != item.Id && m.IsAvailable && m.DeletedAt == null)
                .Take(4)
                .ToListAsync();

            var vm = new MenuItemDetailsViewModel
            {
                Item = item,
                RelatedItems = relatedItems
            };

            return View(vm);
        }

        // GET /Menu/GetItemQuickView/5 - trả về dữ liệu nhanh cho modal xem nhanh
        [HttpGet]
        public async Task<IActionResult> GetItemQuickView(long id)
        {
            var item = await _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (item == null) return NotFound();

            return Json(new
            {
                id = item.Id,
                name = item.Name,
                basePrice = item.BasePrice,
                description = item.Description ?? "Món ngon chất lượng được chế biến từ nguyên liệu tươi mới mỗi ngày.",
                imageUrl = string.IsNullOrEmpty(item.ImageUrl) ? "https://placehold.co/400x300?text=Foodie" : item.ImageUrl,
                isAvailable = item.IsAvailable,
                isBestSeller = item.IsBestSeller,
                restaurantId = item.RestaurantId,
                restaurantName = item.Restaurant?.Name ?? "Nhà hàng Foodie",
                restaurantAddress = item.Restaurant?.Address ?? "",
                categoryName = item.Category?.Name ?? "Món ngon"
            });
        }
    }
}