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
}
