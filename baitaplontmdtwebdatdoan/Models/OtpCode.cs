using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class OtpCode
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string Target { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public byte Attempts { get; set; }

    public bool IsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
