using System;

namespace baitaplontmdtwebdatdoan.Models;

public partial class ChatMessage
{
    public long Id { get; set; }

    public long SenderId { get; set; }

    public long RestaurantId { get; set; }

    public long CustomerId { get; set; }

    public string SenderRole { get; set; } = "customer";

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Sender { get; set; } = null!;

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;
}
