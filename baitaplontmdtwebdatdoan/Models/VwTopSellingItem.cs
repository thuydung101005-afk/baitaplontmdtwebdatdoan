using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VwTopSellingItem
{
    public long MenuItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public long RestaurantId { get; set; }

    public string RestaurantName { get; set; } = null!;

    public int? TotalQuantitySold { get; set; }

    public decimal? TotalRevenue { get; set; }
}
