using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class SupportController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public SupportController(
            FoodieDbContext context,
            IHubContext<ChatHub> chatHub,
            IHttpClientFactory httpFactory,
            IConfiguration config)
        {
            _context = context;
            _chatHub = chatHub;
            _httpFactory = httpFactory;
            _config = config;
        }

        private long? CurrentUserIdOrNull =>
            User.Identity?.IsAuthenticated == true && long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : null;

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

        // GET /Support - Trang Trung tâm Chăm sóc khách hàng (Hỗ trợ cả khách chưa đăng nhập & đã đăng nhập)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var uid = CurrentUserIdOrNull;
            var recentOrders = new List<Order>();
            if (uid.HasValue)
            {
                recentOrders = await _context.Orders
                    .Include(o => o.Restaurant)
                    .Where(o => o.UserId == uid.Value)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            ViewBag.RecentOrders = recentOrders;
            return View();
        }

        // GET /api/support/faq - Public REST API danh sách câu hỏi thường gặp & thông tin CSKH
        [AllowAnonymous]
        [HttpGet("/api/support/faq")]
        public async Task<IActionResult> GetFaqApi()
        {
            var now = DateTime.UtcNow;
            var activeVouchers = await _context.Vouchers
                .Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now)
                .Select(v => new { v.Code, v.Description, v.MinOrderAmount })
                .Take(5)
                .ToListAsync();

            return Json(new
            {
                success = true,
                service = "Foodie Customer Care API v1.0",
                hotline = "1900 1234",
                workingHours = "08:00 - 22:00 (Thứ 2 - Chủ Nhật)",
                features = new[] { "AI Chatbot API (Tra cứu đơn, Voucher, Món ăn)", "Live Agent Handover (Kết nối nhân viên thật 24/7)" },
                activeVouchers,
                faqs = new[]
                {
                    new { question = "Cơ chế Trung gian thanh toán (Foodie Escrow) hoạt động thế nào?", answer = "Khi bạn thanh toán qua VietQR, MoMo, ZaloPay, Thẻ hoặc Ví Foodie, tiền được Cổng trung gian Foodie giữ hộ an toàn. Chỉ khi đơn hàng giao thành công, sàn mới giải ngân cho quán. Nếu đơn hủy, bạn được hoàn 100% tiền ngay lập tức." },
                    new { question = "Làm sao để gặp nhân viên CSKH người thật?", answer = "Bạn chỉ cần bấm nút 'Gặp nhân viên (Người thật)' trên khung chat CSKH hoặc gõ 'gặp người thật', hệ thống sẽ kết nối trực tiếp với chuyên viên hỗ trợ." },
                    new { question = "Khi nào tôi được hủy đơn và hoàn tiền?", answer = "Bạn có thể hủy đơn khi đơn ở trạng thái 'Chờ xác nhận' hoặc 'Đã xác nhận'. Nếu đơn đã giao nhưng món có vấn đề, bạn có thể bấm 'Yêu cầu hoàn tiền' để nhận lại tiền vào Ví Foodie." }
                }
            });
        }

        // POST /api/support/chat - REST API xử lý câu hỏi CSKH bằng AI + Database Context
        [AllowAnonymous]
        [HttpPost("/api/support/chat")]
        public async Task<IActionResult> ChatApi([FromForm] string? prompt, [FromBody] JsonElement? body = null)
        {
            string question = prompt ?? "";
            if (string.IsNullOrWhiteSpace(question) && body.HasValue && body.Value.ValueKind == JsonValueKind.Object)
            {
                if (body.Value.TryGetProperty("prompt", out var p)) question = p.GetString() ?? "";
                else if (body.Value.TryGetProperty("message", out var m)) question = m.GetString() ?? "";
            }

            if (string.IsNullOrWhiteSpace(question))
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung câu hỏi." });

            var reply = await GenerateSmartAiReplyAsync(question.Trim(), CurrentUserIdOrNull);
            return Json(new
            {
                success = true,
                apiProvider = string.IsNullOrWhiteSpace(_config["Gemini:ApiKey"]) ? "Foodie Smart CSKH Engine + DB RAG API" : "Google Gemini AI API + Foodie DB RAG",
                timestamp = DateTime.UtcNow,
                question,
                reply
            });
        }

        // GET /Support/GetMessages?customerId=5
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetMessages(long? customerId = null, string? mode = null)
        {
            var uid = CurrentUserIdOrNull;

            // Trang CSKH của khách: customerId của trang phải trùng với tài khoản đang đăng nhập
            if (mode == "customer" && (customerId ?? 0) != (uid ?? 0))
            {
                return Json(new { success = false, sessionChanged = true, message = "Tài khoản đăng nhập đã thay đổi so với trang này. Vui lòng tải lại trang (F5) rồi thử lại." });
            }

            if (!uid.HasValue)
            {
                // Khách chưa đăng nhập: trả về lời chào mặc định của Trợ lý AI Foodie
                return Json(new
                {
                    success = true,
                    customerId = 0,
                    isHumanMode = false,
                    messages = new[]
                    {
                        new
                        {
                            id = 0L,
                            senderRole = "ai",
                            senderName = "Trợ lý AI Foodie",
                            message = "Xin chào! 👋 Mình là Trợ lý AI Chăm sóc khách hàng của Foodie. Bạn có thể hỏi mình ngay về mã giảm giá Voucher, gợi ý quán ngon, chính sách Trung gian thanh toán hoặc đăng nhập để tra cứu đơn hàng & gặp Nhân viên CSKH người thật nhé!",
                            isHumanMode = false,
                            time = FormatTime(DateTime.UtcNow),
                            isMe = false
                        }
                    }
                });
            }

            var isAdmin = User.IsInRole("admin");
            var targetCustomerId = (isAdmin && customerId.HasValue && customerId.Value > 0)
                ? customerId.Value
                : uid.Value;

            var messages = await _context.SupportMessages
                .Where(m => m.CustomerId == targetCustomerId)
                .OrderBy(m => m.CreatedAt)
                .Take(150)
                .ToListAsync();

            // Nếu chưa có tin nhắn nào và khách hàng mở lần đầu, tạo lời chào từ AI CSKH
            if (!messages.Any() && targetCustomerId == uid.Value)
            {
                var welcome = new SupportMessage
                {
                    CustomerId = targetCustomerId,
                    SenderId = null,
                    SenderRole = "ai",
                    SenderName = "Trợ lý AI Foodie",
                    Message = "Xin chào! 👋 Mình là Trợ lý AI Chăm sóc khách hàng của Foodie. Mình có thể giúp bạn tra cứu đơn hàng, săn mã Voucher giảm giá, giải đáp về Trung gian thanh toán hoặc kết nối trực tiếp với Nhân viên hỗ trợ người thật. Bạn cần hỗ trợ gì hôm nay ạ?",
                    IsHumanMode = false,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SupportMessages.Add(welcome);
                await _context.SaveChangesAsync();
                messages.Add(welcome);
            }

            // Đánh dấu đã đọc
            // Đang xem hội thoại của chính mình (kể cả admin dùng trang khách) thì là phía khách
            var viewerIsCustomer = targetCustomerId == uid.Value;
            var unread = messages.Where(m => !m.IsRead && (viewerIsCustomer ? m.SenderRole != "customer" : m.SenderRole == "customer")).ToList();
            if (unread.Any())
            {
                foreach (var u in unread) u.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var isHumanMode = messages.LastOrDefault()?.IsHumanMode ?? false;

            var result = messages.Select(m => new
            {
                id = m.Id,
                senderRole = m.SenderRole,
                senderName = m.SenderName,
                message = m.Message,
                isHumanMode = m.IsHumanMode,
                time = FormatTime(m.CreatedAt),
                isMe = viewerIsCustomer ? (m.SenderRole == "customer") : (m.SenderRole == "agent" && m.SenderId == uid.Value)
            });

            return Json(new
            {
                success = true,
                customerId = targetCustomerId,
                isHumanMode,
                messages = result
            });
        }

        // POST /Support/SwitchMode - Chuyển đổi giữa Trợ lý AI và Nhân viên CSKH người thật
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SwitchMode(bool humanMode, long? customerId = null, string? mode = null)
        {
            var uid = CurrentUserIdOrNull;

            if (mode == "customer" && (customerId ?? 0) != (uid ?? 0))
            {
                return Json(new { success = false, sessionChanged = true, message = "Tài khoản đăng nhập đã thay đổi so với trang này. Vui lòng tải lại trang (F5) rồi thử lại." });
            }

            if (!uid.HasValue)
            {
                var guestNotice = humanMode
                    ? "🔒 Để kết nối trực tiếp với **Chuyên viên CSKH người thật**, bạn vui lòng **Đăng nhập tài khoản** (bấm nút Đăng nhập ở góc phải trên màn hình) để chuyên viên nắm được thông tin hỗ trợ bạn nhé!"
                    : "🤖 Đang ở chế độ Trợ lý AI Foodie. Bạn có thể đặt câu hỏi tự động 24/7!";
                return Json(new
                {
                    success = true,
                    isGuest = true,
                    isHumanMode = false,
                    senderRole = humanMode ? "agent" : "ai",
                    senderName = "Hệ thống CSKH Foodie",
                    message = guestNotice,
                    time = FormatTime(DateTime.UtcNow)
                });
            }

            var isAdmin = User.IsInRole("admin");
            var targetCustomerId = (isAdmin && customerId.HasValue && customerId.Value > 0)
                ? customerId.Value
                : uid.Value;

            var customer = await _context.Users.FindAsync(targetCustomerId);
            var customerName = customer?.FullName ?? "Khách hàng";
            var now = DateTime.UtcNow;

            var noticeText = humanMode
                ? "👨‍💼 Đã chuyển sang chế độ [Liên hệ Nhân viên người thật]. Yêu cầu của bạn đã được gửi tới bộ phận CSKH Foodie, chuyên viên trực tuyến sẽ phản hồi bạn ngay tại khung chat này!"
                : "🤖 Đã chuyển về chế độ [Trợ lý AI Foodie]. Bạn có thể hỏi thông tin đơn hàng, mã voucher, thực đơn và chính sách tự động 24/7!";

            var sysMsg = new SupportMessage
            {
                CustomerId = targetCustomerId,
                SenderId = null,
                SenderRole = humanMode ? "agent" : "ai",
                SenderName = humanMode ? "Hệ thống CSKH Foodie" : "Trợ lý AI Foodie",
                Message = noticeText,
                IsHumanMode = humanMode,
                IsRead = !humanMode,
                CreatedAt = now
            };

            _context.SupportMessages.Add(sysMsg);
            await _context.SaveChangesAsync();

            var timeStr = FormatTime(now);
            await _chatHub.Clients.Group($"support-{targetCustomerId}")
                .SendAsync("ReceiveSupportMessage", sysMsg.SenderRole, sysMsg.SenderName, sysMsg.Message, timeStr, humanMode);

            await _chatHub.Clients.Group("admin-support-inbox")
                .SendAsync("SupportInboxUpdated", new
                {
                    customerId = targetCustomerId,
                    customerName,
                    senderRole = "customer",
                    message = humanMode ? "[Yêu cầu gặp nhân viên thật]" : "[Đã chuyển về AI]",
                    isHumanMode = humanMode,
                    time = timeStr
                });

            return Json(new { success = true, isHumanMode = humanMode, message = noticeText });
        }

        // POST /Support/SendMessage - Khách hàng gửi tin nhắn CSKH
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string message, bool? forceHumanMode = null, long? customerId = null, string? mode = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "Vui lòng nhập nội dung tin nhắn." });

            var text = message.Trim();
            var uidOrNull = CurrentUserIdOrNull;

            // Trang CSKH của khách: customerId của trang phải trùng với tài khoản đang đăng nhập
            if (mode == "customer" && (customerId ?? 0) != (uidOrNull ?? 0))
            {
                return Json(new { success = false, sessionChanged = true, message = "Tài khoản đăng nhập đã thay đổi so với trang này. Vui lòng tải lại trang (F5) rồi thử lại." });
            }

            // Nếu khách chưa đăng nhập -> Trợ lý AI vẫn trả lời trực tiếp ngay lập tức
            if (!uidOrNull.HasValue)
            {
                var guestLower = text.ToLower();
                bool guestWantsHuman = guestLower.Contains("người thật") || guestLower.Contains("nhân viên") || guestLower.Contains("tư vấn viên");
                var guestReply = guestWantsHuman
                    ? "👨‍💼 Để trò chuyện trực tiếp với **Nhân viên CSKH người thật**, bạn vui lòng **Đăng nhập tài khoản** để chuyên viên định danh và hỗ trợ đơn hàng của bạn nhanh nhất nhé!"
                    : await GenerateSmartAiReplyAsync(text, null);

                return Json(new
                {
                    success = true,
                    isGuest = true,
                    isHumanMode = false,
                    senderRole = guestWantsHuman ? "agent" : "ai",
                    senderName = guestWantsHuman ? "Hệ thống CSKH Foodie" : "Trợ lý AI Foodie",
                    aiReply = guestReply,
                    time = FormatTime(DateTime.UtcNow)
                });
            }

            var uid = uidOrNull.Value;
            var user = await _context.Users.FindAsync(uid);
            var customerName = user?.FullName ?? User.Identity?.Name ?? "Khách hàng";

            var lastMsg = await _context.SupportMessages
                .Where(m => m.CustomerId == uid)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            bool isHumanMode = forceHumanMode ?? (lastMsg?.IsHumanMode ?? false);

            // Nếu khách gõ từ khóa muốn gặp người thật thì tự động bật chế độ gặp nhân viên
            var lower = text.ToLower();
            bool requestedHumanKeyword = lower.Contains("người thật") || lower.Contains("nhân viên") || lower.Contains("tư vấn viên") || lower.Contains("gặp ad") || lower.Contains("khiếu nại");
            if (requestedHumanKeyword && !isHumanMode)
            {
                isHumanMode = true;
            }

            var now = DateTime.UtcNow;
            var custMsg = new SupportMessage
            {
                CustomerId = uid,
                SenderId = uid,
                SenderRole = "customer",
                SenderName = customerName,
                Message = text,
                IsHumanMode = isHumanMode,
                IsRead = false,
                CreatedAt = now
            };

            _context.SupportMessages.Add(custMsg);
            await _context.SaveChangesAsync();

            var timeStr = FormatTime(now);

            // Bắn tin nhắn của khách vào phòng support
            await _chatHub.Clients.Group($"support-{uid}")
                .SendAsync("ReceiveSupportMessage", "customer", customerName, text, timeStr, isHumanMode);

            // Bắn thông báo cho Admin ở trang AdminInbox
            await _chatHub.Clients.Group("admin-support-inbox")
                .SendAsync("SupportInboxUpdated", new
                {
                    customerId = uid,
                    customerName,
                    senderRole = "customer",
                    message = text,
                    isHumanMode,
                    time = timeStr
                });

            // Nếu đang ở chế độ AI (hoặc vừa gõ từ khóa chuyển sang người thật), phản hồi tự động
            if (!isHumanMode)
            {
                var aiReplyText = await GenerateSmartAiReplyAsync(text, uid);
                var aiNow = DateTime.UtcNow.AddMilliseconds(500);
                var aiMsg = new SupportMessage
                {
                    CustomerId = uid,
                    SenderId = null,
                    SenderRole = "ai",
                    SenderName = "Trợ lý AI Foodie (API)",
                    Message = aiReplyText,
                    IsHumanMode = false,
                    IsRead = true,
                    CreatedAt = aiNow
                };
                _context.SupportMessages.Add(aiMsg);
                await _context.SaveChangesAsync();

                var aiTime = FormatTime(aiNow);
                await _chatHub.Clients.Group($"support-{uid}")
                    .SendAsync("ReceiveSupportMessage", "ai", aiMsg.SenderName, aiMsg.Message, aiTime, false);
            }
            else if (requestedHumanKeyword && (lastMsg == null || !lastMsg.IsHumanMode))
            {
                var handoverText = $"👨‍💼 Hệ thống đã nhận thấy bạn cần gặp nhân viên hỗ trợ trực tiếp. Phiên chat đã được chuyển sang [Chế độ Người thật]. Chuyên viên CSKH Foodie sẽ trả lời {customerName} trong giây lát ạ!";
                var handoverMsg = new SupportMessage
                {
                    CustomerId = uid,
                    SenderId = null,
                    SenderRole = "agent",
                    SenderName = "Hệ thống CSKH Foodie",
                    Message = handoverText,
                    IsHumanMode = true,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddMilliseconds(400)
                };
                _context.SupportMessages.Add(handoverMsg);
                await _context.SaveChangesAsync();

                await _chatHub.Clients.Group($"support-{uid}")
                    .SendAsync("ReceiveSupportMessage", "agent", handoverMsg.SenderName, handoverMsg.Message, FormatTime(handoverMsg.CreatedAt), true);
            }

            return Json(new { success = true, isHumanMode });
        }

        // POST /Support/AdminReply - Nhân viên CSKH (Admin) trả lời trực tiếp cho khách hàng
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> AdminReply(long customerId, string message)
        {
            if (customerId <= 0 || string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var adminUser = await _context.Users.FindAsync(CurrentUserId);
            var agentName = $"CSKH - {adminUser?.FullName ?? "Chuyên viên hỗ trợ"}";
            var now = DateTime.UtcNow;

            // Đánh dấu các tin nhắn trước đó của khách là đã đọc
            var unread = await _context.SupportMessages
                .Where(m => m.CustomerId == customerId && m.SenderRole == "customer" && !m.IsRead)
                .ToListAsync();
            foreach (var u in unread) u.IsRead = true;

            var replyMsg = new SupportMessage
            {
                CustomerId = customerId,
                SenderId = CurrentUserId,
                SenderRole = "agent",
                SenderName = agentName,
                Message = message.Trim(),
                IsHumanMode = true,
                IsRead = false,
                CreatedAt = now
            };

            _context.SupportMessages.Add(replyMsg);
            await _context.SaveChangesAsync();

            var timeStr = FormatTime(now);
            await _chatHub.Clients.Group($"support-{customerId}")
                .SendAsync("ReceiveSupportMessage", "agent", agentName, replyMsg.Message, timeStr, true);

            return Json(new { success = true, senderName = agentName, time = timeStr });
        }

        // GET /Support/AdminInbox - Trang quản lý CSKH người thật dành cho Admin
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> AdminInbox(long? customerId = null)
        {
            var allMsgs = await _context.SupportMessages
                .Include(m => m.Customer)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var summaries = allMsgs
                .GroupBy(m => m.CustomerId)
                .Select(g =>
                {
                    var last = g.First();
                    return new SupportConversationSummary
                    {
                        CustomerId = g.Key,
                        CustomerName = last.Customer?.FullName ?? $"Khách hàng #{g.Key}",
                        CustomerEmail = last.Customer?.Email ?? "",
                        CustomerPhone = last.Customer?.Phone ?? "",
                        LastMessage = last.Message,
                        LastSenderRole = last.SenderRole,
                        LastTime = FormatTime(last.CreatedAt),
                        LastTimeUtc = last.CreatedAt,
                        IsHumanMode = last.IsHumanMode,
                        UnreadCount = g.Count(x => !x.IsRead && x.SenderRole == "customer")
                    };
                })
                .OrderByDescending(s => s.IsHumanMode)
                .ThenByDescending(s => s.UnreadCount)
                .ThenByDescending(s => s.LastTimeUtc)
                .ToList();

            ViewBag.InitialCustomerId = customerId;
            return View(summaries);
        }

        // Hàm gọi API AI (Gemini API nếu có key + RAG dữ liệu thật từ Database FoodieDB)
        private async Task<string> GenerateSmartAiReplyAsync(string userMessage, long? userId)
        {
            var now = DateTime.UtcNow;

            // 1. Lấy dữ liệu ngữ cảnh thực tế từ Database (RAG Context)
            var activeVouchers = await _context.Vouchers
                .Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now)
                .OrderBy(v => v.MinOrderAmount)
                .Take(5)
                .ToListAsync();

            var topRestaurants = await _context.Restaurants
                .Where(r => r.Status == "open" && r.DeletedAt == null)
                .OrderByDescending(r => r.AvgRating)
                .Take(4)
                .ToListAsync();

            List<Order> userOrders = new();
            if (userId.HasValue)
            {
                userOrders = await _context.Orders
                    .Include(o => o.Restaurant)
                    .Where(o => o.UserId == userId.Value)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(3)
                    .ToListAsync();
            }

            // 2. Nếu có cấu hình Gemini API Key trong appsettings.json thì gọi API ngoài
            var geminiKey = _config["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(geminiKey))
            {
                try
                {
                    var contextSb = new StringBuilder();
                    contextSb.AppendLine("Bạn là Trợ lý AI Chăm sóc khách hàng của sàn đặt đồ ăn Foodie. Hãy trả lời ngắn gọn, thân thiện bằng tiếng Việt.");
                    if (activeVouchers.Any())
                    {
                        contextSb.AppendLine("Các mã giảm giá đang hoạt động: " +
                            string.Join("; ", activeVouchers.Select(v => $"{v.Code} ({v.Description}, đơn tối thiểu {v.MinOrderAmount:N0}đ)")));
                    }
                    if (userOrders.Any())
                    {
                        contextSb.AppendLine("Đơn hàng gần đây của khách: " +
                            string.Join("; ", userOrders.Select(o => $"#{o.OrderCode} tại {o.Restaurant?.Name} - Trạng thái: {o.OrderStatus}, Thanh toán: {o.PaymentStatus}, Tổng: {o.TotalAmount:N0}đ")));
                    }
                    contextSb.AppendLine("Câu hỏi của khách: " + userMessage);

                    var client = _httpFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(8);
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={geminiKey}";
                    var payload = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = contextSb.ToString() } } }
                        }
                    };
                    var res = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                    if (res.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                        var textOut = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                        if (!string.IsNullOrWhiteSpace(textOut))
                            return textOut.Trim();
                    }
                }
                catch
                {
                    // Tự động chuyển sang Engine nội bộ nếu mạng hoặc API key gặp sự cố
                }
            }

            // 3. Engine AI nội bộ truy vấn trực tiếp dữ liệu thật từ FoodieDB
            var q = userMessage.ToLower();

            if (q.Contains("đơn hàng") || q.Contains("mã đơn") || q.Contains("bao lâu") || q.Contains("giao") || q.Contains("dh"))
            {
                if (!userOrders.Any())
                {
                    return "📦 Hiện tại mình kiểm tra trên hệ thống chưa thấy đơn hàng nào gần đây của bạn. Bạn có thể chọn món tại trang chủ để đặt đơn đầu tiên, hoặc bấm nút [Gặp nhân viên] nếu cần kiểm tra theo số điện thoại nhé!";
                }

                var latest = userOrders.First();
                string statusVn = latest.OrderStatus switch
                {
                    "pending" => "Chờ quán xác nhận",
                    "confirmed" => "Quán đã xác nhận",
                    "preparing" => "Đang chế biến món ăn",
                    "ready_for_pickup" => "Sẵn sàng chờ tài xế lấy",
                    "picked_up" => "Tài xế đã lấy hàng",
                    "delivering" => "Đang trên đường giao đến bạn",
                    "completed" => "Đã giao thành công",
                    "cancelled" => "Đã hủy",
                    _ => latest.OrderStatus
                };
                string payVn = latest.PaymentStatus == "paid" ? "Đã thanh toán (Được bảo vệ bởi Trung gian Foodie Escrow)" :
                               latest.PaymentStatus == "refunded" ? "Đã hoàn tiền vào Ví Foodie" : "Chưa thanh toán (COD/Chờ chuyển khoản)";

                return $"📦 **Tra cứu tự động từ hệ thống:**\n• Đơn gần nhất của bạn là **#{latest.OrderCode}** tại quán **{latest.Restaurant?.Name}**.\n• Trạng thái đơn: **{statusVn}**\n• Thanh toán: **{payVn}** ({latest.TotalAmount:N0}đ).\nBạn có thể vào mục *Đơn hàng của tôi* để xem bản đồ vị trí tài xế trực tiếp, hoặc bấm nút **Gặp nhân viên** nếu muốn hỗ trợ gấp về đơn này nhé!";
            }

            if (q.Contains("voucher") || q.Contains("mã") || q.Contains("giảm giá") || q.Contains("khuyến mãi") || q.Contains("freeship"))
            {
                if (!activeVouchers.Any())
                {
                    return "🎁 Hiện tại kho voucher đang được cập nhật đợt khuyến mãi mới. Bạn nhớ ghé mục Khuyến mãi thường xuyên nhé!";
                }

                var listStr = string.Join("\n", activeVouchers.Select(v => $"• Mã **{v.Code}**: {v.Description} (Đơn từ {v.MinOrderAmount:N0}đ)"));
                return $"🎉 **Các mã giảm giá đang hoạt động trên sàn Foodie hôm nay:**\n{listStr}\n\n👉 Bạn chỉ cần chọn mã ở bước **Thanh toán** hoặc vào mục **Khuyến mãi** để lưu mã nhé!";
            }

            if (q.Contains("thanh toán") || q.Contains("trung gian") || q.Contains("hoàn tiền") || q.Contains("hủy") || q.Contains("ví") || q.Contains("escrow"))
            {
                return "🛡️ **Chính sách Trung gian Thanh toán & Hoàn tiền (Foodie Escrow):**\n1. Khi bạn thanh toán qua **VietQR, MoMo, ZaloPay, Thẻ Visa/Master hoặc Ví Foodie**, tiền sẽ được **Cổng trung gian Foodie giữ hộ**.\n2. Chỉ khi bạn nhận hàng thành công (`Hoàn thành`), sàn mới giải ngân tiền cho Nhà hàng.\n3. Nếu đơn bị hủy hoặc bạn yêu cầu hoàn tiền hợp lệ, **100% số tiền** sẽ được hoàn trả tự động về **Ví Foodie** của bạn ngay lập tức!";
            }

            if (q.Contains("món") || q.Contains("ăn gì") || q.Contains("ngon") || q.Contains("quán") || q.Contains("nhà hàng"))
            {
                if (topRestaurants.Any())
                {
                    var restStr = string.Join(", ", topRestaurants.Select(r => $"**{r.Name}** ({r.AvgRating:0.0}⭐)"));
                    return $"🍜 **Gợi ý từ Trợ lý AI Foodie:**\nCác nhà hàng đang được yêu thích nhất hiện nay gồm: {restStr}.\nBạn có thể ghé mục **Danh mục món ăn** hoặc áp dụng thêm mã **FREESHIP** để đặt món siêu tiết kiệm nhé!";
                }
            }

            return "🤖 Mình là **Trợ lý AI CSKH Foodie (tích hợp API)**. Mình có thể hỗ trợ bạn ngay lập tức các vấn đề:\n• 📦 Gõ **\"kiểm tra đơn hàng\"** để tra cứu tiến độ đơn thời gian thực\n• 🎟️ Gõ **\"mã giảm giá\"** để lấy danh sách Voucher đang mở\n• 🛡️ Gõ **\"trung gian thanh toán\"** hoặc **\"hoàn tiền\"** để xem cơ chế bảo vệ người mua\n• 👨‍💼 Hoặc bấm nút **\"Liên hệ người thật\"** phía trên để chat trực tiếp với Nhân viên CSKH Foodie!";
        }
    }
}

