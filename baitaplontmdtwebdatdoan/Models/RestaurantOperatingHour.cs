using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class RestaurantOperatingHour
{
    public long Id { get; set; }

    public long RestaurantId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly OpenTime { get; set; }

    public TimeOnly CloseTime { get; set; }

    public virtual Restaurant Restaurant { get; set; } = null!;
}
