namespace baitaplontmdtwebdatdoan.Models
{
    public class MenuItemDetailsViewModel
    {
        public MenuItem Item { get; set; } = null!;
        public List<MenuItem> RelatedItems { get; set; } = new();
    }
}
