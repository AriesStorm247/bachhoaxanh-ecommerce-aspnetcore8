// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
            [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                // Don't reveal that the user does not exist
                // Still redirect to VerifyOtp to avoid user enumeration
                // Set fake session data so the countdown timer works identical to a valid user
                var fakeOtp = Random.Shared.Next(100000, 1000000).ToString();
                var fakeExpiry = DateTime.UtcNow.AddMinutes(10);
                HttpContext.Session.SetString("OtpCode", fakeOtp);
                HttpContext.Session.SetString("OtpEmail", Input.Email);
                HttpContext.Session.SetString("OtpExpiry", fakeExpiry.ToString("O"));
                HttpContext.Session.SetInt32("OtpAttempts", 0);

                return RedirectToPage("./VerifyOtp");
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var isPermanent = lockoutEnd.HasValue && (lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes > 30;

                if (isPermanent)
                {
                    ModelState.AddModelError(string.Empty,
                        "Tài khoản của bạn đã bị khóa vĩnh viễn, vui lòng liên hệ qua email: Anhmuoi270280@gmail.com để biết thêm chi tiết");
                }
                else
                {
                    var remaining = lockoutEnd.HasValue
                        ? (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                        : 15;
                    ModelState.AddModelError(string.Empty,
                        $"Tài khoản đang bị tạm khóa. Vui lòng thử lại sau {remaining} phút.");
                }
                return Page();
            }

            // Generate 6-digit OTP
            var otp = Random.Shared.Next(100000, 1000000).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            // Store in session (server-side, secure)
            HttpContext.Session.SetString("OtpCode", otp);
            HttpContext.Session.SetString("OtpEmail", Input.Email);
            HttpContext.Session.SetString("OtpExpiry", expiry.ToString("O"));
            HttpContext.Session.SetInt32("OtpAttempts", 0);

            _logger.LogInformation("OTP generated for {Email}: {Otp}", Input.Email, otp);

            // Build HTML email
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background:#f4f4f4; margin:0; padding:0;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4; padding:30px 0;'>
    <tr><td align='center'>
      <table width='500' cellpadding='0' cellspacing='0' style='background:#fff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.1);'>
        <tr>
          <td style='background:linear-gradient(135deg,#1a7a2e,#38b558); padding:30px; text-align:center;'>
            <h1 style='color:#fff; margin:0; font-size:24px; font-weight:900; letter-spacing:1px;'>🛒 BÁCH HÓA XANH</h1>
            <p style='color:rgba(255,255,255,0.85); margin:8px 0 0; font-size:14px;'>Hệ thống đặt lại mật khẩu</p>
          </td>
        </tr>
        <tr>
          <td style='padding:40px 36px;'>
            <h2 style='color:#1a7a2e; margin:0 0 12px; font-size:20px;'>Mã xác thực của bạn</h2>
            <p style='color:#555; margin:0 0 28px; line-height:1.6;'>
              Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{Input.Email}</strong>.
              Vui lòng sử dụng mã OTP dưới đây để tiến hành xác thực:
            </p>
            <div style='background:#f0faf3; border:2px dashed #38b558; border-radius:12px; padding:24px; text-align:center; margin-bottom:28px;'>
              <span style='font-size:42px; font-weight:900; letter-spacing:14px; color:#1a7a2e; font-family:monospace;'>{otp}</span>
            </div>
            <p style='color:#888; font-size:13px; margin:0 0 8px;'>⏱ Mã có hiệu lực trong <strong>10 phút</strong>.</p>
            <p style='color:#888; font-size:13px; margin:0;'>🔒 Không chia sẻ mã này với bất kỳ ai.</p>
          </td>
        </tr>
        <tr>
          <td style='background:#f8f8f8; padding:18px 36px; text-align:center;'>
            <p style='color:#bbb; font-size:12px; margin:0;'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

            try
            {
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Mã xác thực đặt lại mật khẩu - Bách Hóa XANH",
                    emailBody);

                _logger.LogInformation("OTP email sent successfully to {Email}", Input.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", Input.Email);
                ModelState.AddModelError(string.Empty,
                    $"Không thể gửi email. Lỗi: {ex.Message}");
                return Page();
            }

            return RedirectToPage("./VerifyOtp");
        }
    }
}
