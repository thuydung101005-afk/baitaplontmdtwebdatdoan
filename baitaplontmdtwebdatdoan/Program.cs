using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<baitaplontmdtwebdatdoan.Data.FoodieDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FoodieDB")));

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();   // MỚI

// Thông tin tài khoản nhận tiền để sinh mã VietQR (mục "Bank" trong appsettings.json)
builder.Services.Configure<baitaplontmdtwebdatdoan.Models.BankSettings>(
    builder.Configuration.GetSection("Bank"));

// Gửi email (mã quên mật khẩu). Thông tin SMTP đặt trong User Secrets, mục "Email"
builder.Services.Configure<baitaplontmdtwebdatdoan.Models.EmailSettings>(
    builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<baitaplontmdtwebdatdoan.Services.IAppEmailSender, baitaplontmdtwebdatdoan.Services.SmtpEmailSender>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            var isAjaxOrApi = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || context.Request.Headers["Accept"].ToString().Contains("application/json")
                || context.Request.Path.StartsWithSegments("/Cart/AddAjax")
                || context.Request.Path.StartsWithSegments("/Cart/RemoveAjax")
                || context.Request.Path.StartsWithSegments("/Cart/UpdateQuantityAjax");

            if (isAjaxOrApi)
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OrderHub>("/orderHub");
app.MapHub<ChatHub>("/chatHub");

app.Run();