using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class CartItem
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long RestaurantId { get; set; }

    public long MenuItemId { get; set; }

    public int Quantity { get; set; }

    public string? SelectedOptions { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MenuItem MenuItem { get; set; } = null!;

    public virtual Restaurant Restaurant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
