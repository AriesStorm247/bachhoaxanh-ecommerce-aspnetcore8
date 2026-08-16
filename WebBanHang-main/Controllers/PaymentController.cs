using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHang.Controllers
{
    public class PaymentController : Controller
    {
        private const string AppliedDiscountCodeSessionKey = "AppliedDiscountCode";

        private readonly ApplicationDbContext _context;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IConfiguration _configuration;
        private readonly LoyaltyService _loyaltyService;
        private readonly InventoryBatchService _inventoryBatchService;
        private readonly IEmailSender _emailSender;

        public PaymentController(
            ApplicationDbContext context,
            IPaymentGatewayService paymentGatewayService,
            IConfiguration configuration,
            LoyaltyService loyaltyService,
            InventoryBatchService inventoryBatchService,
            IEmailSender emailSender)
        {
            _context = context;
            _paymentGatewayService = paymentGatewayService;
            _configuration = configuration;
            _loyaltyService = loyaltyService;
            _inventoryBatchService = inventoryBatchService;
            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Demo(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.UserId != userId)
            {
                return Forbid();
            }

            if (order.IsPaid)
            {
                return RedirectToAction("CheckoutSuccess", "Cart", new { orderId = order.Id });
            }

            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> BankTransfer(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.UserId != userId)
            {
                return Forbid();
            }

            if (order.IsPaid)
            {
                return RedirectToAction("CheckoutSuccess", "Cart", new { orderId = order.Id });
            }

            ViewBag.QrUrl = _paymentGatewayService.CreateBankTransferQrUrl(order);
            ViewBag.PaymentCode = _paymentGatewayService.CreateBankTransferCode(order);
            ViewBag.BankCode = _configuration["Payment:BankTransfer:BankCode"] ?? "ACB";
            ViewBag.AccountNumber = _configuration["Payment:BankTransfer:AccountNumber"] ?? "34675617";
            ViewBag.AccountName = _configuration["Payment:BankTransfer:AccountName"] ?? "VO VAN PHU";

            return View(order);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckBankTransfer(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            if (order.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Bạn không có quyền kiểm tra đơn này." });
            }

            if (order.IsPaid)
            {
                return Json(new
                {
                    success = true,
                    message = "Đơn hàng đã được xác nhận thanh toán.",
                    redirectUrl = Url.Action("CheckoutSuccess", "Cart", new { orderId = order.Id })
                });
            }

            var verification = await _paymentGatewayService.VerifyBankTransferAsync(order, HttpContext.RequestAborted);
            if (!verification.IsConfirmed)
            {
                return Json(new { success = false, message = verification.Message });
            }

            var result = await MarkOrderPaidAsync(order.Id);
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            TempData["PaymentMessage"] = "Đã xác nhận chuyển khoản thành công. Đơn hàng đã được gửi sang admin duyệt.";
            return Json(new
            {
                success = true,
                message = result.Message,
                redirectUrl = Url.Action("CheckoutSuccess", "Cart", new { orderId = order.Id })
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteDemo(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.UserId != userId)
            {
                return Forbid();
            }

            var result = await MarkOrderPaidAsync(order.Id);
            if (!result.Success)
            {
                TempData["PaymentMessage"] = result.Message;
                return RedirectToAction("Demo", new { orderId = order.Id });
            }

            TempData["PaymentMessage"] = "Thanh toán online demo thành công. Đơn hàng đã được gửi sang admin duyệt.";
            return RedirectToAction("CheckoutSuccess", "Cart", new { orderId = order.Id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            if (!_paymentGatewayService.ValidateVnPayReturn(Request.Query))
            {
                TempData["PaymentMessage"] = "Thanh toán không hợp lệ hoặc chữ ký VNPay sai.";
                return RedirectToAction("Index", "Home");
            }

            if (!int.TryParse(Request.Query["vnp_TxnRef"].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var orderId))
            {
                TempData["PaymentMessage"] = "Không tìm thấy mã đơn hàng từ VNPay.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var responseCode = Request.Query["vnp_ResponseCode"].ToString();
            var transactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
            var expectedAmount = ((long)(decimal.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero) * 100))
                .ToString(CultureInfo.InvariantCulture);
            var receivedAmount = Request.Query["vnp_Amount"].ToString();

            if (responseCode == "00" && transactionStatus == "00" && receivedAmount == expectedAmount)
            {
                var result = await MarkOrderPaidAsync(order.Id);
                if (!result.Success)
                {
                    TempData["PaymentMessage"] = result.Message;
                    return RedirectToAction("Index", "Cart");
                }

                TempData["PaymentMessage"] = "Thanh toán VNPay thành công. Đơn hàng đã được gửi sang admin duyệt.";
                return RedirectToAction("CheckoutSuccess", "Cart", new { orderId = order.Id });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["PaymentMessage"] = receivedAmount != expectedAmount
                ? "Số tiền VNPay trả về không khớp với đơn hàng. Giỏ hàng vẫn được giữ để bạn thử lại."
                : "Thanh toán VNPay chưa thành công. Giỏ hàng vẫn được giữ để bạn thử lại.";
            return RedirectToAction("Index", "Cart");
        }

        private async Task<(bool Success, string Message)> MarkOrderPaidAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.");
            }

            if (order.IsPaid)
            {
                return (true, "Đơn hàng đã được xác nhận thanh toán trước đó.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var stockMessage = await ValidateOrderStockAsync(order);
            if (stockMessage != null)
            {
                return (false, stockMessage);
            }

            var discountMessage = await DeductDiscountUsageAsync(order);
            if (discountMessage != null)
            {
                return (false, discountMessage);
            }

            try
            {
                await _inventoryBatchService.DeductStockAsync(
                    order.OrderDetails.Select(detail => new InventoryStockItem
                    {
                        ProductId = detail.ProductId,
                        OrderDetailId = detail.Id,
                        Quantity = detail.Quantity,
                        ProductName = detail.Product?.Name ?? "Sản phẩm",
                        Unit = detail.Product?.Unit ?? string.Empty
                    }),
                    order.BranchId ?? 1,
                    order.Id);
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }

            order.Status = 0;
            order.IsPaid = true;
            order.PaidDate = WebBanHang.Services.PromotionService.GetVietnamNow();

            // Tích điểm tích lũy khi thanh toán hóa đơn
            await _loyaltyService.AddPointsFromPaidOrderAsync(order);

            await RemovePurchasedItemsFromCartAsync(order);
            HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Gửi email xác nhận đơn hàng sau khi thanh toán thành công
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
            var recipientEmail = user?.Email ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(recipientEmail))
            {
                await _emailSender.SendOrderConfirmationEmailAsync(order, recipientEmail);
            }

            return (true, "Thanh toán đã được xác nhận.");
        }

        private async Task<string?> DeductDiscountUsageAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.DiscountCode) || order.DiscountAmount <= 0)
            {
                return null;
            }

            var normalizedCode = order.DiscountCode.Trim().ToUpper();
            var discount = await _context.Discounts
                .FirstOrDefaultAsync(d => d.Code.ToUpper() == normalizedCode);

            if (discount == null)
            {
                return "Mã giảm giá của đơn hàng không còn tồn tại.";
            }

            if (discount.Quantity <= 0)
            {
                return $"Mã giảm giá {order.DiscountCode} đã hết lượt sử dụng.";
            }

            discount.Quantity--;
            return null;
        }

        private async Task RemovePurchasedItemsFromCartAsync(Order order)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == order.UserId);

            if (cart == null || !cart.CartItems.Any())
            {
                return;
            }

            foreach (var detail in order.OrderDetails)
            {
                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == detail.ProductId);
                if (cartItem == null)
                {
                    continue;
                }

                if (cartItem.Quantity <= detail.Quantity)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity -= detail.Quantity;
                }
            }
        }

        private async Task<string?> ValidateOrderStockAsync(Order order)
        {
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product == null)
                {
                    return "Có sản phẩm trong đơn hàng không còn tồn tại.";
                }
            }

            return await _inventoryBatchService.ValidateStockAsync(
                order.OrderDetails.Select(detail => new InventoryStockItem
                {
                    ProductId = detail.ProductId,
                    OrderDetailId = detail.Id,
                    Quantity = detail.Quantity,
                    ProductName = detail.Product?.Name ?? "Sản phẩm",
                    Unit = detail.Product?.Unit ?? string.Empty
                }),
                order.BranchId ?? 1);
        }

        [Authorize]
        public async Task<IActionResult> CancelPaymentAndReturnToCart(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order != null && order.UserId == userId && order.Status == 4)
            {
                order.Status = 5; // Giao không thành công
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Cart");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBankTransfer(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            if (order.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Bạn không có quyền hủy đơn này." });
            }

            if (order.IsPaid)
            {
                return Json(new { success = false, message = "Đơn hàng đã thanh toán, không thể hủy giao dịch." });
            }

            if (order.Status == 5)
            {
                return Json(new
                {
                    success = true,
                    message = "Giao dịch đã được hủy trước đó.",
                    redirectUrl = Url.Action("Index", "Cart")
                });
            }

            if (order.Status != 4)
            {
                return Json(new { success = false, message = "Chỉ có thể hủy giao dịch đang chờ chuyển khoản." });
            }

            order.Status = 5;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã hủy giao dịch chuyển khoản cho đơn hàng #{order.Id}.",
                redirectUrl = Url.Action("Index", "Cart")
            });
        }
    }
}
