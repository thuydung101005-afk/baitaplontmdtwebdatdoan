using System.Text;

namespace baitaplontmdtwebdatdoan.Models
{
    /// <summary>
    /// Thông tin tài khoản nhận tiền, đọc từ mục "Bank" trong appsettings.json.
    /// Dùng để sinh mã VietQR có sẵn số tiền + nội dung chuyển khoản.
    /// </summary>
    public class BankSettings
    {
        /// <summary>Mã BIN hoặc tên viết tắt ngân hàng theo VietQR (VD: 970422 hoặc MB).</summary>
        public string BankId { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của ngân hàng (chỉ để hiện trên giao diện).</summary>
        public string BankName { get; set; } = string.Empty;

        /// <summary>Số tài khoản nhận tiền.</summary>
        public string AccountNo { get; set; } = string.Empty;

        /// <summary>Tên chủ tài khoản (viết hoa, không dấu).</summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>Template ảnh QR: compact, compact2, qr_only, print.</summary>
        public string Template { get; set; } = "compact2";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BankId) && !string.IsNullOrWhiteSpace(AccountNo);

        /// <summary>
        /// Tạo link ảnh VietQR. Người mua quét là app ngân hàng tự điền sẵn số tiền và nội dung.
        /// </summary>
        public string BuildQrUrl(decimal amount, string addInfo)
        {
            var template = string.IsNullOrWhiteSpace(Template) ? "compact2" : Template.Trim();
            var sb = new StringBuilder();
            sb.Append("https://img.vietqr.io/image/")
              .Append(Uri.EscapeDataString(BankId.Trim())).Append('-')
              .Append(Uri.EscapeDataString(AccountNo.Trim())).Append('-')
              .Append(Uri.EscapeDataString(template)).Append(".png")
              .Append("?amount=").Append((long)Math.Round(amount, 0, MidpointRounding.AwayFromZero))
              .Append("&addInfo=").Append(Uri.EscapeDataString(addInfo ?? string.Empty));

            if (!string.IsNullOrWhiteSpace(AccountName))
                sb.Append("&accountName=").Append(Uri.EscapeDataString(AccountName.Trim()));

            return sb.ToString();
        }
    }
}
