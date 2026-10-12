using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Review
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long UserId { get; set; }

    public long RestaurantId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public string? Images { get; set; }

    public string? ReplyFromOwner { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
