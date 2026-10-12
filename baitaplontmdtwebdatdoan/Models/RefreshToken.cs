using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public string? IpAddress { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
