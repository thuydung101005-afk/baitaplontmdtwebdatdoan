namespace baitaplontmdtwebdatdoan.Models
{
    public class CheckoutViewModel
    {
        public long MenuItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Note { get; set; }
    }
}