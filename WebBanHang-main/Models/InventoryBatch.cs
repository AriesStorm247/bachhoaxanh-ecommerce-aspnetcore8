using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public class InventoryBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }

        [Required]
        [MaxLength(60)]
        public string BatchCode { get; set; }

        public DateTime ImportDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        [Range(0.0, double.MaxValue)]
        [Column(TypeName = "decimal(18, 3)")]
        public decimal Quantity { get; set; }

        [Range(0.0, double.MaxValue)]
        [Column(TypeName = "decimal(18, 3)")]
        public decimal OriginalQuantity { get; set; }

        [MaxLength(200)]
        public string? SupplierName { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
