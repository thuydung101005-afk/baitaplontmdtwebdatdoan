namespace baitaplontmdtwebdatdoan.Models
{
    /// <summary>
    /// Cấu hình gửi email (mục "Email"). KHÔNG ghi mật khẩu vào appsettings.json vì repo công khai trên GitHub;
    /// hãy đặt bằng User Secrets (xem hướng dẫn trong tin nhắn).
    /// </summary>
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;

        /// <summary>Tài khoản đăng nhập SMTP (với Gmail là địa chỉ Gmail).</summary>
        public string User { get; set; } = string.Empty;

        /// <summary>Mật khẩu ứng dụng (App Password) của Gmail, không phải mật khẩu đăng nhập thường.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Địa chỉ hiển thị là người gửi. Để trống thì dùng User.</summary>
        public string From { get; set; } = string.Empty;

        public string FromName { get; set; } = "Foodie";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(User) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}
