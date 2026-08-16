using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class OpenAIService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(
            IConfiguration config,
            ApplicationDbContext context,
            ILogger<OpenAIService> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public async Task<string> AskAI(string message, string? imageMimeType = null, string? imageData = null)
        {
            string apiKey =
                _config["AI:ApiKey"]
                ??
                throw new Exception(
                    "Gemini ApiKey missing");

            string model =
                _config["AI:Model"]
                ??
                "gemini-2.5-flash";

            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(60);

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            // Lấy toàn bộ ngữ cảnh dữ liệu thực tế của website từ CSDL
            string websiteContext = await GetWebsiteContextAsync();

            var systemPrompt = $"""
                Bạn là Trợ lý AI bán hàng thông minh & Chăm sóc khách hàng của trang web thương mại điện tử WebBanHang (Bách Xanh Xanh).
                
                QUY TẮC VÀ NGUYÊN TẮC TRẢ LỜI:
                1. Dựa trên dữ liệu thực tế của cửa hàng bên dưới để trả lời chính xác 100% các câu hỏi của khách hàng.
                2. Khi khách hỏi về sản phẩm (ví dụ: "Cửa hàng có bán Red Bull không?", "Có bán mì tôm không?", "Giá trứng gà bao nhiêu?"):
                   - Tra cứu kỹ trong danh sách sản phẩm bên dưới (kể cả tìm kiếm tương đối, tên thương hiệu, chủng loại).
                   - Nếu CÓ: Khẳng định CÓ bán, liệt kê rõ tên sản phẩm, giá gốc, giá khuyến mãi (nếu có), đơn vị tính và tình trạng nổi bật. Đề xuất nhiệt tình giúp khách chọn mua.
                   - Nếu KHÔNG CÓ: Thông báo khéo léo và lịch sự rằng sản phẩm này cửa hàng hiện chưa kinh doanh, sau đó gợi ý các sản phẩm cùng ngành hàng hoặc sản phẩm tương tự mà cửa hàng đang có bán.
                3. Khi khách hỏi về mã giảm giá, chương trình khuyến mãi, combo: CHỈ cung cấp các mã giảm giá CHUNG dành cho tất cả khách hàng (có trong mục "MÃ GIẢM GIÁ CHUNG" bên dưới). TUYỆT ĐỐI KHÔNG tiết lộ, gợi ý hay đề cập đến các mã giảm giá cá nhân của từng khách hàng (mã cá nhân là mã được hệ thống tạo riêng cho từng tài khoản, ví dụ dạng WELCOME_xxx, REACH_xxx, LOYAL_xxx). Nếu khách hỏi về mã cá nhân của họ, hướng dẫn họ vào mục "Tài khoản → Voucher của tôi" để xem.
                4. Khi khách hỏi về thông tin cửa hàng, bảo hành, giờ mở cửa, chi nhánh, liên hệ: Trả lời đúng theo thông tin trong dữ liệu bên dưới.
                5. Phong cách trả lời: Thân thiện, lịch sự, chuyên nghiệp, trình bày rõ ràng với các gạch đầu dòng, tô đậm tên sản phẩm/giá bán, dùng emoji thích hợp để tăng tính trải nghiệm. Không lan man quá 1000 từ.

                --- DỮ LIỆU THỰC TẾ HỆ THỐNG WEBBANHANG ---
                {websiteContext}
                --- KẾT THÚC DỮ LIỆU ---

                Khách hỏi:
                {message}
                """;

            var parts = new List<object>
            {
                new
                {
                    text = systemPrompt
                }
            };

            if (!string.IsNullOrEmpty(imageMimeType) && !string.IsNullOrEmpty(imageData))
            {
                parts.Add(new
                {
                    inlineData = new
                    {
                        mimeType = imageMimeType,
                        data = imageData
                    }
                });
            }

            var request = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts.ToArray()
                    }
                }
            };

            var json = JsonSerializer.Serialize(request);

            var response = await client.PostAsync(
                url,
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"));

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API lỗi {StatusCode}: {Body}",
                    (int)response.StatusCode, content.Length > 500 ? content[..500] : content);

                // Thử đọc thông báo lỗi từ Gemini
                try
                {
                    using var errDoc = JsonDocument.Parse(content);
                    var errMsg = errDoc.RootElement
                        .GetProperty("error")
                        .GetProperty("message")
                        .GetString();
                    if (!string.IsNullOrEmpty(errMsg))
                        return $"⚠️ Trợ lý AI tạm thời gặp sự cố: {errMsg}";
                }
                catch { }

                return $"⚠️ Trợ lý AI đang bận (lỗi {(int)response.StatusCode}). Vui lòng thử lại sau hoặc liên hệ hotline **1900 1908**.";
            }

            using var doc = JsonDocument.Parse(content);

            try
            {
                return doc
                    .RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()
                    ?? "Không có phản hồi từ AI";
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "Lỗi parse response Gemini: {Content}",
                    content.Length > 300 ? content[..300] : content);
                return "⚠️ Trợ lý AI không đọc được phản hồi. Vui lòng thử lại.";
            }
        }

        private async Task<string> GetWebsiteContextAsync()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== THÔNG TIN VỀ TRANG WEB VÀ CỬA HÀNG WEBBANHANG ===");
            sb.AppendLine("1. THÔNG TIN CHUNG:");
            sb.AppendLine("- Tên hệ thống: WebBanHang - Siêu thị thực phẩm & hàng tiêu dùng trực tuyến (Bách Xanh Xanh).");
            sb.AppendLine("- Giờ mở cửa: 08:00 - 22:00 tất cả các ngày trong tuần (kể cả Thứ 7, Chủ Nhật và ngày Lễ).");
            sb.AppendLine("- Bộ phận Hỗ trợ khách hàng & Bảo hành: Võ Văn Phú, Số điện thoại: 0779753643, Email: Vovanphu1004@gmail.com.");
            sb.AppendLine("- Chính sách bảo hành: Tất cả sản phẩm tươi sống được đảm bảo chất lượng, hỗ trợ đổi trả trong vòng 24 giờ nếu sản phẩm không đạt chất lượng. Hàng khô, hàng tiêu dùng được đổi trả nếu còn nguyên seal và trong hạn sử dụng.");
            sb.AppendLine("- Liên hệ hỗ trợ trực tiếp: Võ Văn Phú - 0779753643 - Vovanphu1004@gmail.com.");
            sb.AppendLine("- Hotline đặt hàng / tư vấn: 1900 1908.");
            sb.AppendLine("- Hỗ trợ Zalo: https://zalo.me/3518439335916132961, Email: lienhe@bachhoaxanh.com.");
            sb.AppendLine("- Phương thức thanh toán: Tiền mặt khi nhận hàng (COD), Ví MoMo, VNPay, Quét mã QR.");
            sb.AppendLine("- Chương trình tích điểm: Mọi đơn hàng đều tích điểm nâng hạng thành viên và đổi quà/voucher.");
            sb.AppendLine("");
            sb.AppendLine("PHÍ GIAO HÀNG (Shipping fee):");
            sb.AppendLine("- Giao ngay (trong 1 giờ): Phí cơ bản 15.000đ.");
            sb.AppendLine("- Giao theo khung giờ hẹn: Phí cơ bản 12.000đ.");
            sb.AppendLine("- Giao trong 4 giờ: Phí cơ bản 9.000đ.");
            sb.AppendLine("- Giao tiết kiệm (ngày mai): Phí cơ bản 5.000đ.");
            sb.AppendLine("- Cộng thêm phí km: Nếu khoảng cách > 3km, mỗi km vượt thêm cộng 4.000đ/km.");
            sb.AppendLine("- MIỄN PHÍ SHIP (freeship) khi: Đơn hàng từ 300.000đ trở lên (hoặc từ 150.000đ nếu trong đơn có thực phẩm tươi sống) VÀ khoảng cách giao hàng <= 3km.");
            sb.AppendLine("- Mã FREESHIP / FREE_SHIP / FREE-SHIP trong voucher giảm giá sẽ miễn phí ship toàn bộ.");

            // Branches
            try
            {
                var branches = await _context.Branches.AsNoTracking().ToListAsync();
                if (branches.Any())
                {
                    sb.AppendLine("\n2. DANH SÁCH CHI NHÁNH CỬA HÀNG:");
                    foreach (var b in branches)
                    {
                        sb.AppendLine($"- Chi nhánh {b.Name}: {b.Address}, {b.District}, {b.Province}");
                    }
                }
            }
            catch { }

            // Categories
            try
            {
                var categories = await _context.Categories.AsNoTracking().ToListAsync();
                if (categories.Any())
                {
                    sb.AppendLine("\n3. DANH MỤC NGÀNH HÀNG CÓ BÁN:");
                    sb.AppendLine(string.Join(", ", categories.Select(c => c.Name)));
                }
            }
            catch { }

            // Active Promotions & Discounts
            try
            {
                var now = PromotionService.GetVietnamNow();
                var promoInfo = PromotionService.GetCurrentPromotion(now);
                if (promoInfo != null && promoInfo.Type != PromotionType.None)
                {
                    sb.AppendLine($"\n4. CHƯƠNG TRÌNH KHUYẾN MÃI DỊP ĐẶC BIỆT:");
                    sb.AppendLine($"- {promoInfo.Title}: {promoInfo.Subtitle} (Giảm {promoInfo.DiscountPercent * 100:0}% cho {promoInfo.AppliedCategoryDescription})");
                }

                var discounts = await _context.Discounts
                    .Where(d => d.IsSee && d.Quantity > 0 && d.EndDate >= now && d.UserId == null)
                    .AsNoTracking()
                    .ToListAsync();
                if (discounts.Any())
                {
                    sb.AppendLine("\n5. MÃ GIẢM GIÁ CHUNG (dành cho tất cả khách hàng, không phải mã cá nhân):");
                    foreach (var d in discounts)
                    {
                        var minSpend = d.MinOrderValue > 0 ? $" (Đơn tối thiểu {d.MinOrderValue:N0}đ)" : "";
                        var maxDisc = d.MaxDiscount > 0 ? $" (Giảm tối đa {d.MaxDiscount:N0}đ)" : "";
                        sb.AppendLine($"- Mã '{d.Code}': Giảm {d.DiscountValue}%{minSpend}{maxDisc}. HSD: {d.EndDate:dd/MM/yyyy}");
                    }
                }
                else
                {
                    sb.AppendLine("\n5. MÃ GIẢM GIÁ CHUNG: Hiện tại không có mã giảm giá công khai nào đang hoạt động.");
                }

                var combos = await _context.ComboPromotions
                    .Include(c => c.Product1)
                    .Include(c => c.Product2)
                    .Where(c => c.IsActive && (c.ExpiryDate == null || c.ExpiryDate > now))
                    .AsNoTracking()
                    .ToListAsync();
                if (combos.Any())
                {
                    sb.AppendLine("\n6. CHƯƠNG TRÌNH COMBO ƯU ĐÃI:");
                    foreach (var cb in combos)
                    {
                        if (cb.Product1 != null && cb.Product2 != null)
                        {
                            sb.AppendLine($"- {cb.Name}: Mua kèm '{cb.Product1.Name}' và '{cb.Product2.Name}' được giảm {cb.DiscountPercent * 100:0}%!");
                        }
                    }
                }
            }
            catch { }

            // Products catalog
            try
            {
                var now = PromotionService.GetVietnamNow();
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsVisible)
                    .AsNoTracking()
                    .ToListAsync();

                if (products.Any())
                {
                    sb.AppendLine($"\n7. DANH SÁCH TOÀN BỘ SẢN PHẨM ĐANG BÁN TẠI CỬA HÀNG (Tổng cộng {products.Count} sản phẩm):");
                    foreach (var p in products)
                    {
                        var catName = p.Category?.Name ?? "Khác";
                        // Tính giá khuyến mãi an toàn, không dùng static service với HttpContext
                        string priceStr;
                        try
                        {
                            var discPrice = PromotionService.GetDiscountedPrice(p, now);
                            priceStr = $"{p.Price:N0}đ/{p.Unit}";
                            if (discPrice < p.Price)
                                priceStr += $" (ƯU ĐÃI CÒN {discPrice:N0}đ/{p.Unit})";
                        }
                        catch
                        {
                            priceStr = $"{p.Price:N0}đ/{p.Unit}";
                        }

                        var statusList = new List<string>();
                        if (p.IsHot) statusList.Add("Hàng Hot 🔥");
                        if (p.IsBestSeller) statusList.Add("Bán chạy 🏆");
                        var statusStr = statusList.Any() ? $" [{string.Join(", ", statusList)}]" : "";

                        sb.AppendLine($"- {p.Name} | Danh mục: {catName} | Giá: {priceStr}{statusStr}");
                    }
                }
                else
                {
                    sb.AppendLine("\n7. SẢN PHẨM: Hiện chưa có sản phẩm trong cơ sở dữ liệu.");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"\n7. SẢN PHẨM: Không thể tải danh sách sản phẩm ({ex.Message})");
            }

            return sb.ToString();
        }
    }
}
