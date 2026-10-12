using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;
using baitaplontmdtwebdatdoan.Hubs;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<OrderHub> _hub;

        public OrderController(FoodieDbContext context, IHubContext<OrderHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /Order/Checkout - xem lại giỏ trước khi đặt
        public async Task<IActionResult> Checkout(List<long>? selectedIds)
        {
            var query = _context.CartItems
                .Include(c => c.MenuItem)
                .Where(c => c.UserId == CurrentUserId);

            if (selectedIds != null && selectedIds.Any())
                query = query.Where(c => selectedIds.Contains(c.Id));

            var cartItems = await query.ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 món để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var restaurantId = cartItems.First().RestaurantId;
            var subtotal = cartItems.Sum(c => c.MenuItem.BasePrice * c.Quantity);

            var now = DateTime.UtcNow;
            var applicableVouchers = await _context.Vouchers
                .Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now &&
                            (v.RestaurantId == null || v.RestaurantId == restaurantId) &&
                            subtotal >= v.MinOrderAmount)
                .OrderByDescending(v => v.DiscountValue)
                .ToListAsync();

            var user = await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == CurrentUserId);

            var wallet = await _context.Wallets.FindAsync(CurrentUserId);
            var defaultAddress = user?.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).FirstOrDefault();

            ViewBag.ApplicableVouchers = applicableVouchers;
            ViewBag.RestaurantId = restaurantId;
            ViewBag.Subtotal = subtotal;
            ViewBag.CurrentUser = user;
            ViewBag.DefaultAddress = defaultAddress;
            ViewBag.UserAddresses = user?.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToList() ?? new List<Address>();
            ViewBag.WalletBalance = wallet?.Balance ?? 0;

            TempData["SelectedIds"] = string.Join(",", cartItems.Select(c => c.Id));
            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string receiverName,
            string receiverPhone,
            string addressLine,
            string? paymentMethod,
            string? note,
            string selectedIdsCsv,
            string? voucherCode,
            long? selectedAddressId = null)
        {
            if (string.IsNullOrWhiteSpace(selectedIdsCsv))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 món để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var ids = selectedIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => long.TryParse(s, out var id) ? id : 0)
                                    .Where(id => id > 0)
                                    .ToList();

            var cartItems = await _context.CartItems
                .Include(c => c.MenuItem)
                .Where(c => c.UserId == CurrentUserId && ids.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống hoặc món đã chọn không tồn tại.";
                return RedirectToAction("Index", "Cart");
            }

            var unavailable = cartItems.FirstOrDefault(c => !c.MenuItem.IsAvailable || c.MenuItem.DeletedAt != null);
            if (unavailable != null)
            {
                TempData["Error"] = $"Món \"{unavailable.MenuItem.Name}\" hiện đang tạm ngưng phục vụ.";
                return RedirectToAction("Checkout", new { selectedIds = ids });
            }

            Address? deliveryAddress = null;
            if (selectedAddressId.HasValue && selectedAddressId.Value > 0)
            {
                deliveryAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == selectedAddressId.Value && a.UserId == CurrentUserId);
            }

            if (deliveryAddress == null)
            {
                if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(receiverPhone) || string.IsNullOrWhiteSpace(addressLine))
                {
                    TempData["Error"] = "Vui lòng nhập đầy đủ thông tin: Họ tên, Số điện thoại và Địa chỉ giao hàng.";
                    return RedirectToAction("Checkout", new { selectedIds = ids });
                }

                // Lưu địa chỉ giao hàng
                deliveryAddress = new Address
                {
                    UserId = CurrentUserId,
                    ReceiverName = receiverName.Trim(),
                    ReceiverPhone = receiverPhone.Trim(),
                    AddressLine = addressLine.Trim(),
                    Latitude = 10.7850m,
                    Longitude = 106.6950m,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Addresses.Add(deliveryAddress);
                await _context.SaveChangesAsync();
            }

            var restaurantId = cartItems.First().RestaurantId;
            var subtotal = cartItems.Sum(c => c.MenuItem.BasePrice * c.Quantity);
            var shippingFee = 15000m;
            decimal discountAmount = 0;
            Voucher? appliedVoucher = null;

            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                var code = voucherCode.Trim().ToUpper();
                var now = DateTime.UtcNow;

                appliedVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == code && v.IsActive && v.StartDate <= now && v.EndDate >= now);

                if (appliedVoucher != null &&
                    (!appliedVoucher.RestaurantId.HasValue || appliedVoucher.RestaurantId.Value == restaurantId) &&
                    subtotal >= appliedVoucher.MinOrderAmount)
                {
                    var userUsageCount = await _context.VoucherUsages
                        .CountAsync(u => u.VoucherId == appliedVoucher.Id && u.UserId == CurrentUserId);

                    if (userUsageCount < appliedVoucher.UsageLimitPerUser)
                    {
                        if (appliedVoucher.DiscountType == "percent" || appliedVoucher.DiscountType == "percentage")
                        {
                            discountAmount = subtotal * (appliedVoucher.DiscountValue / 100m);
                            if (appliedVoucher.MaxDiscount.HasValue && discountAmount > appliedVoucher.MaxDiscount.Value)
                            {
                                discountAmount = appliedVoucher.MaxDiscount.Value;
                            }
                        }
                        else
                        {
                            discountAmount = appliedVoucher.DiscountValue;
                        }

                        if (discountAmount > subtotal)
                        {
                            discountAmount = subtotal;
                        }
                    }
                }
            }

            var totalAmount = Math.Max(0, subtotal + shippingFee - discountAmount);

            // Xử lý phương thức thanh toán
            var validMethods = new[] { "cod", "bank_transfer", "wallet", "credit_card", "momo", "zalopay" };
            var method = !string.IsNullOrEmpty(paymentMethod) && validMethods.Contains(paymentMethod.ToLower())
                ? paymentMethod.ToLower()
                : "cod";

            string paymentStatus = "unpaid";

            if (method == "wallet")
            {
                var wallet = await _context.Wallets.FindAsync(CurrentUserId);
                if (wallet == null || wallet.Balance < totalAmount)
                {
                    TempData["Error"] = "Số dư Ví Foodie không đủ để thanh toán. Vui lòng nạp thêm hoặc chọn phương thức khác.";
                    return RedirectToAction("Checkout", new { selectedIds = ids });
                }

                wallet.Balance -= totalAmount;
                paymentStatus = "paid";
            }
            else if (method == "credit_card" || method == "momo" || method == "zalopay")
            {
                // Giả lập thanh toán trực tuyến thành công
                paymentStatus = "paid";
            }

            var order = new Order
            {
                OrderCode = "DH" + DateTime.UtcNow.Ticks.ToString().Substring(10),
                UserId = CurrentUserId,
                RestaurantId = restaurantId,
                DeliveryAddressId = deliveryAddress.Id,
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                PaymentMethod = method,
                PaymentStatus = paymentStatus,
                OrderStatus = "pending",
                Note = note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Nếu thanh toán bằng ví, ghi nhận transaction
            if (method == "wallet")
            {
                _context.WalletTransactions.Add(new WalletTransaction
                {
                    UserId = CurrentUserId,
                    Type = "payment",
                    Amount = totalAmount,
                    ReferenceOrderId = order.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            if (appliedVoucher != null && discountAmount > 0)
            {
                _context.VoucherUsages.Add(new VoucherUsage
                {
                    VoucherId = appliedVoucher.Id,
                    UserId = CurrentUserId,
                    OrderId = order.Id,
                    UsedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            foreach (var c in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    MenuItemId = c.MenuItemId,
                    ItemName = c.MenuItem.Name,
                    UnitPrice = c.MenuItem.BasePrice,
                    Quantity = c.Quantity,
                    LineTotal = c.MenuItem.BasePrice * c.Quantity
                });
            }

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "pending",
                ChangedAt = DateTime.UtcNow,
                Note = "Đơn hàng được tạo thành công" + (discountAmount > 0 ? $" (Đã áp dụng mã {appliedVoucher?.Code} - giảm {discountAmount:N0}đ)" : "")
            });

            // Chỉ xoá đúng những món đã chọn thanh toán
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // Bắn SignalR thông báo tới quán ăn theo thời gian thực
            await _hub.Clients.Group($"restaurant-{restaurantId}").SendAsync("NewOrderReceived", order.Id, order.OrderCode, order.TotalAmount);

            return RedirectToAction("Details", new { id = order.Id });
        }

        // GET /Order/MyOrders
        public async Task<IActionResult> MyOrders(string? status)
        {
            var query = _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == CurrentUserId);

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "processing")
                {
                    query = query.Where(o => o.OrderStatus == "pending" || o.OrderStatus == "confirmed" || o.OrderStatus == "preparing");
                }
                else if (status == "shipping")
                {
                    query = query.Where(o => o.OrderStatus == "ready_for_pickup" || o.OrderStatus == "picked_up" || o.OrderStatus == "delivering");
                }
                else
                {
                    query = query.Where(o => o.OrderStatus == status);
                }
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // GET /Order/Details/5
        public async Task<IActionResult> Details(long id)
        {
            var order = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.OrderStatusHistories)
                .Include(o => o.Reviews)
                    .ThenInclude(r => r.User)
                .Include(o => o.Shipper)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId) return Forbid();

            ViewBag.ExistingReview = order.Reviews.FirstOrDefault(r => r.UserId == CurrentUserId);
            ViewBag.RestaurantLat = order.Restaurant?.Latitude ?? 10.7769m;
            ViewBag.RestaurantLng = order.Restaurant?.Longitude ?? 106.7009m;
            ViewBag.DeliveryLat = order.DeliveryAddress?.Latitude ?? 10.7850m;
            ViewBag.DeliveryLng = order.DeliveryAddress?.Longitude ?? 106.6950m;

            return View(order);
        }

        // POST /Order/ConfirmPayment/5 - khách xác nhận đã chuyển khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(long id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId) return Forbid();

            if (order.PaymentStatus == "paid")
            {
                TempData["Error"] = "Đơn hàng này đã được xác nhận thanh toán.";
                return RedirectToAction("Details", new { id });
            }

            order.PaymentStatus = "paid";
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                ChangedAt = DateTime.UtcNow,
                Note = "Khách hàng xác nhận đã chuyển khoản qua VietQR"
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{id}").SendAsync("PaymentUpdated", "paid");

            TempData["SuccessMessage"] = "Xác nhận thanh toán thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST /Order/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long id, string reason)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId) return Forbid();

            // KIỂM TRA NGHIÊM NGẶT: NẾU ĐƠN HÀNG ĐÃ/ĐANG GIAO, ĐANG NẤU, ĐÃ GIAO XONG THÌ TUYỆT ĐỐI KHÔNG ĐƯỢC HỦY
            var nonCancellable = new[] { "preparing", "ready_for_pickup", "picked_up", "delivering", "completed" };
            if (nonCancellable.Contains(order.OrderStatus))
            {
                TempData["Error"] = "Đơn hàng đang được nhà hàng chế biến hoặc đang trên đường giao, không thể hủy đơn lúc này! Quý khách vui lòng liên hệ trực tiếp quán nếu cần hỗ trợ khẩn cấp.";
                return RedirectToAction("Details", new { id });
            }

            if (order.OrderStatus != "pending" && order.OrderStatus != "confirmed")
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái hợp lệ để có thể hủy.";
                return RedirectToAction("Details", new { id });
            }

            // Nếu đã thanh toán bằng ví điện tử, tự động hoàn trả số dư vào ví Foodie
            if (order.PaymentStatus == "paid" && order.PaymentMethod == "wallet")
            {
                var wallet = await _context.Wallets.FindAsync(CurrentUserId);
                if (wallet != null)
                {
                    wallet.Balance += order.TotalAmount;
                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        UserId = CurrentUserId,
                        Type = "refund",
                        Amount = order.TotalAmount,
                        ReferenceOrderId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                    order.PaymentStatus = "refunded";
                }
            }

            order.OrderStatus = "cancelled";
            order.CancelReason = string.IsNullOrWhiteSpace(reason) ? "Khách hàng yêu cầu hủy đơn" : reason.Trim();
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "cancelled",
                ChangedAt = DateTime.UtcNow,
                Note = "Khách hàng huỷ đơn: " + order.CancelReason + (order.PaymentStatus == "refunded" ? $" (Đã tự động hoàn {order.TotalAmount:N0}đ vào Ví Foodie)" : "")
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{id}").SendAsync("StatusUpdated", "cancelled", "Đơn hàng đã bị huỷ");

            TempData["SuccessMessage"] = "Đã huỷ đơn hàng thành công." + (order.PaymentStatus == "refunded" ? $" Đã hoàn {order.TotalAmount:N0}đ vào Ví của bạn." : "");
            return RedirectToAction("Details", new { id });
        }

        // POST /Order/SubmitReview - Đánh giá bình luận sau khi mua hàng xong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(long orderId, byte rating, string? comment)
        {
            var order = await _context.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId) return Forbid();

            if (order.OrderStatus != "completed")
            {
                TempData["Error"] = "Chỉ có thể đánh giá sau khi đơn hàng đã hoàn thành giao nhận.";
                return RedirectToAction("Details", new { id = orderId });
            }

            rating = Math.Clamp(rating, (byte)1, (byte)5);

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.UserId == CurrentUserId);

            if (existingReview != null)
            {
                existingReview.Rating = rating;
                existingReview.Comment = comment?.Trim();
                existingReview.CreatedAt = DateTime.UtcNow;
                TempData["SuccessMessage"] = "Đã cập nhật đánh giá của bạn thành công!";
            }
            else
            {
                var newReview = new Review
                {
                    OrderId = order.Id,
                    UserId = CurrentUserId,
                    RestaurantId = order.RestaurantId,
                    Rating = rating,
                    Comment = comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Reviews.Add(newReview);
                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá! Nhận xét của bạn sẽ giúp quán phục vụ tốt hơn.";
            }

            await _context.SaveChangesAsync();

            // Cập nhật lại Rating trung bình và số lượng review của nhà hàng
            var allReviews = await _context.Reviews
                .Where(r => r.RestaurantId == order.RestaurantId)
                .ToListAsync();

            if (allReviews.Any())
            {
                var rest = await _context.Restaurants.FindAsync(order.RestaurantId);
                if (rest != null)
                {
                    rest.TotalReviews = allReviews.Count;
                    rest.AvgRating = Math.Round((decimal)allReviews.Average(r => (double)r.Rating), 1);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Details", new { id = orderId });
        }

        // POST /Order/RequestRefund/5 - khách yêu cầu hoàn tiền sau khi đã nhận hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefund(long id, string reason)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.UserId != CurrentUserId) return Forbid();

            if (order.OrderStatus != "completed")
            {
                TempData["Error"] = "Chỉ có thể yêu cầu hoàn tiền cho đơn đã giao thành công.";
                return RedirectToAction("Details", new { id });
            }

            if (order.PaymentStatus == "refunded")
            {
                TempData["Error"] = "Đơn hàng này đã được hoàn tiền trước đó.";
                return RedirectToAction("Details", new { id });
            }

            order.PaymentStatus = "refunded";
            order.UpdatedAt = DateTime.UtcNow;

            var wallet = await _context.Wallets.FindAsync(CurrentUserId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = CurrentUserId, Balance = 0 };
                _context.Wallets.Add(wallet);
            }
            wallet.Balance += order.TotalAmount;

            _context.WalletTransactions.Add(new WalletTransaction
            {
                UserId = CurrentUserId,
                Type = "refund",
                Amount = order.TotalAmount,
                ReferenceOrderId = order.Id,
                CreatedAt = DateTime.UtcNow
            });

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.OrderStatus,
                ChangedAt = DateTime.UtcNow,
                Note = "Khách hàng yêu cầu hoàn tiền: " + reason
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{id}").SendAsync("PaymentUpdated", "refunded");

            TempData["SuccessMessage"] = $"Đã hoàn {order.TotalAmount:N0}đ vào Ví Foodie của bạn.";
            return RedirectToAction("Details", new { id });
        }

        // ===== CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (DÀNH CHO CHỦ QUÁN HOẶC ADMIN) =====
        [Authorize(Roles = "restaurant_owner,admin")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.DeliveryAddress)
                .Where(o => o.OrderStatus != "completed" && o.OrderStatus != "cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "restaurant_owner,admin")]
        public async Task<IActionResult> UpdateStatus(long id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                ChangedAt = DateTime.UtcNow,
                Note = "Cập nhật trạng thái: " + GetStatusLabel(newStatus)
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{id}").SendAsync("StatusUpdated", newStatus, GetStatusLabel(newStatus));

            return RedirectToAction("Manage");
        }

        private string GetStatusLabel(string status) => status switch
        {
            "pending" => "Chờ xác nhận",
            "confirmed" => "Đã xác nhận",
            "preparing" => "Đang chuẩn bị",
            "ready_for_pickup" => "Sẵn sàng giao",
            "picked_up" => "Đã lấy hàng",
            "delivering" => "Đang giao",
            "completed" => "Hoàn thành",
            "cancelled" => "Đã huỷ",
            _ => status
        };
    }
}