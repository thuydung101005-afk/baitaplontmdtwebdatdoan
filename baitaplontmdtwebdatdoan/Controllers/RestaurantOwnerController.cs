using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using baitaplontmdtwebdatdoan.Data;
using baitaplontmdtwebdatdoan.Models;
using baitaplontmdtwebdatdoan.Hubs;

namespace baitaplontmdtwebdatdoan.Controllers
{
    [Authorize(Roles = "restaurant_owner")]
    public class RestaurantOwnerController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IHubContext<OrderHub> _hub;
        private readonly IWebHostEnvironment _env;

        public RestaurantOwnerController(FoodieDbContext context, IHubContext<OrderHub> hub, IWebHostEnvironment env)
        {
            _context = context;
            _hub = hub;
            _env = env;
        }

        private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /RestaurantOwner - trang chủ dashboard
        public async Task<IActionResult> Index()
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);

            if (restaurant == null)
                return RedirectToAction("CreateRestaurant");

            return RedirectToAction("Orders");
        }

        // GET /RestaurantOwner/CreateRestaurant - tạo hồ sơ quán lần đầu
        public async Task<IActionResult> CreateRestaurant()
        {
            var existing = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (existing != null) return RedirectToAction("Orders");

            return View(new RestaurantSettingsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRestaurant(RestaurantSettingsViewModel model)
        {
            var existing = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (existing != null) return RedirectToAction("Orders");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string uploadFolder = Path.Combine(_env.WebRootPath, "images", "restaurants");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string? logoUrl = model.LogoUrl;
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                string uniqueFileName = $"logo_{Guid.NewGuid()}_{Path.GetFileName(model.LogoFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(fileStream);
                }
                logoUrl = $"/images/restaurants/{uniqueFileName}";
            }

            string? coverUrl = model.CoverImageUrl;
            if (model.CoverFile != null && model.CoverFile.Length > 0)
            {
                string uniqueFileName = $"cover_{Guid.NewGuid()}_{Path.GetFileName(model.CoverFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CoverFile.CopyToAsync(fileStream);
                }
                coverUrl = $"/images/restaurants/{uniqueFileName}";
            }

            var restaurant = new Restaurant
            {
                OwnerId = CurrentUserId,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Address = model.Address.Trim(),
                Phone = model.Phone.Trim(),
                LogoUrl = logoUrl,
                CoverImageUrl = coverUrl,
                Status = "pending_approval",
                MinOrderAmount = model.MinOrderAmount,
                PrepTimeMinutes = model.PrepTimeMinutes,
                AvgRating = 0,
                TotalReviews = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tạo hồ sơ nhà hàng thành công! Đang chờ quản trị viên phê duyệt.";
            return RedirectToAction("Settings");
        }

        // GET /RestaurantOwner/Settings - Cài đặt hồ sơ & ảnh đại diện, ảnh bìa nhà hàng
        public async Task<IActionResult> Settings()
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            ViewBag.Restaurant = restaurant;
            ViewBag.RestaurantName = restaurant.Name;
            ViewBag.RestaurantStatus = restaurant.Status;

            var vm = new RestaurantSettingsViewModel
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Description = restaurant.Description,
                Address = restaurant.Address ?? string.Empty,
                Phone = restaurant.Phone ?? string.Empty,
                MinOrderAmount = restaurant.MinOrderAmount,
                PrepTimeMinutes = restaurant.PrepTimeMinutes,
                LogoUrl = restaurant.LogoUrl,
                CoverImageUrl = restaurant.CoverImageUrl,
                Status = restaurant.Status,
                AvgRating = restaurant.AvgRating,
                TotalReviews = restaurant.TotalReviews
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(RestaurantSettingsViewModel model)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            if (!ModelState.IsValid)
            {
                ViewBag.Restaurant = restaurant;
                ViewBag.RestaurantName = restaurant.Name;
                ViewBag.RestaurantStatus = restaurant.Status;
                return View(model);
            }

            string uploadFolder = Path.Combine(_env.WebRootPath, "images", "restaurants");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Tải logo / ảnh đại diện quán
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                string uniqueFileName = $"logo_{Guid.NewGuid()}_{Path.GetFileName(model.LogoFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(fileStream);
                }
                restaurant.LogoUrl = $"/images/restaurants/{uniqueFileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.LogoUrl))
            {
                restaurant.LogoUrl = model.LogoUrl.Trim();
            }

            // Tải ảnh bìa cover
            if (model.CoverFile != null && model.CoverFile.Length > 0)
            {
                string uniqueFileName = $"cover_{Guid.NewGuid()}_{Path.GetFileName(model.CoverFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.CoverFile.CopyToAsync(fileStream);
                }
                restaurant.CoverImageUrl = $"/images/restaurants/{uniqueFileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.CoverImageUrl))
            {
                restaurant.CoverImageUrl = model.CoverImageUrl.Trim();
            }

            restaurant.Name = model.Name.Trim();
            restaurant.Description = model.Description?.Trim();
            restaurant.Address = model.Address.Trim();
            restaurant.Phone = model.Phone.Trim();
            restaurant.MinOrderAmount = model.MinOrderAmount;
            restaurant.PrepTimeMinutes = model.PrepTimeMinutes;
            restaurant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật hồ sơ và ảnh nhà hàng thành công!";
            return RedirectToAction("Settings");
        }

        // GET /RestaurantOwner/Orders - Đơn hàng của quán
        public async Task<IActionResult> Orders(string? status = "processing")
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            ViewBag.Restaurant = restaurant;
            ViewBag.RestaurantName = restaurant.Name;
            ViewBag.RestaurantStatus = restaurant.Status;
            ViewBag.CurrentStatus = status ?? "processing";

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.User)
                .Where(o => o.RestaurantId == restaurant.Id);

            if (status == "processing")
            {
                query = query.Where(o => o.OrderStatus == "pending" || o.OrderStatus == "confirmed" || o.OrderStatus == "preparing");
            }
            else if (status == "shipping")
            {
                query = query.Where(o => o.OrderStatus == "ready_for_pickup" || o.OrderStatus == "picked_up" || o.OrderStatus == "delivering");
            }
            else if (status == "completed")
            {
                query = query.Where(o => o.OrderStatus == "completed");
            }
            else if (status == "cancelled")
            {
                query = query.Where(o => o.OrderStatus == "cancelled");
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, string newStatus)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == restaurant.Id);
            if (order == null) return NotFound();

            order.OrderStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                ChangedAt = DateTime.UtcNow,
                Note = "Nhà hàng cập nhật trạng thái"
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"order-{id}").SendAsync("StatusUpdated", newStatus, GetStatusLabel(newStatus));

            return RedirectToAction("Orders");
        }

        // ===== QUẢN LÝ THỰC ĐƠN / MÓN ĂN =====

        // GET /RestaurantOwner/Menu
        public async Task<IActionResult> Menu(long? categoryId, string? search)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            ViewBag.Restaurant = restaurant;
            ViewBag.RestaurantName = restaurant.Name;
            ViewBag.RestaurantStatus = restaurant.Status;

            var categories = await _context.MenuCategories
                .Where(c => c.RestaurantId == restaurant.Id)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchKeyword = search;

            var query = _context.MenuItems
                .Include(m => m.Category)
                .Where(m => m.RestaurantId == restaurant.Id && m.DeletedAt == null);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Name.Contains(search) || (m.Description != null && m.Description.Contains(search)));
            }

            var items = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();

            ViewBag.TotalCount = items.Count;
            ViewBag.AvailableCount = items.Count(i => i.IsAvailable);
            ViewBag.OutOfStockCount = items.Count(i => !i.IsAvailable);

            return View(items);
        }

        // GET /RestaurantOwner/CreateMenuItem
        public async Task<IActionResult> CreateMenuItem()
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            ViewBag.Categories = await _context.MenuCategories
                .Where(c => c.RestaurantId == restaurant.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(new MenuItemFormViewModel());
        }

        // POST /RestaurantOwner/CreateMenuItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMenuItem(MenuItemFormViewModel model)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.MenuCategories
                    .Where(c => c.RestaurantId == restaurant.Id)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(model);
            }

            // Xử lý tạo danh mục mới nếu được nhập
            long? targetCategoryId = model.CategoryId;
            if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
            {
                var trimmed = model.NewCategoryName.Trim();
                var cat = await _context.MenuCategories
                    .FirstOrDefaultAsync(c => c.RestaurantId == restaurant.Id && c.Name.ToLower() == trimmed.ToLower());

                if (cat == null)
                {
                    cat = new MenuCategory
                    {
                        RestaurantId = restaurant.Id,
                        Name = trimmed,
                        DisplayOrder = 0
                    };
                    _context.MenuCategories.Add(cat);
                    await _context.SaveChangesAsync();
                }
                targetCategoryId = cat.Id;
            }

            // Xử lý ảnh tải lên (hỗ trợ nhiều ảnh)
            var uploadedUrls = new List<string>();
            string uploadFolder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            if (model.ImageFiles != null && model.ImageFiles.Count > 0)
            {
                foreach (var file in model.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        uploadedUrls.Add($"/images/products/{uniqueFileName}");
                    }
                }
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }
                uploadedUrls.Add($"/images/products/{uniqueFileName}");
            }

            if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                var manualUrls = model.ImageUrl.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                uploadedUrls.AddRange(manualUrls);
            }

            string? finalImageUrl = uploadedUrls.Count > 0 ? string.Join(",", uploadedUrls.Distinct()) : null;

            var menuItem = new MenuItem
            {
                RestaurantId = restaurant.Id,
                CategoryId = targetCategoryId,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                BasePrice = model.BasePrice,
                ImageUrl = finalImageUrl,
                IsAvailable = model.IsAvailable,
                IsBestSeller = model.IsBestSeller,
                CreatedAt = DateTime.UtcNow
            };

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm món \"{menuItem.Name}\" vào thực đơn!";
            return RedirectToAction("Menu");
        }

        // GET /RestaurantOwner/EditMenuItem/5
        public async Task<IActionResult> EditMenuItem(long id)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurant.Id && m.DeletedAt == null);

            if (item == null) return NotFound();

            ViewBag.Categories = await _context.MenuCategories
                .Where(c => c.RestaurantId == restaurant.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var vm = new MenuItemFormViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                BasePrice = item.BasePrice,
                CategoryId = item.CategoryId,
                ImageUrl = item.ImageUrl,
                IsAvailable = item.IsAvailable,
                IsBestSeller = item.IsBestSeller
            };

            return View(vm);
        }

        // POST /RestaurantOwner/EditMenuItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(long id, MenuItemFormViewModel model)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurant.Id && m.DeletedAt == null);

            if (item == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.MenuCategories
                    .Where(c => c.RestaurantId == restaurant.Id)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View(model);
            }

            // Xử lý tạo danh mục mới nếu được nhập
            long? targetCategoryId = model.CategoryId;
            if (!string.IsNullOrWhiteSpace(model.NewCategoryName))
            {
                var trimmed = model.NewCategoryName.Trim();
                var cat = await _context.MenuCategories
                    .FirstOrDefaultAsync(c => c.RestaurantId == restaurant.Id && c.Name.ToLower() == trimmed.ToLower());

                if (cat == null)
                {
                    cat = new MenuCategory
                    {
                        RestaurantId = restaurant.Id,
                        Name = trimmed,
                        DisplayOrder = 0
                    };
                    _context.MenuCategories.Add(cat);
                    await _context.SaveChangesAsync();
                }
                targetCategoryId = cat.Id;
            }

            // Xử lý upload ảnh mới (hỗ trợ nhiều ảnh)
            var currentUrls = !string.IsNullOrWhiteSpace(model.ImageUrl)
                ? model.ImageUrl.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new List<string>();

            string editUploadFolder = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(editUploadFolder))
            {
                Directory.CreateDirectory(editUploadFolder);
            }

            if (model.ImageFiles != null && model.ImageFiles.Count > 0)
            {
                foreach (var file in model.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        string filePath = Path.Combine(editUploadFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        currentUrls.Add($"/images/products/{uniqueFileName}");
                    }
                }
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                string filePath = Path.Combine(editUploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }
                currentUrls.Add($"/images/products/{uniqueFileName}");
            }

            item.ImageUrl = currentUrls.Count > 0 ? string.Join(",", currentUrls.Distinct()) : null;

            item.Name = model.Name.Trim();
            item.Description = model.Description?.Trim();
            item.BasePrice = model.BasePrice;
            item.CategoryId = targetCategoryId;
            item.IsAvailable = model.IsAvailable;
            item.IsBestSeller = model.IsBestSeller;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật món \"{item.Name}\" thành công!";
            return RedirectToAction("Menu");
        }

        // POST /RestaurantOwner/ToggleItemStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleItemStatus(long id)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurant.Id && m.DeletedAt == null);

            if (item == null) return NotFound();

            item.IsAvailable = !item.IsAvailable;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chuyển trạng thái món \"{item.Name}\" sang: {(item.IsAvailable ? "Đang bán" : "Tạm hết")}.";
            return RedirectToAction("Menu");
        }

        // POST /RestaurantOwner/DeleteMenuItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(long id)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return Forbid();

            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == restaurant.Id && m.DeletedAt == null);

            if (item == null) return NotFound();

            // Soft delete
            item.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xoá món \"{item.Name}\" khỏi thực đơn.";
            return RedirectToAction("Menu");
        }

        // GET /RestaurantOwner/Revenue - Báo cáo doanh thu & Hoa hồng chiết khấu sàn
        public async Task<IActionResult> Revenue(string? period = "30days")
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == CurrentUserId);
            if (restaurant == null) return RedirectToAction("CreateRestaurant");

            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantId == restaurant.Id && o.OrderStatus == "completed");

            var now = DateTime.UtcNow;
            int chartDays = 30;
            if (period == "today")
            {
                var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                query = query.Where(o => o.CreatedAt >= startOfToday);
                chartDays = 7;
            }
            else if (period == "7days")
            {
                var sevenDaysAgo = now.AddDays(-7);
                query = query.Where(o => o.CreatedAt >= sevenDaysAgo);
                chartDays = 7;
            }
            else if (period == "30days")
            {
                var thirtyDaysAgo = now.AddDays(-30);
                query = query.Where(o => o.CreatedAt >= thirtyDaysAgo);
                chartDays = 30;
            }
            else if (period == "all")
            {
                // Toàn bộ thời gian: không lọc theo ngày tạo
                chartDays = 30;
            }

            var completedOrders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            const decimal commissionRate = 18.0m; // 18% hoa hồng sàn tiêu chuẩn
            const decimal paymentFeeRate = 1.5m;  // 1.5% phí xử lý giao dịch

            decimal totalGross = completedOrders.Sum(o => o.Subtotal);
            decimal totalDiscount = completedOrders.Sum(o => o.DiscountAmount);
            decimal totalCommission = Math.Round(totalGross * (commissionRate / 100m), 0);
            decimal totalPaymentFee = Math.Round(totalGross * (paymentFeeRate / 100m), 0);
            decimal netPayout = Math.Max(0, totalGross - totalCommission - totalPaymentFee);

            // Thống kê theo ngày để vẽ biểu đồ
            var dailyStats = new List<DailyRevenuePoint>();
            for (int i = chartDays - 1; i >= 0; i--)
            {
                var targetDate = now.Date.AddDays(-i);
                var ordersOnDate = completedOrders.Where(o => o.CreatedAt.Date == targetDate).ToList();
                var grossOnDate = ordersOnDate.Sum(o => o.Subtotal);
                var commOnDate = Math.Round(grossOnDate * (commissionRate / 100m), 0);
                var payOnDate = Math.Round(grossOnDate * (paymentFeeRate / 100m), 0);
                var netOnDate = Math.Max(0, grossOnDate - commOnDate - payOnDate);

                dailyStats.Add(new DailyRevenuePoint
                {
                    DateLabel = targetDate.ToString("dd/MM"),
                    GrossSales = grossOnDate,
                    NetPayout = netOnDate,
                    OrderCount = ordersOnDate.Count
                });
            }

            // Danh sách sao kê từng đơn hàng
            var settlements = completedOrders.Select(o =>
            {
                var comm = Math.Round(o.Subtotal * (commissionRate / 100m), 0);
                var payFee = Math.Round(o.Subtotal * (paymentFeeRate / 100m), 0);
                var net = Math.Max(0, o.Subtotal - comm - payFee);
                return new OrderSettlementItem
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CompletedAt = o.UpdatedAt,
                    Subtotal = o.Subtotal,
                    CommissionFee = comm,
                    PaymentFee = payFee,
                    NetAmount = net,
                    PaymentMethod = o.PaymentMethod,
                    SettlementStatus = o.PaymentStatus == "paid" ? "settled" : "processing"
                };
            }).ToList();

            var vm = new RestaurantRevenueViewModel
            {
                Restaurant = restaurant,
                Period = period ?? "30days",
                TotalGrossSales = totalGross,
                CommissionRate = commissionRate,
                PaymentFeeRate = paymentFeeRate,
                TotalCommission = totalCommission,
                TotalPaymentFee = totalPaymentFee,
                TotalDiscount = totalDiscount,
                NetPayout = netPayout,
                CompletedOrderCount = completedOrders.Count,
                AverageOrderValue = completedOrders.Count > 0 ? Math.Round(totalGross / completedOrders.Count, 0) : 0,
                DailyStats = dailyStats,
                OrderSettlements = settlements
            };

            return View(vm);
        }

        // POST /RestaurantOwner/RequestPayout - Yêu cầu rút tiền về tài khoản ngân hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPayout(decimal amount, string bankName, string accountNumber, string accountHolder)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(accountNumber))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin tài khoản ngân hàng và số tiền hợp lệ.";
                return RedirectToAction("Revenue");
            }

            TempData["SuccessMessage"] = $"Đã tạo lệnh yêu cầu quyết toán {amount:N0}đ về tài khoản {bankName} ({accountNumber} - {accountHolder}). Foodie Finance sẽ xử lý giải ngân trong vòng 24 giờ làm việc.";
            return RedirectToAction("Revenue");
        }

        private string GetStatusLabel(string status) => status switch
        {
            "pending" => "Chờ xác nhận",
            "confirmed" => "Đã xác nhận",
            "preparing" => "Đang chuẩn bị",
            "ready_for_pickup" => "Sẵn sàng giao",
            "picked_up" => "Đã lấy hàng",
            "delivering" => "Đang giao",
            "completed" => "Hoàn thành",
            "cancelled" => "Đã huỷ",
            _ => status
        };
    }
}