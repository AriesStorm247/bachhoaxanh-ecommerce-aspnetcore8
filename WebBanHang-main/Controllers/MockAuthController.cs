using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;

namespace WebBanHang.Controllers
{
    public class MockAuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly LoyaltyService _loyaltyService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MockAuthController> _logger;

        public MockAuthController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            LoyaltyService loyaltyService,
            IConfiguration configuration,
            ILogger<MockAuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _loyaltyService = loyaltyService;
            _configuration = configuration;
            _logger = logger;
        }

        private void LogDebug(string message)
        {
            try
            {
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "auth-debug.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
                _logger.LogWarning($"[AUTH-DEBUG] {message}");
            }
            catch { }
        }

        private bool IsRealGoogleConfigured()
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            return !string.IsNullOrEmpty(clientId) && clientId != "dummy-google-client-id" && clientId != "YOUR_GOOGLE_CLIENT_ID";
        }

        private bool IsRealFacebookConfigured()
        {
            var appId = _configuration["Authentication:Facebook:AppId"];
            return !string.IsNullOrEmpty(appId) && appId != "dummy-facebook-app-id" && appId != "YOUR_FACEBOOK_APP_ID";
        }

        private ContentResult ClosePopupAndReloadParent(string fallbackUrl, string errorMessage = null, string successMessage = null)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                TempData["Error"] = errorMessage;
            }
            if (!string.IsNullOrEmpty(successMessage))
            {
                TempData["Success"] = successMessage;
            }

            var js = $@"
<script>
    if (window.opener) {{
        try {{
            window.opener.isRedirecting = true;
        }} catch(e) {{}}
        try {{
            window.opener.location.reload();
        }} catch(e) {{
            window.opener.location.href = '{fallbackUrl}';
        }}
        window.close();
    }} else {{
        window.location.href = '{fallbackUrl}';
    }}
</script>";
            return Content(js, "text/html");
        }

        private ContentResult ClosePopupAndRedirectParent(string redirectUrl, string errorMessage = null, string successMessage = null, bool isAdmin = false)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                TempData["Error"] = errorMessage;
            }
            if (!string.IsNullOrEmpty(successMessage))
            {
                TempData["Success"] = successMessage;
            }

            var js = $@"
<script>
    if (window.opener) {{
        try {{
            window.opener.isRedirecting = true;
            if (window.opener.sessionStorage) {{
                window.opener.sessionStorage.setItem('adminSessionActive', '{(isAdmin ? "true" : "false")}');
            }}
        }} catch(e) {{}}
        try {{
            window.opener.location.href = '{redirectUrl}';
        }} catch(e) {{
            window.opener.location.reload();
        }}
        window.close();
    }} else {{
        try {{
            sessionStorage.setItem('adminSessionActive', '{(isAdmin ? "true" : "false")}');
        }} catch(e) {{}}
        window.location.href = '{redirectUrl}';
    }}
