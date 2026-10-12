using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public long? ActorUserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public long EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
