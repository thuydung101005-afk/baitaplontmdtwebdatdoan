using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class ItemOption
{
    public long Id { get; set; }

    public long GroupId { get; set; }

    public string Name { get; set; } = null!;

    public decimal ExtraPrice { get; set; }

    public virtual ItemOptionGroup Group { get; set; } = null!;
}
