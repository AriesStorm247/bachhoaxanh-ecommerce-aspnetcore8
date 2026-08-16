using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Services;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class LeaderboardController : Controller
    {
        private readonly LeaderboardService _leaderboardService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleService _roleService;
        private readonly NotificationService _notificationService;

        public LeaderboardController(
            LeaderboardService leaderboardService,
            UserManager<IdentityUser> userManager,
            RoleService roleService,
            NotificationService notificationService)
        {
            _leaderboardService = leaderboardService;
            _userManager = userManager;
            _roleService = roleService;
            _notificationService = notificationService;
        }

        // GET: /Leaderboard
        [HttpGet]
        public async Task<IActionResult> Index(string period = "month")
        {
            // Chuẩn hóa bộ lọc thời gian
            period = period.ToLower().Trim();
            if (period != "month" && period != "year" && period != "alltime")
            {
                period = "month";
            }

            var top10 = await _leaderboardService.GetTop10SpendingCustomersAsync(period);
            
            ViewBag.Period = period;
            ViewBag.Role = _roleService.GetRole(User);

            // Thông tin cá nhân của người dùng hiện tại
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var userPosition = await _leaderboardService.GetUserLeaderboardPositionAsync(userId, period);
                    ViewBag.UserRank = userPosition.Rank;
                    ViewBag.UserTotalSpent = userPosition.TotalSpent;
                    ViewBag.CurrentUserId = userId;

                    // Lấy ra chi tiêu tối thiểu của Hạng 10 để tính khoảng cách
                    decimal tenthSpent = 0m;
                    if (top10.Count >= 10)
                    {
                        tenthSpent = top10[9].TotalSpent;
                    }
                    else if (top10.Count > 0)
                    {
                        tenthSpent = top10[top10.Count - 1].TotalSpent;
                    }
                    ViewBag.TenthSpent = tenthSpent;
                }
            }

            return View(top10);
        }

        // GET: /Leaderboard/AdminIndex (Dành cho Quản trị viên)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AdminIndex(string period = "month")
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            if (role != 1 && role != 2) // Phải là Admin hoặc Manager
            {
                return RedirectToAction("Index", "Home");
            }

            period = period.ToLower().Trim();
            if (period != "month" && period != "year" && period != "alltime")
            {
                period = "month";
            }

            var top10 = await _leaderboardService.GetTop10SpendingCustomersAsync(period);
            ViewBag.Period = period;

            return View(top10);
        }

        // POST: /Leaderboard/SendChaseNotifications
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendChaseNotifications(string period = "month")
        {
            var role = _roleService.GetRole(User);
            if (role != 1 && role != 2)
            {
                return Unauthorized();
            }

            period = period.ToLower().Trim();
            if (period != "month" && period != "year" && period != "alltime")
            {
                period = "month";
            }

            // 1. Lấy Top 10 hiện tại để xác định chi tiêu của Hạng 10
            var top10 = await _leaderboardService.GetTop10SpendingCustomersAsync(period);
            if (!top10.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu Top 10 để tính toán bám đuổi.";
                return RedirectToAction(nameof(AdminIndex), new { period = period });
            }

            decimal tenthSpent = top10.Count >= 10 ? top10[9].TotalSpent : top10[top10.Count - 1].TotalSpent;

            // 2. Lấy danh sách Hạng 11 - 20
            var nearTop10 = await _leaderboardService.GetNearTop10CustomersAsync(period);
            if (!nearTop10.Any())
            {
                TempData["ErrorMessage"] = "Hiện tại không có khách hàng nào ở vị trí bám đuổi (Hạng 11 - 20) để gửi thông báo.";
                return RedirectToAction(nameof(AdminIndex), new { period = period });
            }

            string periodText = period == "month" ? "Tháng này" : (period == "year" ? "Năm này" : "Tất cả thời gian");
            int count = 0;

            foreach (var user in nearTop10)
            {
                decimal diff = tenthSpent - user.TotalSpent;
                if (diff <= 0) diff = 1000; // Đề phòng trường hợp bằng nhau hoặc vượt nhưng chưa đồng bộ

                string title = $"Cơ hội nhận quà VIP Đua Top {periodText} đang rất gần!";
                string content = $@"
                    <div class='chase-notification' style='font-family: sans-serif; line-height: 1.5;'>
                        <h3 style='color: #d35400;'><i class='bi bi-fire me-2'></i>CƠ HỘI LỌT VÀO TOP 10 ĐANG RẤT GẦN!</h3>
                        <p>Chào bạn <strong>{user.FullName}</strong>,</p>
                        <p>Siêu thị xin thông báo bạn đang xếp hạng ở vị trí <strong>Hạng #{user.Rank}</strong> trên Bảng xếp hạng chi tiêu của chu kỳ <strong>{periodText}</strong>.</p>
    
                        <div style='background-color: #fff8f2; border-left: 4px solid #d35400; padding: 12px; margin: 15px 0; border-radius: 4px;'>
                            <strong>Thống kê khoảng cách bám đuổi của bạn:</strong>
                            <ul style='margin: 8px 0 0 0; padding-left: 20px;'>
                                <li>Doanh số chi tiêu hiện tại của bạn: <strong style='color: #27ae60;'>{user.TotalSpent:N0} đ</strong></li>
                                <li>Doanh số chi tiêu của Hạng #10: <strong>{tenthSpent:N0} đ</strong></li>
                                <li>Khoảng cách cần vượt qua chỉ là: <strong style='color: #e74c3c; font-size: 16px;'>{diff:N0} đ</strong>!</li>
                            </ul>
                        </div>

                        <p>Hãy mua sắm thêm một vài nhu yếu phẩm hoặc thực phẩm tươi ngon hôm nay để nâng cao vị thế, bứt phá lọt vào Top 10 và sở hữu ngay các phần quà VIP cực giá trị từ Bách Hóa Xanh:</p>
                        <ul style='padding-left: 20px;'>
                            <li><strong>Voucher giảm giá 15%</strong> (giảm tối đa 500,000 đ) cho hóa đơn kế tiếp.</li>
                            <li>Huy hiệu thành viên độc quyền <strong>Giàu Nứt Vách</strong> và Khung Avatar tinh tế.</li>
                            <li>Tặng trực tiếp <strong>10,000 Loyalty Points</strong> vào tài khoản thành viên.</li>
                        </ul>

                        <p style='font-style: italic; color: #7f8c8d; font-size: 12px;'>Mẹo mua sắm: Bạn có thể tham khảo danh mục Khuyến mãi hoặc liên hệ tổng đài để mua hàng giao nhanh 2 giờ nhận ngay doanh thu chi tiêu tích lũy!</p>
                    </div>";

                await _notificationService.SendPersonalNotificationAsync(user.UserId, title, content, NotificationType.Promotion);
                count++;
            }

            TempData["SuccessMessage"] = $"Đã gửi thông báo kích cầu thành công cho {count} khách hàng đứng sát nút Top 10 (Hạng 11 - 20).";
            return RedirectToAction(nameof(AdminIndex), new { period = period });
        }
    }
}
