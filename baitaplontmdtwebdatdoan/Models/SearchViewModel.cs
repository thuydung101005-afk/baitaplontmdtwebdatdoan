using System.Collections.Generic;

namespace baitaplontmdtwebdatdoan.Models
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public string? Type { get; set; } = "all"; // all, items, restaurants
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Sort { get; set; }

        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public List<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
        public List<string> AllCategories { get; set; } = new List<string>();

        public int TotalItemMatches => MenuItems.Count;
        public int TotalRestaurantMatches => Restaurants.Count;
    }
}
