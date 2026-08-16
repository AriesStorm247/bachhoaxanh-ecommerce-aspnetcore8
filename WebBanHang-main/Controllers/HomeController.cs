using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize] // Yêu cầu đăng nhập mặc định cho toàn bộ Controller
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RoleService _roleService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly InventoryBatchService _inventoryBatchService;

        public HomeController(ILogger<HomeController> logger, RoleService roleService, ApplicationDbContext context, UserManager<IdentityUser> userManager, InventoryBatchService inventoryBatchService)
        {
            _logger = logger;
            _roleService = roleService;
            _context = context;
            _userManager = userManager;
            _inventoryBatchService = inventoryBatchService;
        }

        // ===== TRANG CHỦ + TÌM KIẾM SẢN PHẨM =====
        [AllowAnonymous] // Cho phép khách chưa đăng nhập vẫn xem được hàng
        public async Task<IActionResult> Index(
            string search,
            int? categoryId,
            int? flashSaleCategoryId,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sortBy = null,
            bool showAll = false,
            bool isPromoOnly = false,
            int page = 1,
            int pageSize = 12)
        {
            ViewBag.Role = _roleService.GetRole(User);
            ViewBag.ShowAll = showAll;
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = categories;

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;

            // Fetch review counts and averages for all products using family matching logic
            var allProductsList = await _context.Products.Select(p => new { p.Id, p.Name }).ToListAsync();
            var allDbReviews = await _context.ProductReviews.ToListAsync();
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "hộp", "cây", "gói", "chai", "lon", "thùng", "lốc", "túi", "khay", "bịch", "vỉ", "tập", "hũ", "68g", "60g", "100g", "200g", "500g", "1kg", "1l", "1.5l", "2l", "6", "12", "24"
            };

            var reviewStats = new Dictionary<int, double[]>();
            foreach (var p in allProductsList)
            {
                var keywords = p.Name.Split(new[] { ' ', ',', '!', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 3 && !stopWords.Contains(w.ToLower()))
                    .ToList();

                var matchingIds = new HashSet<int> { p.Id };
                if (keywords.Count > 0)
                {
                    var rel = allProductsList
                        .Where(x => keywords.Count(k => x.Name.Contains(k, StringComparison.OrdinalIgnoreCase)) >= Math.Min(2, keywords.Count))
                        .Select(x => x.Id);
                    foreach (var rId in rel) matchingIds.Add(rId);
                }

                var prodReviews = allDbReviews.Where(r => matchingIds.Contains(r.ProductId)).ToList();
                if (prodReviews.Count > 0)
                {
                    reviewStats[p.Id] = new double[] { prodReviews.Count, prodReviews.Average(r => r.Rating) };
                }
            }
            ViewBag.DbReviewStats = reviewStats;

            // Kiểm tra xem khách đã chủ động chọn chi nhánh chưa
            bool branchExplicitlyChosen =
                HttpContext.Session.GetString("IsBranchExplicitlyChosen") == "true"
                || Request.Cookies["IsBranchExplicitlyChosen"] == "true";

            int activeBranchId = 0; // 0 = xem tổng thể (chưa chọn chi nhánh)
            if (branchExplicitlyChosen)
            {
                int? sessionBranchId = HttpContext.Session.GetInt32("ActiveBranchId");
                if (sessionBranchId == null && int.TryParse(Request.Cookies["ActiveBranchId"], out int cookieBranchId))
                    sessionBranchId = cookieBranchId;
                activeBranchId = sessionBranchId ?? 0;
            }

            ViewBag.ActiveBranchId = activeBranchId;
            if (activeBranchId > 0)
            {
                var activeBranch = await _context.Branches.FindAsync(activeBranchId);
                ViewBag.ActiveBranchName = activeBranch?.Name ?? "";
            }
            else
            {
                ViewBag.ActiveBranchName = ""; // Chưa chọn chi nhánh
            }

            var allProducts = await _context.Products.Include(p => p.Category).Where(p => p.IsVisible).ToListAsync();
            var allProductIds = allProducts.Select(p => p.Id).ToList();
            var inventoryDict = activeBranchId > 0
                ? await _inventoryBatchService.GetSellableQuantitiesAsync(allProductIds, activeBranchId)
                : await _inventoryBatchService.GetSellableQuantitiesAcrossBranchesAsync(allProductIds);
            foreach (var p in allProducts)
            {
                p.Amount = inventoryDict.TryGetValue(p.Id, out var qty) ? qty : 0m;
            }

            var discountedCategoryIds = allProducts
                .Where(p => p.DiscountedPrice < p.Price)
                .Select(p => p.CategoryId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();
            ViewBag.DiscountedCategoryIds = discountedCategoryIds;

            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsVisible)
                .OrderBy(p => p.Name);

            ViewBag.IsPromoOnly = isPromoOnly;
            ViewBag.FlashSaleCategoryId = flashSaleCategoryId;

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CurrentCategory = categoryId.Value;
                ViewBag.CurrentCategoryName = categories.FirstOrDefault(c => c.Id == categoryId.Value)?.Name;
                ViewBag.SearchQuery = null;
            }
            else if (flashSaleCategoryId.HasValue)
            {
                ViewBag.SearchQuery = null;
            }
            else if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(p => p.Name.Contains(keyword));
                ViewBag.SearchQuery = keyword;
            }

            var products = await query.ToListAsync();
            foreach (var p in products)
            {
                p.Amount = inventoryDict.TryGetValue(p.Id, out var qty) ? qty : 0m;
            }

            if (isPromoOnly)
            {
                products = products.Where(p => p.DiscountedPrice < p.Price).ToList();
            }

            // Filter by Price range
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.DiscountedPrice >= minPrice.Value).ToList();
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.DiscountedPrice <= maxPrice.Value).ToList();
            }

            // Sort by Price
            if (sortBy == "price_asc")
            {
                products = products.OrderBy(p => p.DiscountedPrice).ToList();
            }
            else if (sortBy == "price_desc")
            {
                products = products.OrderByDescending(p => p.DiscountedPrice).ToList();
            }

            int totalItems = products.Count;
            int effectivePageSize = showAll ? 10 : pageSize;
            int totalPages = (int)Math.Ceiling((double)totalItems / effectivePageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = effectivePageSize;

            bool isFiltered = showAll || categoryId.HasValue || !string.IsNullOrWhiteSpace(search) || isPromoOnly || minPrice.HasValue || maxPrice.HasValue || !string.IsNullOrEmpty(sortBy);
            if (isFiltered)
            {
                products = products.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
            }

            return View(products);
        }

        // ===== CHỌN CHI NHÁNH HOẠT ĐỘNG (SESSION & COOKIE) =====
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SelectBranch(int branchId)
        {
            var branch = await _context.Branches.FindAsync(branchId);
            if (branch != null)
            {
                HttpContext.Session.SetInt32("ActiveBranchId", branchId);
                HttpContext.Session.SetString("ActiveBranchName", branch.Name);
                HttpContext.Session.SetString("IsBranchExplicitlyChosen", "true");

                var cookieOptions = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true, IsEssential = true };
                Response.Cookies.Append("ActiveBranchId", branchId.ToString(), cookieOptions);
                Response.Cookies.Append("ActiveBranchName", branch.Name, cookieOptions);
                Response.Cookies.Append("IsBranchExplicitlyChosen", "true", cookieOptions);

                return Json(new { success = true, name = branch.Name });
            }
            return Json(new { success = false, message = "Chi nhánh không tồn tại." });
        }

        // ===== XÓA CHI NHÁNH ĐÃ CHỌN =====
        [AllowAnonymous]
        [HttpPost]
        public IActionResult ClearBranch()
        {
            HttpContext.Session.Remove("ActiveBranchId");
            HttpContext.Session.Remove("ActiveBranchName");
            HttpContext.Session.Remove("IsBranchExplicitlyChosen");

            Response.Cookies.Delete("ActiveBranchId");
            Response.Cookies.Delete("ActiveBranchName");
            Response.Cookies.Delete("IsBranchExplicitlyChosen");

            return Json(new { success = true });
        }

        // ===== TÌM CHI NHÁNH GẦN NHẤT THEO ĐỊA CHỈ GIAO HÀNG =====
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResolveNearestBranch(string addressText, string? nearestBranchName = null)
        {
            if (string.IsNullOrEmpty(addressText))
            {
                return Json(new { success = false, message = "Địa chỉ trống." });
            }

            Branch? targetBranch = null;

            // Nếu frontend truyền lên tên chi nhánh gần nhất đã tính bằng khoảng cách tọa độ, ưu tiên tìm theo tên đó
            if (!string.IsNullOrEmpty(nearestBranchName))
            {
                var cleanedName = nearestBranchName.Replace("Bách Hóa Xanh - ", "").Replace("BHX - ", "").Trim();
                if (cleanedName.Contains("("))
                {
                    cleanedName = cleanedName.Substring(0, cleanedName.IndexOf("(")).Trim();
                }

                // Thực hiện tìm kiếm theo tên hoặc địa chỉ có chứa từ khóa chi nhánh
                targetBranch = await _context.Branches
                    .FirstOrDefaultAsync(b => b.Name.Contains(cleanedName) || b.Address.Contains(cleanedName));
            }

            // Fallback sang phân tích chuỗi địa chỉ nếu không tìm thấy theo tên
            if (targetBranch == null)
            {
                var provinces = await _context.Branches.Select(b => b.Province).Distinct().ToListAsync();
                string matchedProvince = "";
                foreach (var prov in provinces)
                {
                    if (addressText.Contains(prov, StringComparison.OrdinalIgnoreCase) || 
                        (prov == "Thừa Thiên Huế" && addressText.Contains("Huế", StringComparison.OrdinalIgnoreCase)))
                    {
                        matchedProvince = prov;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(matchedProvince))
                {
                    matchedProvince = "Cần Thơ"; // fallback
                }

                var districts = await _context.Branches
                    .Where(b => b.Province == matchedProvince)
                    .Select(b => b.District)
                    .Distinct()
                    .ToListAsync();

                string matchedDistrict = "";
                foreach (var dist in districts)
                {
                    if (!string.IsNullOrEmpty(dist) && addressText.Contains(dist, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedDistrict = dist;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(matchedDistrict))
                {
                    targetBranch = await _context.Branches
                        .FirstOrDefaultAsync(b => b.Province == matchedProvince && b.District == matchedDistrict);
                }

                if (targetBranch == null)
                {
                    targetBranch = await _context.Branches
                        .FirstOrDefaultAsync(b => b.Province == matchedProvince);
                }
            }

            if (targetBranch == null)
            {
                targetBranch = await _context.Branches.FirstOrDefaultAsync() ?? new Branch { Id = 1, Name = "BHX Cần Thơ" };
            }

            HttpContext.Session.SetInt32("ActiveBranchId", targetBranch.Id);
            HttpContext.Session.SetString("ActiveBranchName", targetBranch.Name);

            var cookieOptions = new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(30), HttpOnly = true, IsEssential = true };
            Response.Cookies.Append("ActiveBranchId", targetBranch.Id.ToString(), cookieOptions);
            Response.Cookies.Append("ActiveBranchName", targetBranch.Name, cookieOptions);

            return Json(new { success = true, branchId = targetBranch.Id, branchName = targetBranch.Name, address = targetBranch.Address });
        }

        // ===== LẤY DANH SÁCH CHI NHÁNH SIÊU THỊ =====
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.Branches.OrderBy(b => b.Province).ThenBy(b => b.Name).ToListAsync();
            return Json(branches);
        }

        // ===== LẤY DANH SÁCH PHƯỜNG/XÃ Chuẩn =====

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetWards(string province)
        {
            if (string.IsNullOrEmpty(province)) return Json(new List<string>());
            if (province == "Huế") province = "Thừa Thiên Huế";

            try
            {
                string jsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "data", "wards.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(jsonPath);
                    var allWards = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                    if (allWards != null && allWards.TryGetValue(province, out var list))
                    {
                        return Json(list.OrderBy(w => w).ToList());
                    }
                }
            }
            catch
            {
                // Fallback to database
            }

            var wards = await _context.Branches
                .Where(b => b.Province == province)
                .Select(b => b.District).Distinct()
                .Where(d => d.StartsWith("Phường ") || d.StartsWith("Xã ") || d.StartsWith("Thị trấn "))
                .OrderBy(d => d).ToListAsync();
            return Json(wards);
        }

        // ===== TRANG CHÍNH SÁCH =====
        public IActionResult Privacy()
        {
            ViewBag.Role = _roleService.GetRole(User);
            return View();
        }



        // ===== TRANG THÔNG BÁO TỪ CHỐI TRUY CẬP =====
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View(); // Bạn nên tạo một View AccessDenied.cshtml cho đẹp thay vì Content()
        }

        // ===== TRANG THÔNG TIN THỐNG NHẤT (FOOTER) =====
        [AllowAnonymous]
        public IActionResult Info(string page)
        {
            ViewBag.Role = _roleService.GetRole(User);
            ViewBag.Page = (page ?? "about").ToLower();

            ViewData["Title"] = ViewBag.Page switch
            {
                "about" => "Giới thiệu về chúng tôi",
                "stores" => "Hệ thống cửa hàng",
                "careers" => "Cơ hội tuyển dụng",
                "news" => "Tin tức & Khuyến mãi",
                "privacy" => "Chính sách bảo mật",
                "terms" => "Điều khoản dịch vụ",
                "return" => "Chính sách đổi trả hàng hóa",
                "payment" => "Phương thức thanh toán",
                "guide" => "Hướng dẫn mua hàng",
                "faq" => "Các câu hỏi thường gặp (FAQ)",
                "contact" => "Liên hệ với chúng tôi",
                _ => "Thông tin Bách Hóa Xanh"
            };

            return View();
        }

        // ===== CHECK LOCKOUT STATUS (REAL TIME) =====
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> CheckLockoutStatus()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                    {
                        return Json(new { isLocked = true, email = user.Email });
                    }
                }
            }
            return Json(new { isLocked = false });
        }

        // ===== LẤY DANH SÁCH ĐÁNH GIÁ SẢN PHẨM (DATABASE) =====
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetReviews(int productId)
        {
            var targetProduct = await _context.Products.FindAsync(productId);
            var matchingProductIds = new List<int> { productId };

            if (targetProduct != null && !string.IsNullOrWhiteSpace(targetProduct.Name))
            {
                var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "hộp", "cây", "gói", "chai", "lon", "thùng", "lốc", "túi", "khay", "bịch", "vỉ", "tập", "hũ", "68g", "60g", "100g", "200g", "500g", "1kg", "1l", "1.5l", "2l", "6", "12", "24"
                };

                var keywords = targetProduct.Name.Split(new[] { ' ', ',', '!', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 3 && !stopWords.Contains(w.ToLower()))
                    .ToList();

                if (keywords.Count > 0)
                {
                    var relatedProducts = await _context.Products
                        .Select(p => new { p.Id, p.Name })
                        .ToListAsync();

                    var relatedIds = relatedProducts
                        .Where(p => keywords.Count(k => p.Name.Contains(k, StringComparison.OrdinalIgnoreCase)) >= Math.Min(2, keywords.Count))
                        .Select(p => p.Id);

                    matchingProductIds = matchingProductIds.Union(relatedIds).ToList();
                }
            }

            var reviews = await _context.ProductReviews
                .Where(r => matchingProductIds.Contains(r.ProductId))
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    fullName = r.UserFullName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, reviews = reviews });
        }

        // ===== THÊM ĐÁNH GIÁ SẢN PHẨM MỚI (DATABASE) =====
        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5." });
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                return Json(new { success = false, message = "Nội dung nhận xét không được để trống." });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện đánh giá." });
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var userEmail = User.Identity?.Name ?? "";
            var fullName = profile?.FullName;
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = userEmail.Split('@')[0];
            }

            var newReview = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                UserEmail = userEmail,
                UserFullName = fullName,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(newReview);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cảm ơn bạn đã gửi đánh giá thành công!" });
        }

        // ===== ROUTE DỘNG ROBOTS.TXT =====
        [Route("robots.txt")]
        [AllowAnonymous]
        public IActionResult Robots()
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            var robots = "User-agent: *\n" +
                         "Allow: /\n" +
                         $"Sitemap: {domain}/sitemap.xml\n";
            return Content(robots, "text/plain", System.Text.Encoding.UTF8);
        }

        // ===== ROUTE DỘNG SITEMAP.XML =====
        [Route("sitemap.xml")]
        [AllowAnonymous]
        public async Task<IActionResult> Sitemap()
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // Trang chủ
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{domain}/</loc>");
            xml.AppendLine("    <changefreq>daily</changefreq>");
            xml.AppendLine("    <priority>1.0</priority>");
            xml.AppendLine("  </url>");

            // Các trang thông tin tĩnh
            var infoPages = new[] { "about", "stores", "careers", "news", "privacy", "terms", "return", "payment", "guide", "faq", "contact" };
            foreach (var page in infoPages)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{domain}/Home/Info?page={page}</loc>");
                xml.AppendLine("    <changefreq>weekly</changefreq>");
                xml.AppendLine("    <priority>0.5</priority>");
                xml.AppendLine("  </url>");
            }

            // Danh mục sản phẩm
            var categories = await _context.Categories.ToListAsync();
            foreach (var cat in categories)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{domain}/Home/Index?categoryId={cat.Id}</loc>");
                xml.AppendLine("    <changefreq>daily</changefreq>");
                xml.AppendLine("    <priority>0.8</priority>");
                xml.AppendLine("  </url>");
            }

            // Sản phẩm
            var products = await _context.Products.Where(p => p.IsVisible).ToListAsync();
            foreach (var prod in products)
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{domain}/Home/Index?search={Uri.EscapeDataString(prod.Name)}</loc>");
                xml.AppendLine("    <changefreq>weekly</changefreq>");
                xml.AppendLine("    <priority>0.6</priority>");
                xml.AppendLine("  </url>");
            }

            xml.AppendLine("</urlset>");
            return Content(xml.ToString(), "application/xml", System.Text.Encoding.UTF8);
        }

        // ===== TRANG LỖI HỆ THỐNG =====
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
