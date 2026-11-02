using System;

namespace baitaplontmdtwebdatdoan.Models
{
    public class SupportMessage
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        public long? SenderId { get; set; }

        /// <summary>"customer", "ai", hoặc "agent" (nhân viên CSKH người thật)</summary>
        public string SenderRole { get; set; } = "customer";

        public string SenderName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        /// <summary>True nếu khách đang ở chế độ yêu cầu gặp nhân viên thật</summary>
        public bool IsHumanMode { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User? Customer { get; set; }
    }

    public class SupportConversationSummary
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public string LastSenderRole { get; set; } = "customer";
        public string LastTime { get; set; } = string.Empty;
        public DateTime LastTimeUtc { get; set; }
        public bool IsHumanMode { get; set; }
        public int UnreadCount { get; set; }
    }
}

