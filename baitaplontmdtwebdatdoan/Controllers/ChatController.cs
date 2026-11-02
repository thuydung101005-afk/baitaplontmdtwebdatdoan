using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<ChatHub> _chatHub;

        public ChatController(FoodieDbContext context, IHubContext<ChatHub> chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static readonly TimeZoneInfo VnTimeZone = ResolveVnTimeZone();

        private static TimeZoneInfo ResolveVnTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
                catch { return TimeZoneInfo.Utc; }
            }
        }

        private static string FormatTime(DateTime utc)
        {
            var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(asUtc, VnTimeZone).ToString("HH:mm");
        }

        // GET /Chat/GetMessages?restaurantId=1&customerId=2
        [HttpGet]
        public async Task<IActionResult> GetMessages(long restaurantId, long? customerId)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
                return NotFound(new { success = false, message = "Không tìm thấy quán ăn" });

            var isAdmin = User.IsInRole("admin");
            var isOwner = CurrentUserId == restaurant.OwnerId;

            long targetCustomerId;
            if ((isOwner || isAdmin) && customerId.HasValue && customerId.Value > 0 && customerId.Value != restaurant.OwnerId)
            {
                targetCustomerId = customerId.Value;
            }
            else if (isOwner && (!customerId.HasValue || customerId.Value == 0 || customerId.Value == restaurant.OwnerId))
            {
                return Json(new { success = true, isOwnerSelf = true, messages = new List<object>() });
            }
            else
            {
                targetCustomerId = CurrentUserId;
            }

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.RestaurantId == restaurantId && m.CustomerId == targetCustomerId)
                .OrderBy(m => m.CreatedAt)
                .Take(150)
                .ToListAsync();

            var viewerRole = (isOwner || (isAdmin && targetCustomerId != CurrentUserId)) ? "restaurant" : "customer";
            var unread = messages.Where(m => !m.IsRead && m.SenderRole != viewerRole).ToList();
            if (unread.Any())
            {
                foreach (var u in unread) u.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var result = messages.Select(m => new
            {
                id = m.Id,
                senderId = m.SenderId,
                senderRole = m.SenderRole,
                senderName = m.SenderRole == "restaurant" ? restaurant.Name : (m.Sender?.FullName ?? "Khách hàng"),
                message = m.Message,
                time = FormatTime(m.CreatedAt),
                isMe = m.SenderRole == viewerRole
            });

            return Json(new { success = true, messages = result });
        }

        // POST /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(long restaurantId, long? customerId, string message, string? mode = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Tin nhắn không được để trống." });
            }

            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy quán ăn." });
            }

            var isAdmin = User.IsInRole("admin");
            var isOwner = CurrentUserId == restaurant.OwnerId;

            string senderRole;
            long targetCustomerId;

            if (mode == "customer")
            {
                // Giao diện của khách: customerId phải đúng là người đang đăng nhập
                if (customerId.HasValue && customerId.Value != CurrentUserId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tài khoản đăng nhập đã thay đổi so với trang này. Vui lòng tải lại trang (F5) rồi gửi lại."
                    });
                }
                if (isOwner)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn là chủ quán ăn này. Vui lòng vào mục 'Tin nhắn khách hàng' (Shop Inbox) để trả lời khách nhé!"
                    });
                }

                senderRole = "customer";
                targetCustomerId = CurrentUserId;
            }
            else if (mode == "restaurant")
            {
                // Giao diện của quán (ShopInbox): chỉ chủ quán hoặc admin
                if (!(isOwner || isAdmin) || !customerId.HasValue || customerId.Value <= 0 || customerId.Value == restaurant.OwnerId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền trả lời cuộc trò chuyện này." });
                }

                senderRole = "restaurant";
                targetCustomerId = customerId.Value;
            }
            else
            {
                return Json(new { success = false, message = "Yêu cầu không hợp lệ. Vui lòng tải lại trang." });
            }

            var senderUser = await _context.Users.FindAsync(CurrentUserId);
            var customerUser = targetCustomerId == CurrentUserId
                ? senderUser
                : await _context.Users.FindAsync(targetCustomerId);

            var now = DateTime.UtcNow;
            var chatMsg = new ChatMessage
            {
                SenderId = CurrentUserId,
                RestaurantId = restaurantId,
                CustomerId = targetCustomerId,
                SenderRole = senderRole,
                Message = message.Trim(),
                IsRead = false,
                CreatedAt = now
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            var senderName = senderRole == "restaurant"
                ? restaurant.Name
                : (senderUser?.FullName ?? User.Identity?.Name ?? "Khách hàng");
            var customerName = customerUser?.FullName ?? "Khách hàng";
            var timeStr = FormatTime(now);

            // 1. Bắn SignalR tới phòng chat đang mở
            await _chatHub.Clients.Group($"chat-{restaurantId}-{targetCustomerId}")
                .SendAsync("ReceiveMessage", senderRole, senderName, chatMsg.Message, timeStr);

            // 2. Bắn SignalR cập nhật danh sách bên trái của ShopInbox (Chủ quán & Admin)
            var inboxPayload = new
            {
                restaurantId = restaurant.Id,
                restaurantName = restaurant.Name,
                customerId = targetCustomerId,
                customerName = customerName,
                senderRole = senderRole,
                message = chatMsg.Message,
                time = timeStr
            };
            await _chatHub.Clients.Group($"shop-owner-{restaurant.OwnerId}")
                .SendAsync("ShopInboxUpdated", inboxPayload);
            await _chatHub.Clients.Group("admin-shop-inbox")
                .SendAsync("ShopInboxUpdated", inboxPayload);

            // 3. Tự động trả lời thông minh (Auto-reply) nếu khách nhắn tin lần đầu cho quán
            if (senderRole == "customer")
            {
                var previousMsgCount = await _context.ChatMessages
                    .CountAsync(m => m.RestaurantId == restaurantId && m.CustomerId == targetCustomerId && m.Id != chatMsg.Id);

                if (previousMsgCount == 0)
                {
                    var autoReplyText = $"Dạ chào {customerName}! Cảm ơn bạn đã nhắn tin cho {restaurant.Name}. Quán đã nhận được tin nhắn và sẽ phản hồi bạn ngay ạ. Bạn có thể xem thực đơn hoặc áp mã giảm giá của quán trong lúc chờ nhé! ❤️";
                    var autoReply = new ChatMessage
                    {
                        SenderId = restaurant.OwnerId,
                        RestaurantId = restaurantId,
                        CustomerId = targetCustomerId,
                        SenderRole = "restaurant",
                        Message = autoReplyText,
                        IsRead = true,
                        CreatedAt = DateTime.UtcNow.AddSeconds(1)
                    };
                    _context.ChatMessages.Add(autoReply);
                    await _context.SaveChangesAsync();

                    var autoTime = FormatTime(autoReply.CreatedAt);
                    await _chatHub.Clients.Group($"chat-{restaurantId}-{targetCustomerId}")
                        .SendAsync("ReceiveMessage", "restaurant", restaurant.Name, autoReply.Message, autoTime);
                }
            }

            return Json(new { success = true, time = timeStr });
        }

        // GET /Chat/MyChats - Hộp thư của khách: giao diện nhắn tin trực tiếp với các quán
        [HttpGet]
        public async Task<IActionResult> MyChats(long? restaurantId = null)
        {
            var messages = await _context.ChatMessages
                .Include(m => m.Restaurant)
                .Where(m => m.CustomerId == CurrentUserId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.RestaurantId)
                .Select(g =>
                {
                    var last = g.First();
                    return new ChatConversationViewModel
                    {
                        RestaurantId = g.Key,
                        RestaurantName = last.Restaurant?.Name ?? "Nhà hàng",
                        RestaurantLogoUrl = last.Restaurant?.LogoUrl,
                        LastMessage = last.Message,
                        LastSenderRole = last.SenderRole,
                        LastTimeUtc = last.CreatedAt,
                        UnreadCount = g.Count(x => !x.IsRead && x.SenderRole == "restaurant")
                    };
                })
                .OrderByDescending(c => c.LastTimeUtc)
                .ToList();

            var existingIds = conversations.Select(c => c.RestaurantId).ToHashSet();
            var otherRestaurants = await _context.Restaurants
                .Where(r => r.DeletedAt == null && r.Status == "open" && r.OwnerId != CurrentUserId && !existingIds.Contains(r.Id))
                .OrderByDescending(r => r.AvgRating)
                .Take(15)
                .ToListAsync();

            ViewBag.OtherRestaurants = otherRestaurants;
            ViewBag.InitialRestaurantId = restaurantId;
            return View(conversations);
        }

        // GET /Chat/UnreadCount - số tin nhắn chưa đọc để hiện trên icon tin nhắn ở thanh đầu trang
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Json(new { count = 0 });
            }

            var uid = CurrentUserId;
            int count = 0;

            if (User.IsInRole("admin"))
            {
                var shopUnread = await _context.ChatMessages
                    .CountAsync(m => m.SenderRole == "customer" && !m.IsRead);
                var supportUnread = await _context.SupportMessages
                    .CountAsync(m => m.SenderRole == "customer" && !m.IsRead);
                count = shopUnread + supportUnread;
            }
            else if (User.IsInRole("restaurant_owner"))
            {
                var restIds = await _context.Restaurants
                    .Where(r => r.OwnerId == uid)
                    .Select(r => r.Id)
                    .ToListAsync();

                count = await _context.ChatMessages
                    .CountAsync(m => restIds.Contains(m.RestaurantId) && m.SenderRole == "customer" && !m.IsRead);
            }
            else
            {
                var shopReplyUnread = await _context.ChatMessages
                    .CountAsync(m => m.CustomerId == uid && m.SenderRole == "restaurant" && !m.IsRead);
                var supportReplyUnread = await _context.SupportMessages
                    .CountAsync(m => m.CustomerId == uid && m.SenderRole != "customer" && !m.IsRead);
                count = shopReplyUnread + supportReplyUnread;
            }

            return Json(new { count });
        }

        // GET /Chat/ShopInbox - Dành cho chủ quán hoặc admin xem danh sách tin nhắn từ khách
        [Authorize(Roles = "restaurant_owner,admin")]
        public async Task<IActionResult> ShopInbox(long? restaurantId = null, long? customerId = null)
        {
            var isAdmin = User.IsInRole("admin");

            var myRestaurants = isAdmin
                ? await _context.Restaurants.Where(r => r.DeletedAt == null).ToListAsync()
                : await _context.Restaurants.Where(r => r.OwnerId == CurrentUserId && r.DeletedAt == null).ToListAsync();

            var restIds = myRestaurants.Select(r => r.Id).ToList();

            var messages = await _context.ChatMessages
                .Include(m => m.Customer)
                .Include(m => m.Restaurant)
                .Where(m => restIds.Contains(m.RestaurantId))
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => new { m.RestaurantId, m.CustomerId })
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.CreatedAt).First();
                    return new ShopConversationViewModel
                    {
                        RestaurantId = g.Key.RestaurantId,
                        RestaurantName = last.Restaurant?.Name ?? "Nhà hàng",
                        RestaurantLogoUrl = last.Restaurant?.LogoUrl,
                        CustomerId = g.Key.CustomerId,
                        CustomerName = last.Customer?.FullName ?? $"Khách hàng #{g.Key.CustomerId}",
                        CustomerEmail = last.Customer?.Email,
                        LastMessage = last.Message,
                        LastSenderRole = last.SenderRole,
                        LastTime = FormatTime(last.CreatedAt),
                        LastTimeUtc = last.CreatedAt,
                        UnreadCount = g.Count(x => !x.IsRead && x.SenderRole == "customer")
                    };
                })
                .OrderByDescending(c => c.LastTimeUtc)
                .ToList();

            ViewBag.MyRestaurants = myRestaurants;
            ViewBag.InitialRestaurantId = restaurantId;
            ViewBag.InitialCustomerId = customerId;
            return View(conversations);
        }
    }
}
