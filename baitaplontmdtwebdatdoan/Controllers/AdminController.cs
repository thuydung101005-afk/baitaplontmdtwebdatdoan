using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;

        public AdminController(FoodieDbContext context, IHubContext<OrderHub> orderHub)
        {
            _context = context;
            _orderHub = orderHub;
        }

        private string CurrentAdminId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        // GET /Admin - Tổng quan hệ thống & Duyệt nhà hàng
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

            var completedOrders = await _context.Orders
                .Where(o => o.OrderStatus == "completed")
                .ToListAsync();

            var grossSales = completedOrders.Sum(o => o.Subtotal);
            var platformRevenue = Math.Round(grossSales * 0.195m, 0); // 18% hoa hồng + 1.5% phí TT

            var escrowHolding = await _context.Orders
                .Where(o => o.PaymentStatus == "paid" && o.OrderStatus != "completed" && o.OrderStatus != "cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var activeVoucherCount = await _context.Vouchers.CountAsync(v => v.IsActive);
            var unreadSupportCount = await _context.SupportMessages.CountAsync(m => m.SenderRole == "customer" && !m.IsRead);

            ViewBag.PendingRestaurants = pendingRestaurants;
            ViewBag.ApprovedRestaurants = approvedRestaurants;
            ViewBag.PlatformRevenue = platformRevenue;
            ViewBag.TotalGMV = completedOrders.Sum(o => o.TotalAmount);
            ViewBag.EscrowHolding = escrowHolding;
            ViewBag.ActiveVoucherCount = activeVoucherCount;
            ViewBag.UnreadSupportCount = unreadSupportCount;
            ViewBag.AwaitingPaymentCount = await _context.Payments
                .CountAsync(p => p.Status == "awaiting_admin" && p.Order.PaymentStatus == "unpaid" && p.Order.OrderStatus != "cancelled");

            return View();
        }

        // GET /Admin/Revenue - Báo cáo Doanh thu Sàn & Đối soát Trung gian Thanh toán (Escrow)
        public async Task<IActionResult> Revenue(string? period = "30days", long? restaurantId = null)
        {
            var allRestaurants = await _context.Restaurants
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewBag.AllRestaurants = allRestaurants;
            ViewBag.SelectedRestaurantId = restaurantId;

            var query = _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.User)
                .Include(o => o.Payments)
                .AsQueryable();

            if (restaurantId.HasValue && restaurantId.Value > 0)
            {
                query = query.Where(o => o.RestaurantId == restaurantId.Value);
            }

            var now = DateTime.UtcNow;
            int chartDays = 30;
            if (period == "today")
            {
                var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= startOfToday);
                chartDays = 7;
            }
            else if (period == "7days")
            {
                query = query.Where(o => o.CreatedAt >= now.AddDays(-7));
                chartDays = 7;
            }
            else if (period == "30days")
            {
                query = query.Where(o => o.CreatedAt >= now.AddDays(-30));
                chartDays = 30;
            }
            else
            {
                period = "all";
                chartDays = 30;
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var completed = orders.Where(o => o.OrderStatus == "completed").ToList();
            var cancelled = orders.Where(o => o.OrderStatus == "cancelled").ToList();
            var processing = orders.Where(o => o.OrderStatus != "completed" && o.OrderStatus != "cancelled").ToList();

            const decimal commissionRate = 18.0m;
            const decimal paymentFeeRate = 1.5m;

            decimal totalGrossFood = completed.Sum(o => o.Subtotal);
            decimal totalGMV = completed.Sum(o => o.TotalAmount);
            decimal totalShipping = completed.Sum(o => o.ShippingFee);
            decimal totalDiscount = completed.Sum(o => o.DiscountAmount);

            decimal totalCommission = Math.Round(totalGrossFood * (commissionRate / 100m), 0);
            decimal totalPaymentFee = Math.Round(totalGrossFood * (paymentFeeRate / 100m), 0);
            decimal totalPlatformNet = totalCommission + totalPaymentFee;
            decimal totalMerchantPayout = Math.Max(0, totalGrossFood - totalPlatformNet);

            // Tiền đang giữ tại cổng trung gian (Escrow Holding)
            var escrowOrders = processing.Where(o => o.PaymentStatus == "paid").ToList();
            decimal escrowHoldingAmount = escrowOrders.Sum(o => o.TotalAmount);
            decimal totalRefunded = orders.Where(o => o.PaymentStatus == "refunded").Sum(o => o.TotalAmount);

            // Biểu đồ theo ngày
            var dailyStats = new List<AdminDailyRevenuePoint>();
            for (int i = chartDays - 1; i >= 0; i--)
            {
                var targetDate = now.Date.AddDays(-i);
                var dayOrders = completed.Where(o => o.CreatedAt.Date == targetDate).ToList();
                var gross = dayOrders.Sum(o => o.Subtotal);
                var platRev = Math.Round(gross * ((commissionRate + paymentFeeRate) / 100m), 0);
                var merchNet = Math.Max(0, gross - platRev);

                dailyStats.Add(new AdminDailyRevenuePoint
                {
                    DateLabel = targetDate.ToString("dd/MM"),
                    GrossSales = gross,
                    PlatformRevenue = platRev,
                    MerchantPayout = merchNet,
                    OrderCount = dayOrders.Count
                });
            }

            // Bảng phân tích theo từng Nhà hàng
            var restaurantBreakdowns = allRestaurants.Select(r =>
            {
                var rOrders = completed.Where(o => o.RestaurantId == r.Id).ToList();
                var rGross = rOrders.Sum(o => o.Subtotal);
                var rComm = Math.Round(rGross * (commissionRate / 100m), 0);
                var rPay = Math.Round(rGross * (paymentFeeRate / 100m), 0);
                return new RestaurantRevenueBreakdown
                {
                    RestaurantId = r.Id,
                    RestaurantName = r.Name,
                    LogoUrl = r.LogoUrl,
                    CompletedOrders = rOrders.Count,
                    GrossSales = rGross,
                    CommissionFee = rComm,
                    PaymentFee = rPay,
                    PlatformEarned = rComm + rPay,
                    MerchantNetPayout = Math.Max(0, rGross - rComm - rPay),
                    AvgRating = r.AvgRating
                };
            })
            .OrderByDescending(x => x.PlatformEarned)
            .ThenByDescending(x => x.CompletedOrders)
            .ToList();

            // Sao kê từng đơn hàng kèm trạng thái Trung gian thanh toán
            var orderSettlements = orders.Take(100).Select(o =>
            {
                var comm = Math.Round(o.Subtotal * (commissionRate / 100m), 0);
                var payFee = Math.Round(o.Subtotal * (paymentFeeRate / 100m), 0);
                var net = Math.Max(0, o.Subtotal - comm - payFee);
                var lastPayment = o.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

                string escrowStatus;
                if (o.PaymentStatus == "refunded")
                    escrowStatus = "refunded";
                else if (o.OrderStatus == "completed")
                    escrowStatus = "settled";
                else if (o.PaymentStatus == "paid")
                    escrowStatus = "escrow_held";
                else
                    escrowStatus = "unpaid";

                return new AdminOrderSettlementItem
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    RestaurantName = o.Restaurant?.Name ?? "Nhà hàng",
                    CustomerName = o.User?.FullName ?? "Khách hàng",
                    CreatedAt = o.CreatedAt,
                    OrderStatus = o.OrderStatus,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    EscrowStatus = escrowStatus,
                    TransactionId = lastPayment?.TransactionId,
                    Subtotal = o.Subtotal,
                    ShippingFee = o.ShippingFee,
                    DiscountAmount = o.DiscountAmount,
                    TotalAmount = o.TotalAmount,
                    CommissionFee = comm,
                    PaymentFee = payFee,
                    PlatformRevenue = comm + payFee,
                    MerchantNetAmount = net
                };
            }).ToList();

            var vm = new AdminRevenueViewModel
            {
                Period = period ?? "30days",
                CommissionRate = commissionRate,
                PaymentFeeRate = paymentFeeRate,
                TotalGMV = totalGMV,
                TotalGrossFoodSales = totalGrossFood,
                TotalPlatformCommission = totalCommission,
                TotalPaymentGatewayFee = totalPaymentFee,
                TotalPlatformNetRevenue = totalPlatformNet,
                TotalMerchantPayout = totalMerchantPayout,
                TotalShippingFee = totalShipping,
                TotalVoucherDiscount = totalDiscount,
                EscrowHoldingAmount = escrowHoldingAmount,
                EscrowHoldingCount = escrowOrders.Count,
                TotalRefundedAmount = totalRefunded,
                CompletedOrderCount = completed.Count,
                ProcessingOrderCount = processing.Count,
                CancelledOrderCount = cancelled.Count,
                AverageOrderValue = completed.Count > 0 ? Math.Round(totalGMV / completed.Count, 0) : 0,
                DailyStats = dailyStats,
                RestaurantBreakdowns = restaurantBreakdowns,
                OrderSettlements = orderSettlements
            };

            return View(vm);
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

        // ===== ĐỐI SOÁT & XÁC NHẬN ĐÃ NHẬN TIỀN (thanh toán online) =====

        private static readonly string[] OnlineMethods = { "bank_transfer", "momo", "zalopay", "credit_card" };

        // GET /Admin/PaymentConfirmations - Danh sách giao dịch online chờ Admin xác nhận đã nhận tiền
        public async Task<IActionResult> PaymentConfirmations()
        {
            var payments = await _context.Payments
                .Include(p => p.Order).ThenInclude(o => o.Restaurant)
                .Include(p => p.Order).ThenInclude(o => o.User)
                .Where(p => (p.Status == "awaiting_admin" || p.Status == "pending")
                            && p.Order.PaymentStatus == "unpaid"
                            && p.Order.OrderStatus != "cancelled"
                            && p.Provider != null && OnlineMethods.Contains(p.Provider))
                .ToListAsync();

            // Mỗi đơn chỉ hiện giao dịch mới nhất; khách đã báo chuyển (awaiting_admin) lên trước.
            var rows = payments
                .GroupBy(p => p.OrderId)
                .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
                .OrderByDescending(p => p.Status == "awaiting_admin")
                .ThenBy(p => p.CreatedAt)
                .ToList();

            return View(rows);
        }

        // POST /Admin/ConfirmPaymentReceived/10 - Admin đã đối soát sao kê và xác nhận TIỀN ĐÃ VỀ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPaymentReceived(long paymentId, decimal? receivedAmount)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null || payment.Order == null) return NotFound();

            var order = payment.Order;

            if (order.PaymentStatus == "paid")
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã được xác nhận thanh toán trước đó.";
                return RedirectToAction("PaymentConfirmations");
            }

            if (order.OrderStatus == "cancelled")
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã bị huỷ, không thể xác nhận thanh toán.";
                return RedirectToAction("PaymentConfirmations");
            }

            // Bắt buộc nhập đúng số tiền thực nhận trên sao kê để tránh bấm nhầm / thiếu tiền.
            if (!receivedAmount.HasValue || receivedAmount.Value != payment.Amount)
            {
                TempData["Error"] = $"Số tiền thực nhận phải đúng bằng {payment.Amount:N0}đ của đơn {order.OrderCode}. Nếu khách chuyển thiếu/thừa, hãy từ chối và xử lý riêng.";
                return RedirectToAction("PaymentConfirmations");
            }

            var now = DateTime.UtcNow;
            payment.Status = "escrow_held";
            payment.RawResponse = JsonSerializer.Serialize(new
            {
                escrowState = "HELD_BY_PLATFORM",
                receivedConfirmedBy = "admin:" + CurrentAdminId,
                receivedAmount = receivedAmount.Value,
                receivedConfirmedAtUtc = now
            });

            order.PaymentStatus = "paid";
            order.UpdatedAt = now;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                ChangedAt = now,
                Note = $"Foodie đã đối soát và xác nhận NHẬN ĐƯỢC {receivedAmount.Value:N0}đ (Mã GD: {payment.TransactionId}). Tiền đang được Sàn tạm giữ (Escrow) và sẽ giải ngân cho quán khi giao hàng hoàn tất."
            });

            await _context.SaveChangesAsync();
            await _orderHub.Clients.Group($"order-{order.Id}").SendAsync("PaymentUpdated", "paid");

            TempData["SuccessMessage"] = $"Đã xác nhận nhận {receivedAmount.Value:N0}đ cho đơn {order.OrderCode}. Quán có thể xử lý đơn.";
            return RedirectToAction("PaymentConfirmations");
        }

        // POST /Admin/RejectPayment/10 - Admin KHÔNG thấy tiền về
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(long paymentId, string? reason)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null || payment.Order == null) return NotFound();

            var order = payment.Order;

            if (order.PaymentStatus == "paid")
            {
                TempData["Error"] = $"Đơn {order.OrderCode} đã được xác nhận thanh toán, không thể từ chối.";
                return RedirectToAction("PaymentConfirmations");
            }

            var note = string.IsNullOrWhiteSpace(reason) ? "Chưa nhận được tiền trong tài khoản" : reason.Trim();

            payment.Status = "pending";
            payment.RawResponse = JsonSerializer.Serialize(new
            {
                state = "ADMIN_NOT_RECEIVED",
                rejectedBy = "admin:" + CurrentAdminId,
                reason = note,
                rejectedAtUtc = DateTime.UtcNow
            });
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                ChangedAt = DateTime.UtcNow,
                Note = "Foodie chưa nhận được tiền: " + note + ". Khách vui lòng kiểm tra lại giao dịch và báo lại."
            });

            await _context.SaveChangesAsync();
            await _orderHub.Clients.Group($"order-{order.Id}").SendAsync("PaymentUpdated", "unpaid");

            TempData["SuccessMessage"] = $"Đã ghi nhận chưa nhận được tiền cho đơn {order.OrderCode}. Khách sẽ được báo để chuyển/báo lại.";
            return RedirectToAction("PaymentConfirmations");
        }
    }
}
