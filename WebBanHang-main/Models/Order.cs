using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Ai mua?
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }

        // TRẠNG THÁI: 0: Chờ duyệt, 1: Đã duyệt, 2: Đã hủy
        public int Status { get; set; } = 0;
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0m;

        public string? ShippingMethod { get; set; }
        public decimal ShippingFee { get; set; } = 0m;
        public double ShippingDistance { get; set; } = 0.0;
        public string? ShipperId { get; set; }
        public string? DeliveryStaffName { get; set; }
        public string? PaymentMethod { get; set; } = "COD";

        public int? BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
    }
}
