using WebBanHang.Models;

namespace WebBanHang.ViewModels
{
    public class InventoryReportViewModel
    {
        public List<InventoryReportRow> Rows { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public List<string> Provinces { get; set; } = new();
        public List<string> Districts { get; set; } = new();
        public List<Branch> BranchOptions { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string SelectedProvince { get; set; } = string.Empty;
        public string SelectedDistrict { get; set; } = string.Empty;
        public int? SelectedBranchId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string ExpiryStatus { get; set; } = string.Empty;
        public int LowStockThreshold { get; set; } = 10;
        public int WarningDays { get; set; } = 7;
        public int TotalRows { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages { get; set; } = 1;
        public decimal TotalPhysicalQuantity { get; set; }
        public decimal TotalSellableQuantity { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringBatchCount { get; set; }
        public int ExpiredBatchCount { get; set; }
        public List<CategoryInventorySummary> CategorySummaries { get; set; } = new();
        public List<InventoryStatusSummary> StatusSummaries { get; set; } = new();
    }

    public class InventoryReportRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchLocation { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal PhysicalQuantity { get; set; }
        public decimal SellableQuantity { get; set; }
        public decimal ExpiringQuantity { get; set; }
        public decimal ExpiredQuantity { get; set; }
        public int BatchCount { get; set; }
        public DateTime? NearestExpiryDate { get; set; }
        public decimal InventoryValue { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string ExpiryStatus { get; set; } = string.Empty;
    }

    public class CategoryInventorySummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
    }

    public class InventoryStatusSummary
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