</script>";
            return Content(js, "text/html");
        }

        // --- DEBUG DB ---
        [HttpGet]
        public async Task<IActionResult> DbDebug()
        {
            var users = await _userManager.Users.ToListAsync();
            var resultList = new System.Collections.Generic.List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var logins = await _userManager.GetLoginsAsync(u);
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == u.Id);
                resultList.Add(new
                {
                    UserId = u.Id,
                    Email = u.Email,
                    Roles = roles,
                    Logins = logins.Select(l => new { l.LoginProvider, l.ProviderKey }),
                    Profile = profile == null ? null : new
                    {
                        profile.GoogleId,
                        profile.GoogleEmail,
                        profile.FacebookId,
                        profile.FacebookName
                    }
                });
            }
            return Json(resultList);
        }

        // --- GOOGLE LINK ---
        [HttpGet]
        public async Task<IActionResult> GoogleLink()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (IsRealGoogleConfigured())
            {
                var redirectUrl = Url.Action("GoogleLinkCallback", "MockAuth");
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl, user.Id);
                return Challenge(properties, "Google");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GoogleLink(string email)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Email Google không được để trống.";
                return View();
            }

            var alreadyLinked = await _context.CustomerProfiles.AnyAsync(p => p.GoogleEmail == email && p.UserId != user.Id);
            if (alreadyLinked)
            {
                TempData["Error"] = "Email Google này đã được liên kết với một tài khoản khác.";
                return View();
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            profile.GoogleId = Guid.NewGuid().ToString();
            profile.GoogleEmail = email;
            profile.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), successMessage: $"Đã liên kết tài khoản Google ({email}) thành công!");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleLinkCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), errorMessage: "Không thể lấy thông tin liên kết từ Google.");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var providerKey = info.ProviderKey;

            var alreadyLinked = await _context.CustomerProfiles.AnyAsync(p => p.GoogleEmail == email && p.UserId != user.Id);
            if (alreadyLinked)
            {
                return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), errorMessage: "Email Google này đã được liên kết với một tài khoản khác.");
            }

            await _userManager.AddLoginAsync(user, info);

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            profile.GoogleId = providerKey;
            profile.GoogleEmail = email;
            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), successMessage: $"Đã liên kết tài khoản Google ({email}) thành công!");
        }

        // --- FACEBOOK LINK ---
        [HttpGet]
        public async Task<IActionResult> FacebookLink()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (IsRealFacebookConfigured())
            {
                var redirectUrl = Url.Action("FacebookLinkCallback", "MockAuth");
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl, user.Id);
                return Challenge(properties, "Facebook");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FacebookLink(string name)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên Facebook không được để trống.";
                return View();
            }

            var alreadyLinked = await _context.CustomerProfiles.AnyAsync(p => p.FacebookName == name && p.UserId != user.Id);
            if (alreadyLinked)
            {
                TempData["Error"] = "Tài khoản Facebook này đã được liên kết với một tài khoản khác.";
                return View();
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            profile.FacebookId = Guid.NewGuid().ToString();
            profile.FacebookName = name;
            profile.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), successMessage: $"Đã liên kết tài khoản Facebook ({name}) thành công!");
        }

        [HttpGet]
        public async Task<IActionResult> FacebookLinkCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), errorMessage: "Không thể lấy thông tin liên kết từ Facebook.");
            }

            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.Email);
            var providerKey = info.ProviderKey;

            var alreadyLinked = await _context.CustomerProfiles.AnyAsync(p => p.FacebookId == providerKey && p.UserId != user.Id);
            if (alreadyLinked)
            {
                return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), errorMessage: "Tài khoản Facebook này đã được liên kết với một tài khoản khác.");
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            profile.FacebookId = providerKey;
            profile.FacebookName = name;
            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await _userManager.AddLoginAsync(user, info);

            return ClosePopupAndReloadParent(Url.Action("Edit", "CustomerProfile"), successMessage: $"Đã liên kết tài khoản Facebook ({name}) thành công!");
        }

        // --- GOOGLE LOGIN ---
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            if (IsRealGoogleConfigured())
            {
                var redirectUrl = Url.Action("GoogleLoginCallback", "MockAuth");
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
                return Challenge(properties, "Google");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GoogleLogin(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng nhập Email Google.";
                return View();
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.GoogleEmail == email);
            if (profile != null)
            {
                var user = await _userManager.FindByIdAsync(profile.UserId);
                if (user != null)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    var roles = await _userManager.GetRolesAsync(user);
                    bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                    return ClosePopupAndRedirectParent("/", successMessage: "Đăng nhập Google thành công!", isAdmin: isAdmin);
                }
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                var p = await _loyaltyService.GetOrCreateProfileAsync(existingUser.Id);
                p.GoogleEmail = email;
                p.GoogleId = Guid.NewGuid().ToString();
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                var roles = await _userManager.GetRolesAsync(existingUser);
                bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                return ClosePopupAndRedirectParent("/", successMessage: "Đăng nhập Google thành công!", isAdmin: isAdmin);
            }

            var newUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser, "Google@123");
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Customer");
                
                var newProfile = await _loyaltyService.GetOrCreateProfileAsync(newUser.Id);
                newProfile.GoogleEmail = email;
                newProfile.GoogleId = Guid.NewGuid().ToString();
                newProfile.FullName = email.Split('@')[0];
                newProfile.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return ClosePopupAndRedirectParent("/", successMessage: "Tạo tài khoản mới và đăng nhập thành công!", isAdmin: false);
            }

            TempData["Error"] = "Không thể tạo tài khoản mới: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GoogleLoginCallback(string returnUrl = null)
        {
            LogDebug("=== GoogleLoginCallback Started ===");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                LogDebug("GetExternalLoginInfoAsync returned null");
                return ClosePopupAndRedirectParent("/Identity/Account/Login", errorMessage: "Lỗi xác thực Google hoặc bị hủy.");
            }

            LogDebug($"External Login Info: Provider={info.LoginProvider}, ProviderKey={info.ProviderKey}, Email={info.Principal.FindFirstValue(ClaimTypes.Email)}");

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            LogDebug($"ExternalLoginSignInAsync result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}, RequiresTwoFactor={result.RequiresTwoFactor}");

            if (result.Succeeded)
            {
                LogDebug("ExternalLoginSignInAsync Succeeded. Redirecting parent...");
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                bool isAdmin = false;
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                }
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Đăng nhập Google thành công!", isAdmin: isAdmin);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                LogDebug("Email is null or empty from external principal.");
                return ClosePopupAndRedirectParent("/Identity/Account/Login", errorMessage: "Không lấy được email từ tài khoản Google của bạn.");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                LogDebug($"Found existing user: Id={existingUser.Id}, Email={existingUser.Email}, EmailConfirmed={existingUser.EmailConfirmed}");
                var addLoginRes = await _userManager.AddLoginAsync(existingUser, info);
                LogDebug($"AddLoginAsync result: Succeeded={addLoginRes.Succeeded}, Errors={string.Join(", ", addLoginRes.Errors.Select(e => e.Description))}");
                
                var p = await _loyaltyService.GetOrCreateProfileAsync(existingUser.Id);
                p.GoogleEmail = email;
                p.GoogleId = info.ProviderKey;
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                LogDebug("SignInAsync completed for existing user. Redirecting parent...");
                var roles = await _userManager.GetRolesAsync(existingUser);
                bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Đăng nhập Google thành công!", isAdmin: isAdmin);
            }

            LogDebug($"No existing user found for email {email}. Creating new user...");
            var newUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(newUser, "Google@123");
            LogDebug($"CreateAsync result: Succeeded={createResult.Succeeded}, Errors={string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Customer");
                await _userManager.AddLoginAsync(newUser, info);

                var newProfile = await _loyaltyService.GetOrCreateProfileAsync(newUser.Id);
                newProfile.GoogleEmail = email;
                newProfile.GoogleId = info.ProviderKey;
                newProfile.FullName = email.Split('@')[0];
                newProfile.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(newUser, isPersistent: false);
                LogDebug("SignInAsync completed for new user. Redirecting parent...");
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Tạo tài khoản mới và đăng nhập thành công!", isAdmin: false);
            }

            LogDebug("Failed to create new user.");
            return ClosePopupAndRedirectParent("/Identity/Account/Login", errorMessage: "Không thể tạo tài khoản mới: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        // --- FACEBOOK LOGIN ---
        [HttpGet]
        public IActionResult FacebookLogin()
        {
            if (IsRealFacebookConfigured())
            {
                var redirectUrl = Url.Action("FacebookLoginCallback", "MockAuth");
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);
                return Challenge(properties, "Facebook");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FacebookLogin(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vui lòng nhập Tên Facebook.";
                return View();
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.FacebookName == name);
            if (profile != null)
            {
                var user = await _userManager.FindByIdAsync(profile.UserId);
                if (user != null)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    var roles = await _userManager.GetRolesAsync(user);
                    bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                    return ClosePopupAndRedirectParent("/", successMessage: "Đăng nhập Facebook thành công!", isAdmin: isAdmin);
                }
            }

            var dummyEmail = $"fb_{Guid.NewGuid().ToString("N").Substring(0, 8)}@facebook.com";
            var newUser = new IdentityUser
            {
                UserName = dummyEmail,
                Email = dummyEmail,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser, "Facebook@123");
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Customer");

                var newProfile = await _loyaltyService.GetOrCreateProfileAsync(newUser.Id);
                newProfile.FacebookName = name;
                newProfile.FacebookId = Guid.NewGuid().ToString();
                newProfile.FullName = name;
                newProfile.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return ClosePopupAndRedirectParent("/", successMessage: "Tạo tài khoản mới và đăng nhập thành công!", isAdmin: false);
            }

            TempData["Error"] = "Không thể tạo tài khoản mới: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FacebookLoginCallback(string returnUrl = null)
        {
            LogDebug("=== FacebookLoginCallback Started ===");
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                LogDebug("GetExternalLoginInfoAsync returned null");
                return ClosePopupAndRedirectParent("/Identity/Account/Login", errorMessage: "Lỗi xác thực Facebook hoặc bị hủy.");
            }

            LogDebug($"External Login Info: Provider={info.LoginProvider}, ProviderKey={info.ProviderKey}, Email={info.Principal.FindFirstValue(ClaimTypes.Email)}");

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            LogDebug($"ExternalLoginSignInAsync result: Succeeded={result.Succeeded}, IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}, RequiresTwoFactor={result.RequiresTwoFactor}");

            if (result.Succeeded)
            {
                LogDebug("ExternalLoginSignInAsync Succeeded. Redirecting parent...");
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                bool isAdmin = false;
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                }
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Đăng nhập Facebook thành công!", isAdmin: isAdmin);
            }

            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                email = $"fb_{info.ProviderKey}@facebook.com";
            }
            LogDebug($"External details: Name={name}, Email={email}");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                LogDebug($"Found existing user: Id={existingUser.Id}, Email={existingUser.Email}, EmailConfirmed={existingUser.EmailConfirmed}");
                var addLoginRes = await _userManager.AddLoginAsync(existingUser, info);
                LogDebug($"AddLoginAsync result: Succeeded={addLoginRes.Succeeded}, Errors={string.Join(", ", addLoginRes.Errors.Select(e => e.Description))}");

                var p = await _loyaltyService.GetOrCreateProfileAsync(existingUser.Id);
                p.FacebookName = name ?? email.Split('@')[0];
                p.FacebookId = info.ProviderKey;
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                LogDebug("SignInAsync completed for existing user. Redirecting parent...");
                var roles = await _userManager.GetRolesAsync(existingUser);
                bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Đăng nhập Facebook thành công!", isAdmin: isAdmin);
            }

            LogDebug($"No existing user found for email {email}. Creating new user...");
            var newUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(newUser, "Facebook@123");
            LogDebug($"CreateAsync result: Succeeded={createResult.Succeeded}, Errors={string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            if (createResult.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Customer");
                await _userManager.AddLoginAsync(newUser, info);

                var newProfile = await _loyaltyService.GetOrCreateProfileAsync(newUser.Id);
                newProfile.FacebookName = name ?? email.Split('@')[0];
                newProfile.FacebookId = info.ProviderKey;
                newProfile.FullName = name ?? email.Split('@')[0];
                newProfile.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(newUser, isPersistent: false);
                LogDebug("SignInAsync completed for new user. Redirecting parent...");
                return ClosePopupAndRedirectParent(returnUrl ?? "/", successMessage: "Tạo tài khoản mới và đăng nhập thành công!", isAdmin: false);
            }

            LogDebug("Failed to create new user.");
            return ClosePopupAndRedirectParent("/Identity/Account/Login", errorMessage: "Không thể tạo tài khoản mới: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }
}
