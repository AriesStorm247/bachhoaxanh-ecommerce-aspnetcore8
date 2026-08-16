using System.ComponentModel.DataAnnotations;

namespace WebBanHang.ViewModels
{
    public class CustomerProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        [MaxLength(100)]
        [Display(Name = "Tên khách hàng")]
        public string? FullName { get; set; }

        public string? ShippingAddress { get; set; }

        public string? BankAccountLink { get; set; }

        public int LoyaltyPoints { get; set; }

        public int MembershipLevel { get; set; }

        public string MembershipTierName { get; set; } = string.Empty;

        public int? DisplayMembershipLevel { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? AvailableDisplayLevels { get; set; }

        public string? EquippedAvatarFrame { get; set; }

        public string? EquippedBadge { get; set; }



        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>? AvailableAvatarFrames { get; set; }

        public string? NextTierName { get; set; }

        public int? UpgradeCost { get; set; }

        public bool CanUpgrade { get; set; }

        public List<VoucherStoreItemViewModel> VoucherStoreItems { get; set; } = new();
        public List<AchievementItemViewModel> AchievementItems { get; set; } = new();
        public List<DailyVoucherItemViewModel> DailyVouchers { get; set; } = new();
        public List<OwnedVoucherViewModel> OwnedVouchers { get; set; } = new();
        public int VoucherPage { get; set; } = 1;
        public int VoucherTotalPages { get; set; } = 1;
        public List<FrameStoreItemViewModel> FrameStoreItems { get; set; } = new();
        public List<OwnedAvatarFrameViewModel> OwnedAvatarFrames { get; set; } = new();
        public List<OwnedBadgeViewModel> OwnedBadges { get; set; } = new();


         public List<DailyFrameItemViewModel> DailyFrames { get; set; } = new();
        public int DailyFrameResetsUsed { get; set; }
        public int NextResetCost { get; set; }
        public string? RoleName { get; set; }
        public string? GoogleEmail { get; set; }
        public string? FacebookName { get; set; }
        public List<WebBanHang.Models.Order> OrderHistory { get; set; } = new();
        public int OrderPage { get; set; } = 1;
        public int OrderTotalPages { get; set; } = 1;
        public string? OrderSearch { get; set; }
        public List<WebBanHang.Models.ChatHistory> ChatHistories { get; set; } = new();
        public int ChatPage { get; set; } = 1;
        public int ChatTotalPages { get; set; } = 1;
    }

    public class VoucherStoreItemViewModel
    {
        public int Level { get; set; }
        public string Suffix { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TierName { get; set; } = string.Empty;
        public int RequiredPoints { get; set; }
        public int DiscountValue { get; set; } // percentage e.g. 5, 10
        public decimal MinOrder { get; set; }
        public bool IsExchanged { get; set; }
        public string? GeneratedCode { get; set; }
        public bool CanExchange { get; set; }
    }

    public class AchievementItemViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Requirement { get; set; } = string.Empty;
        public int DiscountValue { get; set; } // percentage e.g. 4, 8
        public bool IsUnlocked { get; set; }
        public string? GeneratedCode { get; set; }
    }

    public class DailyVoucherItemViewModel
    {
        public int Index { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int RequiredPoints { get; set; }
        public int DiscountValue { get; set; }
        public decimal MinOrder { get; set; }
        public bool IsExchanged { get; set; }
        public string? GeneratedCode { get; set; }
        public bool CanExchange { get; set; }
        public bool IsFreeShip { get; set; }
        public int? StartHour { get; set; }
        public bool IsActiveNow { get; set; }
        public string? FreeShipStatusText { get; set; }
        public int FreeShipSessionCount { get; set; }
    }

    public class OwnedVoucherViewModel
    {
        public string Code { get; set; } = string.Empty;
        public int DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public int Quantity { get; set; } // >0 means usable, <=0 means used
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class FrameStoreItemViewModel
    {
        public int Level { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilterStyle { get; set; } = string.Empty;
        public int Cost { get; set; }
        public bool IsOwned { get; set; }
        public bool CanBuy { get; set; }
        public bool RequiresTier { get; set; }
        public string TierName { get; set; } = string.Empty;
    }

    public class OwnedAvatarFrameViewModel
    {
        public int Level { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilterStyle { get; set; } = string.Empty;
        public bool IsEquipped { get; set; }
    }

    public class OwnedBadgeViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsEquipped { get; set; }
    }



    public class DailyFrameItemViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsLimited { get; set; }
        public int Cost { get; set; }
        public bool IsOwned { get; set; }
        public bool CanBuy { get; set; }
    }
}
