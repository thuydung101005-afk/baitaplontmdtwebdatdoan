using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;
using baitaplontmdtwebdatdoan.Services;

namespace baitaplontmdtwebdatdoan.Controllers
{
    public class AccountController : Controller
    {
        private const string ResetPurpose = "reset_password"; // cột purpose tối đa 20 ký tự
        private const int CodeValidMinutes = 10;              // mã có hiệu lực 10 phút
        private const int MaxCodeAttempts = 5;                // nhập sai quá 5 lần thì mã bị huỷ
        private const int MaxCodesPer15Min = 3;               // tối đa 3 mã mỗi 15 phút cho một email

        private readonly FoodieDbContext _context;
        private readonly IAppEmailSender _email;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AccountController> _logger;

        public AccountController(FoodieDbContext context, IAppEmailSender email,
            IWebHostEnvironment env, ILogger<AccountController> logger)
        {
            _context = context;
            _email = email;
            _env = env;
            _logger = logger;
        }

        // Chỉ ở môi trường phát triển và chưa cấu hình email thì báo cách lấy mã
        private void SetDevHint()
        {
            if (_env.IsDevelopment() && !_email.IsConfigured)
                ViewBag.DevHint = "Chưa cấu hình email nên mã không được gửi đi. Ở chế độ phát triển, mã được in ra cửa sổ Output/Console của ứng dụng.";
        }

        // Email luôn lưu dạng chữ thường, không có khoảng trắng thừa
        private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

        // Số điện thoại lưu dạng 0xxxxxxxxx (bỏ khoảng trắng, đổi +84 thành 0)
        private static string NormalizePhone(string? phone)
        {
            var p = (phone ?? string.Empty).Trim().Replace(" ", "").Replace("-", "").Replace(".", "");
            return p.StartsWith("+84") ? "0" + p.Substring(3) : p;
        }

        // Chuyển người dùng sau khi đăng nhập: về trang họ đang xem, nếu không có thì theo vai trò
        private IActionResult RedirectAfterLogin(string? returnUrl, string role)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return role switch
            {
                "restaurant_owner" => RedirectToAction("Orders", "RestaurantOwner"),
                "admin" => RedirectToAction("Index", "Admin"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // ===== ĐĂNG KÝ =====
        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            var email = NormalizeEmail(model.Email);
            var phone = NormalizePhone(model.Phone);
            var fullName = (model.FullName ?? string.Empty).Trim();

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra email hoặc số điện thoại đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Phone == phone))
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
                return View(model);
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = model.Role == "restaurant_owner" ? "restaurant_owner" : "customer",
                Status = "active",
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai người đăng ký cùng lúc với cùng email/SĐT: DB chặn trùng, báo lại thay vì văng trang lỗi
                ModelState.AddModelError("", "Email hoặc số điện thoại này vừa được đăng ký. Vui lòng thử lại.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login", new { returnUrl });
        }

