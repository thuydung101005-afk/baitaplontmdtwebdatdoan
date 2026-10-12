using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Shipper
{
    public long UserId { get; set; }

    public string? VehicleType { get; set; }

    public string? LicensePlate { get; set; }

    public decimal? CurrentLat { get; set; }

    public decimal? CurrentLng { get; set; }

    public bool IsOnline { get; set; }

    public decimal RatingAvg { get; set; }

    public virtual User User { get; set; } = null!;
}
