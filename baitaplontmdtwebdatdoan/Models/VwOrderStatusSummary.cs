using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VwOrderStatusSummary
{
    public string OrderStatus { get; set; } = null!;

    public int? Total { get; set; }

    public decimal? TotalValue { get; set; }
}
