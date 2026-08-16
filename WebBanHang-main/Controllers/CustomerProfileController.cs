using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBanHang.Data;
using WebBanHang.Models;
using WebBanHang.Services;
using WebBanHang.ViewModels;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class CustomerProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly LoyaltyService _loyaltyService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CustomerProfileController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            LoyaltyService loyaltyService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _loyaltyService = loyaltyService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Edit(string? orderSearch, int orderPage = 1, int voucherPage = 1, int chatPage = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            await _context.SaveChangesAsync();

            await _loyaltyService.SyncObsoleteVouchersAsync(user.Id);

            var model = await GetProfileViewModelAsync(user, profile, orderSearch, orderPage, voucherPage, chatPage);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerProfileViewModel model, IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var roles = await _userManager.GetRolesAsync(user);
            model.RoleName = roles.FirstOrDefault();

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            if (!ModelState.IsValid)
            {
                return View(await PrepareInvalidModelAsync(user, profile, model));
            }

            var email = model.Email.Trim();
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được tài khoản khác sử dụng.");
                return View(await PrepareInvalidModelAsync(user, profile, model));
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, email);
                if (!setEmailResult.Succeeded)
                {
                    AddIdentityErrors(setEmailResult);
                    return View(await PrepareInvalidModelAsync(user, profile, model));
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    AddIdentityErrors(setUserNameResult);
                    return View(await PrepareInvalidModelAsync(user, profile, model));
                }
            }

            var phone = model.PhoneNumber?.Trim();
            if (!string.Equals(user.PhoneNumber, phone, StringComparison.Ordinal))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, phone);
                if (!setPhoneResult.Succeeded)
                {
                    AddIdentityErrors(setPhoneResult);
                    return View(await PrepareInvalidModelAsync(user, profile, model));
                }
            }

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(string.Empty, "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif, .webp");
                    return View(await PrepareInvalidModelAsync(user, profile, model));
                }

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{user.Id}_{DateTime.Now.Ticks}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                if (!string.IsNullOrEmpty(profile.AvatarUrl) && profile.AvatarUrl.StartsWith("/uploads/avatars/"))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, profile.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch { /* Ignore delete errors */ }
                    }
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                profile.AvatarUrl = $"/uploads/avatars/{fileName}";
            }
            else
            {
                profile.AvatarUrl = model.AvatarUrl?.Trim();
            }
            profile.FullName = model.FullName?.Trim();

            bool isCustomerOrAdmin = true;
            if (isCustomerOrAdmin)
            {
                profile.ShippingAddress = model.ShippingAddress?.Trim();
                profile.BankAccountLink = model.BankAccountLink?.Trim();
                
                if (model.DisplayMembershipLevel.HasValue && model.DisplayMembershipLevel.Value <= profile.MembershipLevel)
                {
                    profile.DisplayMembershipLevel = model.DisplayMembershipLevel.Value;
                }
                else
                {
                    profile.DisplayMembershipLevel = profile.MembershipLevel;
                }

                if (!string.IsNullOrEmpty(model.EquippedAvatarFrame) && model.EquippedAvatarFrame != "none")
                {
                    if (model.EquippedAvatarFrame.StartsWith("tier-"))
                    {
                        if (int.TryParse(model.EquippedAvatarFrame.Substring(5), out var frameLevel) && frameLevel <= profile.MembershipLevel)
                        {
                            profile.EquippedAvatarFrame = model.EquippedAvatarFrame;
                        }
                        else
                        {
                            profile.EquippedAvatarFrame = "none";
                        }
                    }
                    else if (model.EquippedAvatarFrame.StartsWith("vip-moc-"))
                    {
                        profile.EquippedAvatarFrame = model.EquippedAvatarFrame;
                    }
                    else if (model.EquippedAvatarFrame.StartsWith("daily-normal-") || model.EquippedAvatarFrame.StartsWith("daily-limited-"))
                    {
                        bool owned = await _context.CustomerVouchers.AnyAsync(v =>
                            v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == model.EquippedAvatarFrame);
                        if (owned)
                        {
                            profile.EquippedAvatarFrame = model.EquippedAvatarFrame;
                        }
                        else
                        {
                            profile.EquippedAvatarFrame = "none";
                        }
                    }
                    else
                    {
                        profile.EquippedAvatarFrame = "none";
                    }
                }
                else
                {
                    profile.EquippedAvatarFrame = "none";
                }

                profile.EquippedBadge = model.EquippedBadge ?? "none";
            }

            profile.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);

            TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeTier()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            if (LoyaltyService.TryUpgrade(profile, out var message))
            {
                TempData["Success"] = message;
                TempData["ShowLevelUp"] = profile.MembershipLevel.ToString();
            }
            else
            {
                TempData["Error"] = message;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Edit));
        }

        private async Task<CustomerProfileViewModel> PrepareInvalidModelAsync(IdentityUser user, Models.CustomerProfile profile, CustomerProfileViewModel inputModel)
        {
            var fullModel = await GetProfileViewModelAsync(user, profile, null, 1, 1, 1);
            fullModel.FullName = inputModel.FullName;
            fullModel.Email = inputModel.Email;
            fullModel.PhoneNumber = inputModel.PhoneNumber;
            fullModel.ShippingAddress = inputModel.ShippingAddress;
            fullModel.BankAccountLink = inputModel.BankAccountLink;
            fullModel.DisplayMembershipLevel = inputModel.DisplayMembershipLevel;
            fullModel.EquippedAvatarFrame = inputModel.EquippedAvatarFrame;
            fullModel.EquippedBadge = inputModel.EquippedBadge;
            return fullModel;
        }

        private static CustomerProfileViewModel ToViewModel(IdentityUser user, Models.CustomerProfile profile)
        {
            var model = new CustomerProfileViewModel
            {
                Email = user.Email ?? user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = profile.AvatarUrl,
                FullName = profile.FullName,
                ShippingAddress = profile.ShippingAddress,
                BankAccountLink = profile.BankAccountLink,
                LoyaltyPoints = profile.LoyaltyPoints,
                MembershipLevel = profile.MembershipLevel,
                DisplayMembershipLevel = profile.DisplayMembershipLevel ?? profile.MembershipLevel,
                EquippedAvatarFrame = profile.EquippedAvatarFrame ?? "none",
                EquippedBadge = profile.EquippedBadge ?? "none",
                GoogleEmail = profile.GoogleEmail,
                FacebookName = profile.FacebookName
            };

            FillMembershipInfo(model, profile);
            return model;
        }

        private static void FillMembershipInfo(CustomerProfileViewModel model, Models.CustomerProfile profile)
        {
            model.LoyaltyPoints = profile.LoyaltyPoints;
            model.MembershipLevel = profile.MembershipLevel;
            model.MembershipTierName = LoyaltyService.GetTierName(profile.MembershipLevel);
            model.NextTierName = LoyaltyService.GetNextTierName(profile.MembershipLevel);
            model.UpgradeCost = LoyaltyService.GetUpgradeCost(profile.MembershipLevel);
            model.CanUpgrade = LoyaltyService.CanUpgrade(profile);

            model.AvailableDisplayLevels = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            for (int i = 0; i <= profile.MembershipLevel; i++)
            {
                model.AvailableDisplayLevels.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = i.ToString(),
                    Text = LoyaltyService.GetTierName(i)
                });
            }

            model.AvailableAvatarFrames = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "none", Text = "Không sử dụng" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "tier-0", Text = "Khung Sắt" }
            };
            for (int i = 1; i <= profile.MembershipLevel; i++)
            {
                var tierNameClean = LoyaltyService.GetTierName(i).Replace("Cấp ", "", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(tierNameClean))
                {
                    tierNameClean = char.ToUpper(tierNameClean[0]) + tierNameClean.Substring(1);
                }
                model.AvailableAvatarFrames.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = $"tier-{i}",
                    Text = $"Khung {tierNameClean}"
                });
            }
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private static readonly (string Key, string Title, string Req, int Pct)[] AchievementsDef = new[]
        {
            ("WELCOME", "Chào mừng quý khách", "Đăng nhập vào cửa hàng lần đầu tiên", 4),
            ("REACH_0", "Khởi Đầu Mua Sắm", "Đạt cấp Sắt", 8),
            ("REACH_1", "Khách Hàng Tiềm Năng", "Đạt cấp Đồng", 12),
            ("REACH_2", "Khách Hàng Thân Thiết", "Đạt cấp Bạc", 16),
            ("REACH_3", "Khách Hàng Ưu Tú", "Đạt cấp Vàng", 20),
            ("REACH_4", "Khách Hàng VIP", "Đạt cấp Bạch Kim", 24),
            ("REACH_5", "Đối Tác Tin Cậy", "Đạt cấp Lục Bảo", 28),
            ("REACH_6", "Khách Hàng Kim Cương", "Đạt cấp Kim Cương", 32),
            ("REACH_7", "Khách Hàng Danh Dự", "Đạt cấp Ruby", 36),
            ("REACH_8", "Nhà Sưu Tầm Cao Cấp", "Đạt cấp Niken", 40),
            ("REACH_9", "Khách Hàng Huyền Thoại", "Đạt cấp Titan", 44)
        };

        // Points cost for store vouchers by level
        private static readonly int[] LevelPointsCost = new[] { 500, 1500, 4500, 13500, 40500, 121500, 364500, 1093500, 3280500, 9841500 };

        // Points cost to exchange avatar frames (index = tier level 0..11)
        private static readonly int[] FrameCosts = new[] { 0, 200, 400, 600, 1000, 1500, 2000, 3000, 4000, 5000, 7500, 10000 };

        // VIP metadata: (Level, Key, Name, Cost, ReqLevel, ReqTier, FrameFile, BadgeFile, BadgeName)
        private static readonly (int Level, string Key, string Name, int Cost, int ReqLevel, string ReqTier, string FrameFile, string BadgeFile, string BadgeName)[] VipMeta = new[]
        {
            (1, "vip-1", "Gói VIP 1", 100,     2,  "Cấp bạc",      "khung-vip-moc-1.png",  "thich-thi-nap.png",       "Thích Thì Nạp"),
            (2, "vip-2", "Gói VIP 2", 2333,    5,  "Cấp lục bảo",  "khung-vip-moc-2.webp", "giau-nut-vach.png",       "Giàu Nứt Vách"),
            (3, "vip-3", "Gói VIP 3", 100000,  8,  "Cấp niken",    "khung-vip-moc-3.webp", "vung-tien-nhu-nuoc.png",  "Vung Tiền Như Nước"),
            (4, "vip-4", "Gói VIP 4", 2333333, 11, "Cấp iridium",  "khung-vip-moc-4.webp", "nha-co-mo.png",           "Nhà Có Mỏ")
        };

        // Frame metadata: (fileName, filterStyle, displayName)
        private static readonly (string File, string Filter, string Name)[] FrameMeta = new[]
        {
            ("khung-sat.webp",    "",                                              "Khung Sắt"),
            ("khung-dong.webp",   "",                                              "Khung Đồng"),
            ("khung-sat.webp",    "filter:brightness(1.2) contrast(1.4) saturate(0);", "Khung Bạc"),
            ("khung-vang.webp",   "",                                              "Khung Vàng"),
            ("khung-bach-kim.webp","",                                             "Khung Bạch Kim"),
            ("khung-luc-bao.png", "filter:drop-shadow(0 0 4px #00ffd0);",          "Khung Lục Bảo"),
            ("khung-kim-cuong.webp","filter:drop-shadow(0 0 4px #a1c4fd);",        "Khung Kim Cương"),
            ("khung-ruby.png",    "filter:drop-shadow(0 0 4px #ff0844);",          "Khung Ruby"),
            ("khung-niken.png",   "",                                              "Khung Niken"),
            ("khung-titan.png",   "",                                              "Khung Titan"),
            ("khung-uranium.png", "filter:drop-shadow(0 0 5px #39FF14);",          "Khung Uranium"),
            ("khung-iridium.png", "filter:drop-shadow(0 0 6px #C77DFF) drop-shadow(0 0 8px #E040FB);", "Khung Iridium")
        };

        private class DailyFrameItem
        {
            public string FileName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Cost { get; set; }
            public bool IsLimited { get; set; }
        }

        private static readonly List<DailyFrameItem> MasterNormalFrames = new()
        {
            new() { FileName = "khung-thuong-01.webp", Name = "Khung Thường 01", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-02.png", Name = "Khung Thường 02", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-03.webp", Name = "Khung Thường 03", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-04.png", Name = "Khung Thường 04", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-05.png", Name = "Khung Thường 05", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-06.webp", Name = "Khung Thường 06", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-07.webp", Name = "Khung Thường 07", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-08.png", Name = "Khung Thường 08", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-09.png", Name = "Khung Thường 09", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-10.webp", Name = "Khung Thường 10", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-11.webp", Name = "Khung Thường 11", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-12.png", Name = "Khung Thường 12", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-13.png", Name = "Khung Thường 13", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-14.webp", Name = "Khung Thường 14", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-15.webp", Name = "Khung Thường 15", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-16.webp", Name = "Khung Thường 16", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-17.webp", Name = "Khung Thường 17", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-18.webp", Name = "Khung Thường 18", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-19.webp", Name = "Khung Thường 19", Cost = 300, IsLimited = false },
            new() { FileName = "khung-thuong-20.webp", Name = "Khung Thường 20", Cost = 300, IsLimited = false }
        };

        private static readonly List<DailyFrameItem> MasterLimitedFrames = new()
        {
            new() { FileName = "khung 01.webp", Name = "Khung Giới Hạn 01", Cost = 400, IsLimited = true },
            new() { FileName = "khung 02.png", Name = "Khung Giới Hạn 02", Cost = 400, IsLimited = true },
            new() { FileName = "khung 03.png", Name = "Khung Giới Hạn 03", Cost = 400, IsLimited = true },
            new() { FileName = "khung 04.png", Name = "Khung Giới Hạn 04", Cost = 400, IsLimited = true },
            new() { FileName = "khung 05.png", Name = "Khung Giới Hạn 05", Cost = 400, IsLimited = true },
            new() { FileName = "khung 06.png", Name = "Khung Giới Hạn 06", Cost = 400, IsLimited = true },
            new() { FileName = "khung 07.webp", Name = "Khung Giới Hạn 07", Cost = 400, IsLimited = true },
            new() { FileName = "khung 08.png", Name = "Khung Giới Hạn 08", Cost = 400, IsLimited = true },
            new() { FileName = "khung 09.png", Name = "Khung Giới Hạn 09", Cost = 400, IsLimited = true },
            new() { FileName = "khung 10.webp", Name = "Khung Giới Hạn 10", Cost = 400, IsLimited = true },
            new() { FileName = "khung 11.webp", Name = "Khung Giới Hạn 11", Cost = 400, IsLimited = true },
            new() { FileName = "khung 12.webp", Name = "Khung Giới Hạn 12", Cost = 400, IsLimited = true }
        };

        private string GenerateDailyFramesJson(string userId, DayOfWeek dayOfWeek)
        {
            var ownedFrameKeys = _context.CustomerVouchers
                .Where(v => v.UserId == userId && v.Type == "AvatarFrame")
                .Select(v => v.Key)
                .ToHashSet();

            var random = new Random();
            var selectedFrames = new List<DailyFrameItem>();

            var unownedNormal = MasterNormalFrames.Where(f => !ownedFrameKeys.Contains($"daily-normal-{f.FileName}")).ToList();
            var ownedNormal = MasterNormalFrames.Where(f => ownedFrameKeys.Contains($"daily-normal-{f.FileName}")).ToList();

            var unownedLimited = MasterLimitedFrames.Where(f => !ownedFrameKeys.Contains($"daily-limited-{f.FileName}")).ToList();
            var ownedLimited = MasterLimitedFrames.Where(f => ownedFrameKeys.Contains($"daily-limited-{f.FileName}")).ToList();

            if (dayOfWeek == DayOfWeek.Sunday)
            {
                var selectedNormal = new List<DailyFrameItem>();
                var tempUnownedNormal = new List<DailyFrameItem>(unownedNormal);
                while (selectedNormal.Count < 6 && tempUnownedNormal.Count > 0)
                {
                    int idx = random.Next(tempUnownedNormal.Count);
                    selectedNormal.Add(tempUnownedNormal[idx]);
                    tempUnownedNormal.RemoveAt(idx);
                }
                var tempOwnedNormal = new List<DailyFrameItem>(ownedNormal);
                while (selectedNormal.Count < 6 && tempOwnedNormal.Count > 0)
                {
                    int idx = random.Next(tempOwnedNormal.Count);
                    selectedNormal.Add(tempOwnedNormal[idx]);
                    tempOwnedNormal.RemoveAt(idx);
                }
                selectedFrames.AddRange(selectedNormal);

                var selectedLimited = new List<DailyFrameItem>();
                var tempUnownedLimited = new List<DailyFrameItem>(unownedLimited);
                while (selectedLimited.Count < 2 && tempUnownedLimited.Count > 0)
                {
                    int idx = random.Next(tempUnownedLimited.Count);
                    selectedLimited.Add(tempUnownedLimited[idx]);
                    tempUnownedLimited.RemoveAt(idx);
                }
                var tempOwnedLimited = new List<DailyFrameItem>(ownedLimited);
                while (selectedLimited.Count < 2 && tempOwnedLimited.Count > 0)
                {
                    int idx = random.Next(tempOwnedLimited.Count);
                    selectedLimited.Add(tempOwnedLimited[idx]);
                    tempOwnedLimited.RemoveAt(idx);
                }
                selectedFrames.AddRange(selectedLimited);
            }
            else
            {
                var selectedNormal = new List<DailyFrameItem>();
                var tempUnownedNormal = new List<DailyFrameItem>(unownedNormal);
                while (selectedNormal.Count < 8 && tempUnownedNormal.Count > 0)
                {
                    int idx = random.Next(tempUnownedNormal.Count);
                    selectedNormal.Add(tempUnownedNormal[idx]);
                    tempUnownedNormal.RemoveAt(idx);
                }
                var tempOwnedNormal = new List<DailyFrameItem>(ownedNormal);
                while (selectedNormal.Count < 8 && tempOwnedNormal.Count > 0)
                {
                    int idx = random.Next(tempOwnedNormal.Count);
                    selectedNormal.Add(tempOwnedNormal[idx]);
                    tempOwnedNormal.RemoveAt(idx);
                }
                selectedFrames.AddRange(selectedNormal);
            }

            var listToSerialize = selectedFrames.Select(f => new DailyFrameSaveModel
            {
                FileName = f.FileName,
                IsLimited = f.IsLimited
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(listToSerialize);
        }

        private class DailyFrameSaveModel
        {
            public string FileName { get; set; } = string.Empty;
            public bool IsLimited { get; set; }
        }

        private async Task<CustomerProfileViewModel> GetProfileViewModelAsync(IdentityUser user, Models.CustomerProfile profile, string? orderSearch = null, int orderPage = 1, int voucherPage = 1, int chatPage = 1)
        {
            var model = ToViewModel(user, profile);
            var roles = await _userManager.GetRolesAsync(user);
            model.RoleName = roles.FirstOrDefault();

            var existingVouchers = await _context.CustomerVouchers
                .Where(v => v.UserId == user.Id)
                .ToListAsync();

            var random = new Random();
            bool hasChanges = false;

            // Auto-reset daily frames if new day (or first time)
            var vnNow = PromotionService.GetVietnamNow();
            var today = vnNow.Date;
            bool forceReset = false;

            if (today.DayOfWeek != DayOfWeek.Sunday && !string.IsNullOrEmpty(profile.DailyFramesJson))
            {
                try
                {
                    var existingFrames = System.Text.Json.JsonSerializer.Deserialize<List<DailyFrameSaveModel>>(profile.DailyFramesJson);
                    if (existingFrames != null && existingFrames.Any(f => f.IsLimited))
                    {
                        forceReset = true;
                    }
                }
                catch { }
            }

            if (profile.DailyFramesLastResetDate == null || profile.DailyFramesLastResetDate.Value.Date < today || forceReset)
            {
                profile.DailyFramesJson = GenerateDailyFramesJson(user.Id, today.DayOfWeek);
                profile.DailyFrameResetsUsed = 0;
                profile.DailyFramesLastResetDate = vnNow;
                hasChanges = true;
            }

            foreach (var def in AchievementsDef)
            {
                bool isUnlocked = false;
                if (def.Key == "WELCOME")
                {
                    isUnlocked = true;
                }
                else
                {
                    int requiredLevel = int.Parse(def.Key.Substring(6));
                    isUnlocked = profile.MembershipLevel >= requiredLevel;
                }

                if (isUnlocked)
                {
                    var cv = existingVouchers.FirstOrDefault(v => v.Type == "Achievement" && v.Key == def.Key);
                    if (cv == null)
                    {
                        var suffix = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 5)
                            .Select(s => s[random.Next(s.Length)]).ToArray());
                        var code = $"{def.Key.Replace("REACH_", "REACH")}_{def.Pct}PCT_{suffix}";

                        var discount = new Discount
                        {
                            Code = code,
                            DiscountValue = def.Pct,
                            MinOrderValue = 0,
                            MaxDiscount = 0,
                            StartDate = DateTime.Today.AddDays(-1),
                            EndDate = DateTime.Today.AddMonths(1),
                            Quantity = 1,
                            IsSee = true,
                            UserId = user.Id
                        };
                        _context.Discounts.Add(discount);

                        cv = new CustomerVoucher
                        {
                            UserId = user.Id,
                            Type = "Achievement",
                            Key = def.Key,
                            VoucherCode = code,
                            DiscountValue = def.Pct,
                            UnlockedAt = DateTime.Now
                        };
                        _context.CustomerVouchers.Add(cv);
                        existingVouchers.Add(cv);
                        hasChanges = true;
                    }

                    model.AchievementItems.Add(new AchievementItemViewModel
                    {
                        Key = def.Key,
                        Title = def.Title,
                        Requirement = def.Req,
                        DiscountValue = def.Pct,
                        IsUnlocked = true,
                        GeneratedCode = cv.VoucherCode
                    });
                }
                else
                {
                    model.AchievementItems.Add(new AchievementItemViewModel
                    {
                        Key = def.Key,
                        Title = def.Title,
                        Requirement = def.Req,
                        DiscountValue = def.Pct,
                        IsUnlocked = false,
                        GeneratedCode = null
                    });
                }
            }

            for (int lvl = 0; lvl <= 9; lvl++)
            {
                int points = LevelPointsCost[lvl];
                int pct = 5 * (lvl + 1);

                // Voucher A: Không giới hạn
                {
                    string key = $"STORE_{lvl}_A";
                    var cv = existingVouchers.FirstOrDefault(v => v.Type == "Store" && v.Key == key);
                    bool isExchanged = cv != null;
                    string? code = cv?.VoucherCode;
                    bool canExchange = !isExchanged && profile.LoyaltyPoints >= points && profile.MembershipLevel >= lvl;

                    model.VoucherStoreItems.Add(new VoucherStoreItemViewModel
                    {
                        Level = lvl,
                        Suffix = "A",
                        Title = $"Mã giảm giá {pct}% không giới hạn",
                        TierName = LoyaltyService.GetTierName(lvl),
                        RequiredPoints = points,
                        DiscountValue = pct,
                        MinOrder = 0,
                        IsExchanged = isExchanged,
                        GeneratedCode = code,
                        CanExchange = canExchange
                    });
                }

                // Voucher B: Đơn hàng từ 150K
                {
                    string key = $"STORE_{lvl}_B";
                    var cv = existingVouchers.FirstOrDefault(v => v.Type == "Store" && v.Key == key);
                    bool isExchanged = cv != null;
                    string? code = cv?.VoucherCode;
                    bool canExchange = !isExchanged && profile.LoyaltyPoints >= points && profile.MembershipLevel >= lvl;

                    model.VoucherStoreItems.Add(new VoucherStoreItemViewModel
                    {
                        Level = lvl,
                        Suffix = "B",
                        Title = $"Mã giảm giá {pct}% (Đơn hàng từ 150.000đ)",
                        TierName = LoyaltyService.GetTierName(lvl),
                        RequiredPoints = points,
                        DiscountValue = pct,
                        MinOrder = 150000,
                        IsExchanged = isExchanged,
                        GeneratedCode = code,
                        CanExchange = canExchange
                    });
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            // 1. Populate Daily Vouchers
            var dailyVouchers = GetDailyVouchersForDate(PromotionService.GetVietnamNow().Date);
            var vnNowDaily = PromotionService.GetVietnamNow();
            var todayDaily = vnNowDaily.Date;

            foreach (var item in dailyVouchers)
            {
                var cv = existingVouchers.FirstOrDefault(v => v.Type == "Daily" && v.Key == item.Key);
                bool isExchanged = cv != null;
                string? code = cv?.VoucherCode;

                bool isActiveNow = false;
                string? statusText = null;
                int count = 0;

                if (item.IsFreeShip)
                {
                    var dayOfWeek = vnNowDaily.DayOfWeek;
                    if (dayOfWeek != DayOfWeek.Friday && dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                    {
                        isActiveNow = false;
                        statusText = "Mở bán vào T6, T7, CN hàng tuần";
                    }
                    else
                    {
                        var session1Start = todayDaily.Add(new TimeSpan(8, 30, 0));
                        var session1End = todayDaily.Add(new TimeSpan(9, 30, 0));
                        var session2Start = todayDaily.Add(new TimeSpan(18, 30, 0));
                        var session2End = todayDaily.Add(new TimeSpan(19, 30, 0));

                        if (vnNowDaily < session1Start)
                        {
                            isActiveNow = false;
                            statusText = "SẮP MỞ BÁN ĐỢT 1 (Lúc 08:30 hôm nay)";
                        }
                        else if (vnNowDaily >= session1Start && vnNowDaily < session1End)
                        {
                            int countS1 = await _context.CustomerVouchers.CountAsync(v =>
                                v.Type == "Daily" && v.Key == item.Key && v.UnlockedAt >= session1Start && v.UnlockedAt < session1End);
                            count = countS1;
                            if (countS1 < 100)
                            {
                                isActiveNow = true;
                                statusText = $"ĐANG MỞ BÁN ĐỢT 1 (Còn lại {100 - countS1}/100)";
                            }
                            else
                            {
                                isActiveNow = false;
                                statusText = "ĐỢT 1 ĐÃ HẾT LƯỢT (Đã bán hết 100 mã)";
                            }
                        }
                        else if (vnNowDaily >= session1End && vnNowDaily < session2Start)
                        {
                            isActiveNow = false;
                            statusText = "SẮP MỞ BÁN ĐỢT 2 (Lúc 18:30 hôm nay)";
                        }
                        else if (vnNowDaily >= session2Start && vnNowDaily < session2End)
                        {
                            int countS2 = await _context.CustomerVouchers.CountAsync(v =>
                                v.Type == "Daily" && v.Key == item.Key && v.UnlockedAt >= session2Start && v.UnlockedAt < session2End);
                            count = countS2;
                            if (countS2 < 100)
                            {
                                isActiveNow = true;
                                statusText = $"ĐANG MỞ BÁN ĐỢT 2 (Còn lại {100 - countS2}/100)";
                            }
                            else
                            {
                                isActiveNow = false;
                                statusText = "ĐỢT 2 ĐÃ HẾT LƯỢT (Đã bán hết 100 mã)";
                            }
                        }
                        else
                        {
                            isActiveNow = false;
                            statusText = "ĐÃ HẾT GIỜ MỞ BÁN HÔM NAY";
                        }
                    }
                }
                else
                {
                    isActiveNow = true;
                }

                bool canExchange = !isExchanged && isActiveNow && profile.LoyaltyPoints >= item.RequiredPoints;

                model.DailyVouchers.Add(new DailyVoucherItemViewModel
                {
                    Index = item.Index,
                    Key = item.Key,
                    Title = item.Title,
                    RequiredPoints = item.RequiredPoints,
                    DiscountValue = item.DiscountValue,
                    MinOrder = item.MinOrder,
                    IsExchanged = isExchanged,
                    GeneratedCode = code,
                    CanExchange = canExchange,
                    IsFreeShip = item.IsFreeShip,
                    StartHour = item.StartHour,
                    IsActiveNow = isActiveNow,
                    FreeShipStatusText = statusText,
                    FreeShipSessionCount = count
                });
            }

            // 2. Populate Owned Vouchers
            var ownedDiscounts = await _context.Discounts
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            foreach (var d in ownedDiscounts)
            {
                var cv = existingVouchers.FirstOrDefault(v => v.VoucherCode == d.Code);
                string title = "";
                string sourceType = "Khác";

                if (cv != null)
                {
                    sourceType = cv.Type;
                    if (cv.Type == "Achievement")
                    {
                        var def = AchievementsDef.FirstOrDefault(a => a.Key == cv.Key);
                        title = def.Title != null ? $"Thành tích: {def.Title}" : "Thành tích độc quyền";
                    }
                    else if (cv.Type == "Store")
                    {
                        title = $"Cửa hàng cấp: {LoyaltyService.GetTierName(int.Parse(cv.Key.Split('_')[1]))}";
                    }
                    else if (cv.Type == "Daily")
                    {
                        title = "Cửa hàng: Mã hàng ngày";
                    }
                }
                else
                {
                    title = "Mã giảm giá cá nhân";
                }

                model.OwnedVouchers.Add(new OwnedVoucherViewModel
                {
                    Code = d.Code,
                    DiscountValue = (int)d.DiscountValue,
                    MinOrderValue = d.MinOrderValue,
                    Quantity = d.Quantity,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    SourceType = sourceType,
                    Title = title
                });
            }

            int voucherPageSize = 9;
            int totalVoucherItems = model.OwnedVouchers.Count;
            int totalVoucherPages = (int)Math.Ceiling((double)totalVoucherItems / voucherPageSize);
            if (totalVoucherPages < 1) totalVoucherPages = 1;
            if (voucherPage < 1) voucherPage = 1;
            if (voucherPage > totalVoucherPages) voucherPage = totalVoucherPages;

            model.VoucherPage = voucherPage;
            model.VoucherTotalPages = totalVoucherPages;
            model.OwnedVouchers = model.OwnedVouchers.Skip((voucherPage - 1) * voucherPageSize).Take(voucherPageSize).ToList();

            // 3. Populate Frame Store items and Owned Frames
            var ownedFrameKeys = existingVouchers
                .Where(v => v.Type == "AvatarFrame")
                .Select(v => v.Key)
                .ToHashSet();

            string equippedFrame = profile.EquippedAvatarFrame ?? "none";

            for (int fi = 0; fi < FrameMeta.Length; fi++)
            {
                string fKey = $"tier-{fi}";
                bool isOwned = ownedFrameKeys.Contains(fKey) || fi == 0; // iron frame is free
                bool canBuy = !isOwned && profile.MembershipLevel >= fi && profile.LoyaltyPoints >= FrameCosts[fi];
                bool requiresTier = !isOwned && profile.MembershipLevel < fi;

                model.FrameStoreItems.Add(new FrameStoreItemViewModel
                {
                    Level = fi,
                    Key = fKey,
                    Name = FrameMeta[fi].Name,
                    FileName = FrameMeta[fi].File,
                    FilterStyle = FrameMeta[fi].Filter,
                    Cost = FrameCosts[fi],
                    IsOwned = isOwned,
                    CanBuy = canBuy,
                    RequiresTier = requiresTier,
                    TierName = LoyaltyService.GetTierName(fi)
                });

                if (isOwned)
                {
                    model.OwnedAvatarFrames.Add(new OwnedAvatarFrameViewModel
                    {
                        Level = fi,
                        Key = fKey,
                        Name = FrameMeta[fi].Name,
                        FileName = FrameMeta[fi].File,
                        FilterStyle = FrameMeta[fi].Filter,
                        IsEquipped = equippedFrame == fKey
                    });
                }
            }

            // Populate Daily Frames from JSON
            List<DailyFrameSaveModel> dailyFrameSaves = new();
            if (!string.IsNullOrEmpty(profile.DailyFramesJson))
            {
                try
                {
                    dailyFrameSaves = System.Text.Json.JsonSerializer.Deserialize<List<DailyFrameSaveModel>>(profile.DailyFramesJson) ?? new();
                }
                catch
                {
                    // If JSON is invalid, regenerate
                    var vnNowRegen = PromotionService.GetVietnamNow();
                    profile.DailyFramesJson = GenerateDailyFramesJson(user.Id, vnNowRegen.DayOfWeek);
                    profile.DailyFrameResetsUsed = 0;
                    profile.DailyFramesLastResetDate = vnNowRegen;
                    try
                    {
                        dailyFrameSaves = System.Text.Json.JsonSerializer.Deserialize<List<DailyFrameSaveModel>>(profile.DailyFramesJson) ?? new();
                    }
                    catch { }
                }
            }

            foreach (var dfs in dailyFrameSaves)
            {
                var fKey = dfs.IsLimited ? $"daily-limited-{dfs.FileName}" : $"daily-normal-{dfs.FileName}";
                bool isOwned = ownedFrameKeys.Contains(fKey);
                int cost = dfs.IsLimited ? 400 : 300;
                bool canBuy = !isOwned && profile.LoyaltyPoints >= cost;

                string name = "";
                if (dfs.IsLimited)
                {
                    name = MasterLimitedFrames.FirstOrDefault(f => f.FileName == dfs.FileName)?.Name ?? "Khung Giới Hạn";
                }
                else
                {
                    name = MasterNormalFrames.FirstOrDefault(f => f.FileName == dfs.FileName)?.Name ?? "Khung Hàng Ngày";
                }

                model.DailyFrames.Add(new DailyFrameItemViewModel
                {
                    Key = fKey,
                    Name = name,
                    FileName = dfs.FileName,
                    IsLimited = dfs.IsLimited,
                    Cost = cost,
                    IsOwned = isOwned,
                    CanBuy = canBuy
                });
            }

            model.DailyFrameResetsUsed = profile.DailyFrameResetsUsed;
            model.NextResetCost = profile.DailyFrameResetsUsed switch
            {
                0 => 0,
                1 => 0,
                2 => 0,
                3 => 10,
                4 => 15,
                5 => 20,
                _ => -1
            };

            int orderPageSize = 5;
            var ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == user.Id);

            string searchClean = orderSearch?.Trim().Replace("#", "") ?? "";
            if (!string.IsNullOrEmpty(searchClean))
            {
                ordersQuery = ordersQuery.Where(o => o.Id.ToString().Contains(searchClean));
            }

            var allOrders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            int totalOrderItems = allOrders.Count;
            int totalOrderPages = (int)Math.Ceiling((double)totalOrderItems / orderPageSize);
            if (totalOrderPages < 1) totalOrderPages = 1;
            if (orderPage < 1) orderPage = 1;
            if (orderPage > totalOrderPages) orderPage = totalOrderPages;

            model.OrderSearch = orderSearch;
            model.OrderPage = orderPage;
            model.OrderTotalPages = totalOrderPages;
            model.OrderHistory = allOrders.Skip((orderPage - 1) * orderPageSize).Take(orderPageSize).ToList();

            int chatPageSize = 5;
            var allChatHistories = await _context.ChatHistories
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            int totalChatItems = allChatHistories.Count;
            int totalChatPages = (int)Math.Ceiling((double)totalChatItems / chatPageSize);
            if (totalChatPages < 1) totalChatPages = 1;
            if (chatPage < 1) chatPage = 1;
            if (chatPage > totalChatPages) chatPage = totalChatPages;

            model.ChatPage = chatPage;
            model.ChatTotalPages = totalChatPages;
            model.ChatHistories = allChatHistories.Skip((chatPage - 1) * chatPageSize).Take(chatPageSize).ToList();

            await PopulateVipAndBadgesAsync(user.Id, profile, model);
            return model;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeAvatarFrame(int level)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            if (level < 0 || level >= FrameMeta.Length)
            {
                TempData["Error"] = "Khung avatar không hợp lệ.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);
            string key = $"tier-{level}";
            int cost = FrameCosts[level];

            bool alreadyOwned = await _context.CustomerVouchers.AnyAsync(v =>
                v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == key);
            if (alreadyOwned)
            {
                TempData["Error"] = "Bạn đã sở hữu khung avatar này rồi.";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.MembershipLevel < level)
            {
                TempData["Error"] = $"Bạn chưa đạt cấp {LoyaltyService.GetTierName(level)} để đổi khung này.";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.LoyaltyPoints < cost)
            {
                TempData["Error"] = $"Bạn không đủ điểm để đổi khung này (Cần {cost:N0} điểm).";
                return RedirectToAction(nameof(Edit));
            }

            profile.LoyaltyPoints -= cost;
            profile.UpdatedAt = DateTime.Now;

            _context.CustomerVouchers.Add(new CustomerVoucher
            {
                UserId = user.Id,
                Type = "AvatarFrame",
                Key = key,
                VoucherCode = "UNLOCKED",
                DiscountValue = 0,
                UnlockedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["FrameSuccess"] = $"Đã đổi thành công {FrameMeta[level].Name}! Vào Túi đồ cá nhân để trang bị.";
            TempData["ActiveSection"] = "store";
            TempData["ActiveSubStore"] = "frame";
            TempData["ActiveSubFrameStore"] = "tier";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquipAvatarFrameDirect(string frameId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            if (frameId == "none")
            {
                profile.EquippedAvatarFrame = "none";
            }
            else if (frameId.StartsWith("tier-") &&
                     int.TryParse(frameId.Substring(5), out var frameLevel) &&
                     frameLevel < FrameMeta.Length)
            {
                // Only allow equipping if they own it (bought it or earned via membership)
                bool owned = await _context.CustomerVouchers.AnyAsync(v =>
                    v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == frameId);
                // Allow tier-0 (iron) for free for everyone
                if (!owned && frameLevel > 0)
                {
                    TempData["Error"] = "Bạn chưa sở hữu khung avatar này.";
                    TempData["ActiveSection"] = "inventory";
                    TempData["ActiveSubInventory"] = "frame";
                    return RedirectToAction(nameof(Edit));
                }

                profile.EquippedAvatarFrame = frameId;
            }
            else if (frameId.StartsWith("vip-moc-"))
            {
                bool owned = await _context.CustomerVouchers.AnyAsync(v =>
                    v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == frameId);
                if (!owned)
                {
                    TempData["Error"] = "Bạn chưa sở hữu khung avatar này.";
                    TempData["ActiveSection"] = "inventory";
                    TempData["ActiveSubInventory"] = "frame";
                    return RedirectToAction(nameof(Edit));
                }

                profile.EquippedAvatarFrame = frameId;
            }
            else if (frameId.StartsWith("daily-normal-") || frameId.StartsWith("daily-limited-"))
            {
                bool owned = await _context.CustomerVouchers.AnyAsync(v =>
                    v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == frameId);
                if (!owned)
                {
                    TempData["Error"] = "Bạn chưa sở hữu khung avatar này.";
                    TempData["ActiveSection"] = "inventory";
                    TempData["ActiveSubInventory"] = "frame";
                    return RedirectToAction(nameof(Edit));
                }

                profile.EquippedAvatarFrame = frameId;
            }
            else
            {
                TempData["Error"] = "Khung avatar không hợp lệ.";
                return RedirectToAction(nameof(Edit));
            }

            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            string frameName = "Khung";
            if (frameId == "none") frameName = "Không sử dụng";
            else if (frameId.StartsWith("tier-")) frameName = FrameMeta[int.Parse(frameId.Substring(5))].Name;
            else if (frameId.StartsWith("vip-moc-")) frameName = $"Khung VIP {frameId.Substring(8)}";
            else if (frameId.StartsWith("daily-normal-"))
            {
                var fileName = frameId.Substring("daily-normal-".Length);
                frameName = MasterNormalFrames.FirstOrDefault(f => f.FileName == fileName)?.Name ?? "Khung Hàng Ngày";
            }
            else if (frameId.StartsWith("daily-limited-"))
            {
                var fileName = frameId.Substring("daily-limited-".Length);
                frameName = MasterLimitedFrames.FirstOrDefault(f => f.FileName == fileName)?.Name ?? "Khung Giới Hạn";
            }

            TempData["FrameEquipSuccess"] = frameId == "none"
                ? "Đã tháo khung avatar."
                : $"Đã trang bị {frameName} thành công!";
            TempData["ActiveSection"] = "inventory";
            TempData["ActiveSubInventory"] = "frame";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshDailyFrames()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            if (profile.DailyFrameResetsUsed >= 6)
            {
                TempData["Error"] = "Bạn đã sử dụng hết lượt đổi khung trong ngày hôm nay.";
                TempData["ActiveSection"] = "store";
                TempData["ActiveSubStore"] = "frame";
                TempData["ActiveSubFrameStore"] = "daily";
                return RedirectToAction(nameof(Edit));
            }

            int cost = profile.DailyFrameResetsUsed switch
            {
                0 => 0,
                1 => 0,
                2 => 0,
                3 => 10,
                4 => 15,
                5 => 20,
                _ => 0
            };

            if (cost > 0 && profile.LoyaltyPoints < cost)
            {
                TempData["Error"] = $"Bạn không đủ điểm tích lũy để làm mới (Cần {cost} điểm).";
                TempData["ActiveSection"] = "store";
                TempData["ActiveSubStore"] = "frame";
                TempData["ActiveSubFrameStore"] = "daily";
                return RedirectToAction(nameof(Edit));
            }

            if (cost > 0)
            {
                profile.LoyaltyPoints -= cost;
            }

            var vnNowRefresh = PromotionService.GetVietnamNow();
            profile.DailyFrameResetsUsed++;
            profile.DailyFramesJson = GenerateDailyFramesJson(user.Id, vnNowRefresh.DayOfWeek);
            profile.DailyFramesLastResetDate = vnNowRefresh;
            profile.UpdatedAt = vnNowRefresh;

            await _context.SaveChangesAsync();

            TempData["FrameSuccess"] = "Đã làm mới danh sách khung avatar hàng ngày thành công!";
            TempData["ActiveSection"] = "store";
            TempData["ActiveSubStore"] = "frame";
            TempData["ActiveSubFrameStore"] = "daily";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeDailyFrame(string key)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            List<DailyFrameSaveModel> dailyFrameSaves = new();
            if (!string.IsNullOrEmpty(profile.DailyFramesJson))
            {
                try
                {
                    dailyFrameSaves = System.Text.Json.JsonSerializer.Deserialize<List<DailyFrameSaveModel>>(profile.DailyFramesJson) ?? new();
                }
                catch { }
            }

            var dailyFrame = dailyFrameSaves.FirstOrDefault(f => 
                (f.IsLimited ? $"daily-limited-{f.FileName}" : $"daily-normal-{f.FileName}") == key);

            if (dailyFrame == null)
            {
                TempData["Error"] = "Khung này không nằm trong danh sách cửa hàng của bạn hôm nay.";
                TempData["ActiveSection"] = "store";
                TempData["ActiveSubStore"] = "frame";
                TempData["ActiveSubFrameStore"] = "daily";
                return RedirectToAction(nameof(Edit));
            }

            int cost = dailyFrame.IsLimited ? 400 : 300;

            bool alreadyOwned = await _context.CustomerVouchers.AnyAsync(v =>
                v.UserId == user.Id && v.Type == "AvatarFrame" && v.Key == key);

            if (alreadyOwned)
            {
                TempData["Error"] = "Bạn đã sở hữu khung avatar này rồi.";
                TempData["ActiveSection"] = "store";
                TempData["ActiveSubStore"] = "frame";
                TempData["ActiveSubFrameStore"] = "daily";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.LoyaltyPoints < cost)
            {
                TempData["Error"] = $"Bạn không đủ điểm để đổi khung này (Cần {cost:N0} điểm).";
                TempData["ActiveSection"] = "store";
                TempData["ActiveSubStore"] = "frame";
                TempData["ActiveSubFrameStore"] = "daily";
                return RedirectToAction(nameof(Edit));
            }

            profile.LoyaltyPoints -= cost;
            profile.UpdatedAt = DateTime.Now;

            _context.CustomerVouchers.Add(new CustomerVoucher
            {
                UserId = user.Id,
                Type = "AvatarFrame",
                Key = key,
                VoucherCode = "UNLOCKED",
                DiscountValue = 0,
                UnlockedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            string displayName = dailyFrame.IsLimited
                ? (MasterLimitedFrames.FirstOrDefault(f => f.FileName == dailyFrame.FileName)?.Name ?? "Khung Giới Hạn")
                : (MasterNormalFrames.FirstOrDefault(f => f.FileName == dailyFrame.FileName)?.Name ?? "Khung Hàng Ngày");

            TempData["FrameSuccess"] = $"Đã đổi thành công {displayName}! Vào Túi đồ cá nhân để trang bị.";
            TempData["ActiveSection"] = "store";
            TempData["ActiveSubStore"] = "frame";
            TempData["ActiveSubFrameStore"] = "daily";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeVoucher(int level, string suffix)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            if (level < 0 || level > 9 || (suffix != "A" && suffix != "B"))
            {
                TempData["Error"] = "Cấp độ hoặc loại mã giảm giá không hợp lệ.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            int points = LevelPointsCost[level];
            int pct = 5 * (level + 1);
            decimal minOrder = suffix == "A" ? 0m : 150000m;
            string key = $"STORE_{level}_{suffix}";

            var alreadyExchanged = await _context.CustomerVouchers.AnyAsync(v =>
                v.UserId == user.Id && v.Type == "Store" && v.Key == key);
            if (alreadyExchanged)
            {
                TempData["Error"] = $"Bạn đã đổi mã giảm giá này rồi.";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.LoyaltyPoints < points)
            {
                TempData["Error"] = $"Bạn không đủ điểm để đổi mã này (Cần {points:N0} điểm).";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.MembershipLevel < level)
            {
                TempData["Error"] = $"Bạn chưa đạt cấp {LoyaltyService.GetTierName(level)} để đổi mã này.";
                return RedirectToAction(nameof(Edit));
            }

            profile.LoyaltyPoints -= points;
            profile.UpdatedAt = DateTime.Now;

            var random = new Random();
            var randomSuffix = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            var code = $"STORE_LV{level}{suffix}_{pct}PCT_{randomSuffix}";

            var discount = new Discount
            {
                Code = code,
                DiscountValue = pct,
                MinOrderValue = minOrder,
                MaxDiscount = 0,
                StartDate = DateTime.Today.AddDays(-1),
                EndDate = DateTime.Today.AddDays(14),
                Quantity = 1,
                IsSee = true,
                UserId = user.Id
            };
            _context.Discounts.Add(discount);

            var cv = new CustomerVoucher
            {
                UserId = user.Id,
                Type = "Store",
                Key = key,
                VoucherCode = code,
                DiscountValue = pct,
                UnlockedAt = DateTime.Now
            };
            _context.CustomerVouchers.Add(cv);

            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Đã đổi thành công {discount.Code} bằng {points} điểm.";
            TempData["ActiveSection"] = "store";
            TempData["ActiveSubStore"] = "tier";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeDailyVoucher(int index)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var vnNowExchange = PromotionService.GetVietnamNow();
            var todayExchange = vnNowExchange.Date;

            var dailyVouchers = GetDailyVouchersForDate(todayExchange);
            if (index < 0 || index >= dailyVouchers.Count)
            {
                TempData["Error"] = "Mã giảm giá hàng ngày không hợp lệ.";
                return RedirectToAction(nameof(Edit));
            }

            var item = dailyVouchers[index];

            if (item.IsFreeShip)
            {
                var dayOfWeek = vnNowExchange.DayOfWeek;
                if (dayOfWeek != DayOfWeek.Friday && dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                {
                    TempData["Error"] = "Mã FREE SHIP chỉ được mở bán vào Thứ 6, Thứ 7 và Chủ Nhật.";
                    return RedirectToAction(nameof(Edit));
                }

                var session1Start = todayExchange.Add(new TimeSpan(8, 30, 0));
                var session1End = todayExchange.Add(new TimeSpan(9, 30, 0));
                var session2Start = todayExchange.Add(new TimeSpan(18, 30, 0));
                var session2End = todayExchange.Add(new TimeSpan(19, 30, 0));

                if (vnNowExchange >= session1Start && vnNowExchange < session1End)
                {
                    int countS1 = await _context.CustomerVouchers.CountAsync(v =>
                        v.Type == "Daily" && v.Key == item.Key && v.UnlockedAt >= session1Start && v.UnlockedAt < session1End);
                    if (countS1 >= 100)
                    {
                        TempData["Error"] = "Mã FREE SHIP đợt 1 đã được đổi hết (giới hạn 100 khách hàng).";
                        return RedirectToAction(nameof(Edit));
                    }
                }
                else if (vnNowExchange >= session2Start && vnNowExchange < session2End)
                {
                    int countS2 = await _context.CustomerVouchers.CountAsync(v =>
                        v.Type == "Daily" && v.Key == item.Key && v.UnlockedAt >= session2Start && v.UnlockedAt < session2End);
                    if (countS2 >= 100)
                    {
                        TempData["Error"] = "Mã FREE SHIP đợt 2 đã được đổi hết (giới hạn 100 khách hàng).";
                        return RedirectToAction(nameof(Edit));
                    }
                }
                else
                {
                    TempData["Error"] = "Mã FREE SHIP hiện không trong khung giờ mở bán.";
                    return RedirectToAction(nameof(Edit));
                }
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            var alreadyExchanged = await _context.CustomerVouchers.AnyAsync(v =>
                v.UserId == user.Id && v.Type == "Daily" && v.Key == item.Key);
            if (alreadyExchanged)
            {
                TempData["Error"] = "Bạn đã đổi mã giảm giá này rồi.";
                return RedirectToAction(nameof(Edit));
            }

            if (profile.LoyaltyPoints < item.RequiredPoints)
            {
                TempData["Error"] = $"Bạn không đủ điểm để đổi mã này (Cần {item.RequiredPoints:N0} điểm).";
                return RedirectToAction(nameof(Edit));
            }

            profile.LoyaltyPoints -= item.RequiredPoints;
            profile.UpdatedAt = vnNowExchange;

            var random = new Random();
            var randomSuffix = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            string code;
            if (item.IsFreeShip)
            {
                code = $"DAILY_FREESHIP_{todayExchange:yyyyMMdd}_{randomSuffix}";
            }
            else
            {
                code = $"DAILY_{todayExchange:yyyyMMdd}_{index}_{item.DiscountValue}PCT_{randomSuffix}";
            }

            var discount = new Discount
            {
                Code = code,
                DiscountValue = item.DiscountValue,
                MinOrderValue = item.MinOrder,
                MaxDiscount = 0,
                StartDate = todayExchange.AddDays(-1),
                EndDate = todayExchange.AddDays(14),
                Quantity = 1,
                IsSee = true,
                UserId = user.Id
            };
            _context.Discounts.Add(discount);

            var cv = new CustomerVoucher
            {
                UserId = user.Id,
                Type = "Daily",
                Key = item.Key,
                VoucherCode = code,
                DiscountValue = item.DiscountValue,
                UnlockedAt = vnNowExchange
            };
            _context.CustomerVouchers.Add(cv);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đổi thành công {discount.Code} bằng {item.RequiredPoints} điểm.";
            TempData["ActiveSection"] = "store";
            TempData["ActiveSubStore"] = "daily";
            return RedirectToAction(nameof(Edit));
        }

        private class DailyVoucherModel
        {
            public int Index { get; set; }
            public string Key { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public int RequiredPoints { get; set; }
            public int DiscountValue { get; set; }
            public decimal MinOrder { get; set; }
            public bool IsFreeShip { get; set; }
            public int? StartHour { get; set; }
        }

        private static List<DailyVoucherModel> GetDailyVouchersForDate(DateTime date)
        {
            int seed = date.Year * 10000 + date.Month * 100 + date.Day;
            var rand = new Random(seed);
            int count = rand.Next(5, 8); // từ 5 đến 7 mã giảm giá (inclusive)
            var list = new List<DailyVoucherModel>();

            int[] discountOptions = { 5, 8, 10, 12, 15, 20, 25 };
            decimal[] minOrderOptions = { 0, 50000, 100000, 150000, 200000 };

            for (int i = 0; i < count; i++)
            {
                int pct = discountOptions[rand.Next(discountOptions.Length)];
                decimal minOrder = minOrderOptions[rand.Next(minOrderOptions.Length)];
                
                int basePoints = pct switch
                {
                    5 => 400,
                    8 => 700,
                    10 => 900,
                    12 => 1100,
                    15 => 1400,
                    20 => 2000,
                    25 => 2800,
                    _ => pct * 100
                };
                
                if (minOrder > 0)
                {
                    basePoints = (int)(basePoints * 0.9);
                }
                
                basePoints = (basePoints / 10) * 10;

                string title = minOrder == 0 
                    ? $"Mã giảm giá {pct}% không giới hạn" 
                    : $"Mã giảm giá {pct}% (Đơn hàng từ {minOrder:N0}đ)";

                list.Add(new DailyVoucherModel
                {
                    Index = i,
                    Key = $"DAILY_{seed}_{i}",
                    Title = title,
                    RequiredPoints = basePoints,
                    DiscountValue = pct,
                    MinOrder = minOrder,
                    IsFreeShip = false,
                    StartHour = null
                });
            }

            // Generate FREE SHIP voucher only on weekends (Friday, Saturday, Sunday)
            if (date.DayOfWeek == DayOfWeek.Friday || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                int freeShipPoints = rand.Next(50, 101); // Random points between 50 and 100 inclusive
                list.Add(new DailyVoucherModel
                {
                    Index = count, // Put it at the end of the list
                    Key = $"DAILY_{seed}_FREESHIP",
                    Title = "Mã miễn phí vận chuyển FREE SHIP",
                    RequiredPoints = freeShipPoints,
                    DiscountValue = 0,
                    MinOrder = 0,
                    IsFreeShip = true,
                    StartHour = null
                });
            }

            return list;
        }

        private async Task PopulateVipAndBadgesAsync(string userId, Models.CustomerProfile profile, CustomerProfileViewModel model)
        {
            var existingVouchers = await _context.CustomerVouchers
                .Where(v => v.UserId == userId)
                .ToListAsync();

            var ownedVipKeys = existingVouchers
                .Where(v => v.Type == "Vip")
                .Select(v => v.Key)
                .ToHashSet();

            var ownedBadgeKeys = existingVouchers
                .Where(v => v.Type == "Badge")
                .Select(v => v.Key)
                .ToHashSet();

            var ownedFrameKeys = existingVouchers
                .Where(v => v.Type == "AvatarFrame")
                .Select(v => v.Key)
                .ToHashSet();

            string equippedVip = "none";
            string equippedBadge = profile.EquippedBadge ?? "none";
            string equippedFrame = profile.EquippedAvatarFrame ?? "none";

            // VIP Store and VIP inventory populations are disabled

            // Check and add owned daily frames to OwnedAvatarFrames
            foreach (var frameKey in ownedFrameKeys)
            {
                if (frameKey.StartsWith("daily-normal-"))
                {
                    var fileName = frameKey.Substring("daily-normal-".Length);
                    var name = MasterNormalFrames.FirstOrDefault(f => f.FileName == fileName)?.Name ?? "Khung Hàng Ngày";
                    model.OwnedAvatarFrames.Add(new OwnedAvatarFrameViewModel
                    {
                        Level = 200,
                        Key = frameKey,
                        Name = name,
                        FileName = fileName,
                        FilterStyle = "",
                        IsEquipped = equippedFrame == frameKey
                    });
                }
                else if (frameKey.StartsWith("daily-limited-"))
                {
                    var fileName = frameKey.Substring("daily-limited-".Length);
                    var name = MasterLimitedFrames.FirstOrDefault(f => f.FileName == fileName)?.Name ?? "Khung Giới Hạn";
                    model.OwnedAvatarFrames.Add(new OwnedAvatarFrameViewModel
                    {
                        Level = 300,
                        Key = frameKey,
                        Name = name,
                        FileName = fileName,
                        FilterStyle = "filter: drop-shadow(0 0 5px #ff5722);",
                        IsEquipped = equippedFrame == frameKey
                    });
                }
            }

            // Populate Owned Badges
            foreach (var badgeKey in ownedBadgeKeys)
            {
                string name = "";
                string fileName = "";

                if (badgeKey.StartsWith("vip-badge-"))
                {
                    var vipLevelStr = badgeKey.Substring("vip-badge-".Length);
                    if (int.TryParse(vipLevelStr, out int vipLevel))
                    {
                        var vip = VipMeta.FirstOrDefault(v => v.Level == vipLevel);
                        if (vip != default)
                        {
                            name = vip.BadgeName;
                            fileName = vip.BadgeFile;
                        }
                    }
                }
                else
                {
                    name = badgeKey switch
                    {
                        "thich-thi-nap.png" => "Thích Thì Nạp (Quán Quân)",
                        "vung-tien-nhu-nuoc.png" => "Vung Tiền Như Nước (Á/Quý Quân)",
                        "giau-nut-vach.png" => "Giàu Nứt Vách (Top 4-10)",
                        _ => "Huy hiệu Đua Top"
                    };
                    fileName = badgeKey;
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    model.OwnedBadges.Add(new OwnedBadgeViewModel
                    {
                        Key = badgeKey,
                        Name = name,
                        FileName = fileName,
                        IsEquipped = equippedBadge == badgeKey
                    });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExchangeVip(int level)
        {
            TempData["Error"] = "Chức năng đổi gói VIP đã bị đóng.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquipVip(string vipId)
        {
            TempData["Error"] = "Chức năng trang bị VIP đã bị đóng.";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquipBadgeDirect(string badgeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (false)
            {
                TempData["Error"] = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Edit));
            }

            var profile = await _loyaltyService.GetOrCreateProfileAsync(user.Id);

            if (badgeId == "none")
            {
                profile.EquippedBadge = "none";
            }
            else
            {
                var vip = VipMeta.FirstOrDefault(v => $"vip-badge-{v.Level}" == badgeId);
                bool isLeaderboardBadge = badgeId == "thich-thi-nap.png" || badgeId == "vung-tien-nhu-nuoc.png" || badgeId == "giau-nut-vach.png";
                
                if (vip == default && !isLeaderboardBadge)
                {
                    TempData["Error"] = "Huy hiệu không hợp lệ.";
                    return RedirectToAction(nameof(Edit));
                }

                bool owned = await _context.CustomerVouchers.AnyAsync(v =>
                    v.UserId == user.Id && v.Type == "Badge" && v.Key == badgeId);
                if (!owned)
                {
                    TempData["Error"] = "Bạn chưa sở hữu huy hiệu này.";
                    TempData["ActiveSection"] = "inventory";
                    TempData["ActiveSubInventory"] = "badge";
                    return RedirectToAction(nameof(Edit));
                }

                profile.EquippedBadge = badgeId;
            }

            profile.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            string displayBadgeName = "";
            if (badgeId != "none")
            {
                var vip = VipMeta.FirstOrDefault(v => $"vip-badge-{v.Level}" == badgeId);
                if (vip != default)
                {
                    displayBadgeName = vip.BadgeName;
                }
                else
                {
                    displayBadgeName = badgeId switch
                    {
                        "thich-thi-nap.png" => "Thích Thì Nạp",
                        "vung-tien-nhu-nuoc.png" => "Vung Tiền Như Nước",
                        "giau-nut-vach.png" => "Giàu Nứt Vách",
                        _ => "Đua Top"
                    };
                }
            }
            TempData["Success"] = badgeId == "none" ? "Đã tháo huy hiệu." : $"Đã trang bị huy hiệu {displayBadgeName} thành công!";
            TempData["ActiveSection"] = "inventory";
            TempData["ActiveSubInventory"] = "badge";
            return RedirectToAction(nameof(Edit));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetOffline()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.IsOnline = false;
                    profile.LastActiveTime = DateTime.Now;
                    profile.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            return Ok();
        }
    }
}
