using baitaplontmdtwebdatdoan.Data;

namespace baitaplontmdtwebdatdoan.Models
{
    public class HomeIndexViewModel
    {
        public List<string> Categories { get; set; } = new();
        public List<MenuItem> FeaturedItems { get; set; } = new();
        public List<Restaurant> FeaturedRestaurants { get; set; } = new();
    }
}