using System;

namespace baitaplontmdtwebdatdoan.Models
{
    /// <summary>Một cuộc trò chuyện giữa khách hàng và một quán (dùng cho hộp thư của khách).</summary>
    public class ChatConversationViewModel
    {
        public long RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? RestaurantLogoUrl { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string LastSenderRole { get; set; } = "customer";
        public DateTime LastTimeUtc { get; set; }
        public int UnreadCount { get; set; }
    }

    /// <summary>Một cuộc trò chuyện giữa khách hàng và quán (dùng cho hộp thư của chủ quán/admin).</summary>
    public class ShopConversationViewModel
    {
        public long RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? RestaurantLogoUrl { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string LastSenderRole { get; set; } = "customer";
        public string LastTime { get; set; } = string.Empty;
        public DateTime LastTimeUtc { get; set; }
        public int UnreadCount { get; set; }
    }
}
