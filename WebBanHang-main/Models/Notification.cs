using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebBanHang.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        // Nếu null: Thông báo chung toàn hệ thống (Global)
        // Nếu có giá trị: Thông báo riêng cho khách hàng cụ thể
        public string? UserId { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty; // Hỗ trợ HTML định dạng phong phú

        [Required]
        [Display(Name = "Phân loại")]
        public NotificationType Type { get; set; } = NotificationType.System;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser? User { get; set; }
    }

    public enum NotificationType
    {
        System = 0,      // Hệ thống
        Promotion = 1,   // Khuyến mãi
        Reward = 2       // Quà tặng & Xếp hạng
    }
}
