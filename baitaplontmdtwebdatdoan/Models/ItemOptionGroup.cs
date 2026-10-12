using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class ItemOptionGroup
{
    public long Id { get; set; }

    public long MenuItemId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsRequired { get; set; }

    public int MinSelect { get; set; }

    public int MaxSelect { get; set; }

    public virtual ICollection<ItemOption> ItemOptions { get; set; } = new List<ItemOption>();

    public virtual MenuItem MenuItem { get; set; } = null!;
}
