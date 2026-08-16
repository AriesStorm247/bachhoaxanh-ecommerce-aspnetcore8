using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(0.0, double.MaxValue, ErrorMessage = "Giá bán không được nhỏ hơn 0.")]
        public decimal Price { get; set; }

        [NotMapped]
        public decimal DiscountedPrice => Services.PromotionService.GetDiscountedPrice(this, Services.PromotionService.GetVietnamNow());

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = "Cái";

        public bool IsSoldByWeight { get; set; } = false;

        [NotMapped]
        public decimal Amount { get; set; }

        public string Image { get; set; }

        // Foreign key
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        //Cho phép hiển thị
        public bool IsVisible { get; set; } = true;

        // Hàng hot
        public bool IsHot { get; set; } = false;

        // Bán chạy
        public bool IsBestSeller { get; set; } = false;

        // Mã vạch sản phẩm
        public string? Barcode { get; set; }
    }
}