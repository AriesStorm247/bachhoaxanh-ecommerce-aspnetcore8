using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RoleService _roleService;
        private readonly LoyaltyService _loyaltyService;

        public CustomersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            RoleService roleService,
            LoyaltyService loyaltyService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _roleService = roleService;
            _loyaltyService = loyaltyService;
        }

        public async Task<IActionResult> Index()
        {
            if (!CanManageCustomers())
            {
                return RedirectToAction("Index", "Home");
            }

            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var items = new List<AdminCustomerListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Count == 0)
                {
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }
                    await _userManager.AddToRoleAsync(user, "Customer");
                    roles = await _userManager.GetRolesAsync(user);
                }
                var customerRole = roles.FirstOrDefault(UserRoleGroups.IsCustomerRole);
                if (customerRole == null)
                {
                    continue;
                }

                var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
                items.Add(new AdminCustomerListItemViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    FullName = profile.FullName,
                    PhoneNumber = user.PhoneNumber,
                    AvatarUrl = profile.AvatarUrl,
                    RoleName = customerRole,
                    LoyaltyPoints = profile.LoyaltyPoints,
                    MembershipLevel = profile.MembershipLevel,
                    MembershipTierName = LoyaltyService.GetTierName(profile.MembershipLevel),
                    EquippedAvatarFrame = profile.EquippedAvatarFrame,
                    ShippingAddress = profile.ShippingAddress,
                    BankAccountLink = profile.BankAccountLink,
                    LastActiveTime = profile.LastActiveTime,
                    IsOnline = profile.IsOnline,
                    LockoutEnd = user.LockoutEnd
                });
            }

            await _context.SaveChangesAsync();
            return View(items);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (!CanManageCustomers())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await IsCustomerAsync(user))
            {
                return NotFound();
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            await _context.SaveChangesAsync();

            return View(ToEditViewModel(user, profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCustomerEditViewModel model)
        {
            if (!CanManageCustomers())
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || !await IsCustomerAsync(user))
            {
                return NotFound();
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            if (!ModelState.IsValid)
            {
                model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                return View(model);
            }

            var email = model.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được tài khoản khác sử dụng.");
                model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                return View(model);
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(user, email);
                if (!emailResult.Succeeded)
                {
                    AddIdentityErrors(emailResult);
                    model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                    return View(model);
                }

                var userNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!userNameResult.Succeeded)
                {
                    AddIdentityErrors(userNameResult);
                    model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                    return View(model);
                }
            }

            var phoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber?.Trim());
            if (!phoneResult.Succeeded)
            {
                AddIdentityErrors(phoneResult);
                model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    AddIdentityErrors(resetResult);
                    model.MembershipTierName = LoyaltyService.GetTierName(model.MembershipLevel);
                    return View(model);
                }
            }

            profile.AvatarUrl = model.AvatarUrl?.Trim();
            profile.FullName = model.FullName?.Trim();
            profile.ShippingAddress = model.ShippingAddress?.Trim();
            profile.BankAccountLink = model.BankAccountLink?.Trim();
            profile.LoyaltyPoints = model.LoyaltyPoints;



            if (profile.MembershipLevel != model.MembershipLevel)
            {
                var oldFrames = await _context.CustomerVouchers
                    .Where(v => v.UserId == user.Id && v.Type == "AvatarFrame")
                    .ToListAsync();

                // 1. Synchronize standard membership tier frames (tier-1 to tier-11)
                for (int lvl = 1; lvl <= 11; lvl++)
                {
                    string tierFrameKey = $"tier-{lvl}";
                    var frameVoucher = oldFrames.FirstOrDefault(f => f.Key == tierFrameKey);
                    
                    if (lvl <= model.MembershipLevel)
                    {
                        if (frameVoucher == null)
                        {
                            _context.CustomerVouchers.Add(new CustomerVoucher
                            {
                                UserId = user.Id,
                                Type = "AvatarFrame",
                                Key = tierFrameKey,
                                VoucherCode = "UNLOCKED",
                                DiscountValue = 0,
                                UnlockedAt = DateTime.Now
                            });
                        }
                    }
                    else
                    {
                        if (frameVoucher != null)
                        {
                            _context.CustomerVouchers.Remove(frameVoucher);
                        }
                    }
                }

                // 3. Update equipped status if invalid
                if (model.MembershipLevel >= 0)
                {
                    string newFrameKey = $"tier-{model.MembershipLevel}";
                    bool isEquippedStandardFrameValid = profile.EquippedAvatarFrame != null &&
                        profile.EquippedAvatarFrame.StartsWith("tier-") &&
                        int.TryParse(profile.EquippedAvatarFrame.Substring(5), out int eqLvl) &&
                        eqLvl <= model.MembershipLevel;

                    bool isEquippedDailyFrameValid = profile.EquippedAvatarFrame != null &&
                        (profile.EquippedAvatarFrame.StartsWith("daily-normal-") || profile.EquippedAvatarFrame.StartsWith("daily-limited-"));

                    if (!isEquippedStandardFrameValid && !isEquippedDailyFrameValid)
                    {
                        profile.EquippedAvatarFrame = newFrameKey;
                    }
                }
                else
                {
                    profile.EquippedAvatarFrame = "none";
                }

                // Revoke Badges as they are VIP-only
                var vipBadges = await _context.CustomerVouchers
                    .Where(v => v.UserId == user.Id && v.Type == "Badge")
                    .ToListAsync();
                foreach (var badge in vipBadges)
                {
                    _context.CustomerVouchers.Remove(badge);
                }
                profile.EquippedBadge = "none";

                if (profile.DisplayMembershipLevel.HasValue && profile.DisplayMembershipLevel.Value > model.MembershipLevel)
                {
                    profile.DisplayMembershipLevel = model.MembershipLevel;
                }
            }

            profile.MembershipLevel = model.MembershipLevel;
            profile.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        private bool CanManageCustomers()
        {
            var role = _roleService.GetRole(User);
            ViewBag.Role = role;
            // Chỉ Admin (role=1) được quản lý danh sách khách hàng
            return role == 1;
        }

        // POST: Xóa khách hàng (chỉ Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            if (!CanManageCustomers())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null || !await IsCustomerAsync(user))
            {
                TempData["Error"] = "Không tìm thấy tài khoản khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa CustomerProfile liên quan nếu tồn tại
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                _context.CustomerProfiles.Remove(profile);
            }

            await _userManager.DeleteAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa khách hàng thành công!";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsCustomerAsync(IdentityUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Customer"));
                }
                await _userManager.AddToRoleAsync(user, "Customer");
                roles = await _userManager.GetRolesAsync(user);
            }
            return roles.Any(UserRoleGroups.IsCustomerRole);
        }

        private static AdminCustomerEditViewModel ToEditViewModel(IdentityUser user, CustomerProfile profile)
        {
            return new AdminCustomerEditViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                FullName = profile.FullName,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = profile.AvatarUrl,
                ShippingAddress = profile.ShippingAddress,
                BankAccountLink = profile.BankAccountLink,
                LoyaltyPoints = profile.LoyaltyPoints,
                MembershipLevel = profile.MembershipLevel,
                MembershipTierName = LoyaltyService.GetTierName(profile.MembershipLevel)
            };
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
