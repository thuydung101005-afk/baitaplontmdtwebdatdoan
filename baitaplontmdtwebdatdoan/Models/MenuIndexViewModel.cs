using baitaplontmdtwebdatdoan.Data;

namespace baitaplontmdtwebdatdoan.Models
{
    public class MenuIndexViewModel
    {
        public List<string> AllCategories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public List<MenuItem> Items { get; set; } = new();
    }
}