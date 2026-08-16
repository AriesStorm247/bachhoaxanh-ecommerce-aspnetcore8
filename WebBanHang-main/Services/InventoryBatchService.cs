using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class InventoryStockItem
    {
        public int ProductId { get; set; }
        public int? OrderDetailId { get; set; }
        public decimal Quantity { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class InventoryBatchService
    {
        private readonly ApplicationDbContext _context;

        public InventoryBatchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetSellableQuantityAsync(int productId, int branchId)
        {
            var quantities = await GetSellableQuantitiesAsync(new[] { productId }, branchId);
            return quantities.TryGetValue(productId, out var quantity) ? quantity : 0m;
        }

        public async Task<Dictionary<int, decimal>> GetSellableQuantitiesAsync(IEnumerable<int> productIds, int branchId)
        {
            var ids = productIds.Distinct().ToList();
            var result = ids.ToDictionary(id => id, _ => 0m);
            if (!ids.Any())
            {
                return result;
            }

            var inventory = await _context.ProductInventories
                .AsNoTracking()
                .Where(i => i.BranchId == branchId && ids.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);

            var batches = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => b.BranchId == branchId && ids.Contains(b.ProductId))
                .ToListAsync();

            var today = PromotionService.GetVietnamNow().Date;
            var batchGroups = batches
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var id in ids)
            {
                if (batchGroups.TryGetValue(id, out var productBatches))
                {
                    result[id] = productBatches
                        .Where(b => b.Quantity > 0 && b.ExpiryDate.Date >= today)
                        .Sum(b => b.Quantity);
                }
                else if (inventory.TryGetValue(id, out var quantity))
                {
                    result[id] = quantity;
                }
            }

            return result;
        }

        public async Task<Dictionary<int, decimal>> GetSellableQuantitiesAcrossBranchesAsync(IEnumerable<int> productIds, IEnumerable<int>? branchIds = null)
        {
            var ids = productIds.Distinct().ToList();
            var branchIdList = branchIds?.Distinct().ToList();
            var result = ids.ToDictionary(id => id, _ => 0m);
            if (!ids.Any())
            {
                return result;
            }

            var inventoryQuery = _context.ProductInventories
                .AsNoTracking()
                .Where(i => ids.Contains(i.ProductId));
            var batchQuery = _context.InventoryBatches
                .AsNoTracking()
                .Where(b => ids.Contains(b.ProductId));

            if (branchIdList != null && branchIdList.Any())
            {
                inventoryQuery = inventoryQuery.Where(i => branchIdList.Contains(i.BranchId));
                batchQuery = batchQuery.Where(b => branchIdList.Contains(b.BranchId));
            }

            var inventories = await inventoryQuery.ToListAsync();
            var batches = await batchQuery.ToListAsync();
            var today = PromotionService.GetVietnamNow().Date;

            var batchGroups = batches
                .GroupBy(b => (b.ProductId, b.BranchId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var inventoryKeys = new HashSet<(int ProductId, int BranchId)>();
            foreach (var inventory in inventories)
            {
                var key = (inventory.ProductId, inventory.BranchId);
                inventoryKeys.Add(key);

                if (batchGroups.TryGetValue(key, out var branchBatches))
                {
                    result[inventory.ProductId] += branchBatches
                        .Where(b => b.Quantity > 0 && b.ExpiryDate.Date >= today)
                        .Sum(b => b.Quantity);
                }
                else
                {
                    result[inventory.ProductId] += inventory.Quantity;
                }
            }

            foreach (var group in batchGroups)
            {
                if (inventoryKeys.Contains(group.Key))
                {
                    continue;
                }

                result[group.Key.ProductId] += group.Value
                    .Where(b => b.Quantity > 0 && b.ExpiryDate.Date >= today)
                    .Sum(b => b.Quantity);
            }

            return result;
        }

        public async Task<string?> ValidateStockAsync(IEnumerable<InventoryStockItem> items, int branchId)
        {
            var stockItems = items.ToList();
            var productIds = stockItems.Select(i => i.ProductId).Distinct().ToList();

            var inventories = await _context.ProductInventories
                .AsNoTracking()
                .Where(i => i.BranchId == branchId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);

            var batches = await _context.InventoryBatches
                .AsNoTracking()
                .Where(b => b.BranchId == branchId && productIds.Contains(b.ProductId))
                .ToListAsync();

            var today = PromotionService.GetVietnamNow().Date;
            var batchGroups = batches
                .GroupBy(b => b.ProductId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in stockItems)
            {
                if (batchGroups.TryGetValue(item.ProductId, out var productBatches))
                {
                    var sellableQuantity = productBatches
                        .Where(b => b.Quantity > 0 && b.ExpiryDate.Date >= today)
                        .Sum(b => b.Quantity);
                    var expiredQuantity = productBatches
                        .Where(b => b.Quantity > 0 && b.ExpiryDate.Date < today)
                        .Sum(b => b.Quantity);

                    if (sellableQuantity <= 0 && expiredQuantity > 0)
                    {
                        return $"Sản phẩm {item.ProductName} chỉ còn lô đã hết hạn tại chi nhánh này, không thể bán.";
                    }

                    if (sellableQuantity <= 0)
                    {
                        return $"Sản phẩm {item.ProductName} đã hết hàng tại chi nhánh này.";
                    }

                    if (item.Quantity > sellableQuantity)
                    {
                        return $"Sản phẩm {item.ProductName} chỉ còn {FormatQuantity(sellableQuantity, item.Unit)} {item.Unit} ở các lô còn hạn.";
                    }

                    continue;
                }

                var stock = inventories.TryGetValue(item.ProductId, out var quantity) ? quantity : 0m;
                if (stock <= 0)
                {
                    return $"Sản phẩm {item.ProductName} đã hết hàng tại chi nhánh này.";
                }

                if (item.Quantity > stock)
                {
                    return $"Sản phẩm {item.ProductName} chỉ còn {FormatQuantity(stock, item.Unit)} {item.Unit} tại chi nhánh này.";
                }
            }

            return null;
        }

        public async Task DeductStockAsync(IEnumerable<InventoryStockItem> items, int branchId, int? orderId = null)
        {
            foreach (var item in items)
            {
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == branchId);

                if (inventory == null)
                {
                    inventory = new ProductInventory
                    {
                        ProductId = item.ProductId,
                        BranchId = branchId,
                        Quantity = 0m
                    };
                    _context.ProductInventories.Add(inventory);
                }

                inventory.Quantity -= item.Quantity;
                if (inventory.Quantity < 0)
                {
                    inventory.Quantity = 0m;
                }

                var today = PromotionService.GetVietnamNow().Date;
                var batches = await _context.InventoryBatches
                    .Where(b => b.ProductId == item.ProductId
                        && b.BranchId == branchId
                        && b.Quantity > 0
                        && b.ExpiryDate >= today)
                    .OrderBy(b => b.ExpiryDate)
                    .ThenBy(b => b.ImportDate)
                    .ThenBy(b => b.Id)
                    .ToListAsync();

                if (!batches.Any())
                {
                    continue;
                }

                var remaining = item.Quantity;
                foreach (var batch in batches)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    var deducted = Math.Min(batch.Quantity, remaining);
                    batch.Quantity -= deducted;
                    remaining -= deducted;

                    if (orderId.HasValue && deducted > 0)
                    {
                        _context.InventoryBatchDeductions.Add(new InventoryBatchDeduction
                        {
                            OrderId = orderId.Value,
                            OrderDetailId = item.OrderDetailId,
                            InventoryBatchId = batch.Id,
                            ProductId = item.ProductId,
                            BranchId = branchId,
                            Quantity = deducted,
                            CreatedAt = PromotionService.GetVietnamNow()
                        });
                    }
                }

                if (remaining > 0)
                {
                    throw new InvalidOperationException($"Không đủ lô còn hạn để trừ tồn cho sản phẩm {item.ProductName}.");
                }
            }
        }

        public async Task RestoreStockAsync(IEnumerable<InventoryStockItem> items, int branchId, int? orderId = null)
        {
            foreach (var item in items)
            {
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == branchId);

                if (inventory == null)
                {
                    inventory = new ProductInventory
                    {
                        ProductId = item.ProductId,
                        BranchId = branchId,
                        Quantity = 0m
                    };
                    _context.ProductInventories.Add(inventory);
                }

                inventory.Quantity += item.Quantity;
            }

            if (!orderId.HasValue)
            {
                return;
            }

            var deductions = await _context.InventoryBatchDeductions
                .Include(d => d.InventoryBatch)
                .Where(d => d.OrderId == orderId.Value && !d.IsRestored)
                .ToListAsync();

            foreach (var deduction in deductions)
            {
                if (deduction.InventoryBatch != null)
                {
                    deduction.InventoryBatch.Quantity += deduction.Quantity;
                }

                deduction.IsRestored = true;
            }
        }

        private static string FormatQuantity(decimal quantity, string unit)
        {
            return string.Equals(unit, "kg", StringComparison.OrdinalIgnoreCase)
                ? quantity.ToString("N2")
                : quantity.ToString("N0");
        }
    }
}
