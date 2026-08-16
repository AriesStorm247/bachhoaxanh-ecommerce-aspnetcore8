using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly ApplicationDbContext _context;

        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> OnGet(string returnUrl = null)
        {
            var userId = _signInManager.UserManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    profile.IsOnline = false;
                    profile.LastActiveTime = DateTime.Now;
                    profile.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out via GET.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var userId = _signInManager.UserManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile != null)
                {
                    profile.IsOnline = false;
                    profile.LastActiveTime = DateTime.Now;
                    profile.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
