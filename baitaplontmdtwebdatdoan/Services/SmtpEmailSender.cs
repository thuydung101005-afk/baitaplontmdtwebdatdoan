using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using baitaplontmdtwebdatdoan.Models;

namespace baitaplontmdtwebdatdoan.Services
{
    public interface IAppEmailSender
    {
        /// <summary>Đã cấu hình SMTP đủ để gửi email thật hay chưa.</summary>
        bool IsConfigured { get; }

        /// <summary>Gửi email HTML. Trả về true nếu gửi thành công.</summary>
        Task<bool> SendAsync(string to, string subject, string htmlBody);
    }

    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<bool> SendAsync(string to, string subject, string htmlBody)
        {
            if (!_settings.IsConfigured)
            {
                _logger.LogWarning("Chưa cấu hình mục Email nên không gửi được email tới {To}.", to);
                return false;
            }

            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.User, _settings.Password),
                    Timeout = 15000
                };

                var fromAddress = string.IsNullOrWhiteSpace(_settings.From) ? _settings.User : _settings.From;
                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, _settings.FromName, Encoding.UTF8),
                    Subject = subject,
                    SubjectEncoding = Encoding.UTF8,
                    Body = htmlBody,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email tới {To} thất bại.", to);
                return false;
            }
        }
    }
}
