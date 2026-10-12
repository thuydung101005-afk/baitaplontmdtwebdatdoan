using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Favorite
{
    public long UserId { get; set; }

    public long RestaurantId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
