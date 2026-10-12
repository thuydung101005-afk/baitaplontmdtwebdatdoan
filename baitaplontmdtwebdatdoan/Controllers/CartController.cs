using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly FoodieDbContext _context;

        public CartController(FoodieDbContext context)
        {
            _context = context;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // POST /Cart/Add - thêm món vào giỏ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(long menuItemId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var item = await _context.MenuItems.FindAsync(menuItemId);
            if (item == null || !item.IsAvailable || item.DeletedAt != null)
            {
                TempData["Error"] = "Món ăn này hiện đang tạm ngưng phục vụ.";
                var prevUrl = Request.Headers["Referer"].ToString();
                return Redirect(string.IsNullOrEmpty(prevUrl) ? "/" : prevUrl);
            }

            var existingCart = await _context.CartItems
                .Where(c => c.UserId == CurrentUserId)
                .ToListAsync();

            // Giỏ hàng chỉ chứa món của 1 nhà hàng - nếu khác, làm mới giỏ
            if (existingCart.Any() && existingCart.First().RestaurantId != item.RestaurantId)
            {
                _context.CartItems.RemoveRange(existingCart);
                await _context.SaveChangesAsync();
                TempData["CartMessage"] = "Giỏ hàng đã được làm mới vì bạn chọn món từ nhà hàng khác.";
            }

            var existingLine = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == CurrentUserId && c.MenuItemId == menuItemId);

            if (existingLine != null)
            {
                existingLine.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = CurrentUserId,
                    RestaurantId = item.RestaurantId,
                    MenuItemId = item.Id,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thêm \"{item.Name}\" vào giỏ hàng.";

            var referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }

        // GET /Cart - xem giỏ hàng
        public async Task<IActionResult> Index()
        {
            var cartItems = await _context.CartItems
                .Include(c => c.MenuItem)
                .Where(c => c.UserId == CurrentUserId)
                .ToListAsync();

            return View(cartItems);
        }

        // POST /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(long id, int quantity)
        {
            var line = await _context.CartItems.FindAsync(id);
            if (line == null || line.UserId != CurrentUserId) return NotFound();

            if (quantity <= 0)
                _context.CartItems.Remove(line);
            else
                line.Quantity = quantity;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // POST /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(long id)
        {
            var line = await _context.CartItems.FindAsync(id);
            if (line == null || line.UserId != CurrentUserId) return NotFound();

            _context.CartItems.Remove(line);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        // POST /Cart/RemoveAjax
        [HttpPost]
        public async Task<IActionResult> RemoveAjax(long id)
        {
            var line = await _context.CartItems.FindAsync(id);
            if (line == null || line.UserId != CurrentUserId) return NotFound();

            _context.CartItems.Remove(line);
            await _context.SaveChangesAsync();

            var totalCount = await _context.CartItems
                .Where(c => c.UserId == CurrentUserId)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount = totalCount });
        }

        // POST /Cart/AddAjax - thêm vào giỏ qua AJAX, trả JSON
        [HttpPost]
        public async Task<IActionResult> AddAjax(long menuItemId, int quantity, bool forceNewCart = false)
        {
            if (quantity <= 0) quantity = 1;

            var item = await _context.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == menuItemId);

            if (item == null || !item.IsAvailable || item.DeletedAt != null)
            {
                return Json(new { success = false, message = "Món ăn này hiện đang tạm ngưng phục vụ." });
            }

            var existingCart = await _context.CartItems
                .Include(c => c.Restaurant)
                .Where(c => c.UserId == CurrentUserId)
                .ToListAsync();

            if (existingCart.Any() && existingCart.First().RestaurantId != item.RestaurantId)
            {
                if (!forceNewCart)
                {
                    var oldRestName = existingCart.First().Restaurant?.Name ?? "Quán khác";
                    var newRestName = item.Restaurant?.Name ?? "Quán này";
                    return Json(new
                    {
                        success = false,
                        needConfirmNewCart = true,
                        oldRestaurant = oldRestName,
                        newRestaurant = newRestName,
                        message = $"Giỏ hàng hiện đang có món từ \"{oldRestName}\". Bạn có muốn làm mới giỏ hàng để chọn món từ \"{newRestName}\" không?"
                    });
                }
                else
                {
                    _context.CartItems.RemoveRange(existingCart);
                    await _context.SaveChangesAsync();
                }
            }

            var existingLine = await _context.CartItems.FirstOrDefaultAsync(c =>
                c.UserId == CurrentUserId && c.MenuItemId == menuItemId);

            if (existingLine != null)
                existingLine.Quantity += quantity;
            else
                _context.CartItems.Add(new CartItem
                {
                    UserId = CurrentUserId,
                    RestaurantId = item.RestaurantId,
                    MenuItemId = item.Id,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            var totalCount = await _context.CartItems
                .Where(c => c.UserId == CurrentUserId)
                .SumAsync(c => c.Quantity);

            var cartLine = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.MenuItemId == menuItemId);

            return Json(new { success = true, cartCount = totalCount, cartItemId = cartLine?.Id, itemName = item.Name });
        }

        // GET /Cart/Count - lấy số lượng hiện tại trong giỏ
        [AllowAnonymous]
        public async Task<IActionResult> Count()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Json(new { cartCount = 0 });
            }

            var totalCount = await _context.CartItems
                .Where(c => c.UserId == CurrentUserId)
                .SumAsync(c => c.Quantity);
            return Json(new { cartCount = totalCount });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantityAjax(long id, string action)
        {
            var line = await _context.CartItems.FindAsync(id);
            if (line == null || line.UserId != CurrentUserId) return NotFound();

            bool removed = false;
            if (action == "increase")
            {
                line.Quantity++;
            }
            else
            {
                if (line.Quantity <= 1)
                {
                    _context.CartItems.Remove(line);
                    removed = true;
                }
                else
                {
                    line.Quantity--;
                }
            }

            await _context.SaveChangesAsync();

            var totalCount = await _context.CartItems
                .Where(c => c.UserId == CurrentUserId)
                .SumAsync(c => c.Quantity);

            return Json(new { quantity = removed ? 0 : line.Quantity, removed = removed, cartCount = totalCount });
        }
    }
}