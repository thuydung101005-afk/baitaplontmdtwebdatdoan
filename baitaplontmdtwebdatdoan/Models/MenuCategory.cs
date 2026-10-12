using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class MenuCategory
{
    public long Id { get; set; }

    public long RestaurantId { get; set; }

    public string Name { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public virtual Restaurant Restaurant { get; set; } = null!;
}
