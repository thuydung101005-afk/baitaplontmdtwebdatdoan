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

        // GET /Chat/GetMessages?restaurantId=1&customerId=2
        [HttpGet]
        public async Task<IActionResult> GetMessages(long restaurantId, long? customerId)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null) return NotFound(new { message = "Không tìm thấy quán ăn" });

            long targetCustomerId;
            if (CurrentUserId == restaurant.OwnerId)
            {
                targetCustomerId = customerId ?? 0;
                if (targetCustomerId == 0)
                {
                    return Json(new { messages = new List<object>() });
                }
            }
            else
            {
                targetCustomerId = CurrentUserId;
            }

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.RestaurantId == restaurantId && m.CustomerId == targetCustomerId)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync();

            // Đánh dấu đã đọc các tin nhắn gửi tới người dùng hiện tại
            var unread = messages.Where(m => !m.IsRead && m.SenderId != CurrentUserId).ToList();
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
                time = m.CreatedAt.ToString("HH:mm"),
                isMe = m.SenderId == CurrentUserId
            });

            return Json(new { success = true, messages = result });
        }

        // POST /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(long restaurantId, long? customerId, string message)
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

            string senderRole;
            long targetCustomerId;

            if (CurrentUserId == restaurant.OwnerId)
            {
                senderRole = "restaurant";
                targetCustomerId = customerId ?? 0;
                if (targetCustomerId == 0)
                {
                    return Json(new { success = false, message = "Chưa chọn khách hàng để trả lời." });
                }
            }
            else
            {
                senderRole = "customer";
                targetCustomerId = CurrentUserId;
            }

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

            var senderName = senderRole == "restaurant" ? restaurant.Name : (User.Identity?.Name ?? "Khách hàng");
            var timeStr = now.ToString("HH:mm");

            // Bắn SignalR tới phòng chat
            await _chatHub.Clients.Group($"chat-{restaurantId}-{targetCustomerId}")
                .SendAsync("ReceiveMessage", senderRole, senderName, chatMsg.Message, timeStr);

            // Tự động trả lời thông minh (Auto-reply) nếu khách nhắn tin lần đầu trong phiên
            if (senderRole == "customer")
            {
                var previousMsgCount = await _context.ChatMessages
                    .CountAsync(m => m.RestaurantId == restaurantId && m.CustomerId == targetCustomerId && m.Id != chatMsg.Id);

                if (previousMsgCount == 0)
                {
                    // Lời chào tự động của quán
                    var autoReply = new ChatMessage
                    {
                        SenderId = restaurant.OwnerId,
                        RestaurantId = restaurantId,
                        CustomerId = targetCustomerId,
                        SenderRole = "restaurant",
                        Message = $"Dạ chào bạn! Cảm ơn bạn đã nhắn tin cho {restaurant.Name}. Quán đã nhận được tin nhắn và sẽ phản hồi bạn trong giây lát. Bạn có thể tham khảo Menu hoặc thêm món vào giỏ trong lúc chờ nhé! ❤️",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow.AddSeconds(1)
                    };
                    _context.ChatMessages.Add(autoReply);
                    await _context.SaveChangesAsync();

                    await _chatHub.Clients.Group($"chat-{restaurantId}-{targetCustomerId}")
                        .SendAsync("ReceiveMessage", "restaurant", restaurant.Name, autoReply.Message, autoReply.CreatedAt.ToString("HH:mm"));
                }
            }

            return Json(new { success = true });
        }

        // GET /Chat/ShopInbox - Dành cho chủ quán xem danh sách tin nhắn từ khách
        [Authorize(Roles = "restaurant_owner,admin")]
        public async Task<IActionResult> ShopInbox()
        {
            var myRestaurants = await _context.Restaurants
                .Where(r => r.OwnerId == CurrentUserId && r.DeletedAt == null)
                .ToListAsync();

            var restIds = myRestaurants.Select(r => r.Id).ToList();

            var conversations = await _context.ChatMessages
                .Include(m => m.Customer)
                .Include(m => m.Restaurant)
                .Where(m => restIds.Contains(m.RestaurantId))
                .GroupBy(m => new { m.RestaurantId, m.CustomerId })
                .Select(g => new
                {
                    Restaurant = g.First().Restaurant,
                    Customer = g.First().Customer,
                    LastMessage = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault(),
                    UnreadCount = g.Count(x => !x.IsRead && x.SenderRole == "customer")
                })
                .ToListAsync();

            ViewBag.MyRestaurants = myRestaurants;
            return View(conversations);
        }
    }
}
