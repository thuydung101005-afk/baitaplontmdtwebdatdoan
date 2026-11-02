using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models
{
    public class AdminRevenueViewModel
    {
        public string Period { get; set; } = "30days";
        public decimal CommissionRate { get; set; } = 18.0m;
        public decimal PaymentFeeRate { get; set; } = 1.5m;

        // Chỉ số toàn sàn
        public decimal TotalGMV { get; set; }
        public decimal TotalGrossFoodSales { get; set; }
        public decimal TotalPlatformCommission { get; set; }
        public decimal TotalPaymentGatewayFee { get; set; }
        public decimal TotalPlatformNetRevenue { get; set; }
        public decimal TotalMerchantPayout { get; set; }
        public decimal TotalShippingFee { get; set; }
        public decimal TotalVoucherDiscount { get; set; }

        // Trung gian thanh toán (Escrow)
        public decimal EscrowHoldingAmount { get; set; }
        public int EscrowHoldingCount { get; set; }
        public decimal TotalRefundedAmount { get; set; }

        // Số lượng đơn
        public int CompletedOrderCount { get; set; }
        public int ProcessingOrderCount { get; set; }
        public int CancelledOrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<AdminDailyRevenuePoint> DailyStats { get; set; } = new();
        public List<RestaurantRevenueBreakdown> RestaurantBreakdowns { get; set; } = new();
        public List<AdminOrderSettlementItem> OrderSettlements { get; set; } = new();
    }

    public class AdminDailyRevenuePoint
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal GrossSales { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal MerchantPayout { get; set; }
        public int OrderCount { get; set; }
    }

    public class RestaurantRevenueBreakdown
    {
        public long RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int CompletedOrders { get; set; }
        public decimal GrossSales { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal PaymentFee { get; set; }
        public decimal PlatformEarned { get; set; }
        public decimal MerchantNetPayout { get; set; }
        public decimal AvgRating { get; set; }
    }

    public class AdminOrderSettlementItem
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string RestaurantName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string EscrowStatus { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal PaymentFee { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal MerchantNetAmount { get; set; }
    }
}

