using System;
using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models
{
    public class RestaurantRevenueViewModel
    {
        public Restaurant Restaurant { get; set; } = null!;
        public string Period { get; set; } = "30days";
        public decimal TotalGrossSales { get; set; }
        public decimal CommissionRate { get; set; } = 18.0m; // 18% hoa hồng sàn chuẩn ShopeeFood/GrabFood
        public decimal PaymentFeeRate { get; set; } = 1.5m;   // 1.5% phí xử lý giao dịch công nghệ
        public decimal TotalCommission { get; set; }
        public decimal TotalPaymentFee { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetPayout { get; set; }
        public int CompletedOrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailyRevenuePoint> DailyStats { get; set; } = new();
        public List<OrderSettlementItem> OrderSettlements { get; set; } = new();
    }

    public class DailyRevenuePoint
    {
        public string DateLabel { get; set; } = null!;
        public decimal GrossSales { get; set; }
        public decimal NetPayout { get; set; }
        public int OrderCount { get; set; }
    }

    public class OrderSettlementItem
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public DateTime CompletedAt { get; set; }
        public decimal Subtotal { get; set; }
        public decimal CommissionFee { get; set; }
        public decimal PaymentFee { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string SettlementStatus { get; set; } = "settled";
    }
}
