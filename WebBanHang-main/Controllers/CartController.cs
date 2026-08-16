using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHang.Controllers
{
    public class CartController : Controller
    {
        private string GetOrCreateCartUserId()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(id))
                    return id;
            }

            var guestId = HttpContext.Session.GetString("GuestCartId");
            if (string.IsNullOrEmpty(guestId))
            {
                guestId = "guest_" + Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("GuestCartId", guestId);
            }
            return guestId;
        }
        private const int PendingOnlinePaymentStatus = 4;
        private const string AppliedDiscountCodeSessionKey = "AppliedDiscountCode";

        private readonly ApplicationDbContext _context;
        private readonly IPaymentGatewayService _paymentGatewayService;
        private readonly IDiscountService _discountService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly LoyaltyService _loyaltyService;
        private readonly RoleService _roleService;
        private readonly InventoryBatchService _inventoryBatchService;
        private readonly AprioriService _aprioriService;
        private readonly IEmailSender _emailSender;

        public CartController(
            ApplicationDbContext context,
            IPaymentGatewayService paymentGatewayService,
            IDiscountService discountService,
            UserManager<IdentityUser> userManager,
            LoyaltyService loyaltyService,
            RoleService roleService,
            InventoryBatchService inventoryBatchService,
            AprioriService aprioriService,
            IEmailSender emailSender)
        {
            _context = context;
            _paymentGatewayService = paymentGatewayService;
            _discountService = discountService;
            _userManager = userManager;
            _loyaltyService = loyaltyService;
            _roleService = roleService;
            _inventoryBatchService = inventoryBatchService;
            _aprioriService = aprioriService;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetOrCreateCartUserId();
            if (User.Identity?.IsAuthenticated == true)
            {
                await _loyaltyService.SyncObsoleteVouchersAsync(userId);
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CartItems = new List<CartItem>() };
            }

            await SetDiscountViewBagAsync(cart);
            return View(cart);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = GetOrCreateCartUserId();

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (!product.IsVisible)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm này hiện đang tạm ẩn và chưa thể đặt mua.",
                    cartCount = await GetCartQuantityAsync(userId)
                });
            }

            // Lấy chi nhánh hiện tại và tồn kho chi nhánh
            int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
            if (activeBranchId <= 0) activeBranchId = 1;
            var branchStock = await _inventoryBatchService.GetSellableQuantityAsync(productId, activeBranchId);

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            decimal currentQuantity = cartItem?.Quantity ?? 0m;
            decimal addAmount = product.IsSoldByWeight ? 0.5m : 1.0m; // Đối với hàng bán theo cân mặc định thêm 0.5kg, hàng cái thêm 1

            bool branchExplicitlyChosen = HttpContext.Session.GetString("IsBranchExplicitlyChosen") == "true"
                || Request.Cookies["IsBranchExplicitlyChosen"] == "true";

            if (branchExplicitlyChosen)
            {
                if (branchStock <= 0 || currentQuantity + addAmount > branchStock)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sản phẩm {product.Name} chỉ còn {branchStock} {product.Unit} tại siêu thị đã chọn.",
                        cartCount = await GetCartQuantityAsync(userId)
                    });
                }
            }

            if (cartItem == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = addAmount
                });
            }
            else
            {
                cartItem.Quantity += addAmount;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng của quý khách!",
                cartCount = await GetCartQuantityAsync(userId)
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, decimal amount)
        {
            var userId = GetOrCreateCartUserId();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem != null)
            {
                int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
                if (activeBranchId <= 0) activeBranchId = 1;
                var branchStock = await _inventoryBatchService.GetSellableQuantityAsync(cartItem.ProductId, activeBranchId);

                var newQuantity = cartItem.Quantity + amount;

                bool branchExplicitlyChosen = HttpContext.Session.GetString("IsBranchExplicitlyChosen") == "true"
                    || Request.Cookies["IsBranchExplicitlyChosen"] == "true";

                if (branchExplicitlyChosen && newQuantity > branchStock)
                {
                    TempData["CartMessage"] = $"Sản phẩm {cartItem.Product.Name} chỉ còn {branchStock} {cartItem.Product.Unit} tại siêu thị đã chọn.";
                    return RedirectToAction("Index");
                }

                if (newQuantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = newQuantity;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantityDirect(int cartItemId, decimal quantity)
        {
            var userId = GetOrCreateCartUserId();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem != null)
            {
                int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
                if (activeBranchId <= 0) activeBranchId = 1;
                var branchStock = await _inventoryBatchService.GetSellableQuantityAsync(cartItem.ProductId, activeBranchId);

                bool branchExplicitlyChosen = HttpContext.Session.GetString("IsBranchExplicitlyChosen") == "true"
                    || Request.Cookies["IsBranchExplicitlyChosen"] == "true";

                if (branchExplicitlyChosen && quantity > branchStock)
                {
                    TempData["CartMessage"] = $"Sản phẩm {cartItem.Product.Name} chỉ còn {branchStock} {cartItem.Product.Unit} tại siêu thị đã chọn.";
                    return RedirectToAction("Index");
                }

                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = GetOrCreateCartUserId();

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string? fullName = null, string? phone = null, string? address = null, string? shippingMethod = null, decimal? shippingFee = null, string? shippingDistance = null)
        {
            var userId = GetOrCreateCartUserId();
            var authUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (authUserId != null)
            {
                await _loyaltyService.SyncObsoleteVouchersAsync(authUserId);
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index");
            }

            var profile = authUserId != null
                ? await _context.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == authUserId)
                : null;
            var user = User.Identity?.IsAuthenticated == true ? await _userManager.GetUserAsync(User) : null;

            ViewBag.PrefilledFullName = fullName ?? profile?.FullName;
            ViewBag.ProfilePhone = phone ?? user?.PhoneNumber;
            ViewBag.ProfileAddress = address ?? profile?.ShippingAddress;
            ViewBag.SavedProfileAddress = profile?.ShippingAddress;
            ViewBag.PrefilledShippingMethod = shippingMethod;
            ViewBag.PrefilledShippingFee = shippingFee;
            
            double? parsedDistance = null;
            if (!string.IsNullOrEmpty(shippingDistance))
            {
                parsedDistance = ParseDistance(shippingDistance);
            }
            ViewBag.PrefilledShippingDistance = parsedDistance;

            await SetDiscountViewBagAsync(cart);
            return View(cart);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmOrderTest()
        {
            var cart = new Cart
            {
                UserId = "test-user",
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = 1,
                        Quantity = 1,
                        Product = new Product
                        {
                            Id = 1,
                            Name = "Thịt heo",
                            Price = 50000m,
                            Category = new Category { Id = 1, Name = "Thịt, cá, trứng" }
                        }
                    }
                }
            };
            ViewBag.ProfilePhone = "0123456789";
            ViewBag.ProfileAddress = "42/20A Nguyễn Giản Thanh, Phường 15, Quận 10, Thành phố Hồ Chí Minh";
            ViewBag.SavedProfileAddress = "42/20A Nguyễn Giản Thanh, Phường 15, Quận 10, Thành phố Hồ Chí Minh";
            await SetDiscountViewBagAsync(cart);
            return View("ConfirmOrder", cart);
        }

        [HttpGet]
        public async Task<IActionResult> CheckBranchStock()
        {
            var userId = GetOrCreateCartUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return Json(new { success = true, hasEnoughStock = true, outOfStockProducts = new List<string>() });
            }

            int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
            if (activeBranchId <= 0) activeBranchId = 1;

            var branch = await _context.Branches.FindAsync(activeBranchId);
            var branchName = branch?.Name ?? $"Chi nhánh #{activeBranchId}";

            var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
            var inventories = await _inventoryBatchService.GetSellableQuantitiesAsync(productIds, activeBranchId);

            var outOfStockProducts = new List<string>();

            foreach (var item in cart.CartItems)
            {
                if (item.Product == null) continue;

                var stock = inventories.TryGetValue(item.ProductId, out var qty) ? qty : 0m;
                if (stock <= 0 || item.Quantity > stock)
                {
                    outOfStockProducts.Add(item.Product.Name);
                }
            }

            return Json(new
            {
                success = true,
                hasEnoughStock = outOfStockProducts.Count == 0,
                branchName = branchName,
                outOfStockProducts = outOfStockProducts
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string fullName, string phone, string address, string paymentMethod, string shippingMethod, decimal shippingFee, string? shippingDistance)
        {
            var userId = GetOrCreateCartUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index");
            }

            // Kiểm tra tồn kho tại chi nhánh hiện tại
            int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
            if (activeBranchId <= 0) activeBranchId = 1;
            var stockMessage = await ValidateCartStockAsync(cart.CartItems, activeBranchId);
            if (stockMessage != null)
            {
                TempData["CartMessage"] = stockMessage;
                return RedirectToAction("Index");
            }

            var normalizedPaymentMethod = paymentMethod?.Trim() ?? "COD";
            var isBankTransfer = normalizedPaymentMethod.Equals("QRCode", StringComparison.OrdinalIgnoreCase)
                || normalizedPaymentMethod.Equals("BankTransfer", StringComparison.OrdinalIgnoreCase);
            var isVnPay = normalizedPaymentMethod.Equals("Online", StringComparison.OrdinalIgnoreCase)
                || normalizedPaymentMethod.Equals("VNPay", StringComparison.OrdinalIgnoreCase);
            var needsPaymentConfirmation = isBankTransfer || isVnPay;
            var subtotal = GetCartSubtotal(cart);
            var appliedDiscount = await GetAppliedDiscountAsync(subtotal, trackDiscount: !needsPaymentConfirmation);

            if (appliedDiscount != null && !appliedDiscount.Success)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                TempData["CartMessage"] = appliedDiscount.Message;
                return RedirectToAction("Index");
            }

            // Membership tier discount
            var memberProfile = await _context.CustomerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            var memberLevel = memberProfile?.MembershipLevel ?? 0;
            var memberDiscountPct = LoyaltyService.GetDiscountPercentage(memberLevel);
            var memberDiscountAmount = Math.Round(subtotal * memberDiscountPct, 0);
            var voucherDiscountAmount = appliedDiscount?.DiscountAmount ?? 0m;

            // Combo discounts
            var comboResult = await _aprioriService.GetComboDiscountsForCartAsync(cart.CartItems.ToList());
            var comboDiscountAmount = comboResult.TotalDiscount;

            var totalDiscountAmount = memberDiscountAmount + voucherDiscountAmount + comboDiscountAmount;
            var finalTotal = Math.Max(0m, subtotal - totalDiscountAmount);

            double parsedDistance = ParseDistance(shippingDistance);

            decimal actualShippingFee = shippingFee;
            if (appliedDiscount != null && appliedDiscount.Success && appliedDiscount.Code != null &&
                (appliedDiscount.Code.ToUpper().Contains("FREESHIP") || 
                 appliedDiscount.Code.ToUpper().Contains("FREE_SHIP") || 
                 appliedDiscount.Code.ToUpper().Contains("FREE-SHIP")))
            {
                actualShippingFee = 0;
            }

            var order = new Order
            {
                UserId = userId,
                FullName = fullName,
                Phone = phone,
                Address = address,
                OrderDate = WebBanHang.Services.PromotionService.GetVietnamNow(),
                Status = needsPaymentConfirmation ? PendingOnlinePaymentStatus : 0,
                IsPaid = false,
                TotalAmount = finalTotal + actualShippingFee,
                DiscountCode = appliedDiscount?.Code,
                DiscountAmount = totalDiscountAmount,
                ShippingMethod = shippingMethod,
                ShippingFee = actualShippingFee,
                ShippingDistance = parsedDistance,
                PaymentMethod = normalizedPaymentMethod,
                BranchId = activeBranchId,
                OrderDetails = new List<OrderDetail>()
            };

            foreach (var item in cart.CartItems)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product.DiscountedPrice
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Orders.Add(order);

            if (!needsPaymentConfirmation)
            {
                if (appliedDiscount?.Discount != null)
                {
                    appliedDiscount.Discount.Quantity--;
                    HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                }

                await _context.SaveChangesAsync();
                await DeductStockAsync(order.OrderDetails, activeBranchId, order.Id);
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            if (!needsPaymentConfirmation)
            {
                var userEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail) && User.Identity?.IsAuthenticated == true)
                {
                    var appUser = await _userManager.GetUserAsync(User);
                    userEmail = appUser?.Email;
                }
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await _emailSender.SendOrderConfirmationEmailAsync(order, userEmail);
                }
            }

            if (isBankTransfer)
            {
                return RedirectToAction("BankTransfer", "Payment", new { orderId = order.Id });
            }

            if (isVnPay)
            {
                var paymentUrl = _paymentGatewayService.CreatePaymentUrl(HttpContext, order);
                return Redirect(paymentUrl);
            }

            return RedirectToAction("CheckoutSuccess", new { orderId = order.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetOrCreateCartUserId();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> CheckoutSuccess(int? orderId)
        {
            ViewBag.OrderId = orderId;
            if (orderId.HasValue)
            {
                var userId = GetOrCreateCartUserId();
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value && o.UserId == userId);

                ViewBag.Phone = order?.Phone;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChoosePaymentMethod(string fullName, string phone, string address, string shippingMethod, decimal shippingFee, string? shippingDistance)
        {
            var userId = GetOrCreateCartUserId();
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                return RedirectToAction("Index");
            }

            int activeBranchId = HttpContext.Session.GetInt32("ActiveBranchId") ?? 1;
            if (activeBranchId <= 0) activeBranchId = 1;
            var stockMessage = await ValidateCartStockAsync(cart.CartItems, activeBranchId);
            if (stockMessage != null)
            {
                TempData["CartMessage"] = stockMessage;
                return RedirectToAction("Index");
            }

            var subtotal = GetCartSubtotal(cart);
            var appliedDiscount = await GetAppliedDiscountAsync(subtotal);

            if (appliedDiscount != null && !appliedDiscount.Success)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                TempData["CartMessage"] = appliedDiscount.Message;
                return RedirectToAction("Index");
            }

            // Membership tier discount
            var memberProfileCPM = await _context.CustomerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            var memberLevelCPM = memberProfileCPM?.MembershipLevel ?? 0;
            var memberDiscountPctCPM = LoyaltyService.GetDiscountPercentage(memberLevelCPM);
            var memberDiscountAmountCPM = Math.Round(subtotal * memberDiscountPctCPM, 0);
            var voucherDiscountAmountCPM = appliedDiscount?.DiscountAmount ?? 0m;
            var totalDiscountAmountCPM = memberDiscountAmountCPM + voucherDiscountAmountCPM;
            var finalTotalCPM = Math.Max(0m, subtotal - totalDiscountAmountCPM);

            ViewBag.FullName = fullName;
            ViewBag.Phone = phone;
            ViewBag.Address = address;
            ViewBag.CustomerId = userId;
            ViewBag.SubtotalAmount = subtotal;
            ViewBag.AppliedDiscountCode = appliedDiscount?.Code;
            ViewBag.MembershipDiscountPercent = memberDiscountPctCPM;
            ViewBag.MembershipDiscountAmount = memberDiscountAmountCPM;
            ViewBag.VoucherDiscountAmount = voucherDiscountAmountCPM;
            ViewBag.DiscountAmount = totalDiscountAmountCPM;
            decimal actualShippingFeeCPM = shippingFee;
            if (appliedDiscount != null && appliedDiscount.Success && appliedDiscount.Code != null &&
                (appliedDiscount.Code.ToUpper().Contains("FREESHIP") || 
                 appliedDiscount.Code.ToUpper().Contains("FREE_SHIP") || 
                 appliedDiscount.Code.ToUpper().Contains("FREE-SHIP")))
            {
                actualShippingFeeCPM = 0;
            }

            ViewBag.ShippingMethod = shippingMethod;
            ViewBag.ShippingFee = actualShippingFeeCPM;
            
            double parsedDist = ParseDistance(shippingDistance);
            ViewBag.ShippingDistance = parsedDist.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            
            ViewBag.TotalAmount = finalTotalCPM + actualShippingFeeCPM;

            return View();
        }

        public async Task<IActionResult> Tracking(string? query = null, string? phone = null, int? orderId = null, int page = 1, int pageSize = 5)
        {
            var searchQuery = (query ?? phone ?? (orderId.HasValue ? orderId.Value.ToString() : "")).Trim();
            ViewBag.Query = searchQuery;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return View(new List<Order>());
            }

            // 1. Nếu từ khóa là số và khớp với Mã đơn hàng
            if (int.TryParse(searchQuery, out int targetOrderId))
            {
                var singleOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(o => o.Id == targetOrderId);

                if (singleOrder != null)
                {
                    ViewBag.SingleOrder = singleOrder;
                    return View(new List<Order>());
                }
            }

            // 2. Tìm kiếm theo Số điện thoại hoặc Họ tên khách hàng
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .Where(o => o.Phone == searchQuery || o.FullName.Contains(searchQuery))
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool canCancel = false;
            int cancelledThisMonthCount = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    canCancel = true;
                    int maxCancelLimit = LoyaltyService.GetMaxCancelLimit(profile.MembershipLevel);
                    ViewBag.MaxCancelLimit = maxCancelLimit;

                    var now = WebBanHang.Services.PromotionService.GetVietnamNow();
                    var startOfMonth = new DateTime(now.Year, now.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);
                    cancelledThisMonthCount = await _context.Orders.CountAsync(o => 
                        o.UserId == userId && 
                        o.Status == 5 && 
                        o.OrderDate >= startOfMonth && 
                        o.OrderDate < endOfMonth);
                }
            }

            ViewBag.CanCancel = canCancel;
            ViewBag.CancelledCount = cancelledThisMonthCount;

            var pagedOrders = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedOrders);
        }

        [HttpGet]
        public async Task<IActionResult> SearchProductForTracking(string query)
        {
            var role = _roleService.GetRole(User);
            if (role == 0)
            {
                return Json(new { success = false, message = "Bạn không có quyền truy cập thông tin sản phẩm." });
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Vui lòng nhập từ khóa tìm kiếm." });
            }

            var queryClean = query.Trim().ToLower();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Name.ToLower().Contains(queryClean) || p.Barcode == queryClean)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    DiscountedPrice = p.Price,
                    p.Amount,
                    p.Image,
                    CategoryName = p.Category != null ? p.Category.Name : "Chưa phân loại",
                    p.IsVisible,
                    p.IsHot,
                    p.IsBestSeller,
                    Barcode = p.Barcode ?? ""
                })
                .ToListAsync();

            // Calculate the discounted price using the PromotionService for each product
            var now = WebBanHang.Services.PromotionService.GetVietnamNow();

            int? sessionBranchId = HttpContext.Session.GetInt32("ActiveBranchId");
            if (sessionBranchId == null && int.TryParse(Request.Cookies["ActiveBranchId"], out int cookieBranchId))
                sessionBranchId = cookieBranchId;
            int activeBranchId = sessionBranchId ?? 0;

            var productIds = products.Select(p => p.Id).ToList();
            Dictionary<int, decimal> inventoryDict;
            if (activeBranchId > 0)
            {
                inventoryDict = await _inventoryBatchService.GetSellableQuantitiesAsync(productIds, activeBranchId);
            }
            else
            {
                inventoryDict = await _inventoryBatchService.GetSellableQuantitiesAcrossBranchesAsync(productIds);
            }

            var finalProducts = products.Select(p => {
                var originalProd = _context.Products.Local.FirstOrDefault(x => x.Id == p.Id) 
                                   ?? _context.Products.FirstOrDefault(x => x.Id == p.Id);
                decimal discPrice = originalProd != null 
                    ? WebBanHang.Services.PromotionService.GetDiscountedPrice(originalProd, now)
                    : p.Price;

                decimal stockQty = inventoryDict.TryGetValue(p.Id, out var qty) ? qty : 0m;

                return new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    DiscountedPrice = discPrice,
                    Amount = stockQty,
                    p.Image,
                    p.CategoryName,
                    p.IsVisible,
                    p.IsHot,
                    p.IsBestSeller,
                    p.Barcode
                };
            }).ToList();

            return Json(new { success = true, data = finalProducts });
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrderFromDelivery(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng của bạn." });
            }

            if (order.Status != 2)
            {
                return Json(new { success = false, message = "Đơn hàng phải ở trạng thái đang giao hàng mới có thể hủy." });
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hồ sơ khách hàng của bạn." });
            }

            // Lấy giới hạn hủy đơn hàng dựa theo cấp bậc thành viên
            int maxCancelLimit = LoyaltyService.GetMaxCancelLimit(profile.MembershipLevel);

            // Kiểm tra số đơn hàng đã hủy trong tháng hiện tại
            var now = WebBanHang.Services.PromotionService.GetVietnamNow();
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);
            var cancelledThisMonthCount = await _context.Orders.CountAsync(o => 
                o.UserId == userId && 
                o.Status == 5 && 
                o.OrderDate >= startOfMonth && 
                o.OrderDate < endOfMonth);

            if (cancelledThisMonthCount >= maxCancelLimit)
            {
                return Json(new { success = false, message = $"Bạn đã đạt giới hạn hủy tối đa {maxCancelLimit} đơn hàng trong tháng này." });
            }

            // Tiến hành hủy đơn (Status = 5) và hoàn trả số lượng vào kho
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

            return Json(new { success = true, message = "Hủy đơn hàng thành công! Số lượng sản phẩm đã được hoàn trả vào kho." });
        }

        private async Task SetDiscountViewBagAsync(Cart cart)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var subtotal = GetCartSubtotal(cart);
            var now = WebBanHang.Services.PromotionService.GetVietnamNow();
            var discounts = await _context.Discounts
                .AsNoTracking()
                .Where(d => string.IsNullOrEmpty(d.UserId) || d.UserId == userId)
                .ToListAsync();
            var appliedDiscount = await GetAppliedDiscountAsync(subtotal);

            // Membership tier discount
            var memberProfileSVB = await _context.CustomerProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);
            var memberLevelSVB = memberProfileSVB?.MembershipLevel ?? 0;
            var memberDiscountPctSVB = LoyaltyService.GetDiscountPercentage(memberLevelSVB);
            var memberDiscountAmountSVB = Math.Round(subtotal * memberDiscountPctSVB, 0);
            var voucherDiscountAmountSVB = appliedDiscount?.DiscountAmount ?? 0m;

            // Combo discounts
            var comboResult = await _aprioriService.GetComboDiscountsForCartAsync(cart.CartItems.ToList());
            var comboDiscountAmount = comboResult.TotalDiscount;

            var totalDiscountSVB = memberDiscountAmountSVB + voucherDiscountAmountSVB + comboDiscountAmount;
            var finalTotalSVB = Math.Max(0m, subtotal - totalDiscountSVB);

            ViewBag.SubtotalAmount = subtotal;
            ViewBag.MembershipDiscountPercent = memberDiscountPctSVB;
            ViewBag.MembershipDiscountAmount = memberDiscountAmountSVB;
            ViewBag.AppliedDiscountCode = string.Empty;
            ViewBag.VoucherDiscountAmount = 0m;
            ViewBag.ComboDiscountAmount = comboDiscountAmount;
            ViewBag.AppliedCombos = comboResult.AppliedCombos;
            ViewBag.DiscountAmount = memberDiscountAmountSVB + comboDiscountAmount;
            ViewBag.FinalTotal = finalTotalSVB;
            ViewBag.Discounts = discounts
                .OrderBy(d => GetDiscountSortOrder(d, now))
                .ThenByDescending(d => d.StartDate)
                .ThenBy(d => d.Code)
                .ToList();

            if (appliedDiscount == null)
            {
                return;
            }

            if (!appliedDiscount.Success)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                ViewBag.DiscountMessage = appliedDiscount.Message;
                return;
            }

            ViewBag.AppliedDiscountCode = appliedDiscount.Code;
            ViewBag.VoucherDiscountAmount = appliedDiscount.DiscountAmount;
            ViewBag.DiscountAmount = totalDiscountSVB;
            ViewBag.FinalTotal = finalTotalSVB;
            ViewBag.DiscountMessage = appliedDiscount.Message;
        }

        private async Task<DiscountCalculationResult?> GetAppliedDiscountAsync(decimal subtotal, bool trackDiscount = false)
        {
            var code = HttpContext.Session.GetString(AppliedDiscountCodeSessionKey);
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return await _discountService.CalculateDiscountAsync(code, subtotal, trackDiscount);
        }

        private static decimal GetCartSubtotal(Cart cart)
        {
            return cart.CartItems?.Sum(item => item.Quantity * item.Product.DiscountedPrice) ?? 0m;
        }

        private static int GetDiscountSortOrder(Discount discount, DateTime now)
        {
            if (IsDiscountActive(discount, now))
            {
                return 0;
            }

            if (discount.IsSee && discount.StartDate > now)
            {
                return 1;
            }

            return 2;
        }

        private static bool IsDiscountActive(Discount discount, DateTime now)
        {
            return discount.IsSee
                && discount.Quantity > 0
                && discount.StartDate <= now
                && discount.EndDate >= now;
        }

        private async Task<int> GetCartQuantityAsync(string userId)
        {
            var sum = await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => (decimal?)ci.Quantity) ?? 0m;
            return (int)Math.Ceiling(sum);
        }

        private async Task<string?> ValidateCartStockAsync(IEnumerable<CartItem> cartItems, int activeBranchId)
        {
            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    return "Có sản phẩm trong giỏ hàng không còn tồn tại.";
                }

                if (!item.Product.IsVisible)
                {
                    return $"Sản phẩm {item.Product.Name} hiện đang tạm ẩn, vui lòng bỏ khỏi giỏ hàng.";
                }
            }

            return await _inventoryBatchService.ValidateStockAsync(
                cartItems.Select(item => new InventoryStockItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ProductName = item.Product?.Name ?? "Sản phẩm",
                    Unit = item.Product?.Unit ?? string.Empty
                }),
                activeBranchId);
        }

        private async Task DeductStockAsync(IEnumerable<OrderDetail> orderDetails, int activeBranchId, int orderId)
        {
            await _inventoryBatchService.DeductStockAsync(
                orderDetails.Select(detail => new InventoryStockItem
                {
                    ProductId = detail.ProductId,
                    OrderDetailId = detail.Id,
                    Quantity = detail.Quantity,
                    ProductName = detail.Product?.Name ?? "Sản phẩm",
                    Unit = detail.Product?.Unit ?? string.Empty
                }),
                activeBranchId,
                orderId);
        }

        private static double ParseDistance(string? rawDistance)
        {
            if (string.IsNullOrWhiteSpace(rawDistance))
                return 0.0;

            if (double.TryParse(rawDistance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDist))
            {
                return parsedDist;
            }
            if (double.TryParse(rawDistance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var parsedDist2))
            {
                return parsedDist2;
            }
            var normalized = rawDistance.Replace(',', '.');
            if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDist3))
            {
                return parsedDist3;
            }
            return 0.0;
        }
    }
}
