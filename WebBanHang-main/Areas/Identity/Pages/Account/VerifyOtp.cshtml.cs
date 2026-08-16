// OTP verification page model
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class VerifyOtpModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public VerifyOtpModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string OtpEmail { get; set; }
        public int AttemptsLeft { get; set; } = 5;
        public string ErrorMessage { get; set; }
        public long ExpiryTimestamp { get; set; }
        public int RemainingSeconds { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã xác thực gồm đúng 6 chữ số.")]
            [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã xác thực chỉ gồm 6 chữ số.")]
            [Display(Name = "Mã xác thực")]
            public string OtpCode { get; set; }
        }

        public IActionResult OnGet()
        {
            var storedEmail = HttpContext.Session.GetString("OtpEmail");
            if (string.IsNullOrEmpty(storedEmail))
                return RedirectToPage("./ForgotPassword");

            OtpEmail = storedEmail;
            var attempts = HttpContext.Session.GetInt32("OtpAttempts") ?? 0;
            AttemptsLeft = 5 - attempts;

            var expiryStr = HttpContext.Session.GetString("OtpExpiry");
            if (!string.IsNullOrEmpty(expiryStr) && DateTime.TryParse(expiryStr, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
            {
                ExpiryTimestamp = new DateTimeOffset(expiry).ToUnixTimeMilliseconds();
                RemainingSeconds = (int)Math.Max(0, (expiry - DateTime.UtcNow).TotalSeconds);
            }
            else
            {
                ExpiryTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds();
                RemainingSeconds = 600;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var storedEmail = HttpContext.Session.GetString("OtpEmail");
            if (string.IsNullOrEmpty(storedEmail))
                return RedirectToPage("./ForgotPassword");

            OtpEmail = storedEmail;

            var expiryStr = HttpContext.Session.GetString("OtpExpiry");
            DateTime expiry = DateTime.UtcNow.AddMinutes(10);
            var hasExpiry = !string.IsNullOrEmpty(expiryStr) && DateTime.TryParse(expiryStr, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out expiry);

            ExpiryTimestamp = new DateTimeOffset(expiry).ToUnixTimeMilliseconds();
            RemainingSeconds = (int)Math.Max(0, (expiry - DateTime.UtcNow).TotalSeconds);

            if (!ModelState.IsValid)
            {
                var att = HttpContext.Session.GetInt32("OtpAttempts") ?? 0;
                AttemptsLeft = 5 - att;
                return Page();
            }

            // Check OTP expiry
            if (hasExpiry)
            {
                if (DateTime.UtcNow > expiry)
                {
                    // Clear session
                    HttpContext.Session.Remove("OtpCode");
                    HttpContext.Session.Remove("OtpExpiry");
                    HttpContext.Session.Remove("OtpAttempts");
                    ErrorMessage = "Mã xác thực đã hết hạn. Vui lòng yêu cầu mã mới.";
                    AttemptsLeft = 0;
                    return Page();
                }
            }

            var storedOtp = HttpContext.Session.GetString("OtpCode");
            var currentAttempts = HttpContext.Session.GetInt32("OtpAttempts") ?? 0;

            if (string.IsNullOrEmpty(storedOtp))
            {
                ErrorMessage = "Phiên xác thực không hợp lệ. Vui lòng yêu cầu lại mã.";
                AttemptsLeft = 0;
                return Page();
            }

            if (Input.OtpCode == storedOtp)
            {
                // OTP correct — mark as verified, clear sensitive data
                HttpContext.Session.Remove("OtpCode");
                HttpContext.Session.Remove("OtpExpiry");
                HttpContext.Session.Remove("OtpAttempts");
                HttpContext.Session.SetString("OtpVerified", "true");
                // OtpEmail stays in session for ResetPassword to use

                return RedirectToPage("./ResetPassword");
            }
            else
            {
                // Wrong OTP
                currentAttempts++;
                HttpContext.Session.SetInt32("OtpAttempts", currentAttempts);
                AttemptsLeft = 5 - currentAttempts;

                if (currentAttempts >= 5)
                {
                    // Lock the account for 15 minutes
                    var user = await _userManager.FindByEmailAsync(storedEmail);
                    if (user != null)
                    {
                        await _userManager.SetLockoutEnabledAsync(user, true);
                        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                    }

                    // Clear OTP session
                    HttpContext.Session.Remove("OtpCode");
                    HttpContext.Session.Remove("OtpEmail");
                    HttpContext.Session.Remove("OtpExpiry");
                    HttpContext.Session.Remove("OtpAttempts");

                    TempData["LockoutMessage"] = "Bạn đã nhập sai mã xác thực quá 5 lần. Tài khoản bị tạm khóa 15 phút.";
                    return RedirectToPage("./Login");
                }

                ErrorMessage = $"Mã xác thực sai. Bạn còn {AttemptsLeft} lần thử.";
                return Page();
            }
        }
    }
}
