using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _roleService;

        public ProductsController(ApplicationDbContext context, RoleService roleService)
        {
            _context = context;
            _roleService = roleService;
        }

        private bool CanManageProducts()
        {
            int role = _roleService.GetRole(User);
            ViewBag.Role = role;
            return role == 1 || role == 2;
        }

        // GET: Products  (branchId=0 or null = Tong the)
        public async Task<IActionResult> Index(int? branchId, string? province, string? ward, int page = 1, int pageSize = 10)
        {
            if (province == "Huế") province = "Thừa Thiên Huế";
            if (!CanManageProducts())
                return RedirectToAction("Index", "Home");

            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();

            int selectedBranchId = branchId ?? 0;

            if (selectedBranchId > 0)
            {
                var inventories = await _context.ProductInventories
                    .Where(i => i.BranchId == selectedBranchId)
                    .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);
                foreach (var p in products)
                    p.Amount = inventories.TryGetValue(p.Id, out var qty) ? qty : 0;

                var branch = await _context.Branches.FindAsync(selectedBranchId);
                ViewBag.SelectedBranchName     = branch?.Name ?? "Chi nhánh";
                ViewBag.SelectedBranchProvince = branch?.Province ?? "";
                ViewBag.SelectedBranchDistrict = branch?.District ?? "";
            }
            else if (!string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(ward))
            {
                var branchIds = await _context.Branches
                    .Where(b => b.Province == province && b.District == ward)
                    .Select(b => b.Id)
                    .ToListAsync();

                var totals = await _context.ProductInventories
                    .Where(i => branchIds.Contains(i.BranchId))
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Total = g.Sum(i => i.Quantity) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Total);
                foreach (var p in products)
                    p.Amount = totals.TryGetValue(p.Id, out var total) ? total : 0;

                ViewBag.SelectedBranchName     = $"Chi nhánh {province} - {ward}";
                ViewBag.SelectedBranchProvince = province;
                ViewBag.SelectedBranchDistrict = ward;
            }
            else if (!string.IsNullOrEmpty(province))
            {
                var branchIds = await _context.Branches
                    .Where(b => b.Province == province)
                    .Select(b => b.Id)
                    .ToListAsync();

                var totals = await _context.ProductInventories
                    .Where(i => branchIds.Contains(i.BranchId))
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Total = g.Sum(i => i.Quantity) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Total);
                foreach (var p in products)
                    p.Amount = totals.TryGetValue(p.Id, out var total) ? total : 0;

                ViewBag.SelectedBranchName     = $"Chi nhánh {province}";
                ViewBag.SelectedBranchProvince = province;
                ViewBag.SelectedBranchDistrict = "";
            }
            else
            {
                var totals = await _context.ProductInventories
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Total = g.Sum(i => i.Quantity) })
                    .ToDictionaryAsync(x => x.ProductId, x => x.Total);
                foreach (var p in products)
                    p.Amount = totals.TryGetValue(p.Id, out var total) ? total : 0;

                ViewBag.SelectedBranchName = "Tất cả chi nhánh";
                ViewBag.SelectedBranchProvince = "";
                ViewBag.SelectedBranchDistrict = "";
            }

            ViewBag.Provinces = await _context.Branches
                .Select(b => b.Province).Distinct().OrderBy(p => p).ToListAsync();

            ViewBag.SelectedBranchId = selectedBranchId;

            // Thống kê tổng quan trên toàn bộ danh sách (trước phân trang)
            ViewBag.TotalProducts = products.Count;
            ViewBag.OutOfStock = products.Count(p => p.Amount == 0);
            ViewBag.LowStock = products.Count(p => p.Amount > 0 && p.Amount <= 10);
            ViewBag.MaxPrice = products.Any() ? products.Max(p => p.Price) : 0m;

            int totalItems = products.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedProducts);
        }

        // AJAX GET: Danh sach Phuong/Xa cua 1 tinh
        [HttpGet]
        public async Task<IActionResult> GetWards(string province)
        {
            if (!CanManageProducts()) return Forbid();
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

        // AJAX GET: Danh sach chi nhanh cua 1 tinh + phuong
        [HttpGet]
        public async Task<IActionResult> GetBranches(string province, string ward)
        {
            if (!CanManageProducts()) return Forbid();
            if (province == "Huế") province = "Thừa Thiên Huế";
            if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(ward))
                return Json(new List<object>());
            var branches = await _context.Branches
                .Where(b => b.Province == province && b.District == ward)
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name, b.Address })
                .ToListAsync();
            return Json(branches);
        }

        // AJAX POST: Cap nhat ton kho cho 1 san pham tai 1 chi nhanh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInventory(int productId, int branchId, decimal quantity)
        {
            if (!CanManageProducts())
                return Json(new { success = false, message = "Không có quyền" });
            if (quantity < 0)
                return Json(new { success = false, message = "Số lượng không được âm" });

            var inv = await _context.ProductInventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);
            if (inv == null)
            {
                _context.ProductInventories.Add(new ProductInventory
                    { ProductId = productId, BranchId = branchId, Quantity = quantity });
            }
            else
            {
                inv.Quantity = quantity;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true, newQuantity = quantity });
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id, int? branchId, string? province, string? ward)
        {
            if (province == "Huế") province = "Thừa Thiên Huế";
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();
            var product = await _context.Products.Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            int selectedBranchId = branchId ?? 0;
            if (selectedBranchId > 0)
            {
                var inv = await _context.ProductInventories
                    .FirstOrDefaultAsync(i => i.ProductId == id && i.BranchId == selectedBranchId);
                product.Amount = inv?.Quantity ?? 0;
            }
            else if (!string.IsNullOrEmpty(province) && !string.IsNullOrEmpty(ward))
            {
                var branchIds = await _context.Branches
                    .Where(b => b.Province == province && b.District == ward)
                    .Select(b => b.Id)
                    .ToListAsync();
                product.Amount = await _context.ProductInventories
                    .Where(i => i.ProductId == id && branchIds.Contains(i.BranchId))
                    .SumAsync(i => i.Quantity);
            }
            else if (!string.IsNullOrEmpty(province))
            {
                var branchIds = await _context.Branches
                    .Where(b => b.Province == province)
                    .Select(b => b.Id)
                    .ToListAsync();
                product.Amount = await _context.ProductInventories
                    .Where(i => i.ProductId == id && branchIds.Contains(i.BranchId))
                    .SumAsync(i => i.Quantity);
            }
            else
            {
                product.Amount = await _context.ProductInventories
                    .Where(i => i.ProductId == id)
                    .SumAsync(i => i.Quantity);
            }

            ViewBag.ReturnBranchId = selectedBranchId;
            ViewBag.ReturnProvince = province ?? "";
            ViewBag.ReturnWard = ward ?? "";
            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View(new Product { IsVisible = true });
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Amount,Image,CategoryId,IsVisible,Barcode,Unit,IsSoldByWeight")] Product product)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(product.Barcode))
                {
                    string baseStr = $"893{product.Id:D9}";
                    int sumOdd = 0, sumEven = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        int digit = baseStr[i] - '0';
                        if (i % 2 == 0) sumOdd += digit; else sumEven += digit;
                    }
                    int total = sumOdd + sumEven * 3;
                    int checkd = (10 - (total % 10)) % 10;
                    product.Barcode = baseStr + checkd.ToString();
                    await _context.SaveChangesAsync();
                }

                // Tu dong tao ban ghi ton kho = 0 cho tat ca chi nhanh
                var allBranchIds = await _context.Branches.Select(b => b.Id).ToListAsync();
                if (allBranchIds.Any())
                {
                    const int bsz = 1000;
                    for (int i = 0; i < allBranchIds.Count; i += bsz)
                    {
                        var vals = string.Join(",", allBranchIds.Skip(i).Take(bsz)
                            .Select(bid => $"({product.Id}, {bid}, 0)"));
                        await _context.Database.ExecuteSqlRawAsync(
                            $"INSERT INTO ProductInventories (ProductId, BranchId, Quantity) VALUES {vals}");
                    }
                }
                TempData["Success"] = $"Đã thêm sản phẩm và đồng bộ tồn kho cho {allBranchIds.Count} chi nhánh.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id, int? branchId)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            int selectedBranchId = branchId ?? 0;
            if (selectedBranchId > 0)
            {
                TempData["Error"] = "Chỉ có thể chỉnh sửa thông tin sản phẩm ở chế độ xem Tổng thể.";
                return RedirectToAction(nameof(Index), new { branchId = selectedBranchId });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Amount = await _context.ProductInventories
                .Where(i => i.ProductId == id)
                .SumAsync(i => i.Quantity);

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.ReturnBranchId = selectedBranchId;
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? returnBranchId,
            [Bind("Id,Name,Price,Amount,Image,CategoryId,IsVisible,Barcode,Unit,IsSoldByWeight")] Product product)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            if (id != product.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var cur = await _context.Products.FindAsync(id);
                    if (cur == null) return NotFound();
                    cur.Name = product.Name;
                    cur.Price = product.Price;
                    cur.Amount = product.Amount;
                    cur.Image = product.Image;
                    cur.CategoryId = product.CategoryId;
                    cur.IsVisible = product.IsVisible;
                    cur.Barcode = product.Barcode;
                    cur.Unit = product.Unit;
                    cur.IsSoldByWeight = product.IsSoldByWeight;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã cập nhật thông tin sản phẩm (đồng bộ tất cả chi nhánh).";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index), new { branchId = returnBranchId ?? 0 });
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.ReturnBranchId = returnBranchId ?? 0;
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();
            var product = await _context.Products.Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                var invs = _context.ProductInventories.Where(i => i.ProductId == id);
                _context.ProductInventories.RemoveRange(invs);
                _context.Products.Remove(product);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sản phẩm và tất cả dữ liệu tồn kho liên quan.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id, int? branchId)
        {
            if (!CanManageProducts()) return RedirectToAction("Index", "Home");
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            product.IsVisible = !product.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = product.IsVisible
                ? "Đã cho phép hiển thị sản phẩm trên trang chủ."
                : "Đã ẩn sản phẩm khỏi trang chủ.";
            return RedirectToAction(nameof(Index), new { branchId = branchId ?? 0 });
        }

        private bool ProductExists(int id) =>
            _context.Products.Any(e => e.Id == id);
    }
}
