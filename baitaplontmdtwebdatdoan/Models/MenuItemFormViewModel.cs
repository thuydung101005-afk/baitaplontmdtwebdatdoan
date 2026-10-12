using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace baitaplontmdtwebdatdoan.Models
{
    public class MenuItemFormViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món ăn")]
        [StringLength(200, ErrorMessage = "Tên món không được vượt quá 200 ký tự")]
        [Display(Name = "Tên món ăn")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(1000, 100000000, ErrorMessage = "Giá bán tối thiểu là 1.000đ")]
        [Display(Name = "Giá bán (VNĐ)")]
        public decimal BasePrice { get; set; }

        [Display(Name = "Danh mục")]
        public long? CategoryId { get; set; }

        [Display(Name = "Hoặc tạo danh mục mới")]
        public string? NewCategoryName { get; set; }

        [Display(Name = "Đường dẫn ảnh (URL)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Hoặc tải tệp ảnh từ máy")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Đang bán")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Món bán chạy / Nổi bật")]
        public bool IsBestSeller { get; set; } = false;
    }
}
