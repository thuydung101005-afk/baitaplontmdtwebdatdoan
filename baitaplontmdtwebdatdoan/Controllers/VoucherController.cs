using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class VoucherController : Controller
    {
        private readonly FoodieDbContext _context;

        public VoucherController(FoodieDbContext context)
        {
            _context = context;
        }

        private long? CurrentUserId =>
            User.Identity != null && User.Identity.IsAuthenticated && long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : null;

        // GET /Voucher - Kho Voucher cho Khách hàng
        public async Task<IActionResult> Index(string? filter = "all")
        {
            if (!await _context.Vouchers.AnyAsync())
            {
                var now = DateTime.UtcNow;
                var sampleVouchers = new List<Voucher>
                {
                    new Voucher
                    {
                        Code = "FREESHIP",
                        Description = "Miễn phí vận chuyển 15.000đ cho đơn hàng từ 50.000đ",
                        DiscountType = "free_shipping",
                        DiscountValue = 15000,
                        MinOrderAmount = 50000,
                        UsageLimit = 500,
                        UsageLimitPerUser = 5,
                        StartDate = now.AddDays(-5),
                        EndDate = now.AddMonths(2),
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "FOODIE50",
                        Description = "Giảm trực tiếp 50.000đ cho đơn hàng từ 150.000đ",
                        DiscountType = "fixed_amount",
                        DiscountValue = 50000,
                        MinOrderAmount = 150000,
                        UsageLimit = 200,
                        UsageLimitPerUser = 2,
                        StartDate = now.AddDays(-5),
                        EndDate = now.AddMonths(1),
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "WELCOME10",
                        Description = "Giảm 10% (tối đa 30.000đ) cho đơn hàng từ 60.000đ",
                        DiscountType = "percent",
                        DiscountValue = 10,
                        MaxDiscount = 30000,
                        MinOrderAmount = 60000,
                        UsageLimit = 1000,
                        UsageLimitPerUser = 3,
                        StartDate = now.AddDays(-5),
                        EndDate = now.AddMonths(3),
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "TIECVUI20",
                        Description = "Ưu đãi đặt món nhóm giảm 20% (tối đa 60.000đ) đơn từ 200.000đ",
                        DiscountType = "percent",
                        DiscountValue = 20,
                        MaxDiscount = 60000,
                        MinOrderAmount = 200000,
                        UsageLimit = 300,
                        UsageLimitPerUser = 2,
                        StartDate = now.AddDays(-5),
                        EndDate = now.AddMonths(2),
                        IsActive = true
                    }
                };

                _context.Vouchers.AddRange(sampleVouchers);
                await _context.SaveChangesAsync();
            }

            var nowUtc = DateTime.UtcNow;
            var query = _context.Vouchers
                .Include(v => v.Restaurant)
                .Include(v => v.VoucherUsages)
                .Where(v => v.IsActive && v.StartDate <= nowUtc && v.EndDate >= nowUtc);

            if (filter == "freeship")
            {
                query = query.Where(v => v.DiscountType == "free_shipping");
            }
            else if (filter == "platform")
            {
                query = query.Where(v => v.RestaurantId == null && v.DiscountType != "free_shipping");
            }
            else if (filter == "restaurant")
            {
                query = query.Where(v => v.RestaurantId != null);
            }

            var vouchers = await query.OrderBy(v => v.MinOrderAmount).ToListAsync();
            ViewBag.CurrentFilter = filter ?? "all";
            return View(vouchers);
        }

        // POST /Voucher/ValidateVoucher
        [HttpPost]
        public async Task<IActionResult> ValidateVoucher(string code, decimal subtotal, long? restaurantId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            code = code.Trim().ToUpper();
            var now = DateTime.UtcNow;

            var voucher = await _context.Vouchers
                .Include(v => v.Restaurant)
                .FirstOrDefaultAsync(v => v.Code == code && v.IsActive);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã ngừng hoạt động." });
            }

            if (voucher.StartDate > now || voucher.EndDate < now)
            {
                return Json(new { success = false, message = "Mã giảm giá này chưa tới đợt hoặc đã hết hạn sử dụng." });
            }

            if (voucher.RestaurantId.HasValue && restaurantId.HasValue && voucher.RestaurantId.Value != restaurantId.Value)
            {
                return Json(new
                {
                    success = false,
                    message = $"Mã này chỉ áp dụng riêng cho quán \"{voucher.Restaurant?.Name ?? "nhà hàng chỉ định"}\"."
                });
            }

            if (subtotal < voucher.MinOrderAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Đơn hàng cần tối thiểu {voucher.MinOrderAmount:N0}đ để áp dụng mã này (còn thiếu {(voucher.MinOrderAmount - subtotal):N0}đ)."
                });
            }

            var totalUsages = await _context.VoucherUsages.CountAsync(u => u.VoucherId == voucher.Id);
            if (voucher.UsageLimit.HasValue && totalUsages >= voucher.UsageLimit.Value)
            {
                return Json(new { success = false, message = "Mã giảm giá này đã hết tổng số lượt sử dụng trên toàn sàn." });
            }

            if (CurrentUserId.HasValue)
            {
                var userUsageCount = await _context.VoucherUsages
                    .CountAsync(u => u.VoucherId == voucher.Id && u.UserId == CurrentUserId.Value);

                if (userUsageCount >= voucher.UsageLimitPerUser)
                {
                    return Json(new { success = false, message = $"Bạn đã sử dụng hết {voucher.UsageLimitPerUser} lượt cho phép của mã này." });
                }
            }

            decimal discount = 0;
            const decimal defaultShippingFee = 15000m;
            if (voucher.DiscountType == "percent" || voucher.DiscountType == "percentage")
            {
                discount = subtotal * (voucher.DiscountValue / 100m);
                if (voucher.MaxDiscount.HasValue && discount > voucher.MaxDiscount.Value)
                {
                    discount = voucher.MaxDiscount.Value;
                }
                if (discount > subtotal) discount = subtotal;
            }
            else if (voucher.DiscountType == "free_shipping")
            {
                discount = Math.Min(defaultShippingFee, voucher.DiscountValue);
            }
            else
            {
                discount = Math.Min(subtotal, voucher.DiscountValue);
            }

            return Json(new
            {
                success = true,
                code = voucher.Code,
                discount = discount,
                discountType = voucher.DiscountType,
                description = voucher.Description,
                message = $"Áp dụng mã {voucher.Code} thành công! Được giảm {discount:N0}đ."
            });
        }

        // ===== QUẢN LÝ VOUCHER DÀNH CHO ADMIN (TOÀN SÀN) & CHỦ QUÁN (VOUCHER QUÁN) =====
        [Authorize(Roles = "admin,restaurant_owner")]
        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var isAdmin = User.IsInRole("admin");
            var uid = CurrentUserId!.Value;

            List<Restaurant> restaurants;
            IQueryable<Voucher> query = _context.Vouchers
                .Include(v => v.Restaurant)
                .Include(v => v.VoucherUsages);

            if (isAdmin)
            {
                restaurants = await _context.Restaurants.Where(r => r.DeletedAt == null).OrderBy(r => r.Name).ToListAsync();
            }
            else
            {
                restaurants = await _context.Restaurants.Where(r => r.OwnerId == uid && r.DeletedAt == null).ToListAsync();
                var myRestIds = restaurants.Select(r => r.Id).ToList();
                query = query.Where(v => v.RestaurantId.HasValue && myRestIds.Contains(v.RestaurantId.Value));
            }

            var vouchers = await query.OrderByDescending(v => v.Id).ToListAsync();
            ViewBag.Restaurants = restaurants;
            ViewBag.IsAdmin = isAdmin;
            return View(vouchers);
        }

        // POST /Voucher/Create
        [Authorize(Roles = "admin,restaurant_owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string code,
            string? description,
            string discountType,
            decimal discountValue,
            decimal? maxDiscount,
            decimal minOrderAmount,
            long? restaurantId,
            int? usageLimit,
            int usageLimitPerUser,
            DateTime startDate,
            DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(code) || discountValue <= 0)
            {
                TempData["Error"] = "Vui lòng nhập Mã Voucher và Giá trị giảm hợp lệ.";
                return RedirectToAction("Manage");
            }

            var cleanCode = code.Trim().ToUpper();
            if (await _context.Vouchers.AnyAsync(v => v.Code == cleanCode))
            {
                TempData["Error"] = $"Mã voucher \"{cleanCode}\" đã tồn tại trên hệ thống. Vui lòng chọn mã khác.";
                return RedirectToAction("Manage");
            }

            var isAdmin = User.IsInRole("admin");
            var uid = CurrentUserId!.Value;

            if (!isAdmin)
            {
                var myRest = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == uid && r.DeletedAt == null);
                if (myRest == null)
                {
                    TempData["Error"] = "Bạn cần tạo hồ sơ nhà hàng trước khi phát hành Voucher.";
                    return RedirectToAction("Manage");
                }
                restaurantId = myRest.Id;
            }
            else if (restaurantId.HasValue && restaurantId.Value <= 0)
            {
                restaurantId = null;
            }

            var validTypes = new[] { "percent", "fixed_amount", "free_shipping" };
            var type = validTypes.Contains(discountType) ? discountType : "fixed_amount";

            var newVoucher = new Voucher
            {
                Code = cleanCode,
                Description = string.IsNullOrWhiteSpace(description)
                    ? (type == "percent" ? $"Giảm {discountValue:0}% cho đơn từ {minOrderAmount:N0}đ" : $"Giảm {discountValue:N0}đ cho đơn từ {minOrderAmount:N0}đ")
                    : description.Trim(),
                DiscountType = type,
                DiscountValue = discountValue,
                MaxDiscount = type == "percent" ? maxDiscount : null,
                MinOrderAmount = Math.Max(0, minOrderAmount),
                RestaurantId = restaurantId,
                UsageLimit = usageLimit.HasValue && usageLimit.Value > 0 ? usageLimit.Value : 500,
                UsageLimitPerUser = Math.Max(1, usageLimitPerUser),
                StartDate = startDate == default ? DateTime.UtcNow.AddDays(-1) : startDate,
                EndDate = endDate == default || endDate <= startDate ? DateTime.UtcNow.AddMonths(1) : endDate,
                IsActive = true
            };

            _context.Vouchers.Add(newVoucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã phát hành mã khuyến mãi \"{cleanCode}\" thành công!";
            return RedirectToAction("Manage");
        }

        // POST /Voucher/Edit
        [Authorize(Roles = "admin,restaurant_owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id,
            string? description,
            string discountType,
            decimal discountValue,
            decimal? maxDiscount,
            decimal minOrderAmount,
            int? usageLimit,
            int usageLimitPerUser,
            DateTime endDate)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            var isAdmin = User.IsInRole("admin");
            var uid = CurrentUserId!.Value;
            if (!isAdmin)
            {
                var ownsRest = await _context.Restaurants.AnyAsync(r => r.Id == voucher.RestaurantId && r.OwnerId == uid);
                if (!ownsRest) return Forbid();
            }

            voucher.Description = description?.Trim();
            voucher.DiscountType = discountType;
            voucher.DiscountValue = discountValue;
            voucher.MaxDiscount = discountType == "percent" ? maxDiscount : null;
            voucher.MinOrderAmount = Math.Max(0, minOrderAmount);
            voucher.UsageLimit = usageLimit;
            voucher.UsageLimitPerUser = Math.Max(1, usageLimitPerUser);
            if (endDate != default) voucher.EndDate = endDate;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật voucher \"{voucher.Code}\" thành công!";
            return RedirectToAction("Manage");
        }

        // POST /Voucher/ToggleActive/5
        [Authorize(Roles = "admin,restaurant_owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(long id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            var isAdmin = User.IsInRole("admin");
            var uid = CurrentUserId!.Value;
            if (!isAdmin)
            {
                var ownsRest = await _context.Restaurants.AnyAsync(r => r.Id == voucher.RestaurantId && r.OwnerId == uid);
                if (!ownsRest) return Forbid();
            }

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã {(voucher.IsActive ? "kích hoạt" : "tạm khóa")} mã voucher \"{voucher.Code}\".";
            return RedirectToAction("Manage");
        }

        // POST /Voucher/Delete/5
        [Authorize(Roles = "admin,restaurant_owner")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var voucher = await _context.Vouchers.Include(v => v.VoucherUsages).FirstOrDefaultAsync(v => v.Id == id);
            if (voucher == null) return NotFound();

            var isAdmin = User.IsInRole("admin");
            var uid = CurrentUserId!.Value;
            if (!isAdmin)
            {
                var ownsRest = await _context.Restaurants.AnyAsync(r => r.Id == voucher.RestaurantId && r.OwnerId == uid);
                if (!ownsRest) return Forbid();
            }

            if (voucher.VoucherUsages.Any())
            {
                _context.VoucherUsages.RemoveRange(voucher.VoucherUsages);
            }
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa mã voucher \"{voucher.Code}\".";
            return RedirectToAction("Manage");
        }
    }
}
