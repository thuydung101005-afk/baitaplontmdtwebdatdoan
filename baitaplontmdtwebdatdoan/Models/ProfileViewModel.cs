using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace baitaplontmdtwebdatdoan.Models
{
    public class ProfileViewModel
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Vai trò tài khoản")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Ảnh đại diện hiện tại")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Tải ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<Address> Addresses { get; set; } = new();

        public ChangePasswordViewModel ChangePassword { get; set; } = new();
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu mới không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AddressFormModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại nhận hàng")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        public string AddressLine { get; set; } = string.Empty;

        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Label { get; set; } = "Nhà riêng";
        public bool IsDefault { get; set; }
    }
}
