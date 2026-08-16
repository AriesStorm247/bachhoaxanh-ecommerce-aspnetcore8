using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Data;
using WebBanHang.Services;
using WebBanHang.Models;

namespace WebBanHang.Controllers
{
    public class HRMController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RoleService _roleService;
        private readonly ApplicationDbContext _context;

        public HRMController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            RoleService roleService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleService = roleService;
            _context = context;
        }

        // Admin=1 hoặc Manager=2 đều có thể xem danh sách nhân sự
        private bool CanViewHRM()
        {
            int role = _roleService.GetRole(User);
            ViewBag.Role = role;
            return role == 1 || role == 2;
        }

        // Chỉ Admin=1 mới được gán quyền / xóa nhân sự
        private bool CanManageHRM()
        {
            int role = _roleService.GetRole(User);
            ViewBag.Role = role;
            return role == 1;
        }

        // GET: Danh sách nhân sự (Admin + Manager xem được)
        public async Task<IActionResult> Index()
        {
            if (!CanViewHRM())
                return RedirectToAction("Index", "Home");

            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var items = new List<WebBanHang.ViewModels.HRMListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var staffRole = roles.FirstOrDefault(r => r != "Admin" && r != "Customer");
                if (staffRole != null)
                {
                    var profile = await _context.CustomerProfiles
                        .Include(p => p.WorkingBranch)
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);
                    items.Add(new WebBanHang.ViewModels.HRMListItemViewModel
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        FullName = profile?.FullName,
                        PhoneNumber = user.PhoneNumber,
                        RoleName = staffRole,
                        LastActiveTime = profile?.LastActiveTime,
                        IsOnline = profile?.IsOnline ?? false,
                        LockoutEnd = user.LockoutEnd,
                        AccessFailedCount = user.AccessFailedCount,
                        AvatarUrl = profile?.AvatarUrl,
                        EquippedAvatarFrame = profile?.EquippedAvatarFrame,

                        EquippedBadge = profile?.EquippedBadge,
                        MembershipLevel = profile?.MembershipLevel ?? 0,
                        WorkingBranchId = profile?.WorkingBranchId,
                        WorkingBranchName = profile?.WorkingBranch?.Name
                    });
                }
            }

            // Dropdown gán quyền: các role có trong hệ thống ngoại trừ Admin và User
            ViewBag.AllRoles = await _roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .ToListAsync();

            ViewBag.Branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            ViewBag.AllRoles = await _roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string email, string password, string roleName)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(roleName) && roleName != "Admin" && roleName != "Customer")
                {
                    await _userManager.AddToRoleAsync(user, roleName);
                }

                TempData["Success"] = "Tạo tài khoản thành công";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.AllRoles = await _roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .ToListAsync();
            return View();
        }

        // POST: Gán quyền (chỉ Admin)
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(roleName) && roleName != "Admin" && roleName != "Customer")
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, roleName);
                TempData["Success"] = $"Đã gán quyền {roleName} cho {user.UserName}";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Xóa nhân sự (chỉ Admin)
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    TempData["Error"] = "Không thể xóa tài khoản Admin!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.DeleteAsync(user);
                TempData["Success"] = "Đã xóa tài khoản nhân sự thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Lấy thông tin nhân sự để điền vào modal sửa (chỉ Admin)
        [HttpGet]
        public async Task<IActionResult> EditStaff(string id)
        {
            if (!CanManageHRM())
                return Json(new { success = false, message = "Không có quyền." });

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy nhân sự." });

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    UserId = user.Id,
                    MembershipLevel = 0,
                    LoyaltyPoints = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.CustomerProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                userId = user.Id,
                email = user.Email ?? string.Empty,
                fullName = profile.FullName ?? string.Empty,
                phoneNumber = user.PhoneNumber ?? string.Empty,
                avatarUrl = profile.AvatarUrl ?? string.Empty,
                shippingAddress = profile.ShippingAddress ?? string.Empty,
                bankAccountLink = profile.BankAccountLink ?? string.Empty,
                loyaltyPoints = profile.LoyaltyPoints,
                membershipLevel = profile.MembershipLevel,
                equippedVip = "none",
                workingBranchId = profile.WorkingBranchId
            });
        }

        // POST: Sửa thông tin nhân sự (chỉ Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(WebBanHang.ViewModels.HRMEditViewModel model)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản nhân sự.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["Error"] = "Không thể sửa thông tin tài khoản Admin!";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật email / username nếu thay đổi
            var newEmail = model.Email.Trim();
            if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(newEmail);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    TempData["Error"] = "Email này đã được tài khoản khác sử dụng.";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.SetEmailAsync(user, newEmail);
                await _userManager.SetUserNameAsync(user, newEmail);
            }

            // Cập nhật số điện thoại
            await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber?.Trim());

            // Thay đổi mật khẩu nếu được nhập
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    var errorMsg = string.Join(" ", resetResult.Errors.Select(e => e.Description));
                    TempData["Error"] = "Không thể đổi mật khẩu: " + errorMsg;
                    return RedirectToAction(nameof(Index));
                }
            }

            // Cập nhật CustomerProfile
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };
                _context.CustomerProfiles.Add(profile);
            }

            profile.FullName = model.FullName?.Trim();
            profile.AvatarUrl = model.AvatarUrl?.Trim();
            profile.ShippingAddress = model.ShippingAddress?.Trim();
            profile.BankAccountLink = model.BankAccountLink?.Trim();
            profile.LoyaltyPoints = model.LoyaltyPoints;
            profile.WorkingBranchId = model.WorkingBranchId;

            // Đồng bộ cấp bậc và VIP (giống CustomersController)
            int oldLevel = profile.MembershipLevel;
            int newLevel = model.MembershipLevel;
            
            if (oldLevel != newLevel)
            {
                var oldFrames = await _context.CustomerVouchers
                    .Where(v => v.UserId == user.Id && v.Type == "AvatarFrame")
                    .ToListAsync();

                for (int lvl = 1; lvl <= 11; lvl++)
                {
                    string tierFrameKey = $"tier-{lvl}";
                    var frameVoucher = oldFrames.FirstOrDefault(f => f.Key == tierFrameKey);
                    if (lvl <= newLevel)
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

                if (profile.DisplayMembershipLevel.HasValue && profile.DisplayMembershipLevel.Value > newLevel)
                {
                    profile.DisplayMembershipLevel = newLevel;
                }
            }

            profile.MembershipLevel = newLevel;



            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sửa thông tin thành công";
            return RedirectToAction(nameof(Index));
        }


        // POST: Khóa vĩnh viễn nhân sự (chỉ Admin)
        [HttpPost]
        public async Task<IActionResult> LockUser(string userId)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    TempData["Error"] = "Không thể khóa tài khoản Admin!";
                    return RedirectToAction(nameof(Index));
                }

                await _userManager.SetLockoutEnabledAsync(user, true);
                // Set lockout end to 100 years in the future (permanent)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["Success"] = $"Đã khóa vĩnh viễn tài khoản {user.UserName}";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Mở khóa nhân sự (chỉ Admin)
        [HttpPost]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            if (!CanManageHRM())
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
                TempData["Success"] = $"Đã mở khóa tài khoản {user.UserName}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
