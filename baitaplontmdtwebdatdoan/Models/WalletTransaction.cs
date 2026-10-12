using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class WalletTransaction
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public long? ReferenceOrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order? ReferenceOrder { get; set; }

    public virtual User User { get; set; } = null!;
}
