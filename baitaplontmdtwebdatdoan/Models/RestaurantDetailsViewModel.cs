using System.Collections.Generic;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Models
{
    public class RestaurantDetailsViewModel
    {
        public Restaurant Restaurant { get; set; } = null!;
        public List<MenuItem> MenuItems { get; set; } = new();
        public List<MenuCategory> Categories { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
    }
}