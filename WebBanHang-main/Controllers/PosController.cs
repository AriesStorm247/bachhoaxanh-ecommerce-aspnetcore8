using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class PosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _roleService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LoyaltyService _loyaltyService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PosController> _logger;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly InventoryBatchService _inventoryBatchService;

        public PosController(
            ApplicationDbContext context,
            RoleService roleService,
            UserManager<IdentityUser> userManager,
            LoyaltyService loyaltyService,
            IEmailSender emailSender,
            ILogger<PosController> logger,
            IPaymentGatewayService paymentGatewayService,
            InventoryBatchService inventoryBatchService)
        {
            _context = context;
            _roleService = roleService;
            _userManager = userManager;
            _loyaltyService = loyaltyService;
            _emailSender = emailSender;
            _logger = logger;
            _paymentGatewayService = paymentGatewayService;
            _inventoryBatchService = inventoryBatchService;
        }

        private bool IsAuthorized()
        {
            var role = _roleService.GetRole(User);
            return role == 1 || role == 5; // 1 = Admin, 5 = Thu ngân
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthorized())
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var cashierName = !string.IsNullOrWhiteSpace(profile?.FullName) ? profile.FullName : User.Identity?.Name;
            ViewBag.CashierName = cashierName;
            ViewBag.WorkingBranchId = profile?.WorkingBranchId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode, int branchId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(barcode)) return BadRequest();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Barcode == barcode.Trim());

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm có mã vạch này!" });
            }

            if (!product.IsVisible)
            {
                return Json(new { success = false, message = $"Sản phẩm {product.Name} hiện đang tạm ẩn." });
            }

            var stock = await _inventoryBatchService.GetSellableQuantityAsync(product.Id, branchId);

            var discountedPrice = product.DiscountedPrice;

            return Json(new
            {
                success = true,
                id = product.Id,
                name = product.Name,
                price = product.Price,
                discountedPrice = discountedPrice,
                amount = stock,
                unit = product.Unit,
                isSoldByWeight = product.IsSoldByWeight,
                image = product.Image,
                barcode = product.Barcode,
                categoryName = product.Category?.Name ?? "Chưa phân loại"
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term, int branchId)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());

            var normalizedTerm = term.ToLower().Trim();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsVisible && (p.Name.ToLower().Contains(normalizedTerm) || (p.Barcode != null && p.Barcode.Contains(normalizedTerm))))
                .Take(15)
                .ToListAsync();

            var productIds = products.Select(p => p.Id).ToList();
            var inventoryDict = await _inventoryBatchService.GetSellableQuantitiesAsync(productIds, branchId);

            var result = products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                discountedPrice = p.DiscountedPrice,
                amount = inventoryDict.TryGetValue(p.Id, out var qty) ? qty : 0m,
                unit = p.Unit,
                isSoldByWeight = p.IsSoldByWeight,
                image = p.Image,
                barcode = p.Barcode,
                categoryName = p.Category?.Name ?? "Chưa phân loại"
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerByPhone(string phone)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (string.IsNullOrWhiteSpace(phone)) return BadRequest();

            var cleanedPhone = phone.Trim();

            // Find IdentityUser by phone or email
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == cleanedPhone || u.Email == cleanedPhone);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng trong hệ thống!" });
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            }

            var level = profile.MembershipLevel;
            var discountPercent = LoyaltyService.GetDiscountPercentage(level);
            var tierName = LoyaltyService.GetTierName(level);

            // Sync and get available vouchers
            await _loyaltyService.SyncObsoleteVouchersAsync(user.Id);
            var customerVouchers = await _context.CustomerVouchers
                .Where(cv => cv.UserId == user.Id)
                .ToListAsync();

            var now = PromotionService.GetVietnamNow();
            var activeVouchers = new List<object>();

            foreach (var cv in customerVouchers)
            {
                var discount = await _context.Discounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Code == cv.VoucherCode && d.IsSee && d.StartDate <= now && d.EndDate >= now && d.Quantity > 0);

                if (discount != null)
                {
                    var calculatedPct = DiscountPercent.Normalize(discount.DiscountValue);
                    activeVouchers.Add(new
                    {
                        code = discount.Code,
                        value = discount.DiscountValue,
                        normalizedPercent = calculatedPct,
                        minOrderValue = discount.MinOrderValue,
                        maxDiscount = discount.MaxDiscount,
                        description = $"Giảm {calculatedPct}% (tối đa {discount.MaxDiscount:N0}đ) cho đơn từ {discount.MinOrderValue:N0}đ"
                    });
                }
            }

            return Json(new
            {
                success = true,
                userId = user.Id,
                fullName = profile.FullName ?? user.UserName ?? "Hội viên BHX",
                phone = user.PhoneNumber ?? "",
                email = user.Email ?? "",
                loyaltyPoints = profile.LoyaltyPoints,
                membershipLevel = level,
                tierName = tierName,
                discountPercent = discountPercent,
                vouchers = activeVouchers
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateInStoreOrder([FromBody] InStoreOrderRequest request)
        {
            if (!IsAuthorized()) return Unauthorized();
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Danh sách sản phẩm trống!" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Validate và lấy sản phẩm
                    var orderDetails = new List<OrderDetail>();
                    var stockItems = new List<InventoryStockItem>();
                    decimal subtotal = 0m;

                    foreach (var item in request.Items)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null)
                        {
                            return Json(new { success = false, message = $"Sản phẩm ID {item.ProductId} không tồn tại!" });
                        }

                        if (!product.IsVisible)
                        {
                            return Json(new { success = false, message = $"Sản phẩm {product.Name} đã bị ẩn." });
                        }

                        var stock = await _inventoryBatchService.GetSellableQuantityAsync(product.Id, request.BranchId);

                        if (stock < item.Quantity)
                        {
                            return Json(new { success = false, message = $"Sản phẩm {product.Name} chỉ còn {stock} {product.Unit} ở các lô còn hạn, không đủ bán ({item.Quantity} {product.Unit})!" });
                        }

                        var itemPrice = product.DiscountedPrice;
                        subtotal += item.Quantity * itemPrice;

                        orderDetails.Add(new OrderDetail
                        {
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            Price = itemPrice
                        });

                        stockItems.Add(new InventoryStockItem
                        {
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            ProductName = product.Name,
                            Unit = product.Unit
                        });
                    }

                    // 2. Tính chiết khấu & voucher
                    decimal memberDiscountAmount = 0m;
                    decimal voucherDiscountAmount = 0m;
                    string? customerUserId = null;
                    string customerName = "Khách mua trực tiếp";
                    string customerPhone = request.CustomerPhone ?? "";
                    string customerEmail = "";

                    CustomerProfile? profile = null;
                    if (!string.IsNullOrWhiteSpace(request.CustomerUserId) || !string.IsNullOrWhiteSpace(request.CustomerPhone))
                    {
                        var lookupValue = request.CustomerPhone?.Trim();
                        IdentityUser? user = null;

                        if (!string.IsNullOrWhiteSpace(request.CustomerUserId))
                        {
                            user = await _userManager.FindByIdAsync(request.CustomerUserId.Trim());
                        }

                        if (user == null && !string.IsNullOrWhiteSpace(lookupValue))
                        {
                            user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == lookupValue || u.Email == lookupValue);
                        }

                        if (user != null)
                        {
                            customerUserId = user.Id;
                            customerPhone = user.PhoneNumber ?? "";
                            customerEmail = user.Email ?? "";
                            profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                            profile ??= await _loyaltyService.GetOrCreateProfileAsync(user.Id);

                            var profileName = profile?.FullName;
                            customerName = !string.IsNullOrWhiteSpace(profileName)
                                ? profileName
                                : user.UserName ?? user.Email ?? "Hội viên BHX";

                            if (profile != null)
                            {
                                var memberPct = LoyaltyService.GetDiscountPercentage(profile.MembershipLevel);
                                memberDiscountAmount = Math.Round(subtotal * memberPct, 0);
                            }
                        }
                    }

                    Discount? discount = null;
                    if (!string.IsNullOrWhiteSpace(request.DiscountCode) && customerUserId != null)
                    {
                        // Kiểm tra voucher của customer
                        var now = PromotionService.GetVietnamNow();
                        discount = await _context.Discounts.FirstOrDefaultAsync(d =>
                            d.Code.ToUpper() == request.DiscountCode.Trim().ToUpper()
                            && d.IsSee
                            && d.StartDate <= now
                            && d.EndDate >= now
                            && d.Quantity > 0
                            && (string.IsNullOrEmpty(d.UserId) || d.UserId == customerUserId));

                        if (discount == null)
                        {
                            return Json(new { success = false, message = "Mã giảm giá không khả dụng hoặc không thuộc về khách hàng này!" });
                        }

                        if (discount.Code.ToUpper().Contains("FREESHIP") || 
                            discount.Code.ToUpper().Contains("FREE_SHIP") || 
                            discount.Code.ToUpper().Contains("FREE-SHIP"))
                        {
                            return Json(new { success = false, message = "Mã FREE SHIP chỉ áp dụng cho đơn hàng giao tận nơi khi mua hàng Online!" });
                        }

                        if (subtotal < discount.MinOrderValue)
                        {
                            return Json(new { success = false, message = $"Giá trị đơn chưa đạt tối thiểu {discount.MinOrderValue:N0}đ để dùng voucher." });
                        }

                        var pct = DiscountPercent.Normalize(discount.DiscountValue);
                        voucherDiscountAmount = subtotal * pct / 100m;
                        if (discount.MaxDiscount > 0 && voucherDiscountAmount > discount.MaxDiscount)
                        {
                            voucherDiscountAmount = discount.MaxDiscount;
                        }
                        voucherDiscountAmount = Math.Round(Math.Min(voucherDiscountAmount, subtotal), 0);

                        // Trừ số lượng sử dụng của voucher
                        discount.Quantity--;

                        // Xoá voucher khỏi kho Voucher khách hàng sở hữu
                        var cv = await _context.CustomerVouchers.FirstOrDefaultAsync(v => v.UserId == customerUserId && v.VoucherCode == discount.Code);
                        if (cv != null)
                        {
                            _context.CustomerVouchers.Remove(cv);
                        }
                    }

                    decimal totalDiscount = memberDiscountAmount + voucherDiscountAmount;
                    decimal finalTotal = Math.Max(0m, subtotal - totalDiscount);

                    bool isBankTransfer = request.PaymentMethod == "Chuyển khoản";

                    // 3. Tạo Đơn Hàng Mua Trực Tiếp (Status = 3: Đã hoàn thành, hoặc 4: Chờ thanh toán nếu chuyển khoản)
                    var order = new Order
                    {
                        UserId = customerUserId ?? "guest-user",
                        FullName = customerName,
                        Phone = customerPhone,
                        Address = "Mua trực tiếp tại cửa hàng",
                        OrderDate = PromotionService.GetVietnamNow(),
                        Status = isBankTransfer ? 4 : 3, // 4 = PendingOnlinePaymentStatus, 3 = Đã giao hàng / Hoàn thành
                        IsPaid = !isBankTransfer,
                        PaidDate = !isBankTransfer ? PromotionService.GetVietnamNow() : null,
                        TotalAmount = finalTotal,
                        DiscountCode = discount?.Code,
                        DiscountAmount = totalDiscount,
                        ShippingMethod = "Direct Purchase",
                        ShippingFee = 0m,
                        ShippingDistance = 0.0,
                        PaymentMethod = request.PaymentMethod,
                        BranchId = request.BranchId,
                        OrderDetails = orderDetails
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    for (int i = 0; i < stockItems.Count && i < orderDetails.Count; i++)
                    {
                        stockItems[i].OrderDetailId = orderDetails[i].Id;
                    }

                    await _inventoryBatchService.DeductStockAsync(stockItems, request.BranchId, order.Id);
                    await _context.SaveChangesAsync();

                    if (isBankTransfer)
                    {
                        var transferCode = _paymentGatewayService.CreatePosBankTransferCode(order);
                        var qrUrl = _paymentGatewayService.CreatePosBankTransferQrUrl(order);

                        await transaction.CommitAsync();

                        return Json(new
                        {
                            success = true,
                            requiresVerification = true,
                            orderId = order.Id,
                            orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm:ss"),
                            subtotal = subtotal,
                            memberDiscount = memberDiscountAmount,
                            voucherDiscount = voucherDiscountAmount,
                            discountCode = discount?.Code ?? "",
                            finalTotal = finalTotal,
                            customerName = customerName,
                            paymentMethod = order.PaymentMethod,
                            transferCode = transferCode,
                            qrUrl = qrUrl
                        });
                    }

                    // 4. Cộng điểm tích lũy & xét nâng cấp VIP cho hội viên
                    int earnedPoints = 0;
                    string levelUpMessage = "";
                    if (profile != null)
                    {
                        earnedPoints = await _loyaltyService.AddPointsFromPaidOrderAsync(order);
                        await _context.SaveChangesAsync();
                    }

                    // Commit Database Transaction
                    await transaction.CommitAsync();

                    // 5. Gửi email hóa đơn điện tử cho khách hàng nếu có email
                    if (!string.IsNullOrWhiteSpace(customerEmail))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var emailBody = BuildReceiptEmailHtml(order, customerName, subtotal, memberDiscountAmount, voucherDiscountAmount, finalTotal, earnedPoints);
                                await _emailSender.SendEmailAsync(customerEmail, $"Hóa đơn điện tử Bách Hóa XANH - Đơn hàng #{order.Id}", emailBody);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Lỗi khi gửi email hóa đơn cho đơn hàng {OrderId}", order.Id);
                            }
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        orderId = order.Id,
                        orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm:ss"),
                        subtotal = subtotal,
                        memberDiscount = memberDiscountAmount,
                        voucherDiscount = voucherDiscountAmount,
                        discountCode = discount?.Code ?? "",
                        finalTotal = finalTotal,
                        earnedPoints = earnedPoints,
                        levelUpMessage = levelUpMessage,
                        customerName = customerName,
                        paymentMethod = order.PaymentMethod
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi trong quá trình tạo đơn POS.");
                    return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống: " + ex.Message });
                }
            }
        }

        private string BuildReceiptEmailHtml(Order order, string customerName, decimal subtotal, decimal memberDiscount, decimal voucherDiscount, decimal finalTotal, int earnedPoints)
        {
            var sb = new StringBuilder();
            sb.Append($@"
<div style=""font-family: 'Be Vietnam Pro', Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e5e0; border-radius: 12px; background-color: #ffffff;"">
    <div style=""text-align: center; border-bottom: 2px solid #2c9e47; padding-bottom: 15px; margin-bottom: 20px;"">
        <h2 style=""color: #1a7a2e; margin: 0; font-size: 26px; font-weight: 900;"">Bách Hóa <span style=""color: #ffd600;"">XANH</span></h2>
        <p style=""color: #666; font-size: 12px; margin: 5px 0 0 0; font-style: italic;"">Siêu thị gần nhà, giá tiết kiệm</p>
    </div>
    
    <h3 style=""color: #333; font-size: 18px; font-weight: 700; margin-top: 0; text-align: center;"">HÓA ĐƠN MUA HÀNG ĐIỆN TỬ</h3>
    <p style=""font-size: 13px; color: #555; text-align: center; margin-bottom: 25px;"">Mã đơn hàng: <strong>#{order.Id}</strong> | Thời gian: {order.OrderDate:dd/MM/yyyy HH:mm:ss}</p>

    <div style=""background-color: #f9fbf9; padding: 15px; border-radius: 8px; margin-bottom: 20px; font-size: 14px; border-left: 4px solid #2c9e47;"">
        <p style=""margin: 0 0 8px 0;""><strong>Khách hàng:</strong> {customerName}</p>
        <p style=""margin: 0 0 8px 0;""><strong>Số điện thoại:</strong> {order.Phone}</p>
        <p style=""margin: 0;""><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
    </div>

    <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 13px;"">
        <thead>
            <tr style=""background-color: #e8f7ec; color: #1a7a2e;"">
                <th style=""padding: 10px; text-align: left; border-bottom: 1.5px solid #2c9e47;"">Sản phẩm</th>
                <th style=""padding: 10px; text-align: center; border-bottom: 1.5px solid #2c9e47; width: 60px;"">SL</th>
                <th style=""padding: 10px; text-align: right; border-bottom: 1.5px solid #2c9e47; width: 90px;"">Đơn giá</th>
                <th style=""padding: 10px; text-align: right; border-bottom: 1.5px solid #2c9e47; width: 100px;"">Thành tiền</th>
            </tr>
        </thead>
        <tbody>");

            foreach (var detail in order.OrderDetails)
            {
                var productName = _context.Products.Find(detail.ProductId)?.Name ?? "Sản phẩm";
                sb.Append($@"
            <tr style=""border-bottom: 1px solid #eee;"">
                <td style=""padding: 10px; color: #333;""><strong>{productName}</strong></td>
                <td style=""padding: 10px; text-align: center; color: #666;"">{detail.Quantity}</td>
                <td style=""padding: 10px; text-align: right; color: #666;"">{detail.Price:N0}đ</td>
                <td style=""padding: 10px; text-align: right; color: #333; font-weight: bold;"">{(detail.Quantity * detail.Price):N0}đ</td>
            </tr>");
            }

            sb.Append($@"
        </tbody>
    </table>

    <div style=""border-top: 2px dashed #ececec; padding-top: 15px; font-size: 14px;"">
        <table style=""width: 100%;"">
            <tr>
                <td style=""padding: 4px 0; color: #666;"">Cộng tiền hàng:</td>
                <td style=""padding: 4px 0; text-align: right; font-weight: bold;"">{subtotal:N0}đ</td>
            </tr>");

            if (memberDiscount > 0)
            {
                sb.Append($@"
            <tr>
                <td style=""padding: 4px 0; color: #666;"">Chiết khấu thành viên:</td>
                <td style=""padding: 4px 0; text-align: right; color: #c94b00; font-weight: bold;"">-{memberDiscount:N0}đ</td>
            </tr>");
            }

            if (voucherDiscount > 0)
            {
                sb.Append($@"
            <tr>
                <td style=""padding: 4px 0; color: #666;"">Voucher giảm giá:</td>
                <td style=""padding: 4px 0; text-align: right; color: #c94b00; font-weight: bold;"">-{voucherDiscount:N0}đ</td>
            </tr>");
            }

            sb.Append($@"
            <tr style=""font-size: 16px;"">
                <td style=""padding: 10px 0 0 0; color: #1a7a2e; font-weight: bold;"">TỔNG THANH TOÁN:</td>
                <td style=""padding: 10px 0 0 0; text-align: right; color: #e82828; font-weight: 900; font-size: 18px;"">{finalTotal:N0}đ</td>
            </tr>
        </table>
    </div>");

            if (earnedPoints > 0)
            {
                sb.Append($@"
    <div style=""background-color: #fffde7; border: 1.5px solid #fff59d; border-radius: 8px; padding: 12px; margin-top: 20px; font-size: 13px; color: #827717; text-align: center;"">
        🎉 Đơn hàng này giúp bạn tích lũy thêm <strong>+{earnedPoints} điểm</strong> hội viên!
    </div>");
            }

            sb.Append($@"
    <div style=""text-align: center; font-size: 11px; color: #999; margin-top: 30px; border-top: 1px solid #f0f0f0; padding-top: 15px;"">
        Cảm ơn quý khách đã mua sắm tại Bách Hóa XANH!<br />
        Hotline hỗ trợ: 1800 6936 | Website: bachhoaxanh.com
    </div>
</div>");

            return sb.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> CheckPosBankTransfer(int orderId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            if (order.IsPaid)
            {
                decimal paidSubtotal = order.OrderDetails.Sum(d => d.Quantity * d.Price);
                decimal paidMemberDiscount = 0m;
                decimal paidVoucherDiscount = 0m;

                if (order.UserId != "guest-user")
                {
                    var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                    if (profile != null)
                    {
                        var memberPct = LoyaltyService.GetDiscountPercentage(profile.MembershipLevel);
                        paidMemberDiscount = Math.Round(paidSubtotal * memberPct, 0);
                    }
                }

                if (!string.IsNullOrWhiteSpace(order.DiscountCode))
                {
                    paidVoucherDiscount = order.DiscountAmount - paidMemberDiscount;
                    if (paidVoucherDiscount < 0) paidVoucherDiscount = 0;
                }

                return Json(new
                {
                    success = true,
                    message = "Đơn hàng đã được xác nhận thanh toán trước đó.",
                    orderId = order.Id,
                    orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm:ss"),
                    subtotal = paidSubtotal,
                    memberDiscount = paidMemberDiscount,
                    voucherDiscount = paidVoucherDiscount,
                    discountCode = order.DiscountCode ?? "",
                    finalTotal = order.TotalAmount,
                    customerName = order.FullName,
                    paymentMethod = order.PaymentMethod
                });
            }

            // Gọi PaymentGatewayService để kiểm tra chuyển khoản (dùng prefix POS: BHX_)
            var verification = await _paymentGatewayService.VerifyPosBankTransferAsync(order, HttpContext.RequestAborted);
            if (!verification.IsConfirmed)
            {
                return Json(new { success = false, message = verification.Message });
            }

            // Xác nhận thanh toán thành công
            order.IsPaid = true;
            order.Status = 3; // Đã giao hàng / Hoàn thành
            order.PaidDate = PromotionService.GetVietnamNow();

            // Cộng điểm tích lũy & xét nâng cấp VIP cho hội viên
            int earnedPoints = 0;
            if (order.UserId != "guest-user")
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                if (profile != null)
                {
                    earnedPoints = await _loyaltyService.AddPointsFromPaidOrderAsync(order);
                }
            }

            await _context.SaveChangesAsync();

            // Gửi email hóa đơn điện tử cho khách hàng nếu có nhập email để tích điểm
            string customerEmail = "";
            if (order.UserId != "guest-user")
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                customerEmail = user?.Email ?? "";
            }

            // Tính toán breakdown để gửi email chính xác
            decimal subtotal = order.OrderDetails.Sum(d => d.Quantity * d.Price);
            decimal memberDiscountAmount = 0m;
            decimal voucherDiscountAmount = 0m;

            if (order.UserId != "guest-user")
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == order.UserId);
                if (profile != null)
                {
                    var memberPct = LoyaltyService.GetDiscountPercentage(profile.MembershipLevel);
                    memberDiscountAmount = Math.Round(subtotal * memberPct, 0);
                }
            }

            if (!string.IsNullOrWhiteSpace(order.DiscountCode))
            {
                voucherDiscountAmount = order.DiscountAmount - memberDiscountAmount;
                if (voucherDiscountAmount < 0) voucherDiscountAmount = 0;
            }

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailBody = BuildReceiptEmailHtml(order, order.FullName, subtotal, memberDiscountAmount, voucherDiscountAmount, order.TotalAmount, earnedPoints);
                        await _emailSender.SendEmailAsync(customerEmail, $"Hóa đơn điện tử Bách Hóa XANH - Đơn hàng #{order.Id}", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi email hóa đơn cho đơn hàng {OrderId}", order.Id);
                    }
                });
            }

            return Json(new
            {
                success = true,
                message = "Thanh toán chuyển khoản thành công!",
                orderId = order.Id,
                orderDate = order.OrderDate.ToString("dd/MM/yyyy HH:mm:ss"),
                subtotal = subtotal,
                memberDiscount = memberDiscountAmount,
                voucherDiscount = voucherDiscountAmount,
                discountCode = order.DiscountCode ?? "",
                finalTotal = order.TotalAmount,
                earnedPoints = earnedPoints,
                customerName = order.FullName,
                paymentMethod = order.PaymentMethod
            });
        }

        [HttpPost]
        public async Task<IActionResult> CancelPosOrder(int orderId)
        {
            if (!IsAuthorized()) return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            if (order.Status == 5)
            {
                return Json(new { success = true, message = "Đơn hàng đã được hủy trước đó." });
            }

            if (order.IsPaid)
            {
                return Json(new { success = false, message = "Đơn hàng đã thanh toán, không thể hủy." });
            }

            // Hủy đơn (Status = 5) và hoàn trả số lượng vào kho của chi nhánh
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

            // Nếu đơn hàng có sử dụng voucher, hoàn trả số lượng sử dụng của voucher
            if (!string.IsNullOrWhiteSpace(order.DiscountCode) && order.UserId != "guest-user")
            {
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code.ToUpper() == order.DiscountCode.Trim().ToUpper());
                if (discount != null)
                {
                    discount.Quantity++;
                    
                    // Thêm lại voucher vào kho Voucher khách hàng sở hữu
                    var cv = new CustomerVoucher
                    {
                        UserId = order.UserId,
                        VoucherCode = discount.Code
                    };
                    _context.CustomerVouchers.Add(cv);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đơn hàng #{orderId} đã được hủy và hoàn trả tồn kho." });
        }
    }

    public class InStoreOrderRequest
    {
        public string? CustomerUserId { get; set; }
        public string? CustomerPhone { get; set; }
        public string PaymentMethod { get; set; } = "Tiền mặt";
        public string? DiscountCode { get; set; }
        public List<InStoreOrderItem> Items { get; set; }
        public int BranchId { get; set; }
    }

    public class InStoreOrderItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
