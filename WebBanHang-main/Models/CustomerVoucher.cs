using System;
using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class CustomerVoucher
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty; // "Achievement" or "Store"

        [Required]
        public string Key { get; set; } = string.Empty; // e.g., "WELCOME", "REACH_0", "STORE_3"

        [Required]
        public string VoucherCode { get; set; } = string.Empty; // e.g., "WELCOME4_vophu1004"

        public decimal DiscountValue { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.Now;
    }
}
