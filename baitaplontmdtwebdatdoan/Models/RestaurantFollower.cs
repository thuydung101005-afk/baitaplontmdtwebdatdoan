using System;

namespace baitaplontmdtwebdatdoan.Models;

public partial class RestaurantFollower
{
    public long UserId { get; set; }

    public long RestaurantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
