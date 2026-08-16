using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class LoyaltyService
    {
        private static readonly string[] TierNames =
        {
            "Cấp sắt",
            "Cấp đồng",
            "Cấp bạc",
            "Cấp vàng",
            "Cấp bạch kim",
            "Cấp lục bảo",
            "Cấp kim cương",
            "Cấp ruby",
            "Cấp niken",
            "Cấp titan",
            "Cấp uranium",
            "Cấp iridium"
        };

        private static readonly int[] UpgradeCosts =
        {
            100,       // Sắt → Đồng
            200,       // Đồng → Bạc
            500,       // Bạc → Vàng
            1500,      // Vàng → Bạch kim
            5000,      // Bạch kim → Lục bảo
            20000,     // Lục bảo → Kim cương
            80000,     // Kim cương → Ruby
            200000,    // Ruby → Niken
            500000,    // Niken → Titan
            1500000,   // Titan → Uranium
            5000000    // Uranium → Iridium
        };

        private readonly ApplicationDbContext _context;

        public LoyaltyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public static IReadOnlyList<string> MembershipTierNames => TierNames;

        public static string GetTierName(int level)
        {
            return TierNames[Math.Clamp(level, 0, TierNames.Length - 1)];
        }

        public static string? GetNextTierName(int level)
        {
            return level >= TierNames.Length - 1 ? null : TierNames[level + 1];
        }

        public static int? GetUpgradeCost(int level)
        {
            return level >= UpgradeCosts.Length ? null : UpgradeCosts[level];
        }

        public static decimal GetDiscountPercentage(int level)
        {
            return level switch
            {
                0  => 0.02m, // Sắt
                1  => 0.04m, // Đồng
                2  => 0.06m, // Bạc
                3  => 0.09m, // Vàng
                4  => 0.13m, // Bạch kim
                5  => 0.17m, // Lục bảo
                6  => 0.21m, // Kim cương
                7  => 0.26m, // Ruby
                8  => 0.30m, // Niken
                9  => 0.35m, // Titan
                10 => 0.38m, // Uranium
                11 => 0.40m, // Iridium
                _  => level > 11 ? 0.40m : 0m
            };
        }

        public static int GetMaxCancelLimit(int level)
        {
            return level switch
            {
                0  => 2,
                1  => 3,
                2  => 4,
                3  => 5,
                4  => 6,
                5  => 8,
                6  => 10,
                7  => 12,
                8  => 14,
                9  => 16,
                10 => 18,
                11 => 20,
                _  => level > 11 ? 20 : 2
            };
        }


        public static int CalculatePoints(decimal paidAmount)
        {
            return Math.Max(0, (int)Math.Floor(paidAmount / 200m));
        }

        public async Task<CustomerProfile> GetOrCreateProfileAsync(string userId)
        {
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null)
            {
                return profile;
            }

            profile = new CustomerProfile
            {
                UserId = userId,
                MembershipLevel = 0,
                LoyaltyPoints = 0,
                CreatedAt = PromotionService.GetVietnamNow(),
                UpdatedAt = PromotionService.GetVietnamNow()
            };

            _context.CustomerProfiles.Add(profile);
            return profile;
        }

        public async Task<int> AddPointsFromPaidOrderAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.UserId))
            {
                return 0;
            }

            var earnedPoints = CalculatePoints(order.TotalAmount + order.DiscountAmount);
            if (earnedPoints <= 0)
            {
                return 0;
            }

            var profile = await GetOrCreateProfileAsync(order.UserId);
            profile.LoyaltyPoints += earnedPoints;
            profile.UpdatedAt = PromotionService.GetVietnamNow();

            return earnedPoints;
        }

        public static bool CanUpgrade(CustomerProfile profile)
        {
            var cost = GetUpgradeCost(profile.MembershipLevel);
            return cost.HasValue && profile.LoyaltyPoints >= cost.Value;
        }

        public static bool TryUpgrade(CustomerProfile profile, out string message)
        {
            var cost = GetUpgradeCost(profile.MembershipLevel);
            if (!cost.HasValue)
            {
                message = "Bạn đã đạt cấp thẻ cao nhất.";
                return false;
            }

            if (profile.LoyaltyPoints < cost.Value)
            {
                message = $"Bạn cần {cost.Value:N0} điểm để lên {GetNextTierName(profile.MembershipLevel)}.";
                return false;
            }

            profile.LoyaltyPoints -= cost.Value;
            profile.MembershipLevel++;
            profile.DisplayMembershipLevel = profile.MembershipLevel;
            profile.UpdatedAt = PromotionService.GetVietnamNow();
            message = $"Đã nâng hạng lên {GetTierName(profile.MembershipLevel)}.";
            return true;
        }

        public async Task SyncObsoleteVouchersAsync(string userId)
        {
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return;

            if (profile.DisplayMembershipLevel.HasValue && profile.DisplayMembershipLevel.Value > profile.MembershipLevel)
            {
                profile.DisplayMembershipLevel = profile.MembershipLevel;
                _context.Entry(profile).State = EntityState.Modified;
            }

            var levelVouchers = await _context.CustomerVouchers
                .Where(v => v.UserId == userId)
                .ToListAsync();

            var obsoleteVouchers = new List<CustomerVoucher>();
            var obsoleteCodes = new List<string>();

            foreach (var cv in levelVouchers)
            {
                bool isObsolete = false;
                if (cv.Type == "Achievement" && cv.Key.StartsWith("REACH_"))
                {
                    if (int.TryParse(cv.Key.Substring(6), out int requiredLevel))
                    {
                        if (requiredLevel > profile.MembershipLevel)
                        {
                            isObsolete = true;
                        }
                    }
                }
                else if (cv.Type == "Store")
                {
                    var parts = cv.Key.Split('_');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int lvl))
                    {
                        if (lvl > profile.MembershipLevel)
                        {
                            isObsolete = true;
                        }
                    }
                }
                else if (cv.Type == "AvatarFrame")
                {
                    if (cv.Key.StartsWith("tier-"))
                    {
                        if (int.TryParse(cv.Key.Substring(5), out int lvl))
                        {
                            if (lvl > profile.MembershipLevel)
                            {
                                isObsolete = true;
                            }
                        }
                    }
                    else if (cv.Key.StartsWith("vip-moc-")) isObsolete = true;
                }
                else if (cv.Type == "Vip")
                {
                    isObsolete = true;
                }
                else if (cv.Type == "Badge")
                {
                    if (cv.Key.StartsWith("vip-badge-")) isObsolete = true;
                }

                if (isObsolete)
                {
                    obsoleteVouchers.Add(cv);
                    if (!string.IsNullOrEmpty(cv.VoucherCode))
                    {
                        obsoleteCodes.Add(cv.VoucherCode);
                    }
                }
            }

            if (obsoleteVouchers.Any())
            {
                _context.CustomerVouchers.RemoveRange(obsoleteVouchers);

                var codesToDelete = obsoleteCodes.Where(c => c != "UNLOCKED").ToList();
                if (codesToDelete.Any())
                {
                    var discountsToDelete = await _context.Discounts
                        .Where(d => codesToDelete.Contains(d.Code))
                        .ToListAsync();

                    _context.Discounts.RemoveRange(discountsToDelete);
                }
            }

            bool profileChanged = false;
            if (profile.EquippedAvatarFrame != null && profile.EquippedAvatarFrame != "none")
            {
                bool isValidFrame = false;
                if (profile.EquippedAvatarFrame.StartsWith("tier-"))
                {
                    if (int.TryParse(profile.EquippedAvatarFrame.Substring(5), out int lvl))
                    {
                        isValidFrame = lvl <= profile.MembershipLevel;
                    }
                }
                else if (profile.EquippedAvatarFrame.StartsWith("daily-normal-") || profile.EquippedAvatarFrame.StartsWith("daily-limited-"))
                {
                    isValidFrame = levelVouchers.Any(v => v.Type == "AvatarFrame" && v.Key == profile.EquippedAvatarFrame);
                }

                if (!isValidFrame)
                {
                    profile.EquippedAvatarFrame = "none";
                    profileChanged = true;
                }
            }

            if (profile.EquippedBadge != null && profile.EquippedBadge != "none")
            {
                bool isValidBadge = false;

                if (!isValidBadge)
                {
                    profile.EquippedBadge = "none";
                    profileChanged = true;
                }
            }

            if (profileChanged)
            {
                _context.Entry(profile).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task SyncAllObsoleteVouchersAsync()
        {
            var profiles = await _context.CustomerProfiles.ToListAsync();
            var profileMap = profiles.ToDictionary(p => p.UserId);
            var userIds = profiles.Select(p => p.UserId).ToList();

            var levelVouchers = await _context.CustomerVouchers
                .Where(v => userIds.Contains(v.UserId))
                .ToListAsync();

            var obsoleteVouchers = new List<CustomerVoucher>();
            var obsoleteCodes = new List<string>();

            foreach (var cv in levelVouchers)
            {
                if (profileMap.TryGetValue(cv.UserId, out var profile))
                {
                    bool isObsolete = false;
                    if (cv.Type == "Achievement" && cv.Key.StartsWith("REACH_"))
                    {
                        if (int.TryParse(cv.Key.Substring(6), out int requiredLevel))
                        {
                            if (requiredLevel > profile.MembershipLevel)
                            {
                                isObsolete = true;
                            }
                        }
                    }
                    else if (cv.Type == "Store")
                    {
                        var parts = cv.Key.Split('_');
                        if (parts.Length > 1 && int.TryParse(parts[1], out int lvl))
                        {
                            if (lvl > profile.MembershipLevel)
                            {
                                isObsolete = true;
                            }
                        }
                    }
                    else if (cv.Type == "AvatarFrame")
                    {
                        if (cv.Key.StartsWith("tier-"))
                        {
                            if (int.TryParse(cv.Key.Substring(5), out int lvl))
                            {
                                if (lvl > profile.MembershipLevel)
                                {
                                    isObsolete = true;
                                }
                            }
                        }
                        else if (cv.Key.StartsWith("vip-moc-")) isObsolete = true;
                    }
                    else if (cv.Type == "Vip")
                    {
                        isObsolete = true;
                    }
                    else if (cv.Type == "Badge")
                    {
                        if (cv.Key.StartsWith("vip-badge-")) isObsolete = true;
                    }

                    if (isObsolete)
                    {
                        obsoleteVouchers.Add(cv);
                        if (!string.IsNullOrEmpty(cv.VoucherCode))
                        {
                            obsoleteCodes.Add(cv.VoucherCode);
                        }
                    }
                }
            }

            foreach (var profile in profiles)
            {
                bool profileChanged = false;
                if (profile.DisplayMembershipLevel.HasValue && profile.DisplayMembershipLevel.Value > profile.MembershipLevel)
                {
                    profile.DisplayMembershipLevel = profile.MembershipLevel;
                    profileChanged = true;
                }

                if (profile.EquippedAvatarFrame != null && profile.EquippedAvatarFrame != "none")
                {
                    bool isValidFrame = false;
                    if (profile.EquippedAvatarFrame.StartsWith("tier-"))
                    {
                        if (int.TryParse(profile.EquippedAvatarFrame.Substring(5), out int lvl))
                        {
                            isValidFrame = lvl <= profile.MembershipLevel;
                        }
                    }
                    else if (profile.EquippedAvatarFrame.StartsWith("daily-normal-") || profile.EquippedAvatarFrame.StartsWith("daily-limited-"))
                    {
                        isValidFrame = levelVouchers.Any(v => v.UserId == profile.UserId && v.Type == "AvatarFrame" && v.Key == profile.EquippedAvatarFrame);
                    }

                    if (!isValidFrame)
                    {
                        profile.EquippedAvatarFrame = "none";
                        profileChanged = true;
                    }
                }

                if (profile.EquippedBadge != null && profile.EquippedBadge != "none")
                {
                    bool isValidBadge = false;

                    if (!isValidBadge)
                    {
                        profile.EquippedBadge = "none";
                        profileChanged = true;
                    }
                }

                if (profileChanged)
                {
                    _context.Entry(profile).State = EntityState.Modified;
                }
            }

            if (obsoleteVouchers.Any())
            {
                _context.CustomerVouchers.RemoveRange(obsoleteVouchers);

                var codesToDelete = obsoleteCodes.Where(c => c != "UNLOCKED").ToList();
                if (codesToDelete.Any())
                {
                    var discountsToDelete = await _context.Discounts
                        .Where(d => codesToDelete.Contains(d.Code))
                        .ToListAsync();

                    _context.Discounts.RemoveRange(discountsToDelete);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
