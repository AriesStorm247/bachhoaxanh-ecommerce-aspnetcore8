// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Clear session verification data to ensure they get a fresh one when checking email
            HttpContext.Session.Remove("AdminMathQuestion");
            HttpContext.Session.Remove("AdminMathAnswer");
            HttpContext.Session.Remove("AdminCaptchaCode");
            HttpContext.Session.Remove("AdminCaptchaSvg");
            HttpContext.Session.Remove("AdminMathPassed");
            HttpContext.Session.Remove("AdminCaptchaPassed");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            _logger.LogWarning($"--- POST LOGIN ---");
            _logger.LogWarning($"Input.Email: '{Input?.Email}'");
            _logger.LogWarning($"Input.Password: '{Input?.Password}'");
            if (Request.HasFormContentType)
            {
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogWarning($"Form Key: '{key}' = '{Request.Form[key]}'");
                }
            }

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Check if the email belongs to an Admin user
                var user = await _userManager.FindByEmailAsync(Input.Email);
                bool isAdmin = false;
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                }

                if (isAdmin)
                {
                    bool mathPassed    = HttpContext.Session.GetString("AdminMathPassed")    == "true";
                    bool captchaPassed = HttpContext.Session.GetString("AdminCaptchaPassed") == "true";
                    string passcodeStr = Request.Form["AdminPasscode"];
                    const string correctPasscode = "100405";
                    bool passcodePassed = passcodeStr == correctPasscode;

                    if (!mathPassed || !captchaPassed || !passcodePassed)
                    {
                        string errorMsg;
                        if (!mathPassed)
                            errorMsg = "Chưa hoàn thành xác minh câu hỏi toán học.";
                        else if (!captchaPassed)
                            errorMsg = "Chưa hoàn thành xác minh mã Captcha.";
                        else
                            errorMsg = "Mật mã bảo mật không chính xác.";

                        ModelState.AddModelError(string.Empty, errorMsg);

                        // Regenerate verification for next attempt
                        GenerateVerification();

                        // Keep the verification container visible on return
                        ViewData["IsAdminLogin"] = true;
                        ViewData["MathQuestion"] = HttpContext.Session.GetString("AdminMathQuestion");
                        ViewData["CaptchaSvg"]   = HttpContext.Session.GetString("AdminCaptchaSvg");

                        return Page();
                    }

                    // Clear step-flags after successful full verification
                    HttpContext.Session.Remove("AdminMathPassed");
                    HttpContext.Session.Remove("AdminCaptchaPassed");
                }

                // Force isPersistent: false for admin accounts to avoid saving session cookie on closing browser
                bool isPersistent = isAdmin ? false : Input.RememberMe;
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = isPersistent });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout", new { email = Input.Email });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
                    if (isAdmin)
                    {
                        GenerateVerification();
                        ViewData["IsAdminLogin"] = true;
                        ViewData["MathQuestion"] = HttpContext.Session.GetString("AdminMathQuestion");
                        ViewData["CaptchaSvg"] = HttpContext.Session.GetString("AdminCaptchaSvg");
                    }
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private void EnsureVerificationGenerated()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminMathAnswer")))
            {
                GenerateVerification();
            }
        }

        private void GenerateVerification()
        {
            var random   = new Random();
            int opIndex  = random.Next(0, 4); // 0=+  1=-  2=×  3=÷
            int a, b, answer;
            string questionText;

            switch (opIndex)
            {
                case 0: // Cộng
                    a = random.Next(-100, 101);
                    b = random.Next(-100, 101);
                    answer       = a + b;
                    questionText = $"{a} + {b} = ?";
                    break;

                case 1: // Trừ
                    a = random.Next(-100, 101);
                    b = random.Next(-100, 101);
                    answer       = a - b;
                    questionText = $"{a} - {b} = ?";
                    break;

                case 2: // Nhân (giới hạn -12..12 để dễ tính)
                    a = random.Next(-12, 13);
                    b = random.Next(-12, 13);
                    answer       = a * b;
                    questionText = $"{a} \u00d7 {b} = ?";
                    break;

                default: // Chia – đảm bảo kết quả là số nguyên
                    b = random.Next(-10, 11);
                    while (b == 0) b = random.Next(-10, 11);
                    answer = random.Next(-20, 21);
                    a      = answer * b;
                    questionText = $"{a} \u00f7 {b} = ?";
                    break;
            }

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var captchaCode = new string(
                Enumerable.Repeat(chars, 5).Select(s => s[random.Next(s.Length)]).ToArray());
            var captchaSvg = GenerateCaptchaSvg(captchaCode);

            HttpContext.Session.SetString("AdminMathQuestion", questionText);
            HttpContext.Session.SetString("AdminMathAnswer",   answer.ToString());
            HttpContext.Session.SetString("AdminCaptchaCode",  captchaCode);
            HttpContext.Session.SetString("AdminCaptchaSvg",   captchaSvg);

            // Reset step-completion flags
            HttpContext.Session.Remove("AdminMathPassed");
            HttpContext.Session.Remove("AdminCaptchaPassed");
        }

        private string GenerateCaptchaSvg(string code)
        {
            var random = new Random();
            var width = 150;
            var height = 50;
            var svg = new StringBuilder();
            svg.Append($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg' style='background-color: #f0f0f0; border-radius: 8px;'>");
            
            for (int i = 0; i < 5; i++)
            {
                int x1 = random.Next(width);
                int y1 = random.Next(height);
                int x2 = random.Next(width);
                int y2 = random.Next(height);
                svg.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='rgba(0,0,0,0.15)' stroke-width='2' />");
            }

            for (int i = 0; i < 20; i++)
            {
                int cx = random.Next(width);
                int cy = random.Next(height);
                int r = random.Next(2, 5);
                svg.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='rgba(0,0,0,0.1)' />");
            }

            var charWidth = width / (code.Length + 1);
            for (int i = 0; i < code.Length; i++)
            {
                var ch = code[i];
                var fontSize = random.Next(22, 32);
                var rotate = random.Next(-25, 25);
                var x = (i + 0.5) * charWidth + random.Next(-5, 5);
                var y = 35 + random.Next(-5, 5);
                var color = $"rgb({random.Next(20, 100)},{random.Next(20, 100)},{random.Next(20, 100)})";
                
                svg.Append($"<text x='{x}' y='{y}' font-size='{fontSize}' font-weight='bold' fill='{color}' transform='rotate({rotate} {x} {y})' font-family='Courier New, monospace'>{ch}</text>");
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        public async Task<JsonResult> OnGetCheckAdminAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return new JsonResult(new { isAdmin = false });

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return new JsonResult(new { isAdmin = false });

            var roles   = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);

            if (isAdmin)
            {
                GenerateVerification();
                return new JsonResult(new
                {
                    isAdmin      = true,
                    mathQuestion = HttpContext.Session.GetString("AdminMathQuestion"),
                    captchaSvg   = HttpContext.Session.GetString("AdminCaptchaSvg")
                });
            }

            return new JsonResult(new { isAdmin = false });
        }

        // ── Validate step 1: math ──────────────────────────────────────────────
        public JsonResult OnGetValidateStep1(string answer)
        {
            string correct = HttpContext.Session.GetString("AdminMathAnswer");
            if (!string.IsNullOrEmpty(correct) && answer == correct)
            {
                HttpContext.Session.SetString("AdminMathPassed", "true");
                return new JsonResult(new { valid = true });
            }
            return new JsonResult(new { valid = false, message = "Kết quả không đúng, vui lòng thử lại." });
        }

        // ── Validate step 2: captcha ───────────────────────────────────────────
        public JsonResult OnGetValidateStep2(string answer)
        {
            string correct = HttpContext.Session.GetString("AdminCaptchaCode");
            if (!string.IsNullOrEmpty(correct) &&
                string.Equals(answer, correct, StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.SetString("AdminCaptchaPassed", "true");
                return new JsonResult(new { valid = true });
            }

            // Wrong captcha → regenerate automatically
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var newCode = new string(
                Enumerable.Repeat(chars, 5).Select(s => s[random.Next(s.Length)]).ToArray());
            var newSvg = GenerateCaptchaSvg(newCode);
            HttpContext.Session.SetString("AdminCaptchaCode", newCode);
            HttpContext.Session.SetString("AdminCaptchaSvg",  newSvg);
            HttpContext.Session.Remove("AdminCaptchaPassed");

            return new JsonResult(new { valid = false, captchaSvg = newSvg, message = "Mã captcha không đúng, vui lòng thử lại." });
        }

        // ── Refresh captcha only (step 2 button) ──────────────────────────────
        public JsonResult OnGetRefreshCaptcha()
        {
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new string(
                Enumerable.Repeat(chars, 5).Select(s => s[random.Next(s.Length)]).ToArray());
            var svg  = GenerateCaptchaSvg(code);
            HttpContext.Session.SetString("AdminCaptchaCode", code);
            HttpContext.Session.SetString("AdminCaptchaSvg",  svg);
            HttpContext.Session.Remove("AdminCaptchaPassed");
            return new JsonResult(new { captchaSvg = svg });
        }

        // ── Kept for compatibility ─────────────────────────────────────────────
        public JsonResult OnGetRefreshVerification()
        {
            GenerateVerification();
            return new JsonResult(new
            {
                mathQuestion = HttpContext.Session.GetString("AdminMathQuestion"),
                captchaSvg   = HttpContext.Session.GetString("AdminCaptchaSvg")
            });
        }
    }
}
