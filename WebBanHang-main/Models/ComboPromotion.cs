using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class ComboPromotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId1 { get; set; }

        [Required]
        public int ProductId2 { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 3)")]
        [Range(0.0, 1.0, ErrorMessage = "Tỷ lệ giảm giá phải nằm trong khoảng từ 0 đến 1.")]
        public decimal DiscountPercent { get; set; } = 0.10m; // Mặc định giảm 10%

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public double Support { get; set; }

        public double Confidence { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? ExpiryDate { get; set; }

        [ForeignKey("ProductId1")]
        public virtual Product? Product1 { get; set; }

        [ForeignKey("ProductId2")]
        public virtual Product? Product2 { get; set; }
    }
}
