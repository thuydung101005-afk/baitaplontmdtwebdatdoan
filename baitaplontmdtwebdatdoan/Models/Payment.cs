using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Payment
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public string? Provider { get; set; }

    public string? TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? RawResponse { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
