using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 1. Số lượng loại mặt hàng (SKU) theo danh mục
            var productCounts = await _context.Products
                .Where(p => p.CategoryId != null)
                .GroupBy(p => p.CategoryId!.Value)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            // 2. Tổng số lượng tồn kho (tất cả chi nhánh) theo danh mục
            var stockTotals = await (from pi in _context.ProductInventories
                                     join p in _context.Products on pi.ProductId equals p.Id
                                     where p.CategoryId != null
                                     group pi by p.CategoryId!.Value into g
                                     select new { CategoryId = g.Key, TotalStock = g.Sum(x => x.Quantity) })
                                    .ToDictionaryAsync(x => x.CategoryId, x => (long)Math.Round(x.TotalStock));

            // 3. Tổng mặt hàng & Tổng tồn kho toàn hệ thống
            int totalProducts = await _context.Products.CountAsync();
            long totalStock = await _context.ProductInventories.SumAsync(i => (long)Math.Round(i.Quantity));
            int uncategorizedCount = await _context.Products.CountAsync(p => p.CategoryId == null);

            ViewBag.ProductCounts = productCounts;
            ViewBag.StockTotals = stockTotals;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalStock = totalStock;
            ViewBag.UncategorizedCount = uncategorizedCount;

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            var categoryName = name?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var existed = await _context.Categories.AnyAsync(c => c.Name == categoryName);
            if (existed)
            {
                TempData["Error"] = "Danh mục này đã tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Add(new Category { Name = categoryName });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm danh mục mới.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var categoryName = name?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            var existed = await _context.Categories.AnyAsync(c => c.Id != id && c.Name == categoryName);
            if (existed)
            {
                TempData["Error"] = "Tên danh mục đã được dùng.";
                return RedirectToAction(nameof(Index));
            }

            category.Name = categoryName;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .ToListAsync();

            foreach (var product in products)
            {
                product.CategoryId = null;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa danh mục. Các sản phẩm thuộc danh mục này được chuyển về chưa phân loại.";
            return RedirectToAction(nameof(Index));
        }
    }
}
