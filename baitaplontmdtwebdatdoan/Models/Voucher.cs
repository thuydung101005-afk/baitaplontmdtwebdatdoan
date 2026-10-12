using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models;

public partial class Voucher
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public decimal MinOrderAmount { get; set; }

    public long? RestaurantId { get; set; }

    public int? UsageLimit { get; set; }

    public int UsageLimitPerUser { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Restaurant? Restaurant { get; set; }

    public virtual ICollection<VoucherUsage> VoucherUsages { get; set; } = new List<VoucherUsage>();
}
