using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ComboPromotionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AprioriService _aprioriService;

        public ComboPromotionController(ApplicationDbContext context, AprioriService aprioriService)
        {
            _context = context;
            _aprioriService = aprioriService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var combos = await _context.ComboPromotions
                .Include(c => c.Product1)
                .Include(c => c.Product2)
                .ToListAsync();

            int totalItems = combos.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var pagedCombos = combos.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedCombos);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var combo = await _context.ComboPromotions.FindAsync(id);
            if (combo != null)
            {
                combo.IsActive = !combo.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscount(int id, decimal discountPercent)
        {
            var combo = await _context.ComboPromotions.FindAsync(id);
            if (combo != null)
            {
                if (discountPercent < 0) discountPercent = 0;
                if (discountPercent > 100) discountPercent = 100;
                // Nếu người dùng nhập phần trăm như 10, chuyển thành 0.10m
                combo.DiscountPercent = discountPercent > 1 ? discountPercent / 100m : discountPercent;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateExpiryDate(int id, DateTime? expiryDate)
        {
            var combo = await _context.ComboPromotions.FindAsync(id);
            if (combo != null)
            {
                combo.ExpiryDate = expiryDate;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.ComboPromotions.FindAsync(id);
            if (combo != null)
            {
                _context.ComboPromotions.Remove(combo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Analyze(double minSupport = 0.02, double minConfidence = 0.20)
        {
            ViewBag.MinSupport = minSupport;
            ViewBag.MinConfidence = minConfidence;

            var recommendations = await _aprioriService.RunAprioriAsync(minSupport, minConfidence);
            
            // Lọc ra các khuyến nghị đã tồn tại dưới dạng combo để đánh dấu trên giao diện
            var existingComboKeys = await _context.ComboPromotions
                .Select(c => c.ProductId1 < c.ProductId2 ? $"{c.ProductId1}_{c.ProductId2}" : $"{c.ProductId2}_{c.ProductId1}")
                .ToListAsync();

            ViewBag.ExistingComboKeys = existingComboKeys;
            return View(recommendations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCombo(int productId1, int productId2, decimal discountPercent, string name, double support, double confidence)
        {
            // Tránh tạo trùng lặp
            var key1 = productId1 < productId2 ? productId1 : productId2;
            var key2 = productId1 < productId2 ? productId2 : productId1;

            var exists = await _context.ComboPromotions.AnyAsync(c => 
                (c.ProductId1 == key1 && c.ProductId2 == key2) || 
                (c.ProductId1 == key2 && c.ProductId2 == key1));

            if (!exists)
            {
                var combo = new ComboPromotion
                {
                    ProductId1 = key1,
                    ProductId2 = key2,
                    DiscountPercent = discountPercent > 1 ? discountPercent / 100m : discountPercent,
                    Name = string.IsNullOrWhiteSpace(name) ? "Combo Khuyến Mãi" : name.Trim(),
                    Support = support,
                    Confidence = confidence,
                    IsActive = true
                };

                _context.ComboPromotions.Add(combo);
                await _context.SaveChangesAsync();
                TempData["ComboMessage"] = $"Đã tạo thành công combo: {combo.Name}";
            }
            else
            {
                TempData["ComboError"] = "Combo này đã tồn tại trong hệ thống.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
