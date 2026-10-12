using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class ShipperLocationsLog
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long ShipperId { get; set; }

    public decimal Lat { get; set; }

    public decimal Lng { get; set; }

    public DateTime LoggedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User Shipper { get; set; } = null!;
}
