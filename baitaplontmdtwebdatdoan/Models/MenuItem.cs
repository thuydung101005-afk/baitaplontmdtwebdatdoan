using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace baitaplontmdtwebdatdoan.Models;

public partial class MenuItem
{
    public long Id { get; set; }

    public long RestaurantId { get; set; }

    public long? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [NotMapped]
    public List<string> Images => string.IsNullOrWhiteSpace(ImageUrl)
        ? new List<string>()
        : ImageUrl.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [NotMapped]
    public string PrimaryImageUrl => Images.FirstOrDefault() ?? "https://placehold.co/600x450?text=Foodie";

    public decimal BasePrice { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsBestSeller { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual MenuCategory? Category { get; set; }

    public virtual ICollection<ItemOptionGroup> ItemOptionGroups { get; set; } = new List<ItemOptionGroup>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Restaurant Restaurant { get; set; } = null!;
}
