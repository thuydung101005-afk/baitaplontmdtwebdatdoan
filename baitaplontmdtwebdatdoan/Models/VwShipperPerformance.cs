using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VwShipperPerformance
{
    public long ShipperId { get; set; }

    public string ShipperName { get; set; } = null!;

    public decimal RatingAvg { get; set; }

    public int? TotalDelivered { get; set; }

    public int? TotalCancelled { get; set; }
}
