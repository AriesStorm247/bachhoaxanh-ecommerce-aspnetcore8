using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class LeaderboardSettlementJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeaderboardSettlementJob> _logger;

        public LeaderboardSettlementJob(IServiceScopeFactory scopeFactory, ILogger<LeaderboardSettlementJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Leaderboard Settlement Background Job is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = PromotionService.GetVietnamNow();
                    
                    // Kiểm tra nếu là ngày 01 của tháng và giờ nằm trong khoảng 04:00 - 04:59 sáng
                    if (now.Day == 1 && now.Hour == 4)
                    {
                        // Tính toán năm và tháng của chu kỳ trước đó (tháng vừa qua)
                        var prevMonthDate = now.AddMonths(-1);
                        int year = prevMonthDate.Year;
                        int month = prevMonthDate.Month;

                        _logger.LogInformation($"Checking settlement status for {month}/{year}.");

                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                            string settlementKey = $"LEADERBOARD_SETTLEMENT_{year}_{month:D2}";
                            
                            // Kiểm tra xem đã kết toán tháng này chưa
                            bool alreadySettled = await db.CustomerVouchers
                                .AnyAsync(v => v.Key == settlementKey, cancellationToken: stoppingToken);

                            if (!alreadySettled)
                            {
                                _logger.LogInformation($"Starting settlement process for period {month:D2}/{year}.");
                                await ProcessSettlementAsync(db, notificationService, year, month, settlementKey);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during leaderboard settlement check.");
                }

                // Chờ 15 phút trước khi kiểm tra lại
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task ProcessSettlementAsync(ApplicationDbContext db, NotificationService notificationService, int year, int month, string settlementKey)
        {
            using (var transaction = await db.Database.BeginTransactionAsync())
            {
                try
                {
                    // 0. Reset tất cả các khung avatar và huy hiệu Đua Top cũ của tất cả khách hàng (hết hạn lúc 03:59:59 ngày 1 hàng tháng)
                    var oldTopFrameUsers = await db.CustomerProfiles
                        .Where(p => p.EquippedAvatarFrame == "khung-top-1.webp" 
                                 || p.EquippedAvatarFrame == "khung-top-2-3.webp" 
                                 || p.EquippedAvatarFrame == "khung-top-4-10.webp")
                        .ToListAsync();

                    foreach (var p in oldTopFrameUsers)
                    {
                        p.EquippedAvatarFrame = "none";
                    }

                    var oldTopBadgeUsers = await db.CustomerProfiles
                        .Where(p => p.EquippedBadge == "thich-thi-nap.png" 
                                 || p.EquippedBadge == "vung-tien-nhu-nuoc.png" 
                                 || p.EquippedBadge == "giau-nut-vach.png")
                        .ToListAsync();

                    foreach (var p in oldTopBadgeUsers)
                    {
                        p.EquippedBadge = "none";
                    }

                    // Xóa quyền sở hữu khung và huy hiệu Đua Top cũ của chu kỳ trước trong ví vật phẩm
                    var oldLeaderboardVouchers = await db.CustomerVouchers
                        .Where(v => (v.Type == "AvatarFrame" && (v.Key == "khung-top-1.webp" || v.Key == "khung-top-2-3.webp" || v.Key == "khung-top-4-10.webp"))
                                 || (v.Type == "Badge" && (v.Key == "thich-thi-nap.png" || v.Key == "vung-tien-nhu-nuoc.png" || v.Key == "giau-nut-vach.png")))
                        .ToListAsync();

                    if (oldLeaderboardVouchers.Any())
                    {
                        db.CustomerVouchers.RemoveRange(oldLeaderboardVouchers);
                    }

                    // 1. Xác định khoảng thời gian của tháng cần kết toán
                    var startOfMonth = new DateTime(year, month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1);

                    // 2. Lấy dữ liệu chi tiêu thực tế của các đơn hàng thành công (Status = 3)
                    var spendings = await db.Orders.AsNoTracking()
                        .Where(o => o.Status == 3 && o.UserId != null && o.UserId != "guest-user" 
                                    && o.OrderDate >= startOfMonth && o.OrderDate < endOfMonth)
                        .GroupBy(o => o.UserId)
                        .Select(g => new
                        {
                            UserId = g.Key,
                            TotalSpent = g.Sum(o => o.TotalAmount)
                        })
                        .OrderByDescending(x => x.TotalSpent)
                        .Take(10)
                        .ToListAsync();

                    _logger.LogInformation($"Found {spendings.Count} customers to award for period {month:D2}/{year}.");

                    int rank = 1;
                    foreach (var item in spendings)
                    {
                        var profile = await db.CustomerProfiles
                            .FirstOrDefaultAsync(p => p.UserId == item.UserId);

                        if (profile == null)
                        {
                            profile = new CustomerProfile
                            {
                                UserId = item.UserId,
                                LoyaltyPoints = 0,
                                MembershipLevel = 0,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };
                            db.CustomerProfiles.Add(profile);
                        }

                        // Định nghĩa quà tặng cho từng hạng
                        int pointsReward = 0;
                        int voucherPercent = 0;
                        decimal maxDiscount = 0m;
                        string framePath = "";
                        string badgePath = "";
                        string giftName = "";
                        string rankName = "";

                        if (rank == 1)
                        {
                            pointsReward = 50000;
                            voucherPercent = 50;
                            maxDiscount = 2000000m;
                            framePath = "khung-top-1.webp";
                            badgePath = "thich-thi-nap.png";
                            giftName = "Giỏ quà Tết/Sự kiện đặc biệt trị giá 1.500.000 đ (thiết kế cao cấp độc quyền)";
                            rankName = "Quán Quân";
                        }
                        else if (rank <= 3)
                        {
                            pointsReward = 25000;
                            voucherPercent = 30;
                            maxDiscount = 1000000m;
                            framePath = "khung-top-2-3.webp";
                            badgePath = "vung-tien-nhu-nuoc.png";
                            giftName = "Hộp quà nhu yếu phẩm trị giá 1.000.000 đ";
                            rankName = rank == 2 ? "Á Quân" : "Quý Quân";
                        }
                        else
                        {
                            pointsReward = 10000;
                            voucherPercent = 15;
                            maxDiscount = 500000m;
                            framePath = "khung-top-4-10.webp";
                            badgePath = "giau-nut-vach.png";
                            giftName = "Túi canvas thời trang của siêu thị kèm sổ tay thương hiệu";
                            rankName = "Chiến Thần Tiêu Dùng";
                        }

                        // A. Cộng điểm loyalty points
                        profile.LoyaltyPoints += pointsReward;
                        profile.UpdatedAt = DateTime.Now;
                        db.Entry(profile).State = EntityState.Modified;

                        // B. Tự động trang bị khung avatar và huy hiệu mới
                        profile.EquippedAvatarFrame = framePath;
                        profile.EquippedBadge = badgePath;

                        // C. Tạo voucher giảm giá kỹ thuật số
                        string voucherCode = $"TOP{rank}_T{month:D2}{year}_{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
                        var discount = new Discount
                        {
                            Code = voucherCode,
                            DiscountValue = voucherPercent,
                            IsSee = true,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(30),
                            Quantity = 1,
                            MinOrderValue = 0,
                            MaxDiscount = maxDiscount,
                            UserId = item.UserId
                        };
                        db.Discounts.Add(discount);

                        // D. Thêm bản ghi CustomerVoucher đánh dấu sở hữu voucher và lưu mốc kết toán
                        var cv = new CustomerVoucher
                        {
                            UserId = item.UserId,
                            Type = "LeaderboardVoucher",
                            Key = settlementKey, // Marker lưu trữ lịch sử
                            VoucherCode = voucherCode,
                            DiscountValue = voucherPercent,
                            UnlockedAt = DateTime.Now
                        };
                        db.CustomerVouchers.Add(cv);

                        // Thêm quyền sở hữu vật phẩm mỹ thuật để có thể đổi sau này
                        db.CustomerVouchers.Add(new CustomerVoucher
                        {
                            UserId = item.UserId,
                            Type = "AvatarFrame",
                            Key = framePath,
                            VoucherCode = "UNLOCKED",
                            DiscountValue = 0,
                            UnlockedAt = DateTime.Now
                        });

                        db.CustomerVouchers.Add(new CustomerVoucher
                        {
                            UserId = item.UserId,
                            Type = "Badge",
                            Key = badgePath,
                            VoucherCode = "UNLOCKED",
                            DiscountValue = 0,
                            UnlockedAt = DateTime.Now
                        });

                        // E. Gửi thông báo hộp thư cá nhân
                        string title = $"Chúc mừng bạn đạt {rankName} tiêu dùng tháng {month:D2}/{year}!";
                        string content = $@"
                            <div class='reward-notification'>
                                <h3 style='color: #1a7a2e;'>BÁCH HÓA XANH XIN CHÚC MỪNG CHIẾN THẦN TIÊU DÙNG!</h3>
                                <p>Chào bạn,</p>
                                <p>Siêu thị trân trọng ghi nhận và chúc mừng bạn đã xuất sắc đạt thứ hạng <strong>Hạng {rank} ({rankName})</strong> trong bảng đua top chi tiêu tháng {month:D2}/{year} vừa qua trên toàn hệ thống siêu thị.</p>
    
                                <div style='background-color: #f3f4f6; border-left: 4px solid #ffd600; padding: 12px; margin: 15px 0;'>
                                    <strong>Phần quà đặc quyền dành cho bạn đã được chuyển vào ví tài khoản:</strong>
                                    <ul style='margin-top: 5px; padding-left: 20px;'>
                                        <li><strong>Loyalty Points:</strong> +{pointsReward:N0} điểm tích lũy (Cộng trực tiếp vào ví thành viên).</li>
                                        <li><strong>Voucher mua sắm:</strong> Giảm {voucherPercent}% (tối đa {maxDiscount:N0} đ) cho đơn hàng kế tiếp.</li>
                                        <li><strong>Mã Voucher:</strong> <span style='font-family: monospace; font-size: 16px; font-weight: bold; color: #ff6b00;'>{voucherCode}</span></li>
                                        <li><strong>Danh hiệu:</strong> Đã trang bị Khung hình đại diện và Huy hiệu độc quyền trên Hồ sơ cá nhân.</li>
                                    </ul>
                                </div>
                                <div style='background-color: #e8f7ec; padding: 12px; border-radius: 6px; margin: 15px 0;'>
                                    <strong>Quà tặng hiện vật tại quầy:</strong><br/>
                                    Bạn được tặng thêm: <em>{giftName}</em>.<br/>
                                    Vui lòng mang thông báo này đến quầy dịch vụ tại cửa hàng Bách Hóa Xanh gần nhất để nhân viên siêu thị xác thực thông tin tài khoản và trao quà hiện vật trực tiếp cho bạn trước ngày kết thúc tháng.
                                </div>

                                <p style='font-style: italic; color: #666;'>Cảm ơn bạn đã luôn tin tưởng và đồng hành mua sắm cùng hệ thống siêu thị Bách Hóa Xanh!</p>
                            </div>";

                        await notificationService.SendPersonalNotificationAsync(item.UserId, title, content, NotificationType.Reward);

                        rank++;
                    }

                    // Lưu các thay đổi
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"Successfully completed settlement for period {month:D2}/{year}.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Failed to process settlement transaction for period {month:D2}/{year}.");
                }
            }
        }
    }
}
