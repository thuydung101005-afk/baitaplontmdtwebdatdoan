using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace baitaplontmdtwebdatdoan.Models
{
    public class RestaurantSettingsViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhà hàng")]
        [StringLength(200, ErrorMessage = "Tên nhà hàng không được quá 200 ký tự")]
        [Display(Name = "Tên quán / Nhà hàng")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Giới thiệu / Mô tả về nhà hàng")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại hotline")]
        public string Phone { get; set; } = string.Empty;

        [Range(0, 10000000, ErrorMessage = "Đơn tối thiểu không hợp lệ")]
        [Display(Name = "Đơn hàng tối thiểu (VNĐ)")]
        public decimal MinOrderAmount { get; set; } = 20000;

        [Range(5, 180, ErrorMessage = "Thời gian chuẩn bị từ 5 - 180 phút")]
        [Display(Name = "Thời gian chuẩn bị trung bình (phút)")]
        public int PrepTimeMinutes { get; set; } = 20;

        [Display(Name = "Đường dẫn Logo (URL)")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Đường dẫn Ảnh bìa (URL)")]
        public string? CoverImageUrl { get; set; }

        [Display(Name = "Tải tệp Logo / Ảnh đại diện quán")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Tải tệp Ảnh bìa nhà hàng")]
        public IFormFile? CoverFile { get; set; }

        public string? Status { get; set; }
        public decimal AvgRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
