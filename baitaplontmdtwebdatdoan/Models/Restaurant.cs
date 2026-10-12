using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Restaurant
{
    public long Id { get; set; }

    public long OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? LogoUrl { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Phone { get; set; }

    public decimal AvgRating { get; set; }

    public int TotalReviews { get; set; }

    public string Status { get; set; } = null!;

    public decimal MinOrderAmount { get; set; }

    public int PrepTimeMinutes { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<MenuCategory> MenuCategories { get; set; } = new List<MenuCategory>();

    public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<RestaurantOperatingHour> RestaurantOperatingHours { get; set; } = new List<RestaurantOperatingHour>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