        // ===== ĐĂNG NHẬP =====
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Đã đăng nhập rồi thì không cần vào lại trang đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var email = NormalizeEmail(model.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Tài khoản đã bị xoá mềm coi như không tồn tại
            if (user == null || user.DeletedAt != null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                model.Password = string.Empty;
                return View(model);
            }

            if (user.Status == "locked")
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.");
                model.Password = string.Empty;
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    // Không chọn "Ghi nhớ" thì cookie mất khi đóng trình duyệt; chọn thì giữ 7 ngày
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                });

            return RedirectAfterLogin(returnUrl, user.Role);
        }

        // ===== QUÊN MẬT KHẨU: BƯỚC 1 - NHẬP EMAIL, HỆ THỐNG GỬI MÃ 6 SỐ =====
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = NormalizeEmail(model.Email);
            var now = DateTime.UtcNow;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

            // Dù email có tồn tại hay không đều phản hồi giống nhau, để không lộ ai có tài khoản
            if (user != null && user.Status != "locked")
            {
                var since = now.AddMinutes(-15);
                var recentCount = await _context.OtpCodes.CountAsync(o =>
                    o.Target == email && o.Purpose == ResetPurpose && o.CreatedAt >= since);

                if (recentCount < MaxCodesPer15Min)
                {
                    // Huỷ các mã cũ chưa dùng, chỉ mã mới nhất có hiệu lực
                    var oldCodes = await _context.OtpCodes
                        .Where(o => o.Target == email && o.Purpose == ResetPurpose && !o.IsUsed)
                        .ToListAsync();
                    foreach (var o in oldCodes) o.IsUsed = true;

                    var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

                    _context.OtpCodes.Add(new OtpCode
                    {
                        UserId = user.Id,
                        Target = email,
                        OtpHash = BCrypt.Net.BCrypt.HashPassword(code),
                        Purpose = ResetPurpose,
                        Attempts = 0,
                        IsUsed = false,
                        ExpiresAt = now.AddMinutes(CodeValidMinutes),
                        CreatedAt = now
                    });
                    await _context.SaveChangesAsync();

                    var html = $@"
<div style=""font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #eee;border-radius:12px"">
  <h2 style=""color:#FF7A00;margin-top:0"">Foodie - Đặt lại mật khẩu</h2>
  <p>Xin chào <b>{WebUtility.HtmlEncode(user.FullName)}</b>,</p>
  <p>Mã xác nhận để đặt lại mật khẩu của bạn là:</p>
  <p style=""font-size:32px;letter-spacing:8px;font-weight:bold;background:#FFF6EE;padding:12px;text-align:center;border-radius:8px"">{code}</p>
  <p>Mã có hiệu lực trong {CodeValidMinutes} phút. Nếu bạn không yêu cầu, hãy bỏ qua email này, tài khoản của bạn vẫn an toàn.</p>
</div>";

                    var sent = await _email.SendAsync(email, "Mã đặt lại mật khẩu Foodie", html);

                    // Môi trường phát triển chưa cấu hình email: in mã ra console để thử chức năng
                    if (!sent && _env.IsDevelopment())
                        _logger.LogWarning("[DEV] Mã đặt lại mật khẩu cho {Email}: {Code} (hết hạn sau {Minutes} phút)",
                            email, code, CodeValidMinutes);
                }
            }

            TempData["SuccessMessage"] = "Nếu email này đã đăng ký, chúng tôi đã gửi mã xác nhận 6 số. Vui lòng kiểm tra hộp thư (cả mục Spam).";
            return RedirectToAction("ResetPassword", new { email });
        }

        // ===== QUÊN MẬT KHẨU: BƯỚC 2 - NHẬP MÃ VÀ MẬT KHẨU MỚI =====
        [HttpGet]
        public IActionResult ResetPassword(string? email = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            SetDevHint();
            return View(new ResetPasswordViewModel { Email = email ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            SetDevHint();

            if (!ModelState.IsValid)
                return View(model);

            var email = NormalizeEmail(model.Email);
            var now = DateTime.UtcNow;

            var otp = await _context.OtpCodes
                .Where(o => o.Target == email && o.Purpose == ResetPurpose && !o.IsUsed && o.ExpiresAt > now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                ModelState.AddModelError("", "Mã không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu gửi mã mới.");
                return View(model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Code.Trim(), otp.OtpHash))
            {
                otp.Attempts++;
                if (otp.Attempts >= MaxCodeAttempts) otp.IsUsed = true; // sai quá nhiều lần thì huỷ mã
                await _context.SaveChangesAsync();

                ModelState.AddModelError("", otp.IsUsed
                    ? "Bạn đã nhập sai quá nhiều lần. Vui lòng yêu cầu gửi mã mới."
                    : $"Mã xác nhận không đúng. Bạn còn {MaxCodeAttempts - otp.Attempts} lần thử.");
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == otp.UserId && u.DeletedAt == null);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản. Vui lòng yêu cầu gửi mã mới.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.UpdatedAt = now;
            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
            return RedirectToAction("Login");
        }

        // ===== THÔNG TIN TÀI KHOẢN (PROFILE) =====
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile(string? tab = "info")
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            ViewBag.ActiveTab = tab ?? "info";

            var vm = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                Addresses = user.Addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToList()
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null) return RedirectToAction("Login");

            var phone = NormalizePhone(model.Phone);
            var fullName = (model.FullName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Họ và tên không được để trống.";
                return RedirectToAction("Profile", new { tab = "info" });
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Error"] = "Số điện thoại không được để trống.";
                return RedirectToAction("Profile", new { tab = "info" });
            }

            // Kiểm tra trùng số điện thoại với tài khoản khác
            if (await _context.Users.AnyAsync(u => u.Phone == phone && u.Id != user.Id))
            {
                TempData["Error"] = "Số điện thoại này đã được sử dụng bởi tài khoản khác.";
                return RedirectToAction("Profile", new { tab = "info" });
            }

            // Xử lý tải ảnh đại diện
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "images", "avatars");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string uniqueFileName = $"avatar_{Guid.NewGuid()}_{Path.GetFileName(model.AvatarFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(fileStream);
                }
                user.AvatarUrl = $"/images/avatars/{uniqueFileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
            {
                user.AvatarUrl = model.AvatarUrl.Trim();
            }

            user.FullName = fullName;
            user.Phone = phone;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Cập nhật lại Claims identity để tên mới hiển thị trên Header ngay lập tức
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân và ảnh đại diện thành công!";
            return RedirectToAction("Profile", new { tab = "info" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Thông tin đổi mật khẩu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Profile", new { tab = "password" });
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Profile", new { tab = "password" });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile", new { tab = "password" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressFormModel model)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, out var userId))
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(model.ReceiverName) || string.IsNullOrWhiteSpace(model.ReceiverPhone) || string.IsNullOrWhiteSpace(model.AddressLine))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ tên người nhận, số điện thoại và địa chỉ nhận hàng.";
                return RedirectToAction("Profile", new { tab = "address" });
            }

            if (model.IsDefault)
            {
                var existingDefault = await _context.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var a in existingDefault) a.IsDefault = false;
            }

            var address = new Address
            {
                UserId = userId,
                Label = model.Label ?? "Nhà riêng",
                ReceiverName = model.ReceiverName.Trim(),
                ReceiverPhone = NormalizePhone(model.ReceiverPhone),
                AddressLine = model.AddressLine.Trim(),
                Ward = model.Ward?.Trim(),
                District = model.District?.Trim(),
                City = model.City?.Trim(),
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm địa chỉ giao hàng mới!";
            return RedirectToAction("Profile", new { tab = "address" });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(long id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idClaim, out var userId))
                return RedirectToAction("Login");

            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (address != null)
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xoá địa chỉ giao hàng.";
            }

            return RedirectToAction("Profile", new { tab = "address" });
        }

        // ===== ĐĂNG XUẤT =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
