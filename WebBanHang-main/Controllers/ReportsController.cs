using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _roleService;
        private readonly IMemoryCache _cache;

        public ReportsController(ApplicationDbContext context, RoleService roleService, IMemoryCache cache)
        {
            _context = context;
            _roleService = roleService;
            _cache = cache;
        }

        private bool CanViewReports()
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            return role == 1 || role == 2;
        }

        private sealed class InventoryReportData
        {
            public List<InventoryReportRow> OrderedRows { get; set; } = new();
            public decimal TotalPhysicalQuantity { get; set; }
            public decimal TotalSellableQuantity { get; set; }
            public decimal TotalInventoryValue { get; set; }
            public int OutOfStockCount { get; set; }
            public int LowStockCount { get; set; }
            public int ExpiringBatchCount { get; set; }
            public int ExpiredBatchCount { get; set; }
            public List<CategoryInventorySummary> CategorySummaries { get; set; } = new();
            public List<InventoryStatusSummary> StatusSummaries { get; set; } = new();
        }

        private sealed class InventoryBatchReportSummary
        {
            public int ProductId { get; set; }
            public int BranchId { get; set; }
            public int BatchCount { get; set; }
            public decimal SellableQuantity { get; set; }
            public decimal ExpiringQuantity { get; set; }
            public decimal ExpiredQuantity { get; set; }
            public DateTime? NearestExpiryDate { get; set; }
        }

        private async Task<List<string>> GetInventoryProvincesAsync()
        {
            return await _cache.GetOrCreateAsync("inventory-report:provinces:v1", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.Province != null && b.Province != string.Empty)
                    .Select(b => b.Province)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();
            }) ?? new List<string>();
        }

        private async Task<List<string>> GetInventoryDistrictsAsync(string province)
        {
            var selectedProvince = province.Trim();
            if (string.IsNullOrEmpty(selectedProvince))
            {
                return new List<string>();
            }

            return await _cache.GetOrCreateAsync($"inventory-report:districts:v1:{selectedProvince}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.Province == selectedProvince && b.District != null && b.District != string.Empty)
                    .Select(b => b.District)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();
            }) ?? new List<string>();
        }

        private async Task<List<Branch>> GetInventoryBranchesAsync(string province, string district)
        {
            var selectedProvince = province.Trim();
            var selectedDistrict = district.Trim();
            if (string.IsNullOrEmpty(selectedProvince) || string.IsNullOrEmpty(selectedDistrict))
            {
                return new List<Branch>();
            }

            return await _cache.GetOrCreateAsync($"inventory-report:branches:v1:{selectedProvince}:{selectedDistrict}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.Province == selectedProvince && b.District == selectedDistrict)
                    .OrderBy(b => b.Name)
                    .ToListAsync();
            }) ?? new List<Branch>();
        }

        public async Task<IActionResult> Index(string type = "day", DateTime? startDate = null, DateTime? endDate = null, int? status = null, string paymentMethod = null)
        {
            if (!CanViewReports())
            {
                return RedirectToAction("Index", "Home");
            }

            var today = Services.PromotionService.GetVietnamNow().Date;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            ViewBag.Filter = type;

            // CARD THỐNG KÊ TỔNG QUAN HỆ THỐNG
            ViewBag.TotalRevenue =
                await _context.OrderDetails
                .Where(d => d.Order.Status == 3)
                .SumAsync(d => d.Price * d.Quantity);

            ViewBag.TodayRevenue =
                await _context.OrderDetails
                .Where(d =>
                    d.Order.Status == 3 &&
                    d.Order.OrderDate.Date == today)
                .SumAsync(d => d.Price * d.Quantity);

            ViewBag.MonthRevenue =
                await _context.OrderDetails
                .Where(d =>
                    d.Order.Status == 3 &&
                    d.Order.OrderDate >= thisMonth)
                .SumAsync(d => d.Price * d.Quantity);

            ViewBag.OrderCount =
                await _context.Orders
                .CountAsync(x => x.Status == 3);

            ViewBag.CancelledOrderCount =
                await _context.Orders
                .CountAsync(x => x.Status == 5);

            // ----------------------------------------------------
            // THỐNG KÊ THEO BỘ LỌC KỲ NÀY / PHƯƠNG THỨC THANH TOÁN
            // ----------------------------------------------------
            var orderQuery = _context.Orders.AsQueryable();
            if (startDate.HasValue)
            {
                orderQuery = orderQuery.Where(o => o.OrderDate >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                var endLimit = endDate.Value.Date.AddDays(1);
                orderQuery = orderQuery.Where(o => o.OrderDate < endLimit);
            }

            var statusFilter = status ?? 3; // Mặc định là đơn hàng thành công (Status = 3)
            var filteredOrders = orderQuery.Where(o => o.Status == statusFilter);

            // Áp dụng bộ lọc Phương thức thanh toán vào filteredOrders
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                if (paymentMethod == "COD")
                {
                    filteredOrders = filteredOrders.Where(o => 
                        o.ShippingMethod != "Direct Purchase" &&
                        (((o.PaymentMethod == null || o.PaymentMethod == "") && !o.IsPaid) || 
                        (o.PaymentMethod == "COD"))
                    );
                }
                else if (paymentMethod == "Online")
                {
                    filteredOrders = filteredOrders.Where(o => 
                        o.ShippingMethod != "Direct Purchase" &&
                        (((o.PaymentMethod == null || o.PaymentMethod == "") && o.IsPaid) || 
                        (o.PaymentMethod != null && o.PaymentMethod != "" && o.PaymentMethod != "COD"))
                    );
                }
                else if (paymentMethod == "Direct")
                {
                    filteredOrders = filteredOrders.Where(o => o.ShippingMethod == "Direct Purchase");
                }
            }

            // Thống kê doanh thu và số lượng đơn hàng trong kỳ
            var rangeOrderCount = await filteredOrders.CountAsync();
            var rangeRevenue = await _context.OrderDetails
                .Where(d => filteredOrders.Select(o => o.Id).Contains(d.OrderId))
                .SumAsync(d => d.Price * d.Quantity);

            // Thống kê đơn hàng Ship COD (loại bỏ mua trực tiếp tại cửa hàng)
            var codOrders = filteredOrders.Where(o => 
                o.ShippingMethod != "Direct Purchase" &&
                (((o.PaymentMethod == null || o.PaymentMethod == "") && !o.IsPaid) || 
                (o.PaymentMethod == "COD"))
            );
            var codCount = await codOrders.CountAsync();
            var codRevenue = await _context.OrderDetails
                .Where(d => codOrders.Select(o => o.Id).Contains(d.OrderId))
                .SumAsync(d => d.Price * d.Quantity);

            // Thống kê đơn hàng Thanh toán trực tuyến (loại bỏ mua trực tiếp tại cửa hàng)
            var onlineOrders = filteredOrders.Where(o => 
                o.ShippingMethod != "Direct Purchase" &&
                (((o.PaymentMethod == null || o.PaymentMethod == "") && o.IsPaid) || 
                (o.PaymentMethod != null && o.PaymentMethod != "" && o.PaymentMethod != "COD"))
            );
            var onlineCount = await onlineOrders.CountAsync();
            var onlineRevenue = await _context.OrderDetails
                .Where(d => onlineOrders.Select(o => o.Id).Contains(d.OrderId))
                .SumAsync(d => d.Price * d.Quantity);

            // Thống kê đơn hàng mua trực tiếp tại cửa hàng
            var directOrders = filteredOrders.Where(o => o.ShippingMethod == "Direct Purchase");
            var directCount = await directOrders.CountAsync();
            var directRevenue = await _context.OrderDetails
                .Where(d => directOrders.Select(o => o.Id).Contains(d.OrderId))
                .SumAsync(d => d.Price * d.Quantity);

            // Gửi dữ liệu thống kê bộ lọc xuống ViewBag
            ViewBag.RangeRevenue = rangeRevenue;
            ViewBag.RangeOrderCount = rangeOrderCount;
            ViewBag.CodRevenue = codRevenue;
            ViewBag.CodCount = codCount;
            ViewBag.OnlineRevenue = onlineRevenue;
            ViewBag.OnlineCount = onlineCount;
            ViewBag.DirectRevenue = directRevenue;
            ViewBag.DirectCount = directCount;

            // Truyền ngày lọc thực tế xuống View (Có thể null để hiển thị trống trên input khi xóa lọc)
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;
            ViewBag.PaymentMethod = paymentMethod;

            //--------------------------------
            // BIỂU ĐỒ DOANH THU ĐỒNG BỘ THEO NGÀY
            //--------------------------------
            var labels = new List<string>();
            var data = new List<decimal>();

            // Chỉ nạp dữ liệu biểu đồ nếu bộ lọc ngày hoạt động
            if (startDate.HasValue && endDate.HasValue)
            {
                var chartStartDate = startDate.Value;
                var chartEndDate = endDate.Value;

                var chartOrders = _context.Orders.Where(o => o.Status == statusFilter && o.OrderDate >= chartStartDate.Date && o.OrderDate < chartEndDate.Date.AddDays(1));

                // Áp dụng bộ lọc Phương thức thanh toán vào biểu đồ
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    if (paymentMethod == "COD")
                    {
                        chartOrders = chartOrders.Where(o => 
                            o.ShippingMethod != "Direct Purchase" &&
                            (((o.PaymentMethod == null || o.PaymentMethod == "") && !o.IsPaid) || 
                            (o.PaymentMethod == "COD"))
                        );
                    }
                    else if (paymentMethod == "Online")
                    {
                        chartOrders = chartOrders.Where(o => 
                            o.ShippingMethod != "Direct Purchase" &&
                            (((o.PaymentMethod == null || o.PaymentMethod == "") && o.IsPaid) || 
                            (o.PaymentMethod != null && o.PaymentMethod != "" && o.PaymentMethod != "COD"))
                        );
                    }
                    else if (paymentMethod == "Direct")
                    {
                        chartOrders = chartOrders.Where(o => o.ShippingMethod == "Direct Purchase");
                    }
                }

                // Truy vấn doanh thu theo ngày của các đơn hàng trong kỳ hiển thị biểu đồ
                var dailyRevenueQuery = await _context.OrderDetails
                    .Where(d => chartOrders.Select(o => o.Id).Contains(d.OrderId))
                    .GroupBy(d => d.Order.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.Price * x.Quantity)
                    })
                    .ToDictionaryAsync(x => x.Date, x => x.Revenue);

                for (var date = chartStartDate.Date; date <= chartEndDate.Date; date = date.AddDays(1))
                {
                    labels.Add(date.ToString("dd/MM"));
                    data.Add(dailyRevenueQuery.TryGetValue(date, out var rev) ? rev : 0);
                }

                ViewBag.ChartStartDate = chartStartDate.ToString("yyyy-MM-dd");
                ViewBag.ChartEndDate = chartEndDate.ToString("yyyy-MM-dd");
            }
            else
            {
                ViewBag.ChartStartDate = null;
                ViewBag.ChartEndDate = null;
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            return View();
        }

        public async Task<IActionResult> Inventory(string? province, string? district, int? branchId, int? categoryId, string? stockStatus, string? expiryStatus, int lowStockThreshold = 10, int warningDays = 7, int page = 1, int pageSize = 50)
        {
            if (!CanViewReports())
            {
                return RedirectToAction("Index", "Home");
            }

            if (lowStockThreshold <= 0)
            {
                lowStockThreshold = 10;
            }

            if (warningDays <= 0)
            {
                warningDays = 7;
            }

            var allowedPageSizes = new[] { 25, 50, 100, 200 };
            if (!allowedPageSizes.Contains(pageSize))
            {
                pageSize = 50;
            }

            if (page <= 0)
            {
                page = 1;
            }

            var today = Services.PromotionService.GetVietnamNow().Date;
            var warningLimit = today.AddDays(warningDays);
            var selectedProvince = province?.Trim() ?? string.Empty;
            var selectedDistrict = district?.Trim() ?? string.Empty;
            Branch? selectedBranch = null;

            if (branchId.HasValue)
            {
                selectedBranch = await _context.Branches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == branchId.Value);

                if (selectedBranch != null)
                {
                    selectedProvince = selectedBranch.Province;
                    selectedDistrict = selectedBranch.District;
                }
            }

            var provinces = await GetInventoryProvincesAsync();
            var districts = string.IsNullOrEmpty(selectedProvince)
                ? new List<string>()
                : await GetInventoryDistrictsAsync(selectedProvince);
            var branchOptions = string.IsNullOrEmpty(selectedProvince) || string.IsNullOrEmpty(selectedDistrict)
                ? new List<Branch>()
                : await GetInventoryBranchesAsync(selectedProvince, selectedDistrict);

            if (selectedBranch != null && !branchOptions.Any(b => b.Id == selectedBranch.Id))
            {
                branchOptions.Add(selectedBranch);
                branchOptions = branchOptions.OrderBy(b => b.Name).ToList();
            }

            var categories = await _cache.GetOrCreateAsync("inventory-report:categories:v1", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }) ?? new List<Category>();

            stockStatus = stockStatus?.Trim().ToLowerInvariant() ?? string.Empty;
            expiryStatus = expiryStatus?.Trim().ToLowerInvariant() ?? string.Empty;

            var reportCacheKey = string.Join('|',
                "inventory-report:v3",
                selectedProvince,
                selectedDistrict,
                branchId?.ToString() ?? string.Empty,
                categoryId?.ToString() ?? string.Empty,
                stockStatus,
                expiryStatus,
                lowStockThreshold,
                warningDays,
                today.ToString("yyyyMMdd"));

            var reportData = await _cache.GetOrCreateAsync(reportCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);

                var inventoryQuery = _context.ProductInventories
                    .AsNoTracking()
                    .AsQueryable();

                if (branchId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(i => i.BranchId == branchId.Value);
                }
                else
                {
                    if (!string.IsNullOrEmpty(selectedProvince))
                    {
                        inventoryQuery = inventoryQuery.Where(i => i.Branch.Province == selectedProvince);
                    }

                    if (!string.IsNullOrEmpty(selectedDistrict))
                    {
                        inventoryQuery = inventoryQuery.Where(i => i.Branch.District == selectedDistrict);
                    }
                }

                if (categoryId.HasValue)
                {
                    inventoryQuery = inventoryQuery.Where(i => i.Product.CategoryId == categoryId.Value);
                }

                var inventories = await inventoryQuery
                    .OrderBy(i => i.Branch.Name)
                    .ThenBy(i => i.Product.Name)
                    .Select(i => new
                    {
                        i.ProductId,
                        ProductName = i.Product.Name,
                        ProductUnit = i.Product.Unit,
                        ProductPrice = i.Product.Price,
                        CategoryName = i.Product.Category != null ? i.Product.Category.Name : "Chưa phân loại",
                        i.BranchId,
                        BranchName = i.Branch.Name,
                        BranchDistrict = i.Branch.District,
                        BranchProvince = i.Branch.Province,
                        i.Quantity
                    })
                    .ToListAsync();

                var productIds = inventories.Select(i => i.ProductId).Distinct().ToList();
                var branchIds = inventories.Select(i => i.BranchId).Distinct().ToList();

                var batchSummariesByInventory = new Dictionary<(int ProductId, int BranchId), InventoryBatchReportSummary>();
                if (productIds.Any() && branchIds.Any())
                {
                    var batchSummaries = await _context.InventoryBatches
                        .AsNoTracking()
                        .Where(b => productIds.Contains(b.ProductId) && branchIds.Contains(b.BranchId))
                        .GroupBy(b => new { b.ProductId, b.BranchId })
                        .Select(g => new InventoryBatchReportSummary
                        {
                            ProductId = g.Key.ProductId,
                            BranchId = g.Key.BranchId,
                            BatchCount = g.Count(),
                            SellableQuantity = g.Sum(b => b.Quantity > 0 && b.ExpiryDate >= today ? b.Quantity : 0m),
                            ExpiringQuantity = g.Sum(b => b.Quantity > 0 && b.ExpiryDate >= today && b.ExpiryDate <= warningLimit ? b.Quantity : 0m),
                            ExpiredQuantity = g.Sum(b => b.Quantity > 0 && b.ExpiryDate < today ? b.Quantity : 0m),
                            NearestExpiryDate = g.Min(b => b.Quantity > 0 ? (DateTime?)b.ExpiryDate : null)
                        })
                        .ToListAsync();

                    batchSummariesByInventory = batchSummaries
                        .ToDictionary(b => (b.ProductId, b.BranchId));
                }

                var rows = new List<InventoryReportRow>();
                foreach (var inventory in inventories)
                {
                    batchSummariesByInventory.TryGetValue((inventory.ProductId, inventory.BranchId), out var batchSummary);

                    var hasBatches = batchSummary != null && batchSummary.BatchCount > 0;
                    decimal sellableQuantity = hasBatches ? batchSummary.SellableQuantity : inventory.Quantity;
                    decimal expiringQuantity = hasBatches ? batchSummary.ExpiringQuantity : 0m;
                    decimal expiredQuantity = hasBatches ? batchSummary.ExpiredQuantity : 0m;
                    DateTime? nearestExpiry = hasBatches ? batchSummary.NearestExpiryDate : null;
                    int batchCount = hasBatches ? batchSummary.BatchCount : 0;

                    var rowStockStatus = sellableQuantity <= 0
                        ? "out"
                        : sellableQuantity <= lowStockThreshold ? "low" : "ok";
                    var rowExpiryStatus = expiredQuantity > 0
                        ? "expired"
                        : expiringQuantity > 0 ? "expiring" : "safe";

                    rows.Add(new InventoryReportRow
                    {
                        ProductId = inventory.ProductId,
                        ProductName = inventory.ProductName ?? "Sản phẩm",
                        Unit = inventory.ProductUnit ?? string.Empty,
                        CategoryName = inventory.CategoryName ?? "Chưa phân loại",
                        BranchId = inventory.BranchId,
                        BranchName = inventory.BranchName ?? $"Chi nhánh #{inventory.BranchId}",
                        BranchLocation = $"{inventory.BranchDistrict}, {inventory.BranchProvince}",
                        Price = inventory.ProductPrice,
                        PhysicalQuantity = inventory.Quantity,
                        SellableQuantity = sellableQuantity,
                        ExpiringQuantity = expiringQuantity,
                        ExpiredQuantity = expiredQuantity,
                        BatchCount = batchCount,
                        NearestExpiryDate = nearestExpiry,
                        InventoryValue = sellableQuantity * inventory.ProductPrice,
                        StockStatus = rowStockStatus,
                        ExpiryStatus = rowExpiryStatus
                    });
                }

                if (!string.IsNullOrEmpty(stockStatus))
                {
                    rows = rows.Where(r => r.StockStatus == stockStatus).ToList();
                }

                if (!string.IsNullOrEmpty(expiryStatus))
                {
                    rows = rows.Where(r => r.ExpiryStatus == expiryStatus).ToList();
                }

                var orderedRows = rows
                    .OrderBy(r => r.StockStatus == "out" ? 0 : r.StockStatus == "low" ? 1 : 2)
                    .ThenBy(r => r.NearestExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(r => r.BranchName)
                    .ThenBy(r => r.ProductName)
                    .ToList();

                return new InventoryReportData
                {
                    OrderedRows = orderedRows,
                    TotalPhysicalQuantity = rows.Sum(r => r.PhysicalQuantity),
                    TotalSellableQuantity = rows.Sum(r => r.SellableQuantity),
                    TotalInventoryValue = rows.Sum(r => r.InventoryValue),
                    OutOfStockCount = rows.Count(r => r.StockStatus == "out"),
                    LowStockCount = rows.Count(r => r.StockStatus == "low"),
                    ExpiringBatchCount = rows.Count(r => r.ExpiryStatus == "expiring"),
                    ExpiredBatchCount = rows.Count(r => r.ExpiryStatus == "expired"),
                    CategorySummaries = rows
                        .GroupBy(r => r.CategoryName)
                        .Select(g => new CategoryInventorySummary
                        {
                            CategoryName = g.Key,
                            Quantity = g.Sum(x => x.SellableQuantity),
                            Value = g.Sum(x => x.InventoryValue)
                        })
                        .OrderByDescending(x => x.Value)
                        .Take(10)
                        .ToList(),
                    StatusSummaries = new List<InventoryStatusSummary>
                    {
                        new() { Label = "Ổn định", Count = rows.Count(r => r.StockStatus == "ok" && r.ExpiryStatus == "safe") },
                        new() { Label = "Tồn thấp", Count = rows.Count(r => r.StockStatus == "low") },
                        new() { Label = "Hết hàng", Count = rows.Count(r => r.StockStatus == "out") },
                        new() { Label = "Sắp hết hạn", Count = rows.Count(r => r.ExpiryStatus == "expiring") },
                        new() { Label = "Có hàng hết hạn", Count = rows.Count(r => r.ExpiryStatus == "expired") }
                    }
                };
            }) ?? new InventoryReportData();

            var totalRows = reportData.OrderedRows.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var model = new InventoryReportViewModel
            {
                Rows = reportData.OrderedRows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Branches = branchOptions,
                Provinces = provinces,
                Districts = districts,
                BranchOptions = branchOptions,
                Categories = categories,
                SelectedProvince = selectedProvince,
                SelectedDistrict = selectedDistrict,
                SelectedBranchId = branchId,
                SelectedCategoryId = categoryId,
                StockStatus = stockStatus,
                ExpiryStatus = expiryStatus,
                LowStockThreshold = lowStockThreshold,
                WarningDays = warningDays,
                TotalRows = totalRows,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalPhysicalQuantity = reportData.TotalPhysicalQuantity,
                TotalSellableQuantity = reportData.TotalSellableQuantity,
                TotalInventoryValue = reportData.TotalInventoryValue,
                OutOfStockCount = reportData.OutOfStockCount,
                LowStockCount = reportData.LowStockCount,
                ExpiringBatchCount = reportData.ExpiringBatchCount,
                ExpiredBatchCount = reportData.ExpiredBatchCount,
                CategorySummaries = reportData.CategorySummaries,
                StatusSummaries = reportData.StatusSummaries
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> InventoryDistricts(string province)
        {
            if (!CanViewReports())
            {
                return Forbid();
            }

            var districts = await GetInventoryDistrictsAsync(province ?? string.Empty);
            return Json(districts.Select(d => new { value = d, label = d }));
        }

        [HttpGet]
        public async Task<IActionResult> InventoryBranches(string province, string district)
        {
            if (!CanViewReports())
            {
                return Forbid();
            }

            var branches = await GetInventoryBranchesAsync(province ?? string.Empty, district ?? string.Empty);
            return Json(branches.Select(b => new { value = b.Id.ToString(), label = b.Name }));
        }
    }
}
