using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Hubs;

// Tự động chuyển thư mục làm việc về đúng thư mục chứa appsettings.json và wwwroot
// nếu người dùng chạy từ thư mục gốc của Solution
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
{
    var subProjectDir = Path.Combine(Directory.GetCurrentDirectory(), "baitaplontmdtwebdatdoan");
    if (File.Exists(Path.Combine(subProjectDir, "appsettings.json")))
    {
        Directory.SetCurrentDirectory(subProjectDir);
    }
}

var builder = WebApplication.CreateBuilder(args);

string ResolveConnectionString(IConfiguration config)
{
    var configured = config.GetConnectionString("FoodieDB")
        ?? "Server=.\\SQLEXPRESS01;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;";

    var candidates = new List<string>
    {
        configured,
        "Server=.\\SQLEXPRESS01;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;",
        "Server=.\\SQLEXPRESS;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;",
        "Server=.\\SQLEXPRESS02;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;",
        "Server=.;Database=FoodieDB;Trusted_Connection=True;TrustServerCertificate=True;"
    };

    foreach (var candidate in candidates.Distinct())
    {
        try
        {
            var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(candidate)
            {
                ConnectTimeout = 2
            };
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
            conn.Open();
            return candidate;
        }
        catch
        {
            // Thử instance tiếp theo
        }
    }

    return configured;
}

var activeConnectionString = ResolveConnectionString(builder.Configuration);

builder.Services.AddDbContext<baitaplontmdtwebdatdoan.Data.FoodieDbContext>(options =>
    options.UseSqlServer(activeConnectionString));

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

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
                || context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/Chat/GetMessages")
                || context.Request.Path.StartsWithSegments("/Chat/SendMessage")
                || context.Request.Path.StartsWithSegments("/Cart/AddAjax")
                || context.Request.Path.StartsWithSegments("/Cart/RemoveAjax")
                || context.Request.Path.StartsWithSegments("/Cart/UpdateQuantityAjax")
                || context.Request.Path.StartsWithSegments("/Favorite/Toggle")
                || context.Request.Path.StartsWithSegments("/Follow/Toggle");

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

// Khởi tạo bảng mở rộng trong nền (không chặn quá trình khởi động web)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FoodieDbContext>();
        await db.Database.ExecuteSqlRawAsync(@"
            IF OBJECT_ID('dbo.chat_messages', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.chat_messages (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    sender_id BIGINT NOT NULL,
                    restaurant_id BIGINT NOT NULL,
                    customer_id BIGINT NOT NULL,
                    sender_role NVARCHAR(50) NOT NULL DEFAULT 'customer',
                    message NVARCHAR(MAX) NOT NULL,
                    is_read BIT NOT NULL DEFAULT 0,
                    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END

            IF OBJECT_ID('dbo.support_messages', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.support_messages (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    customer_id BIGINT NOT NULL,
                    sender_id BIGINT NULL,
                    sender_role NVARCHAR(50) NOT NULL DEFAULT 'customer',
                    sender_name NVARCHAR(150) NOT NULL DEFAULT N'Khách hàng',
                    message NVARCHAR(MAX) NOT NULL,
                    is_human_mode BIT NOT NULL DEFAULT 0,
                    is_read BIT NOT NULL DEFAULT 0,
                    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                );
            END

            IF OBJECT_ID('dbo.restaurant_followers', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.restaurant_followers (
                    user_id BIGINT NOT NULL,
                    restaurant_id BIGINT NOT NULL,
                    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT pk_restaurant_followers PRIMARY KEY (user_id, restaurant_id)
                );
            END
        ");
    }
    catch
    {
    }
});

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
