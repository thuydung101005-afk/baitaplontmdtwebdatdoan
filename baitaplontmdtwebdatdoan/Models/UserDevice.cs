using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class UserDevice
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string DeviceToken { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
