using System.ComponentModel.DataAnnotations;

namespace WebBanHang.ViewModels
{
    public class AdminCustomerListItemViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Display(Name = "Tên khách hàng")]
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public int LoyaltyPoints { get; set; }

        public int MembershipLevel { get; set; }

        public string MembershipTierName { get; set; } = string.Empty;

        public string? EquippedAvatarFrame { get; set; }

        public string? ShippingAddress { get; set; }

        public string? BankAccountLink { get; set; }

        public DateTime? LastActiveTime { get; set; }

        public bool IsOnline { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }
    }

    public class AdminCustomerEditViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Tên khách hàng")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public string? ShippingAddress { get; set; }

        public string? BankAccountLink { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm tích lũy không được âm.")]
        public int LoyaltyPoints { get; set; }

        [Range(0, 11, ErrorMessage = "Cấp thẻ không hợp lệ.")]
        public int MembershipLevel { get; set; }

        public string MembershipTierName { get; set; } = string.Empty;



        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }
    }

    public class HRMListItemViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime? LastActiveTime { get; set; }
        public bool IsOnline { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public string? AvatarUrl { get; set; }
        public string? EquippedAvatarFrame { get; set; }

        public string? EquippedBadge { get; set; }
        public int MembershipLevel { get; set; }
        public int? WorkingBranchId { get; set; }
        public string? WorkingBranchName { get; set; }
    }

    public class HRMEditViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public string? ShippingAddress { get; set; }

        public string? BankAccountLink { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm tích lũy không được âm.")]
        public int LoyaltyPoints { get; set; }

        [Range(0, 11, ErrorMessage = "Cấp thẻ không hợp lệ.")]
        public int MembershipLevel { get; set; }



        public int? WorkingBranchId { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }
    }
}
