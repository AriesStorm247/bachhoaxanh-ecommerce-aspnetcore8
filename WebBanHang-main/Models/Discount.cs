using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Phần trăm giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Giá trị đơn tối thiểu")]
        public decimal MinOrderValue { get; set; }

        [Display(Name = "Giảm tối đa")]
        public decimal MaxDiscount { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Số lượt dùng")]
        public int Quantity { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsSee { get; set; }

        public string? UserId { get; set; }
    }
}
