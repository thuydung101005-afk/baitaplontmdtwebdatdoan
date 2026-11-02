using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class PaymentGatewayController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly BankSettings _bank;

        public PaymentGatewayController(
            FoodieDbContext context,
            IHubContext<OrderHub> orderHub,
            IOptions<BankSettings> bank)
        {
            _context = context;
            _orderHub = orderHub;
            _bank = bank.Value;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /PaymentGateway/Checkout?paymentId=10 (hoặc orderId=5)
        [HttpGet]
        public async Task<IActionResult> Checkout(long? paymentId = null, long? orderId = null)
        {
            Payment? payment = null;

            if (paymentId.HasValue && paymentId.Value > 0)
            {
                payment = await _context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o.Restaurant)
                    .Include(p => p.Order)
                        .ThenInclude(o => o.DeliveryAddress)
                    .FirstOrDefaultAsync(p => p.Id == paymentId.Value);
            }
            else if (orderId.HasValue && orderId.Value > 0)
            {
                payment = await _context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o.Restaurant)
                    .Include(p => p.Order)
                        .ThenInclude(o => o.DeliveryAddress)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId.Value);

                if (payment == null)
                {
                    var ord = await _context.Orders
                        .Include(o => o.Restaurant)
                        .Include(o => o.DeliveryAddress)
                        .FirstOrDefaultAsync(o => o.Id == orderId.Value);

                    if (ord != null && ord.UserId == CurrentUserId)
                    {
                        payment = new Payment
                        {
                            OrderId = ord.Id,
                            Provider = ord.PaymentMethod,
                            TransactionId = $"GW-{ord.PaymentMethod.ToUpper()}-{DateTime.UtcNow.Ticks.ToString().Substring(10)}",
                            Amount = ord.TotalAmount,
                            Status = ord.PaymentStatus == "paid" ? "escrow_held" : "pending",
                            CreatedAt = DateTime.UtcNow,
                            Order = ord
                        };
                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (payment == null || payment.Order == null)
                return NotFound();

            if (payment.Order.UserId != CurrentUserId && !User.IsInRole("admin"))
                return Forbid();

            if (payment.Order.PaymentStatus == "paid")
            {
                TempData["SuccessMessage"] = "Đơn hàng này đã được thanh toán qua Cổng trung gian FoodiePay.";
                return RedirectToAction("Details", "Order", new { id = payment.OrderId });
            }

            ViewBag.Bank = _bank;
            ViewBag.QrUrl = _bank.IsConfigured
                ? _bank.BuildQrUrl(payment.Amount, payment.Order.OrderCode)
                : $"https://img.vietqr.io/image/970436-1023127044-compact2.png?amount={(long)payment.Amount}&addInfo={Uri.EscapeDataString(payment.Order.OrderCode)}";

            return View(payment);
        }

        // POST /PaymentGateway/ProcessPayment - Xử lý kết quả thanh toán từ cổng trung gian
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(
            long paymentId,
            string decision,
            string? cardNumber = null,
            string? cardHolder = null,
            string? otpCode = null)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Restaurant)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null || payment.Order == null)
                return NotFound();

            if (payment.Order.UserId != CurrentUserId)
                return Forbid();

            var order = payment.Order;

            if (decision == "cancel")
            {
                payment.Status = "cancelled";
                payment.RawResponse = JsonSerializer.Serialize(new
                {
                    gateway = "FoodiePay Intermediary Gateway",
                    result = "USER_CANCELLED",
                    cancelledAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                TempData["Error"] = "Bạn đã hủy phiên thanh toán trực tuyến. Bạn có thể bấm 'Thanh toán ngay qua Cổng trung gian' bất cứ lúc nào để hoàn tất đơn hàng.";
                return RedirectToAction("Details", "Order", new { id = order.Id });
            }

            // Xác nhận thanh toán thành công -> Tiền vào Tài khoản Trung gian Sàn (Escrow Held)
            var maskedCard = !string.IsNullOrWhiteSpace(cardNumber) && cardNumber.Length >= 4
                ? "**** **** **** " + cardNumber.Trim().Substring(cardNumber.Trim().Length - 4)
                : "Verified Digital Account";

            payment.Status = "escrow_held";
            payment.RawResponse = JsonSerializer.Serialize(new
            {
                gateway = "FoodiePay Intermediary Escrow v2.0",
                transactionId = payment.TransactionId,
                provider = payment.Provider,
                amount = payment.Amount,
                payerAccount = maskedCard,
                cardHolder = cardHolder ?? User.Identity?.Name,
                escrowState = "HELD_BY_PLATFORM",
                commissionRate = "18%",
                gatewayFeeRate = "1.5%",
                paidAtUtc = DateTime.UtcNow
            });

            order.PaymentStatus = "paid";
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                ChangedAt = DateTime.UtcNow,
                Note = $"Cổng Trung Gian Thanh Toán FoodiePay xác nhận đã thu {payment.Amount:N0}đ (Mã GD: {payment.TransactionId}). Tiền đang được Sàn tạm giữ (Escrow) và sẽ giải ngân cho quán khi giao hàng hoàn tất."
            });

            await _context.SaveChangesAsync();

            await _orderHub.Clients.Group($"order-{order.Id}").SendAsync("PaymentUpdated", "paid");

            TempData["SuccessMessage"] = $"Thanh toán thành công {payment.Amount:N0}đ qua Cổng trung gian FoodiePay (Mã GD: {payment.TransactionId}). Khoản tiền của bạn được bảo vệ bởi cơ chế Giữ tiền Trung gian (Escrow)!";
            return RedirectToAction("Details", "Order", new { id = order.Id });
        }
    }
}

