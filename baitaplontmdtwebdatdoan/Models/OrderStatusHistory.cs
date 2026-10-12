using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class OrderStatusHistory
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public string? Note { get; set; }

    public virtual Order Order { get; set; } = null!;
}
