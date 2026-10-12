using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class HomeController : Controller
    {
        private readonly FoodieDbContext _context;

        public HomeController(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.MenuCategories
                .Select(c => c.Name)
                .Distinct()
                .Take(8)
                .ToListAsync();

            var featuredItems = await _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.IsAvailable == true && m.DeletedAt == null)
                .OrderByDescending(m => m.IsBestSeller)
                .ThenByDescending(m => m.Id)
                .Take(5)
                .ToListAsync();

            var featuredRestaurants = await _context.Restaurants
                .Where(r => r.Status == "open" && r.DeletedAt == null)
                .OrderByDescending(r => r.AvgRating)
                .Take(5)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                Categories = categories,
                FeaturedItems = featuredItems,
                FeaturedRestaurants = featuredRestaurants
            };

            return View(vm);
        }

        // GET /Home/Search
        public async Task<IActionResult> Search(string? q, string? type, string? category, decimal? minPrice, decimal? maxPrice, string? sort)
        {
            q = q?.Trim();
            type = string.IsNullOrEmpty(type) ? "all" : type;

            var allCategories = await _context.MenuCategories
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            var itemQuery = _context.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Restaurant)
                .Where(m => m.IsAvailable == true && m.DeletedAt == null && m.Restaurant.Status == "open" && m.Restaurant.DeletedAt == null);

            var restQuery = _context.Restaurants
                .Where(r => r.Status == "open" && r.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(q))
            {
                itemQuery = itemQuery.Where(m => m.Name.Contains(q) || 
                                                (m.Description != null && m.Description.Contains(q)) || 
                                                (m.Category != null && m.Category.Name.Contains(q)));

                restQuery = restQuery.Where(r => r.Name.Contains(q) || 
                                                (r.Description != null && r.Description.Contains(q)) || 
                                                (r.Address != null && r.Address.Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                itemQuery = itemQuery.Where(m => m.Category != null && m.Category.Name == category);
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                itemQuery = itemQuery.Where(m => m.BasePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                itemQuery = itemQuery.Where(m => m.BasePrice <= maxPrice.Value);
            }

            itemQuery = sort switch
            {
                "price_asc" => itemQuery.OrderBy(m => m.BasePrice),
                "price_desc" => itemQuery.OrderByDescending(m => m.BasePrice),
                "bestseller" => itemQuery.OrderByDescending(m => m.IsBestSeller).ThenBy(m => m.Name),
                _ => itemQuery.OrderByDescending(m => m.IsBestSeller).ThenByDescending(m => m.Id)
            };

            restQuery = restQuery.OrderByDescending(r => r.AvgRating).ThenByDescending(r => r.TotalReviews);

            var items = await itemQuery.ToListAsync();
            var restaurants = await restQuery.ToListAsync();

            var vm = new SearchViewModel
            {
                Query = q,
                Type = type,
                Category = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Sort = sort,
                MenuItems = items,
                Restaurants = restaurants,
                AllCategories = allCategories
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}