using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class LeaderboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public LeaderboardService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // Lấy danh sách Top 10 khách hàng chi tiêu nhiều nhất (truy vấn trực tiếp để cập nhật thời gian thực)
        public async Task<List<LeaderboardRowViewModel>> GetTop10SpendingCustomersAsync(string periodType)
        {
            return await FetchTop10SpendingCustomersFromDbAsync(periodType);
        }

        // Lấy danh sách các khách hàng đứng sát nút Top 10 (Hạng 11 - 20) để gửi thông báo bám đuổi kích cầu
        public async Task<List<LeaderboardRowViewModel>> GetNearTop10CustomersAsync(string periodType)
        {
            var query = _context.Orders.AsNoTracking()
                .Where(o => o.Status == 3 && o.UserId != null && o.UserId != "guest-user");

            var now = PromotionService.GetVietnamNow();
            if (periodType == "month")
            {
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                query = query.Where(o => o.OrderDate >= startOfMonth && o.OrderDate < startOfMonth.AddMonths(1));
            }
            else if (periodType == "year")
            {
                var startOfYear = new DateTime(now.Year, 1, 1);
                query = query.Where(o => o.OrderDate >= startOfYear && o.OrderDate < startOfYear.AddYears(1));
            }

            var spendings = await query
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Skip(10)
                .Take(10)
                .ToListAsync();

            var userIds = spendings.Select(x => x.UserId).ToList();
            var profiles = await _context.CustomerProfiles.AsNoTracking()
                .Where(p => userIds.Contains(p.UserId))
                .Include(p => p.User)
                .ToDictionaryAsync(p => p.UserId);

            var result = new List<LeaderboardRowViewModel>();
            int rank = 11;
            foreach (var item in spendings)
            {
                profiles.TryGetValue(item.UserId, out var profile);
                var fullName = profile?.FullName;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = profile?.User?.Email?.Split('@')[0] ?? profile?.User?.UserName ?? "Khách hàng";
                }

                result.Add(new LeaderboardRowViewModel
                {
                    Rank = rank++,
                    UserId = item.UserId,
                    FullName = fullName,
                    AvatarUrl = profile?.AvatarUrl,
                    TotalSpent = item.TotalSpent,
                    MembershipLevel = profile?.MembershipLevel ?? 0,
                    MembershipTierName = LoyaltyService.GetTierName(profile?.MembershipLevel ?? 0),
                    EquippedAvatarFrame = profile?.EquippedAvatarFrame,
                    EquippedBadge = profile?.EquippedBadge
                });
            }

            return result;
        }

        // Lấy danh sách Top khách hàng (ví dụ Top 20) để điền vào dropdown chọn người nhận tin
        public async Task<List<LeaderboardRowViewModel>> GetTopSpendingCustomersListAsync(string periodType, int count)
        {
            var query = _context.Orders.AsNoTracking()
                .Where(o => o.Status == 3 && o.UserId != null && o.UserId != "guest-user");

            var now = PromotionService.GetVietnamNow();
            if (periodType == "month")
            {
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                query = query.Where(o => o.OrderDate >= startOfMonth && o.OrderDate < startOfMonth.AddMonths(1));
            }
            else if (periodType == "year")
            {
                var startOfYear = new DateTime(now.Year, 1, 1);
                query = query.Where(o => o.OrderDate >= startOfYear && o.OrderDate < startOfYear.AddYears(1));
            }

            var spendings = await query
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(count)
                .ToListAsync();

            var userIds = spendings.Select(x => x.UserId).ToList();
            var profiles = await _context.CustomerProfiles.AsNoTracking()
                .Where(p => userIds.Contains(p.UserId))
                .Include(p => p.User)
                .ToDictionaryAsync(p => p.UserId);

            var result = new List<LeaderboardRowViewModel>();
            int rank = 1;
            foreach (var item in spendings)
            {
                profiles.TryGetValue(item.UserId, out var profile);
                var fullName = profile?.FullName;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = profile?.User?.Email?.Split('@')[0] ?? profile?.User?.UserName ?? "Khách hàng";
                }

                // Lấy ra số điện thoại hoặc email để điền vào form
                var contactInfo = profile?.User?.PhoneNumber ?? profile?.User?.Email ?? "";

                result.Add(new LeaderboardRowViewModel
                {
                    Rank = rank++,
                    UserId = item.UserId,
                    FullName = fullName,
                    AvatarUrl = contactInfo, // Lưu thông tin liên hệ (SĐT/Email) vào AvatarUrl để view đọc được
                    TotalSpent = item.TotalSpent,
                    MembershipLevel = profile?.MembershipLevel ?? 0,
                    MembershipTierName = LoyaltyService.GetTierName(profile?.MembershipLevel ?? 0)
                });
            }

            return result;
        }

        // Truy vấn thực tế từ Database
        private async Task<List<LeaderboardRowViewModel>> FetchTop10SpendingCustomersFromDbAsync(string periodType)
        {
            var query = _context.Orders.AsNoTracking()
                .Where(o => o.Status == 3 && o.UserId != null && o.UserId != "guest-user");

            var now = PromotionService.GetVietnamNow();
            if (periodType == "month")
            {
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                query = query.Where(o => o.OrderDate >= startOfMonth && o.OrderDate < startOfMonth.AddMonths(1));
            }
            else if (periodType == "year")
            {
                var startOfYear = new DateTime(now.Year, 1, 1);
                query = query.Where(o => o.OrderDate >= startOfYear && o.OrderDate < startOfYear.AddYears(1));
            }

            var spendings = await query
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToListAsync();

            var userIds = spendings.Select(x => x.UserId).ToList();
            var profiles = await _context.CustomerProfiles.AsNoTracking()
                .Where(p => userIds.Contains(p.UserId))
                .Include(p => p.User)
                .ToDictionaryAsync(p => p.UserId);

            var result = new List<LeaderboardRowViewModel>();
            int rank = 1;
            foreach (var item in spendings)
            {
                profiles.TryGetValue(item.UserId, out var profile);
                var fullName = profile?.FullName;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = profile?.User?.Email?.Split('@')[0] ?? profile?.User?.UserName ?? "Khách hàng";
                }

                result.Add(new LeaderboardRowViewModel
                {
                    Rank = rank++,
                    UserId = item.UserId,
                    FullName = fullName,
                    AvatarUrl = profile?.AvatarUrl,
                    TotalSpent = item.TotalSpent,
                    MembershipLevel = profile?.MembershipLevel ?? 0,
                    MembershipTierName = LoyaltyService.GetTierName(profile?.MembershipLevel ?? 0),
                    EquippedAvatarFrame = profile?.EquippedAvatarFrame,
                    EquippedBadge = profile?.EquippedBadge
                });
            }

            return result;
        }

        // Lấy thứ hạng hiện tại và tổng chi tiêu của user đang đăng nhập
        public async Task<UserLeaderboardPositionViewModel> GetUserLeaderboardPositionAsync(string userId, string periodType)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new UserLeaderboardPositionViewModel { Rank = 0, TotalSpent = 0 };
            }

            var query = _context.Orders.AsNoTracking()
                .Where(o => o.Status == 3 && o.UserId != null && o.UserId != "guest-user");

            var now = PromotionService.GetVietnamNow();
            if (periodType == "month")
            {
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                query = query.Where(o => o.OrderDate >= startOfMonth && o.OrderDate < startOfMonth.AddMonths(1));
            }
            else if (periodType == "year")
            {
                var startOfYear = new DateTime(now.Year, 1, 1);
                query = query.Where(o => o.OrderDate >= startOfYear && o.OrderDate < startOfYear.AddYears(1));
            }

            var userSpendings = await query
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var targetUser = userSpendings.FirstOrDefault(x => x.UserId == userId);
            if (targetUser == null)
            {
                return new UserLeaderboardPositionViewModel { Rank = 0, TotalSpent = 0 };
            }

            int rank = userSpendings.Count(x => x.TotalSpent > targetUser.TotalSpent) + 1;
            return new UserLeaderboardPositionViewModel
            {
                Rank = rank,
                TotalSpent = targetUser.TotalSpent
            };
        }
    }

    public class LeaderboardRowViewModel
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public decimal TotalSpent { get; set; }
        public int MembershipLevel { get; set; }
        public string MembershipTierName { get; set; } = string.Empty;
        public string? EquippedAvatarFrame { get; set; }
        public string? EquippedBadge { get; set; }
    }

    public class UserLeaderboardPositionViewModel
    {
        public int Rank { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
