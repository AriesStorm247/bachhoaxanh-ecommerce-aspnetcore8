// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace WebBanHang.Areas.Identity.Pages.Account
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public LockoutModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public bool IsPermanent { get; set; }

        public async Task OnGetAsync(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && user.LockoutEnd.HasValue)
                {
                    // Nếu thời gian khóa còn lại lớn hơn 30 phút, coi như khóa vĩnh viễn (admin khóa)
                    IsPermanent = (user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes > 30;
                }
            }
        }
    }
}
