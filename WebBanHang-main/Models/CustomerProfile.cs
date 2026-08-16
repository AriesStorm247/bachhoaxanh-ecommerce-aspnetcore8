using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebBanHang.Models
{
    public class CustomerProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        [MaxLength(100)]
        [Display(Name = "Tên khách hàng")]
        public string? FullName { get; set; }

        public string? ShippingAddress { get; set; }

        public int LoyaltyPoints { get; set; }

        public int MembershipLevel { get; set; }

        public int? DisplayMembershipLevel { get; set; }

        public string? EquippedAvatarFrame { get; set; }

        public string? EquippedBadge { get; set; }

        public string? DailyFramesJson { get; set; }

        public int DailyFrameResetsUsed { get; set; }

        public DateTime? DailyFramesLastResetDate { get; set; }

        public string? BankAccountLink { get; set; }

        public DateTime? LastActiveTime { get; set; }

        public bool IsOnline { get; set; }

        public string? GoogleId { get; set; }
        public string? GoogleEmail { get; set; }
        public string? FacebookId { get; set; }
        public string? FacebookName { get; set; }

        public int? WorkingBranchId { get; set; }

        [ForeignKey(nameof(WorkingBranchId))]
        public Branch? WorkingBranch { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }
    }
}
