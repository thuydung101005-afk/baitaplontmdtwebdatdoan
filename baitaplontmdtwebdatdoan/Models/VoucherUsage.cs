using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class VoucherUsage
{
    public long Id { get; set; }

    public long VoucherId { get; set; }

    public long UserId { get; set; }

    public long OrderId { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
