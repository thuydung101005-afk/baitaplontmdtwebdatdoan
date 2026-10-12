using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VwRestaurantRevenue
{
    public long RestaurantId { get; set; }

    public string RestaurantName { get; set; } = null!;

    public int? TotalOrders { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal AvgRating { get; set; }
}
