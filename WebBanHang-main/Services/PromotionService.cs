using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public enum PromotionType
    {
        None,
        Weekend,
        DoubleDate,
        Holiday,
        Weekday
    }

    public class PromotionInfo
    {
        public PromotionType Type { get; set; }
        public decimal DiscountPercent { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string AppliedCategoryDescription { get; set; }
        public string Theme { get; set; } // "Spring", "Summer", "Autumn", "Winter", "Holiday", "Weekend", "Weekday", "Default"
    }

    public static class PromotionService
    {
        private static List<Category> _categoriesCache = new();
        private static DateTime _cacheExpiration = DateTime.MinValue;
        private static readonly object _cacheLock = new();

        // Trả về giờ hiện tại theo múi giờ Việt Nam (UTC+7)
        public static DateTime GetVietnamNow()
        {
            var vnZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"
            );
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);
        }

        // Check if the current date is a Vietnamese solar holiday
        public static bool IsVnHoliday(DateTime date, out string holidayName)
        {
            holidayName = "";
            int m = date.Month;
            int d = date.Day;

            // Tết Dương Lịch
            if (m == 1 && d == 1) { holidayName = "Tết Dương Lịch"; return true; }
            // Giỗ tổ Hùng Vương (lunar 10/3, in 2026 it is April 26)
            if (date.Year == 2026 && m == 4 && d == 26) { holidayName = "Giỗ tổ Hùng Vương"; return true; }
            // Ngày Giải Phóng Miền Nam
            if (m == 4 && d == 30) { holidayName = "Giải phóng Miền Nam"; return true; }
            // Ngày Quốc Tế Lao Động
            if (m == 5 && d == 1) { holidayName = "Quốc tế Lao động"; return true; }
            // Ngày Quốc Khánh
            if (m == 9 && (d == 2 || d == 3)) { holidayName = "Quốc khánh"; return true; }
            // Tết Nguyên Đán 2026 (Official solar dates: Feb 16 to Feb 20)
            if (date.Year == 2026 && m == 2 && d >= 16 && d <= 20) { holidayName = "Tết Nguyên Đán"; return true; }

            return false;
        }

        // Check if the current date is a double-date (1/1, 2/2, etc.)
        public static bool IsDoubleDate(DateTime date, out string doubleDateName)
        {
            doubleDateName = "";
            if (date.Day == date.Month)
            {
                doubleDateName = $"{date.Day}/{date.Month}";
                return true;
            }
            return false;
        }

        // Check if the current date is a weekend (Friday, Saturday, Sunday)
        public static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Friday || 
                   date.DayOfWeek == DayOfWeek.Saturday || 
                   date.DayOfWeek == DayOfWeek.Sunday;
        }

        // Check if the current date is a weekday (Monday, Tuesday, Wednesday, Thursday)
        public static bool IsWeekdayPromo(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Monday ||
                   date.DayOfWeek == DayOfWeek.Tuesday ||
                   date.DayOfWeek == DayOfWeek.Wednesday ||
                   date.DayOfWeek == DayOfWeek.Thursday;
        }

        // Get the season of a month
        public static string GetSeason(int month)
        {
            if (month >= 1 && month <= 3) return "Spring"; // Xuân: 1, 2, 3
            if (month >= 4 && month <= 6) return "Summer"; // Hạ: 4, 5, 6
            if (month >= 7 && month <= 9) return "Autumn"; // Thu: 7, 8, 9
            return "Winter"; // Đông: 10, 11, 12
        }

        // Check if a category name matches "Thịt, cá, trứng" or "Rau, củ, trái cây"
        public static bool IsWeekendCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return false;
            var n = categoryName.ToLowerInvariant();
            return (n.Contains("thịt") && n.Contains("trứng")) || 
                   (n.Contains("rau") && n.Contains("củ") && n.Contains("trái"));
        }

        // Get all categories from database with memory caching
        public static List<Category> GetAllCategories()
        {
            lock (_cacheLock)
            {
                if (_categoriesCache.Any() && DateTime.Now < _cacheExpiration)
                {
                    return _categoriesCache;
                }

                try
                {
                    var httpContext = new HttpContextAccessor().HttpContext;
                    var db = httpContext?.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
                    if (db != null)
                    {
                        var list = db.Categories.ToList();
                        if (list.Any())
                        {
                            _categoriesCache = list;
                            _cacheExpiration = DateTime.Now.AddMinutes(5); // Cache for 5 minutes
                        }
                    }
                }
                catch
                {
                    // Fallback to empty or existing cache if database is inaccessible
                }

                return _categoriesCache;
            }
        }

        // Get weekday eligible categories (10 categories total)
        public static List<Category> GetWeekdayEligibleCategories()
        {
            var all = GetAllCategories();
            return all.Where(c => !IsWeekendCategory(c.Name)).OrderBy(c => c.Id).ToList();
        }

        // Get weekday discounted categories (0, 1, or 2 selected dynamically based on date seed)
        public static List<Category> GetWeekdayDiscountedCategories(DateTime date)
        {
            var eligible = GetWeekdayEligibleCategories();
            if (!eligible.Any()) return new List<Category>();

            // Stable date seed stripping time
            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            var random = new Random(seed);

            int count = random.Next(0, 3); // 0, 1, or 2
            if (count == 0) return new List<Category>();

            var selected = new List<Category>();
            var available = new List<Category>(eligible);
            for (int i = 0; i < count && available.Any(); i++)
            {
                int index = random.Next(available.Count);
                selected.Add(available[index]);
                available.RemoveAt(index);
            }
            return selected;
        }

        public static List<int> GetWeekdayDiscountedCategoryIds(DateTime date)
        {
            return GetWeekdayDiscountedCategories(date).Select(c => c.Id).ToList();
        }

        public static List<string> GetWeekdayDiscountedCategoryNames(DateTime date)
        {
            return GetWeekdayDiscountedCategories(date).Select(c => c.Name).ToList();
        }

        // Get current promotion details
        public static PromotionInfo GetCurrentPromotion(DateTime date)
        {
            if (IsVnHoliday(date, out string holidayName))
            {
                return new PromotionInfo
                {
                    Type = PromotionType.Holiday,
                    DiscountPercent = 0.15m,
                    Title = $"Đại Lễ {holidayName}",
                    Subtitle = "GIẢM GIÁ 15% CHO TẤT CẢ CÁC MẶT HÀNG",
                    AppliedCategoryDescription = "Áp dụng cho toàn bộ sản phẩm",
                    Theme = "Holiday"
                };
            }

            if (IsDoubleDate(date, out string doubleDateName))
            {
                string season = GetSeason(date.Month);
                string seasonTitle = season switch
                {
                    "Spring" => "Lễ Hội Mùa Xuân",
                    "Summer" => "Chào Hè Rực Rỡ",
                    "Autumn" => "Thu Vàng Ấm Áp",
                    _ => "Đại Tiệc Mùa Đông"
                };

                return new PromotionInfo
                {
                    Type = PromotionType.DoubleDate,
                    DiscountPercent = 0.12m,
                    Title = $"{seasonTitle} {doubleDateName}",
                    Subtitle = "GIẢM GIÁ 12% CHO TẤT CẢ MẶT HÀNG",
                    AppliedCategoryDescription = "Áp dụng cho toàn bộ sản phẩm",
                    Theme = season
                };
            }

            if (IsWeekend(date))
            {
                string dayOfWeekStr = date.DayOfWeek switch
                {
                    DayOfWeek.Friday => "Thứ 6",
                    DayOfWeek.Saturday => "Thứ 7",
                    _ => "Chủ Nhật"
                };

                return new PromotionInfo
                {
                    Type = PromotionType.Weekend,
                    DiscountPercent = 0.10m,
                    Title = $"Khuyến Mãi Cuối Tuần ({dayOfWeekStr})",
                    Subtitle = "GIẢM GIÁ 10% CHO THỊT CÁ TRỨNG & RAU CỦ QUẢ",
                    AppliedCategoryDescription = "Chỉ áp dụng cho Thịt, cá, trứng & Rau, củ, trái cây",
                    Theme = "Weekend"
                };
            }

            if (IsWeekdayPromo(date))
            {
                var weekdayCats = GetWeekdayDiscountedCategories(date);
                if (weekdayCats.Any())
                {
                    string categoryNamesStr = string.Join(" & ", weekdayCats.Select(c => c.Name));
                    return new PromotionInfo
                    {
                        Type = PromotionType.Weekday,
                        DiscountPercent = 0.07m,
                        Title = "Khuyến Mãi",
                        Subtitle = $"Giảm giá 7% cho {categoryNamesStr}",
                        AppliedCategoryDescription = $"Chỉ áp dụng cho {categoryNamesStr}",
                        Theme = "Weekday"
                    };
                }
            }

            return new PromotionInfo
            {
                Type = PromotionType.None,
                DiscountPercent = 0m,
                Title = "Bách Hóa Xanh",
                Subtitle = "SIÊU THỊ GẦN NHÀ, GIÁ TIẾT KIỆM MỖI NGÀY",
                AppliedCategoryDescription = "",
                Theme = "Default"
            };
        }

        // Get discounted price for a product
        public static decimal GetDiscountedPrice(Product product, DateTime date)
        {
            if (product == null) return 0m;
            
            var promo = GetCurrentPromotion(date);
            if (promo.Type == PromotionType.None)
            {
                return product.Price;
            }

            if (promo.Type == PromotionType.Holiday || promo.Type == PromotionType.DoubleDate)
            {
                return Math.Round(product.Price * (1m - promo.DiscountPercent), 0);
            }

            if (promo.Type == PromotionType.Weekend)
            {
                string catName = product.Category?.Name ?? "";
                if (IsWeekendCategory(catName))
                {
                    return Math.Round(product.Price * (1m - promo.DiscountPercent), 0);
                }
            }

            if (promo.Type == PromotionType.Weekday)
            {
                if (product.CategoryId.HasValue)
                {
                    var discountedIds = GetWeekdayDiscountedCategoryIds(date);
                    if (discountedIds.Contains(product.CategoryId.Value))
                    {
                        return Math.Round(product.Price * (1m - promo.DiscountPercent), 0);
                    }
                }
                else
                {
                    string catName = product.Category?.Name ?? "";
                    if (!string.IsNullOrEmpty(catName))
                    {
                        var discountedNames = GetWeekdayDiscountedCategoryNames(date);
                        if (discountedNames.Contains(catName))
                        {
                            return Math.Round(product.Price * (1m - promo.DiscountPercent), 0);
                        }
                    }
                }
            }

            return product.Price;
        }

        public static string GetPromoGradient(string theme) => theme switch
        {
            "Spring" => "linear-gradient(135deg, #ff758c 0%, #ff7eb3 100%)", // Hoa đào / anh đào hồng
            "Summer" => "linear-gradient(135deg, #f83600 0%, #f9d423 100%)", // Nắng hè vàng cam
            "Autumn" => "linear-gradient(135deg, #f39c12 0%, #d35400 100%)", // Lá phong đỏ cam
            "Winter" => "linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)", // Tuyết rơi xanh lục băng
            "Holiday" => "linear-gradient(135deg, #d31027 0%, #ea384d 100%)", // Lễ hội đỏ vàng
            "Weekend" => "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)", // Tươi ngon xanh lá
            "Weekday" => "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)", // Xanh lá tươi
            _ => "linear-gradient(135deg, #1a7a2e 0%, #38b558 100%)" // Mặc định xanh lá
        };

        public static string GetPromoIcon(string theme) => theme switch
        {
            "Spring" => "bi-flower1",
            "Summer" => "bi-sun-fill",
            "Autumn" => "bi-tree-fill",
            "Winter" => "bi-snowflake",
            "Holiday" => "bi-gift-fill",
            "Weekend" => "bi-lightning-charge-fill",
            "Weekday" => "bi-lightning-charge-fill",
            _ => "bi-basket2-fill"
        };

        public static string GetPromoIconByPromo(PromotionInfo promo, DateTime date)
        {
            if (promo.Type == PromotionType.Weekend)
            {
                return "bi-egg-fried"; // Thịt, cá, trứng
            }

            if (promo.Type == PromotionType.Weekday)
            {
                var discountedCats = GetWeekdayDiscountedCategories(date);
                if (discountedCats.Any())
                {
                    var catName = discountedCats.First().Name.ToLowerInvariant();
                    if (catName.Contains("bánh") || catName.Contains("kẹo") || catName.Contains("banh") || catName.Contains("keo")) return "bi-cake2-fill";
                    if (catName.Contains("bia") || catName.Contains("nước") || catName.Contains("nuoc") || catName.Contains("giải khát") || catName.Contains("giai khat")) return "bi-cup-straw";
                    if (catName.Contains("chăm sóc") || catName.Contains("cá nhân") || catName.Contains("cham soc") || catName.Contains("ca nhan")) return "bi-person-hearts";
                    if (catName.Contains("dầu") || catName.Contains("gia vị") || catName.Contains("dau") || catName.Contains("gia vi")) return "bi-droplet-half";
                    if (catName.Contains("gạo") || catName.Contains("đồ khô") || catName.Contains("gao") || catName.Contains("do kho")) return "bi-box-seam-fill";
                    if (catName.Contains("kem") || catName.Contains("sữa chua") || catName.Contains("sua chua")) return "bi-thermometer-snow";
                    if (catName.Contains("mì") || catName.Contains("cháo") || catName.Contains("chao")) return "bi-cup-hot-fill";
                    if (catName.Contains("mẹ") || catName.Contains("bé")) return "bi-emoji-laughing-fill";
                    if (catName.Contains("vệ sinh") || catName.Contains("nhà cửa") || catName.Contains("ve sinh")) return "bi-house-heart-fill";
                    if (catName.Contains("sữa")) return "bi-cup-fill";
                }
            }

            return GetPromoIcon(promo.Theme);
        }

        public static List<string> GetPromoEmojis(PromotionInfo promo, DateTime date)
        {
            var emojis = new List<string>();

            if (promo.Type == PromotionType.Weekend)
            {
                emojis.Add("🥩");
                emojis.Add("🥦");
                emojis.Add("🥚");
                return emojis;
            }

            if (promo.Type == PromotionType.Weekday)
            {
                var discountedCats = GetWeekdayDiscountedCategories(date);
                foreach (var cat in discountedCats)
                {
                    var catName = cat.Name.ToLowerInvariant();
                    if (catName.Contains("bánh") || catName.Contains("kẹo") || catName.Contains("banh") || catName.Contains("keo"))
                    {
                        emojis.Add("🍭");
                        emojis.Add("🍫");
                    }
                    else if (catName.Contains("bia") || catName.Contains("nước") || catName.Contains("nuoc") || catName.Contains("giải khát") || catName.Contains("giai khat"))
                    {
                        emojis.Add("🥤");
                        emojis.Add("🍺");
                    }
                    else if (catName.Contains("chăm sóc") || catName.Contains("cham soc") || catName.Contains("cá nhân") || catName.Contains("ca nhan"))
                    {
                        emojis.Add("🧼");
                        emojis.Add("🧴");
                    }
                    else if (catName.Contains("dầu") || catName.Contains("dau") || catName.Contains("gia vị") || catName.Contains("gia vi"))
                    {
                        emojis.Add("🌶️");
                        emojis.Add("🧂");
                    }
                    else if (catName.Contains("gạo") || catName.Contains("gao") || catName.Contains("bột") || catName.Contains("bot") || catName.Contains("đồ khô") || catName.Contains("do kho"))
                    {
                        emojis.Add("🌾");
                        emojis.Add("🍞");
                    }
                    else if (catName.Contains("kem") || catName.Contains("sữa chua") || catName.Contains("sua chua"))
                    {
                        emojis.Add("🍦");
                        emojis.Add("🍨");
                    }
                    else if (catName.Contains("mì") || catName.Contains("miến") || catName.Contains("cháo"))
                    {
                        emojis.Add("🍜");
                        emojis.Add("🍲");
                    }
                    else if (catName.Contains("mẹ") || catName.Contains("bé"))
                    {
                        emojis.Add("🍼");
                        emojis.Add("🧸");
                    }
                    else if (catName.Contains("vệ sinh") || catName.Contains("nhà cửa"))
                    {
                        emojis.Add("🧹");
                        emojis.Add("🧽");
                    }
                    else if (catName.Contains("sữa"))
                    {
                        emojis.Add("🥛");
                        emojis.Add("🍼");
                    }
                }

                if (emojis.Count == 0)
                {
                    emojis.AddRange(new[] { "🍎", "🍭", "🥤" });
                }
                else if (emojis.Count == 1)
                {
                    emojis.Add("🛒");
                    emojis.Add("🛍️");
                }
                else if (emojis.Count == 2)
                {
                    emojis.Add("🛒");
                }

                return emojis.Take(3).ToList();
            }

            emojis.Add("🍎");
            emojis.Add("🛒");
            emojis.Add("🍕");
            return emojis;
        }

        // Check if a category has active discounts today
        public static bool IsCategoryDiscounted(string categoryName, DateTime date)
        {
            var promo = GetCurrentPromotion(date);
            if (promo.Type == PromotionType.None) return false;
            if (promo.Type == PromotionType.Holiday || promo.Type == PromotionType.DoubleDate) return true;
            if (promo.Type == PromotionType.Weekend) return IsWeekendCategory(categoryName);
            if (promo.Type == PromotionType.Weekday)
            {
                var discountedNames = GetWeekdayDiscountedCategoryNames(date);
                return discountedNames.Contains(categoryName);
            }
            return false;
        }
    }
}
