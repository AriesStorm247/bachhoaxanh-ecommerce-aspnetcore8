using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class AprioriResultItem
    {
        public int ProductId1 { get; set; }
        public int ProductId2 { get; set; }
        public string ProductName1 { get; set; } = string.Empty;
        public string ProductName2 { get; set; } = string.Empty;
        public double Support { get; set; }
        public double Confidence1To2 { get; set; } // Confidence Product1 -> Product2
        public double Confidence2To1 { get; set; } // Confidence Product2 -> Product1
        public int CountTogether { get; set; }
    }

    public class CartComboResult
    {
        public decimal TotalDiscount { get; set; } = 0m;
        public List<AppliedComboInfo> AppliedCombos { get; set; } = new();
    }

    public class AppliedComboInfo
    {
        public int ComboId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class AprioriService
    {
        private readonly ApplicationDbContext _context;

        public AprioriService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Chạy thuật toán Apriori trên lịch sử đơn hàng (không tính đơn đã hủy) để tìm các cặp sản phẩm thường mua cùng nhau
        /// </summary>
        public async Task<List<AprioriResultItem>> RunAprioriAsync(double minSupport = 0.01, double minConfidence = 0.20)
        {
            // Lấy tất cả các đơn hàng thành công (Status != 2: Đã hủy)
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.Status != 2 && o.OrderDetails.Any())
                .ToListAsync();

            int totalTransactions = orders.Count;
            if (totalTransactions == 0)
            {
                return new List<AprioriResultItem>();
            }

            // Trích xuất các danh sách sản phẩm theo từng đơn hàng (Giao dịch)
            var transactions = orders.Select(o => o.OrderDetails.Select(od => od.ProductId).Distinct().ToList()).ToList();

            // Bước 1: Đếm số lần xuất hiện của từng sản phẩm riêng lẻ (L1)
            var itemCounts = new Dictionary<int, int>();
            foreach (var transaction in transactions)
            {
                foreach (var itemId in transaction)
                {
                    if (!itemCounts.ContainsKey(itemId))
                    {
                        itemCounts[itemId] = 0;
                    }
                    itemCounts[itemId]++;
                }
            }

            // Lọc ra các sản phẩm vượt qua ngưỡng Support tối thiểu
            var frequent1 = itemCounts
                .Where(kvp => (double)kvp.Value / totalTransactions >= minSupport)
                .Select(kvp => kvp.Key)
                .ToHashSet();

            // Bước 2: Tạo các cặp sản phẩm từ tập sản phẩm phổ biến L1 và đếm tần suất đi kèm nhau (C2)
            var pairCounts = new Dictionary<(int, int), int>();
            foreach (var transaction in transactions)
            {
                var filteredItems = transaction.Where(id => frequent1.Contains(id)).ToList();
                if (filteredItems.Count < 2) continue;

                for (int i = 0; i < filteredItems.Count; i++)
                {
                    for (int j = i + 1; j < filteredItems.Count; j++)
                    {
                        int id1 = filteredItems[i];
                        int id2 = filteredItems[j];
                        var pair = id1 < id2 ? (id1, id2) : (id2, id1);

                        if (!pairCounts.ContainsKey(pair))
                        {
                            pairCounts[pair] = 0;
                        }
                        pairCounts[pair]++;
                    }
                }
            }

            // Bước 3: Tính toán Support, Confidence và sinh luật kết hợp
            var results = new List<AprioriResultItem>();
            var allProducts = await _context.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

            foreach (var kvp in pairCounts)
            {
                var pair = kvp.Key;
                int countTogether = kvp.Value;
                double support = (double)countTogether / totalTransactions;

                if (support >= minSupport)
                {
                    int id1 = pair.Item1;
                    int id2 = pair.Item2;

                    int count1 = itemCounts[id1];
                    int count2 = itemCounts[id2];

                    double confidence1To2 = (double)countTogether / count1;
                    double confidence2To1 = (double)countTogether / count2;

                    // Chỉ giữ lại nếu ít nhất 1 chiều đạt độ tin cậy tối thiểu
                    if (confidence1To2 >= minConfidence || confidence2To1 >= minConfidence)
                    {
                        results.Add(new AprioriResultItem
                        {
                            ProductId1 = id1,
                            ProductId2 = id2,
                            ProductName1 = allProducts.TryGetValue(id1, out var name1) ? name1 : $"Sản phẩm #{id1}",
                            ProductName2 = allProducts.TryGetValue(id2, out var name2) ? name2 : $"Sản phẩm #{id2}",
                            Support = Math.Round(support, 4),
                            Confidence1To2 = Math.Round(confidence1To2, 4),
                            Confidence2To1 = Math.Round(confidence2To1, 4),
                            CountTogether = countTogether
                        });
                    }
                }
            }

            return results
                .OrderByDescending(r => r.Support)
                .ThenByDescending(r => Math.Max(r.Confidence1To2, r.Confidence2To1))
                .ToList();
        }

        /// <summary>
        /// Tính toán tổng tiền giảm giá combo cho giỏ hàng hiện tại
        /// </summary>
        public async Task<CartComboResult> GetComboDiscountsForCartAsync(List<CartItem> cartItems)
        {
            var result = new CartComboResult();
            if (cartItems == null || !cartItems.Any())
            {
                return result;
            }

            var now = PromotionService.GetVietnamNow();
            var activeCombos = await _context.ComboPromotions
                .Include(cp => cp.Product1)
                .Include(cp => cp.Product2)
                .Where(cp => cp.IsActive && (cp.ExpiryDate == null || cp.ExpiryDate >= now))
                .ToListAsync();

            if (!activeCombos.Any())
            {
                return result;
            }

            var cartQuantities = cartItems.ToDictionary(ci => ci.ProductId, ci => ci.Quantity);

            // Sắp xếp combo theo số tiền giảm lớn nhất để tối ưu hóa lợi ích khách hàng
            var comboDetails = activeCombos.Select(c => {
                var p1Price = c.Product1?.DiscountedPrice ?? 0m;
                var p2Price = c.Product2?.DiscountedPrice ?? 0m;
                var discountPerCombo = (p1Price + p2Price) * c.DiscountPercent;
                return new { Combo = c, DiscountVal = discountPerCombo };
            }).OrderByDescending(x => x.DiscountVal).ToList();

            foreach (var detail in comboDetails)
            {
                var combo = detail.Combo;
                if (!cartQuantities.ContainsKey(combo.ProductId1) || !cartQuantities.ContainsKey(combo.ProductId2))
                {
                    continue;
                }

                decimal qty1 = cartQuantities[combo.ProductId1];
                decimal qty2 = cartQuantities[combo.ProductId2];

                if (qty1 <= 0 || qty2 <= 0)
                {
                    continue;
                }

                decimal numCombosDecimal = Math.Min(qty1, qty2);
                int numCombos = (int)Math.Floor(numCombosDecimal);

                if (numCombos > 0)
                {
                    var p1Price = combo.Product1?.DiscountedPrice ?? 0m;
                    var p2Price = combo.Product2?.DiscountedPrice ?? 0m;
                    decimal discountAmount = numCombos * (p1Price + p2Price) * combo.DiscountPercent;

                    result.TotalDiscount += discountAmount;
                    result.AppliedCombos.Add(new AppliedComboInfo
                    {
                        ComboId = combo.Id,
                        Name = combo.Name,
                        Quantity = numCombos,
                        DiscountAmount = Math.Round(discountAmount, 0)
                    });

                    // Khấu trừ số lượng sản phẩm đã sử dụng trong combo
                    cartQuantities[combo.ProductId1] -= numCombos;
                    cartQuantities[combo.ProductId2] -= numCombos;
                }
            }

            return result;
        }
    }
}
