using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class OauthAccount
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUid { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
