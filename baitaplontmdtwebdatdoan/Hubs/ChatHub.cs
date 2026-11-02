using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using baitaplontmdtwebdatdoan.Data;

namespace baitaplontmdtwebdatdoan.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly FoodieDbContext _context;

        public ChatHub(FoodieDbContext context)
        {
            _context = context;
        }

        private static string RoomName(long restaurantId, long customerId) => $"chat-{restaurantId}-{customerId}";
        private static string SupportRoomName(long customerId) => $"support-{customerId}";

        // Tham gia phòng chat giữa Khách hàng và Quán ăn
        public async Task JoinChatRoom(string restaurantId, string customerId)
        {
            if (!long.TryParse(restaurantId, out var rid) || !long.TryParse(customerId, out var cid))
                return;

            if (!long.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                return;

            var isAdmin = Context.User?.IsInRole("admin") == true;
            var allowed = uid == cid || isAdmin || await _context.Restaurants.AnyAsync(r => r.Id == rid && r.OwnerId == uid);
            if (!allowed)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, RoomName(rid, cid));
        }

        // Rời phòng chat
        public async Task LeaveChatRoom(string restaurantId, string customerId)
        {
            if (!long.TryParse(restaurantId, out var rid) || !long.TryParse(customerId, out var cid))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomName(rid, cid));
        }

        // Chủ quán hoặc Admin tham gia kênh thông báo hộp thư quán
        public async Task JoinShopInbox()
        {
            if (!long.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"shop-owner-{uid}");
            if (Context.User?.IsInRole("admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin-shop-inbox");
            }
        }

        // Tham gia phòng CSKH trực tuyến (Khách hàng <-> Nhân viên CSKH / AI)
        public async Task JoinSupportRoom(string customerId)
        {
            if (!long.TryParse(customerId, out var cid))
                return;

            if (!long.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var uid))
                return;

            var isAdmin = Context.User?.IsInRole("admin") == true;
            if (uid != cid && !isAdmin)
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, SupportRoomName(cid));
        }

        public async Task LeaveSupportRoom(string customerId)
        {
            if (!long.TryParse(customerId, out var cid))
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SupportRoomName(cid));
        }

        // Admin / Nhân viên CSKH lắng nghe toàn bộ tin nhắn CSKH mới
        public async Task JoinAdminSupportInbox()
        {
            if (Context.User?.IsInRole("admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin-support-inbox");
            }
        }
    }
}
