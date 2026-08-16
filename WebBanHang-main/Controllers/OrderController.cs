using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHang.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LoyaltyService _loyaltyService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<OrdersController> _logger;
        private readonly InventoryBatchService _inventoryBatchService;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager, LoyaltyService loyaltyService, IEmailSender emailSender, ILogger<OrdersController> logger, InventoryBatchService inventoryBatchService)
        {
            _context = context;
            _userManager = userManager;
            _loyaltyService = loyaltyService;
            _emailSender = emailSender;
            _logger = logger;
            _inventoryBatchService = inventoryBatchService;
        }

        // Bước 1: Hiện danh sách đơn hàng mới (Status = 0) hoặc đơn đã duyệt thành công khi lọc theo ngày
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query;

            if (startDate.HasValue || endDate.HasValue)
            {
                query = _context.Orders.Where(o => o.Status == 1 || o.Status == 2 || o.Status == 3 || o.Status == 5);

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    var endLimit = endDate.Value.Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < endLimit);
                }
            }
            else
            {
                query = _context.Orders.Where(o => o.Status == 0);
            }

            // Chỉ hiển thị các đơn COD hoặc đơn trực tuyến đã thanh toán thành công
            query = query.Where(o => o.IsPaid || o.PaymentMethod == "COD");

            var allNewOrders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            int totalItems = allNewOrders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var pagedOrders = allNewOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedOrders);
        }

        // Nút duyệt tất cả đơn hàng mới sang trạng thái Chờ soạn hàng (Status = 1)
        [HttpPost]
        public async Task<IActionResult> ApproveAllOrders()
        {
            var newOrders = await _context.Orders
                .Where(o => o.Status == 0 && (o.IsPaid || o.PaymentMethod == "COD"))
                .ToListAsync();

            foreach (var order in newOrders)
            {
                order.Status = 1;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã duyệt tất cả {newOrders.Count} đơn hàng mới!";
            return RedirectToAction(nameof(Index));
        }

        // Nút soạn nhanh tất cả các đơn hàng đang chờ soạn (Status = 1 -> Status = 2)
        [HttpPost]
        public async Task<IActionResult> QuickPrepareAllOrders()
        {
            var prepareOrders = await _context.Orders
                .Where(o => o.Status == 1 && (o.IsPaid || o.PaymentMethod == "COD"))
                .ToListAsync();

            foreach (var order in prepareOrders)
            {
                order.Status = 2;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã soạn nhanh tất cả {prepareOrders.Count} đơn hàng!";
            return RedirectToAction(nameof(PrepareList));
        }

        // Bước 2: Hàm "Nhận đơn" - Chuyển đơn sang trạng thái Soạn hàng (Status = 1)
        [HttpPost]
        public async Task<IActionResult> ReceiveOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = 1;
                await _context.SaveChangesAsync();
                return RedirectToAction("PrepareList", new { id = order.Id });
            }
            return RedirectToAction(nameof(Index));
        }

        // Trang danh sách đơn đang chờ soạn hàng (Status = 1) hoặc đã soạn thành công khi lọc theo ngày
        public async Task<IActionResult> PrepareList(DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query;

            if (startDate.HasValue || endDate.HasValue)
            {
                query = _context.Orders.Where(o => o.Status == 2 || o.Status == 3 || o.Status == 5);

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    var endLimit = endDate.Value.Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < endLimit);
                }
            }
            else
            {
                query = _context.Orders.Where(o => o.Status == 1);
            }

            // Chỉ hiển thị các đơn COD hoặc đơn trực tuyến đã thanh toán thành công
            query = query.Where(o => o.IsPaid || o.PaymentMethod == "COD");

            var processingOrders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            int totalItems = processingOrders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var pagedOrders = processingOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedOrders);
        }

        // GET: Orders/Process/5 — Truyền danh sách Shipper xuống view
        public async Task<IActionResult> Process(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
                return NotFound();

            // Lấy danh sách tất cả user thuộc role Shipper
            var shippers = await _userManager.GetUsersInRoleAsync("Shipper");
            var shipperIds = shippers.Select(s => s.Id).ToList();
            var profiles = await _context.CustomerProfiles
                .Where(p => shipperIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p.FullName);

            ViewBag.Shippers = shippers.ToList();
            ViewBag.ShipperProfiles = profiles;

            return View(order);
        }

        // POST: ExportOrder — Nhận shipperId, lưu DB, trả JSON
        [HttpPost]
        public async Task<IActionResult> ExportOrder(int id, string? shipperId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (string.IsNullOrWhiteSpace(shipperId))
                return Json(new { success = false, message = "Vui lòng chọn shipper." });

            order.Status = 2;
            order.ShipperId = shipperId;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đơn hàng #{id} đã xuất kho thành công!" });
        }

        // Hàm lấy danh sách đơn đang đi trên đường (Status = 2) hoặc đơn giao thành công / hủy khi lọc theo ngày
        public async Task<IActionResult> Shipping(DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query;

            if (startDate.HasValue || endDate.HasValue)
            {
                query = _context.Orders.Where(o => o.Status == 3 || o.Status == 5);

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    var endLimit = endDate.Value.Date.AddDays(1);
                    query = query.Where(o => o.OrderDate < endLimit);
                }
            }
            else
            {
                query = _context.Orders.Where(o => o.Status == 2);
            }

            // Chỉ hiển thị các đơn COD hoặc đơn trực tuyến đã thanh toán thành công
            query = query.Where(o => o.IsPaid || o.PaymentMethod == "COD");

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            int totalItems = orders.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            var pagedOrders = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedOrders);
        }

        // POST: Admin/Staff xác nhận hoàn tất — tự động điền DeliveryStaffName
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = 3;
                if (!order.IsPaid)
                {
                    await _loyaltyService.AddPointsFromPaidOrderAsync(order);
                    order.IsPaid = true;
                    order.PaidDate = WebBanHang.Services.PromotionService.GetVietnamNow();
                }

                // Tự động điền tên Shipper vào DeliveryStaffName
                if (!string.IsNullOrEmpty(order.ShipperId))
                {
                    var shipperProfile = await _context.CustomerProfiles
                        .FirstOrDefaultAsync(p => p.UserId == order.ShipperId);
                    if (shipperProfile != null && !string.IsNullOrWhiteSpace(shipperProfile.FullName))
                    {
                        order.DeliveryStaffName = shipperProfile.FullName;
                    }
                    else
                    {
                        var shipper = await _userManager.FindByIdAsync(order.ShipperId);
                        if (shipper != null)
                        {
                            var displayName = !string.IsNullOrWhiteSpace(shipper.UserName) && shipper.UserName != shipper.Email
                                ? shipper.UserName
                                : shipper.Email;
                            order.DeliveryStaffName = displayName;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["CompletedOrderId"] = id;
                TempData["SuccessMessage"] = $"Đơn hàng #{id} đã được giao và thanh toán thành công!";
            }
            return RedirectToAction(nameof(Shipping));
        }

        // GET: Orders/GetOrderStatus/{id} - Lấy trạng thái đơn hàng thời gian thực
        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }
            return Json(new { success = true, status = order.Status });
        }

        // GET: Danh sách đơn hàng dành cho Shipper đang đăng nhập
        public async Task<IActionResult> ShipperOrders()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Where(o => o.Status == 2 && o.ShipperId == currentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lấy thông tin về quyền hủy đơn của từng khách hàng
            var now = WebBanHang.Services.PromotionService.GetVietnamNow();
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var customerIds = orders.Select(o => o.UserId).Distinct().ToList();
            var profiles = await _context.CustomerProfiles
                .Where(p => customerIds.Contains(p.UserId))
                .ToListAsync();

            var cancelCounts = await _context.Orders
                .Where(o => customerIds.Contains(o.UserId) && o.Status == 5 && o.OrderDate >= startOfMonth && o.OrderDate < endOfMonth)
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count);

            ViewBag.CustomerProfiles = profiles.ToDictionary(p => p.UserId, p => p);
            ViewBag.CustomerCancelCounts = cancelCounts;

            return View(orders);
        }

        // POST: Shipper xác nhận giao hàng thành công — trả JSON, tự điền DeliveryStaffName
        [HttpPost]
        public async Task<IActionResult> ShipperCompleteOrder(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (order.ShipperId != currentUserId)
                return Json(new { success = false, message = "Bạn không có quyền xác nhận đơn hàng này." });

            order.Status = 3;
            if (!order.IsPaid)
            {
                await _loyaltyService.AddPointsFromPaidOrderAsync(order);
                order.IsPaid = true;
                order.PaidDate = WebBanHang.Services.PromotionService.GetVietnamNow();
            }

            // Tự động điền tên Shipper vào DeliveryStaffName
            var shipperProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (shipperProfile != null && !string.IsNullOrWhiteSpace(shipperProfile.FullName))
            {
                order.DeliveryStaffName = shipperProfile.FullName;
            }
            else
            {
                var shipper = await _userManager.FindByIdAsync(currentUserId);
                if (shipper != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(shipper.UserName) && shipper.UserName != shipper.Email
                        ? shipper.UserName
                        : shipper.Email;
                    order.DeliveryStaffName = displayName;
                }
            }

            await _context.SaveChangesAsync();

            // Gửi email hóa đơn cho khách hàng
            await SendInvoiceEmailAsync(order);

            return Json(new { success = true, message = $"Đơn hàng #{id} đã giao thành công!" });
        }

        private async Task SendInvoiceEmailAsync(Order order)
        {
            try
            {
                var customer = await _userManager.FindByIdAsync(order.UserId);
                if (customer == null || string.IsNullOrEmpty(customer.Email))
                {
                    _logger.LogWarning("Không thể gửi email hóa đơn cho đơn hàng #{OrderId} vì không tìm thấy email khách hàng.", order.Id);
                    return;
                }

                var subtotalAmount = order.OrderDetails != null ? order.OrderDetails.Sum(x => x.Price * x.Quantity) : 0m;
                var productRowsHtml = "";

                if (order.OrderDetails != null)
                {
                    foreach (var item in order.OrderDetails)
                    {
                        var productName = item.Product?.Name ?? "Sản phẩm";
                        var lineTotal = item.Price * item.Quantity;
                        productRowsHtml += $@"
                            <tr>
                                <td style=""padding: 12px; border-bottom: 1px solid #f1f5f9; font-weight: 500;"">{productName}</td>
                                <td style=""padding: 12px; border-bottom: 1px solid #f1f5f9; text-align: center; color: #666666;"">{item.Quantity}</td>
                                <td style=""padding: 12px; border-bottom: 1px solid #f1f5f9; text-align: right;"">{item.Price.ToString("N0")} ₫</td>
                                <td style=""padding: 12px; border-bottom: 1px solid #f1f5f9; text-align: right; font-weight: bold;"">{lineTotal.ToString("N0")} ₫</td>
                            </tr>";
                    }
                }

                var shippingFeeHtml = "";
                if (order.ShippingFee > 0)
                {
                    shippingFeeHtml = $@"
                        <tr>
                            <td style=""padding: 4px 0; color: #666666;"">Phí vận chuyển:</td>
                            <td style=""padding: 4px 0; text-align: right; font-weight: 500;"">+{order.ShippingFee.ToString("N0")} ₫</td>
                        </tr>";
                }

                var discountHtml = "";
                if (order.DiscountAmount > 0)
                {
                    var discountCodeText = !string.IsNullOrEmpty(order.DiscountCode) ? $" ({order.DiscountCode})" : "";
                    discountHtml = $@"
                        <tr>
                            <td style=""padding: 4px 0; color: #008744;"">Giảm giá{discountCodeText}:</td>
                            <td style=""padding: 4px 0; text-align: right; color: #008744; font-weight: 500;"">-{order.DiscountAmount.ToString("N0")} ₫</td>
                        </tr>";
                }

                var shippingMethodDisplay = string.IsNullOrEmpty(order.ShippingMethod) ? "Giao hàng nhanh" : order.ShippingMethod;
                var paymentStatusDisplay = order.IsPaid ? "Đã thanh toán" : "Thanh toán khi nhận hàng (COD)";

                var htmlMessage = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #eef2f6; border-radius: 12px; background-color: #ffffff; color: #333333;"">
    <!-- Header -->
    <div style=""text-align: center; border-bottom: 2px dashed #e9ecef; padding-bottom: 20px; margin-bottom: 20px;"">
        <h2 style=""color: #008744; margin: 0 0 5px 0; font-size: 24px; font-weight: bold;"">BÁCH HÓA XANH</h2>
        <p style=""color: #666666; margin: 0; font-size: 13px;"">Siêu thị gần nhà, giá tốt tiết kiệm</p>
        <p style=""color: #888888; margin: 5px 0 0 0; font-size: 11px; line-height: 1.4;"">
            Địa chỉ: 42/20A Nguyễn Giản Thanh, Phường 15, Quận 10, TP. Hồ Chí Minh<br>
            Hotline: 1900 1908 | Email: contact@bachhoaxanh.com
        </p>
        <h3 style=""color: #1a1a1a; margin: 15px 0 0 0; font-size: 18px; font-weight: bold; letter-spacing: 0.5px;"">HÓA ĐƠN BÁN HÀNG</h3>
    </div>

    <!-- Info Grid -->
    <table style=""width: 100%; font-size: 13px; border-collapse: collapse; margin-bottom: 20px;"">
        <tr>
            <td style=""padding: 5px 0; color: #888888; width: 40%;"">Mã đơn hàng:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">#{order.Id}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888;"">Thời gian đặt:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{order.OrderDate.ToString("dd/MM/yyyy HH:mm")}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888;"">Khách hàng:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{order.FullName}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888;"">Số điện thoại:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{order.Phone}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888; vertical-align: top;"">Địa chỉ nhận hàng:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{order.Address}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888;"">Phương thức giao hàng:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{shippingMethodDisplay}</td>
        </tr>
        <tr>
            <td style=""padding: 5px 0; color: #888888;"">Trạng thái thanh toán:</td>
            <td style=""padding: 5px 0; font-weight: bold; color: #2d3748;"">{paymentStatusDisplay}</td>
        </tr>
    </table>

    <!-- Product Table -->
    <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 13px;"">
        <thead>
            <tr style=""background-color: #f8fafc; border-bottom: 1px solid #e2e8f0;"">
                <th style=""text-align: left; padding: 10px; color: #64748b; font-weight: 600;"">Sản phẩm</th>
                <th style=""text-align: center; padding: 10px; color: #64748b; font-weight: 600; width: 10%;"">SL</th>
                <th style=""text-align: right; padding: 10px; color: #64748b; font-weight: 600; width: 20%;"">Đơn giá</th>
                <th style=""text-align: right; padding: 10px; color: #64748b; font-weight: 600; width: 25%;"">Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            {productRowsHtml}
        </tbody>
    </table>

    <!-- Summary -->
    <div style=""border-top: 2px dashed #e9ecef; padding-top: 15px; margin-bottom: 20px; font-size: 13px;"">
        <table style=""width: 100%; border-collapse: collapse;"">
            <tr>
                <td style=""padding: 4px 0; color: #666666;"">Tạm tính:</td>
                <td style=""padding: 4px 0; text-align: right; font-weight: 500;"">{subtotalAmount.ToString("N0")} ₫</td>
            </tr>
            {shippingFeeHtml}
            {discountHtml}
            <tr style=""font-size: 16px; font-weight: bold; color: #d93838; border-top: 1px solid #e2e8f0;"">
                <td style=""padding: 10px 0 0 0;"">TỔNG CỘNG:</td>
                <td style=""padding: 10px 0 0 0; text-align: right;"">{order.TotalAmount.ToString("N0")} ₫</td>
            </tr>
        </table>
    </div>

    <!-- Callout Box for disclaimer as requested -->
    <div style=""background-color: #fff9e6; border-left: 4px solid #ffc107; padding: 12px; border-radius: 4px; margin-bottom: 20px; font-size: 12px; color: #664d03; line-height: 1.5;"">
        <strong>Chú thích:</strong> Nếu khách hàng nhận được email này nhưng không nhận được đơn hàng, liên hệ tới <a href=""mailto:lienhe@bachhoaxanh.com"" style=""color: #008744; text-decoration: underline; font-weight: bold;"">lienhe@bachhoaxanh.com</a> để được hỗ trợ.
    </div>

    <!-- Footer -->
    <div style=""text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #f1f5f9; padding-top: 15px; margin-top: 20px;"">
        <p style=""margin: 0 0 5px 0; font-weight: bold; color: #333333;"">Cảm ơn quý khách đã mua sắm tại Bách Hóa Xanh!</p>
        <p style=""margin: 0;"">Hẹn gặp lại quý khách trong lần mua sắm tiếp theo.</p>
    </div>
</div>";

                await _emailSender.SendEmailAsync(customer.Email, $"Hóa đơn điện tử đơn hàng #{order.Id} - Bách Hóa Xanh", htmlMessage);
                _logger.LogInformation("Đã gửi email hóa đơn thành công cho đơn hàng #{OrderId} tới email {Email}.", order.Id, customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi gửi email hóa đơn cho đơn hàng #{OrderId}.", order.Id);
            }
        }

        // POST: Shipper hủy đơn hàng theo đặc quyền của khách hàng — hoàn hàng về kho
        [HttpPost]
        public async Task<IActionResult> ShipperCancelOrder(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

            if (order.ShipperId != currentUserId)
                return Json(new { success = false, message = "Bạn không có quyền thao tác trên đơn hàng này." });

            if (order.Status != 2)
                return Json(new { success = false, message = "Đơn hàng không ở trạng thái đang giao." });

            var customerProfile = await _context.CustomerProfiles
                .FirstOrDefaultAsync(p => p.UserId == order.UserId);

            if (customerProfile == null)
            {
                return Json(new { success = false, message = "Khách hàng không có hồ sơ thành viên để kiểm tra điều kiện hủy." });
            }

            // Kiểm tra giới hạn số lần hủy đơn trong tháng dựa trên cấp bậc thành viên
            int maxCancelLimit = LoyaltyService.GetMaxCancelLimit(customerProfile.MembershipLevel);

            var now = WebBanHang.Services.PromotionService.GetVietnamNow();
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            var cancelledThisMonthCount = await _context.Orders.CountAsync(o => 
                o.UserId == order.UserId && 
                o.Status == 5 && 
                o.OrderDate >= startOfMonth && 
                o.OrderDate < endOfMonth);

            if (cancelledThisMonthCount >= maxCancelLimit)
            {
                return Json(new { success = false, message = $"Khách hàng đã hết lượt hủy đơn trong tháng này (Tối đa {maxCancelLimit} lần/tháng). Bắt buộc phải nhận hàng." });
            }

            // Thực hiện hủy đơn (Status = 5) và hoàn trả số lượng vào kho của chi nhánh
            order.Status = 5;
            int orderBranchId = order.BranchId ?? 1;

            await _inventoryBatchService.RestoreStockAsync(
                order.OrderDetails.Select(detail => new InventoryStockItem
                {
                    ProductId = detail.ProductId,
                    OrderDetailId = detail.Id,
                    Quantity = detail.Quantity,
                    ProductName = detail.Product?.Name ?? "Sản phẩm",
                    Unit = detail.Product?.Unit ?? string.Empty
                }),
                orderBranchId,
                order.Id);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đơn hàng #{id} đã được hủy thành công." });
        }

        // 1. Trang danh sách lịch sử (Chỉ hiển thị Thành công & Đã hủy)
        public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate, int? status, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query;

            if (status.HasValue)
            {
                query = _context.Orders.Where(o => o.Status == status.Value);
            }
            else
            {
                query = _context.Orders.Where(o => o.Status == 3 || o.Status == 5);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endLimit = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < endLimit);
            }

            var history = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            int totalItems = history.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            var pagedHistory = history.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedHistory);
        }

        // 2. Trang xem chi tiết một đơn hàng bất kỳ
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. Trang in hóa đơn
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            if (order.Status == 5)
            {
                return BadRequest("Đơn hàng đã hủy không thể in hóa đơn.");
            }

            return View(order);
        }
    }
}
