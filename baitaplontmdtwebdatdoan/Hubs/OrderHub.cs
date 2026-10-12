using Microsoft.AspNetCore.SignalR;

namespace baitaplontmdtwebdatdoan.Hubs
{
    public class OrderHub : Hub
    {
        // Client (trình duyệt) gọi hàm này để "đăng ký lắng nghe" 1 đơn hàng cụ thể
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }

        // Quán ăn gọi hàm này để lắng nghe khi có đơn hàng mới phát sinh
        public async Task JoinRestaurantGroup(string restaurantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"restaurant-{restaurantId}");
        }

        // Cập nhật vị trí tài xế theo thời gian thực tới nhóm đơn hàng
        public async Task UpdateDriverLocation(string orderId, double lat, double lng, string note)
        {
            await Clients.Group($"order-{orderId}").SendAsync("DriverLocationUpdated", lat, lng, note);
        }
    }
}