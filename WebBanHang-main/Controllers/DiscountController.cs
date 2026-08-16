using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class DiscountController : Controller
    {
        private const string AppliedDiscountCodeSessionKey = "AppliedDiscountCode";

        private readonly ApplicationDbContext _context;
        private readonly IDiscountService _discountService;
        private readonly LoyaltyService _loyaltyService;

        public DiscountController(ApplicationDbContext context, IDiscountService discountService, LoyaltyService loyaltyService)
        {
            _context = context;
            _discountService = discountService;
            _loyaltyService = loyaltyService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            await _loyaltyService.SyncAllObsoleteVouchersAsync();

            var discounts = await _context.Discounts
                .OrderByDescending(d => d.StartDate)
                .ThenBy(d => d.Code)
                .ToListAsync();

            int totalItems = discounts.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var pagedDiscounts = discounts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedDiscounts);
        }

        public IActionResult Create()
        {
            return View(new Discount
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                Quantity = 1,
                IsSee = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,DiscountValue,MinOrderValue,MaxDiscount,StartDate,EndDate,Quantity,IsSee")] Discount discount)
        {
            NormalizeDiscount(discount);
            await ValidateDiscountAsync(discount);

            if (!ModelState.IsValid)
            {
                return View(discount);
            }

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm mã giảm giá mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            return View(discount);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound();
            }

            return View(discount);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountValue,MinOrderValue,MaxDiscount,StartDate,EndDate,Quantity,IsSee")] Discount discount)
        {
            if (id != discount.Id)
            {
                return NotFound();
            }

            NormalizeDiscount(discount);
            await ValidateDiscountAsync(discount, discount.Id);

            if (!ModelState.IsValid)
            {
                return View(discount);
            }

            var currentDiscount = await _context.Discounts.FindAsync(id);
            if (currentDiscount == null)
            {
                return NotFound();
            }

            try
            {
                currentDiscount.Code = discount.Code;
                currentDiscount.DiscountValue = discount.DiscountValue;
                currentDiscount.MinOrderValue = discount.MinOrderValue;
                currentDiscount.MaxDiscount = discount.MaxDiscount;
                currentDiscount.StartDate = discount.StartDate;
                currentDiscount.EndDate = discount.EndDate;
                currentDiscount.Quantity = discount.Quantity;
                currentDiscount.IsSee = discount.IsSee;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật mã giảm giá.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DiscountExists(discount.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var discount = await _context.Discounts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discount == null)
            {
                return NotFound();
            }

            return View(discount);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa mã giảm giá.";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GetOrCreateCartUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            var guestId = HttpContext.Session.GetString("GuestCartId");
            if (string.IsNullOrEmpty(guestId))
            {
                guestId = "guest_" + Guid.NewGuid().ToString("N");
                HttpContext.Session.SetString("GuestCartId", guestId);
            }
            return guestId;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(string code)
        {
            var userId = GetOrCreateCartUserId();
            var isAuthenticated = User.Identity?.IsAuthenticated == true;

            if (isAuthenticated)
            {
                await _loyaltyService.SyncObsoleteVouchersAsync(userId);
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            var discountCodeClean = code.Trim();
            var discount = await _context.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.Code == discountCodeClean);
            if (discount == null)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            }

            if (!isAuthenticated && !string.IsNullOrEmpty(discount.UserId))
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                return Json(new { success = false, message = "Mã giảm giá này là mã riêng thuộc tài khoản cá nhân. Vui lòng đăng nhập để sử dụng!" });
            }

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .ThenInclude(p => p.Category)
                .Where(ci => ci.Cart.UserId == userId)
                .ToListAsync();
            var subtotal = cartItems.Sum(item => item.Quantity * item.Product.DiscountedPrice);

            if (subtotal <= 0)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                return Json(new { success = false, message = "Giỏ hàng của bạn đang trống!" });
            }

            var result = await _discountService.CalculateDiscountAsync(discountCodeClean, subtotal);
            if (!result.Success)
            {
                HttpContext.Session.Remove(AppliedDiscountCodeSessionKey);
                return Json(new { success = false, message = result.Message });
            }

            HttpContext.Session.SetString(AppliedDiscountCodeSessionKey, result.Code ?? discountCodeClean);

            var memberProfile = isAuthenticated
                ? await _context.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId)
                : null;
            var memberLevel = memberProfile?.MembershipLevel ?? 0;
            var memberDiscountPct = LoyaltyService.GetDiscountPercentage(memberLevel);
            var memberDiscountAmount = Math.Round(subtotal * memberDiscountPct, 0);
            var combinedFinalTotal = Math.Max(0m, result.FinalTotal - memberDiscountAmount);

            return Json(new
            {
                success = true,
                code = result.Code,
                subtotal = result.Subtotal,
                discountAmount = result.DiscountAmount,
                memberDiscountAmount = memberDiscountAmount,
                finalTotal = combinedFinalTotal,
                message = result.Message
            });
        }

        private async Task ValidateDiscountAsync(Discount discount, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(discount.Code))
            {
                ModelState.AddModelError(nameof(discount.Code), "Vui lòng nhập mã giảm giá.");
            }
            else
            {
                var existed = await _context.Discounts.AnyAsync(d =>
                    (!currentId.HasValue || d.Id != currentId.Value)
                    && d.Code.ToUpper() == discount.Code.ToUpper());

                if (existed)
                {
                    ModelState.AddModelError(nameof(discount.Code), "Mã giảm giá này đã tồn tại.");
                }
            }

            if (discount.DiscountValue <= 0)
            {
                ModelState.AddModelError(nameof(discount.DiscountValue), "Phần trăm giảm phải lớn hơn 0.");
            }
            else if (discount.DiscountValue > 100)
            {
                ModelState.AddModelError(nameof(discount.DiscountValue), "Phần trăm giảm không được vượt quá 100%.");
            }

            if (discount.MinOrderValue < 0)
            {
                ModelState.AddModelError(nameof(discount.MinOrderValue), "Giá trị đơn tối thiểu không được âm.");
            }

            if (discount.MaxDiscount < 0)
            {
                ModelState.AddModelError(nameof(discount.MaxDiscount), "Giảm tối đa không được âm.");
            }

            if (discount.Quantity < 0)
            {
                ModelState.AddModelError(nameof(discount.Quantity), "Số lượt dùng không được âm.");
            }

            if (discount.EndDate <= discount.StartDate)
            {
                ModelState.AddModelError(nameof(discount.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            }
        }

        private static void NormalizeDiscount(Discount discount)
        {
            discount.Code = (discount.Code ?? string.Empty).Trim().ToUpperInvariant();
            discount.DiscountValue = DiscountPercent.Normalize(discount.DiscountValue);
        }

        private bool DiscountExists(int id)
        {
            return _context.Discounts.Any(e => e.Id == id);
        }
    }
}
