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

        private long? CurrentUserId => User.Identity != null && User.Identity.IsAuthenticated && long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        // GET /Voucher
        public async Task<IActionResult> Index()
        {
            // Tự động thêm voucher mẫu nếu chưa có dữ liệu
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
            var vouchers = await _context.Vouchers
                .Include(v => v.Restaurant)
                .Where(v => v.IsActive && v.StartDate <= nowUtc && v.EndDate >= nowUtc)
                .OrderBy(v => v.MinOrderAmount)
                .ToListAsync();

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
                .FirstOrDefaultAsync(v => v.Code == code && v.IsActive);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã hết hạn." });
            }

            if (voucher.StartDate > now || voucher.EndDate < now)
            {
                return Json(new { success = false, message = "Mã giảm giá này chưa bắt đầu hoặc đã hết thời gian sử dụng." });
            }

            if (voucher.RestaurantId.HasValue && restaurantId.HasValue && voucher.RestaurantId.Value != restaurantId.Value)
            {
                return Json(new { success = false, message = "Mã giảm giá này chỉ áp dụng cho nhà hàng cụ thể." });
            }

            if (subtotal < voucher.MinOrderAmount)
            {
                return Json(new { 
                    success = false, 
                    message = $"Đơn hàng chưa đạt mức tối thiểu {voucher.MinOrderAmount:N0}đ để sử dụng mã này." 
                });
            }

            // Kiểm tra số lượt dùng cá nhân nếu đã đăng nhập
            if (CurrentUserId.HasValue)
            {
                var userUsageCount = await _context.VoucherUsages
                    .CountAsync(u => u.VoucherId == voucher.Id && u.UserId == CurrentUserId.Value);

                if (userUsageCount >= voucher.UsageLimitPerUser)
                {
                    return Json(new { success = false, message = $"Bạn đã dùng hết {voucher.UsageLimitPerUser} lượt cho phép của mã này." });
                }
            }

            // Tính số tiền được giảm
            decimal discount = 0;
            if (voucher.DiscountType == "percent" || voucher.DiscountType == "percentage")
            {
                discount = subtotal * (voucher.DiscountValue / 100m);
                if (voucher.MaxDiscount.HasValue && discount > voucher.MaxDiscount.Value)
                {
                    discount = voucher.MaxDiscount.Value;
                }
            }
            else // "fixed_amount" or "free_shipping"
            {
                discount = voucher.DiscountValue;
            }

            if (discount > subtotal)
            {
                discount = subtotal;
            }

            return Json(new {
                success = true,
                code = voucher.Code,
                discount = discount,
                description = voucher.Description,
                message = $"Áp dụng mã {voucher.Code} thành công! Đã giảm {discount:N0}đ."
            });
        }
    }
}
