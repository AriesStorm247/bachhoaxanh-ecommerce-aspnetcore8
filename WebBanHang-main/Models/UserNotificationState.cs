using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebBanHang.Models
{
    public class UserNotificationState
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int NotificationId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser? User { get; set; }

        [ForeignKey(nameof(NotificationId))]
        public virtual Notification? Notification { get; set; }
    }
}
