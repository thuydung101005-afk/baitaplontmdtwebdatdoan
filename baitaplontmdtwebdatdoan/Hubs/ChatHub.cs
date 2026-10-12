using Microsoft.AspNetCore.SignalR;

namespace baitaplontmdtwebdatdoan.Hubs
{
    public class ChatHub : Hub
    {
        // Tham gia phòng chat giữa Khách hàng và Quán ăn
        public async Task JoinChatRoom(string restaurantId, string customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{restaurantId}-{customerId}");
        }

        // Rời phòng chat
        public async Task LeaveChatRoom(string restaurantId, string customerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{restaurantId}-{customerId}");
        }

        // Gửi tin nhắn tức thì
        public async Task SendMessageToRoom(string restaurantId, string customerId, string senderRole, string senderName, string message, string time)
        {
            await Clients.Group($"chat-{restaurantId}-{customerId}")
                .SendAsync("ReceiveMessage", senderRole, senderName, message, time);
        }
    }
}
