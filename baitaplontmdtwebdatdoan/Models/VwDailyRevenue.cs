using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VwDailyRevenue
{
    public DateOnly? OrderDate { get; set; }

    public int? TotalOrders { get; set; }

    public decimal? TotalRevenue { get; set; }

    public decimal? TotalShippingFee { get; set; }

    public decimal? TotalDiscount { get; set; }
}
