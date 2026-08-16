using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class InventoryBatchesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleService _roleService;

        public InventoryBatchesController(ApplicationDbContext context, RoleService roleService)
        {
            _context = context;
            _roleService = roleService;
        }

        private bool CanManageInventory()
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            return role == 1 || role == 2;
        }

        public async Task<IActionResult> Index(int? productId, int? branchId, string? status, int warningDays = 7, int page = 1, int pageSize = 10)
        {
            if (!CanManageInventory())
            {
                return RedirectToAction("Index", "Home");
            }

            if (warningDays <= 0)
            {
                warningDays = 7;
            }

            var today = PromotionService.GetVietnamNow().Date;
            var warningLimit = today.AddDays(warningDays);

            var query = _context.InventoryBatches
                .Include(b => b.Product)
                .ThenInclude(p => p.Category)
                .Include(b => b.Branch)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(b => b.ProductId == productId.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(b => b.BranchId == branchId.Value);
            }

            status = status?.Trim().ToLowerInvariant();
            query = status switch
            {
                "expired" => query.Where(b => b.Quantity > 0 && b.ExpiryDate < today),
                "expiring" => query.Where(b => b.Quantity > 0 && b.ExpiryDate >= today && b.ExpiryDate <= warningLimit),
                "empty" => query.Where(b => b.Quantity <= 0),
                "active" => query.Where(b => b.Quantity > 0 && b.ExpiryDate > warningLimit),
                _ => query
            };

            var batches = await query
                .OrderBy(b => b.ExpiryDate)
                .ThenBy(b => b.Product.Name)
                .ThenBy(b => b.Branch.Name)
                .ToListAsync();

            var allBatches = await _context.InventoryBatches.AsNoTracking().ToListAsync();

            ViewBag.Products = await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Branches = await _context.Branches
                .OrderBy(b => b.Province)
                .ThenBy(b => b.Name)
                .ToListAsync();
            ViewBag.SelectedProductId = productId;
            ViewBag.SelectedBranchId = branchId;
            ViewBag.SelectedStatus = status ?? "";
            ViewBag.WarningDays = warningDays;
            ViewBag.Today = today;
            ViewBag.TotalBatchCount = allBatches.Count;
            ViewBag.ActiveBatchCount = allBatches.Count(b => b.Quantity > 0 && b.ExpiryDate.Date > warningLimit);
            ViewBag.ExpiringBatchCount = allBatches.Count(b => b.Quantity > 0 && b.ExpiryDate.Date >= today && b.ExpiryDate.Date <= warningLimit);
            ViewBag.ExpiredBatchCount = allBatches.Count(b => b.Quantity > 0 && b.ExpiryDate.Date < today);

            int totalItems = batches.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var pagedBatches = batches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(pagedBatches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int branchId, string? batchCode, DateTime importDate, DateTime expiryDate, decimal quantity, string? supplierName, string? note)
        {
            if (!CanManageInventory())
            {
                return RedirectToAction("Index", "Home");
            }

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng lô phải lớn hơn 0.";
                return RedirectToAction(nameof(Index), new { productId, branchId });
            }

            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId);
            if (!productExists || !branchExists)
            {
                TempData["Error"] = "Sản phẩm hoặc chi nhánh không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (importDate == default)
            {
                importDate = PromotionService.GetVietnamNow().Date;
            }

            if (expiryDate == default)
            {
                TempData["Error"] = "Vui lòng nhập hạn sử dụng.";
                return RedirectToAction(nameof(Index), new { productId, branchId });
            }

            if (expiryDate.Date < importDate.Date)
            {
                TempData["Error"] = "Hạn sử dụng không được nhỏ hơn ngày nhập.";
                return RedirectToAction(nameof(Index), new { productId, branchId });
            }

            var normalizedBatchCode = string.IsNullOrWhiteSpace(batchCode)
                ? $"LO-{branchId}-{productId}-{PromotionService.GetVietnamNow():yyyyMMddHHmmss}"
                : batchCode.Trim();

            var duplicated = await _context.InventoryBatches.AnyAsync(b =>
                b.ProductId == productId &&
                b.BranchId == branchId &&
                b.BatchCode == normalizedBatchCode);

            if (duplicated)
            {
                TempData["Error"] = "Mã lô này đã tồn tại cho sản phẩm và chi nhánh đã chọn.";
                return RedirectToAction(nameof(Index), new { productId, branchId });
            }

            _context.InventoryBatches.Add(new InventoryBatch
            {
                ProductId = productId,
                BranchId = branchId,
                BatchCode = normalizedBatchCode,
                ImportDate = importDate.Date,
                ExpiryDate = expiryDate.Date,
                Quantity = quantity,
                OriginalQuantity = quantity,
                SupplierName = supplierName?.Trim(),
                Note = note?.Trim(),
                CreatedAt = PromotionService.GetVietnamNow()
            });

            await AddInventoryQuantityAsync(productId, branchId, quantity);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã nhập lô {normalizedBatchCode} với {quantity:N2} đơn vị.";
            return RedirectToAction(nameof(Index), new { productId, branchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, decimal quantity)
        {
            if (!CanManageInventory())
            {
                return RedirectToAction("Index", "Home");
            }

            if (quantity < 0)
            {
                TempData["Error"] = "Số lượng lô không được âm.";
                return RedirectToAction(nameof(Index));
            }

            var batch = await _context.InventoryBatches.FindAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            var delta = quantity - batch.Quantity;
            batch.Quantity = quantity;
            if (quantity > batch.OriginalQuantity)
            {
                batch.OriginalQuantity = quantity;
            }

            await AddInventoryQuantityAsync(batch.ProductId, batch.BranchId, delta);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật số lượng lô hàng.";
            return RedirectToAction(nameof(Index), new { productId = batch.ProductId, branchId = batch.BranchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!CanManageInventory())
            {
                return RedirectToAction("Index", "Home");
            }

            var batch = await _context.InventoryBatches.FindAsync(id);
            if (batch == null)
            {
                return NotFound();
            }

            var hasDeduction = await _context.InventoryBatchDeductions.AnyAsync(d => d.InventoryBatchId == id);
            if (hasDeduction)
            {
                TempData["Error"] = "Không thể xóa lô đã phát sinh bán hàng. Hãy chỉnh số lượng về 0 nếu cần ngừng bán.";
                return RedirectToAction(nameof(Index), new { productId = batch.ProductId, branchId = batch.BranchId });
            }

            await AddInventoryQuantityAsync(batch.ProductId, batch.BranchId, -batch.Quantity);
            _context.InventoryBatches.Remove(batch);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa lô hàng và đồng bộ lại tổng tồn kho.";
            return RedirectToAction(nameof(Index), new { productId = batch.ProductId, branchId = batch.BranchId });
        }

        private async Task AddInventoryQuantityAsync(int productId, int branchId, decimal delta)
        {
            if (delta == 0)
            {
                return;
            }

            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);

            if (inventory == null)
            {
                inventory = new ProductInventory
                {
                    ProductId = productId,
                    BranchId = branchId,
                    Quantity = 0m
                };
                _context.ProductInventories.Add(inventory);
            }

            inventory.Quantity += delta;
            if (inventory.Quantity < 0)
            {
                inventory.Quantity = 0m;
            }
        }
    }
}
