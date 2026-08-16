// Custom OTP-based Reset Password page model
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            var verified = HttpContext.Session.GetString("OtpVerified");
            var email = HttpContext.Session.GetString("OtpEmail");
            if (verified != "true" || string.IsNullOrEmpty(email))
                return RedirectToPage("./ForgotPassword");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var verified = HttpContext.Session.GetString("OtpVerified");
            var email = HttpContext.Session.GetString("OtpEmail");
            if (verified != "true" || string.IsNullOrEmpty(email))
                return RedirectToPage("./ForgotPassword");

            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToPage("./Login");

            // Generate a fresh reset token and apply immediately
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, Input.Password);

            if (result.Succeeded)
            {
                // Clear any lockout
                await _userManager.ResetAccessFailedCountAsync(user);
                await _userManager.SetLockoutEndDateAsync(user, null);

                // Auto-confirm email if not already done (user proved email ownership via OTP)
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    await _userManager.ConfirmEmailAsync(user, emailToken);
                }

                // Clear OTP session data
                HttpContext.Session.Remove("OtpVerified");
                HttpContext.Session.Remove("OtpEmail");

                // Redirect to Login with success message
                TempData["SuccessMessage"] = "✅ Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToPage("./Login");
            }

            // Show errors in Vietnamese
            foreach (var error in result.Errors)
            {
                var msg = error.Code switch
                {
                    "PasswordTooShort"           => "Mật khẩu phải có ít nhất 8 ký tự.",
                    "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất một ký tự đặc biệt (!@#$%...).",
                    "PasswordRequiresDigit"      => "Mật khẩu phải có ít nhất một chữ số (0-9).",
                    "PasswordRequiresUpper"      => "Mật khẩu phải có ít nhất một chữ hoa (A-Z).",
                    "PasswordRequiresLower"      => "Mật khẩu phải có ít nhất một chữ thường (a-z).",
                    _                           => error.Description
                };
                ModelState.AddModelError(string.Empty, msg);
            }
            return Page();
        }
    }
}
