using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

using Microsoft.AspNetCore.Identity.UI.Services;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly RoleService _roleService;
        private readonly LeaderboardService _leaderboardService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public NotificationController(
            UserManager<IdentityUser> userManager,
            NotificationService notificationService,
            RoleService roleService,
            LeaderboardService leaderboardService,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _roleService = roleService;
            _leaderboardService = leaderboardService;
            _context = context;
            _emailSender = emailSender;
        }

        // Xem toàn bộ hộp thư thông báo
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var notifications = await _notificationService.GetNotificationsAsync(userId);
            ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId);
            ViewBag.Role = _roleService.GetRole(User);

            return View(notifications);
        }

        // API đánh dấu đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, userId);
            return Json(new { success = true });
        }

        // API đánh dấu đọc tất cả
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }

        // API lấy số lượng chưa đọc động
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new { count = 0 });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });

            int count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count = count });
        }

        // API lấy nhanh danh sách 5 thông báo mới nhất cho Popover
        [HttpGet]
        public async Task<IActionResult> GetQuickNotifications()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var list = await _notificationService.GetNotificationsAsync(userId);
            var quickList = list.Take(5).Select(n => new
            {
                id = n.Id,
                title = n.Title,
                isRead = n.IsRead,
                type = n.Type.ToString(),
                createdAt = n.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return Json(quickList);
        }

        // GET: Admin soạn thông báo
        [HttpGet]
        public async Task<IActionResult> Send()
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            if (role != 1 && role != 2) // Admin hoặc Manager mới được gửi
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách Top 20 khách hàng chi tiêu nhiều nhất tháng này để điền dropdown chọn nhanh
            var topCustomers = await _leaderboardService.GetTopSpendingCustomersListAsync("month", 20);
            ViewBag.TopCustomers = topCustomers;

            return View();
        }

        // POST: Admin gửi thông báo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string title, string content, NotificationType type, string? phoneOrEmail)
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            if (role != 1 && role != 2)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Tiêu đề và nội dung không được để trống.";
                return View();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(phoneOrEmail))
                {
                    // Gửi toàn bộ hệ thống
                    await _notificationService.SendGlobalNotificationAsync(title, content, type);
                    TempData["SuccessMessage"] = "Đã phát thông báo chung toàn bộ siêu thị thành công!";
                }
                else
                {
                    // Tách danh sách nếu có dấu phẩy (gửi hàng loạt)
                    var targetList = phoneOrEmail.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    if (targetList.Count == 0)
                    {
                        TempData["ErrorMessage"] = "Danh sách người nhận không hợp lệ.";
                        return View();
                    }

                    int successCount = 0;

                    foreach (var cleanTarget in targetList)
                    {
                        var targetUser = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.PhoneNumber == cleanTarget || u.Email == cleanTarget);

                        bool sentInApp = false;

                        if (targetUser != null)
                        {
                            try
                            {
                                // Lấy thông tin họ tên từ hồ sơ để cá nhân hóa
                                string fullName = "Khách hàng";
                                int userRank = 0;
                                if (_context != null)
                                {
                                    var profile = await _context.CustomerProfiles.AsNoTracking()
                                        .FirstOrDefaultAsync(p => p.UserId == targetUser.Id);
                                    if (profile != null && !string.IsNullOrWhiteSpace(profile.FullName))
                                    {
                                        fullName = profile.FullName.Trim();
                                    }
                                }

                                if (fullName == "Khách hàng")
                                {
                                    fullName = targetUser.Email?.Split('@')[0] ?? targetUser.UserName ?? "Khách hàng";
                                }

                                // Lấy thứ hạng đua top tháng này của người dùng
                                var pos = await _leaderboardService.GetUserLeaderboardPositionAsync(targetUser.Id, "month");
                                userRank = pos.Rank;

                                // Xác định nội dung phần thưởng tương ứng thứ hạng của khách hàng
                                string voucherText = "Voucher tri ân khách hàng thân thiết.";
                                string giftText = "Bộ ly sứ cao cấp vẽ vàng Bách Hóa Xanh";

                                if (type == NotificationType.Reward)
                                {
                                    int voucherPercent = 30;
                                    decimal maxDiscount = 1000000;

                                    if (userRank == 1)
                                    {
                                        voucherPercent = 50;
                                        maxDiscount = 2000000;
                                        giftText = "Nồi chiên không dầu Philips HD9252";
                                    }
                                    else if (userRank == 2 || userRank == 3)
                                    {
                                        voucherPercent = 30;
                                        maxDiscount = 1000000;
                                        giftText = "Bộ nồi inox cao cấp 3 đáy Sunhouse";
                                    }
                                    else if (userRank >= 4 && userRank <= 10)
                                    {
                                        voucherPercent = 15;
                                        maxDiscount = 500000;
                                        giftText = "Giỏ quà Tết sum vầy Bách Hóa Xanh";
                                    }

                                    // Tạo mã voucher độc quyền duy nhất cho khách hàng
                                    string voucherCode = $"REWARD_T{DateTime.Now.Month:D2}{DateTime.Now.Year}_{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
                                    if (userRank >= 1 && userRank <= 10)
                                    {
                                        voucherCode = $"TOP{userRank}_T{DateTime.Now.Month:D2}{DateTime.Now.Year}_{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";
                                    }

                                    // Thêm bản ghi Discount vào CSDL
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
                                        UserId = targetUser.Id
                                    };
                                    _context.Discounts.Add(discount);

                                    // Thêm quyền sở hữu CustomerVoucher vào ví của User
                                    var cv = new CustomerVoucher
                                    {
                                        UserId = targetUser.Id,
                                        Type = "Discount",
                                        Key = "RewardVoucher",
                                        VoucherCode = voucherCode,
                                        DiscountValue = voucherPercent,
                                        UnlockedAt = DateTime.Now
                                    };
                                    _context.CustomerVouchers.Add(cv);

                                    await _context.SaveChangesAsync();

                                    voucherText = $"Voucher giảm {voucherPercent}% (Tối đa giảm {maxDiscount:N0}đ) cho hóa đơn kế tiếp. Mã của bạn: <strong style='color:#e74c3c;'>{voucherCode}</strong>";
                                }

                                // Thực hiện cá nhân hóa phần Chào bạn trong thư
                                string personalizedContent = content;
                                if (personalizedContent.Contains("<p>Chào bạn,</p>"))
                                {
                                    personalizedContent = personalizedContent.Replace("<p>Chào bạn,</p>", $"<p>Chào bạn <strong>{fullName}</strong>,</p>");
                                }
                                else
                                {
                                    personalizedContent = System.Text.RegularExpressions.Regex.Replace(
                                        personalizedContent, 
                                        @"<p>Chào bạn <strong>.*?</strong>,</p>", 
                                        $"<p>Chào bạn <strong>{fullName}</strong>,</p>"
                                    );
                                }

                                // Thay thế các mẫu placeholders phần thưởng động
                                personalizedContent = personalizedContent.Replace("[VOUCHER_INFO]", voucherText);
                                personalizedContent = personalizedContent.Replace("[QUATANG_INFO]", giftText);

                                await _notificationService.SendPersonalNotificationAsync(targetUser.Id, title, personalizedContent, type);
                                sentInApp = true;
                                successCount++;
                            }
                            catch (Exception inAppEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to send in-app notification: {inAppEx.Message}");
                            }
                        }

                        // Nếu KHÔNG gửi được trên web (hoặc không tìm thấy User trên web), thì mới gửi Email làm phương án dự phòng (fallback)
                        if (!sentInApp)
                        {
                            string targetEmail = "";
                            if (cleanTarget.Contains("@"))
                            {
                                targetEmail = cleanTarget;
                            }
                            else if (targetUser != null && !string.IsNullOrEmpty(targetUser.Email))
                            {
                                targetEmail = targetUser.Email;
                            }

                            if (!string.IsNullOrEmpty(targetEmail) && _emailSender != null)
                            {
                                try
                                {
                                    // Thay thế các mẫu placeholders phần thưởng động (sử dụng thông tin chung vì không có thông tin user cụ thể trên web)
                                    string fallbackContent = content
                                        .Replace("<p>Chào bạn,</p>", "<p>Chào bạn,</p>")
                                        .Replace("[VOUCHER_INFO]", "Voucher tri ân khách hàng thân thiết.")
                                        .Replace("[QUATANG_INFO]", "Bộ ly sứ cao cấp vẽ vàng Bách Hóa Xanh");

                                    await _emailSender.SendEmailAsync(targetEmail, title, fallbackContent);
                                    successCount++;
                                }
                                catch (Exception emailEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Fallback email failed for {targetEmail}: {emailEx.Message}");
                                }
                            }
                        }
                    }

                    if (successCount == 0)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khách hàng nào khớp với danh sách Số điện thoại hoặc Email đã nhập.";
                    }
                    else if (successCount < targetList.Count)
                    {
                        TempData["SuccessMessage"] = $"Đã gửi thông báo cá nhân hóa thành công cho {successCount}/{targetList.Count} khách hàng (một số liên hệ không tồn tại).";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"Đã gửi thông báo riêng cá nhân hóa thành công cho cả {successCount} khách hàng!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hệ thống: {ex.Message}";
                return View();
            }

            return RedirectToAction(nameof(Send));
        }
    }
}
