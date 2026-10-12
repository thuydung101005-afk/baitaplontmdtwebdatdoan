using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Wallet
{
    public long UserId { get; set; }

    public decimal Balance { get; set; }

    public virtual User User { get; set; } = null!;
}
