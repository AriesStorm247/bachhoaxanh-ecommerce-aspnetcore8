using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEquippedVip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('dbo.InventoryBatchDeductions', 'U') IS NOT NULL DROP TABLE dbo.InventoryBatchDeductions;");
            migrationBuilder.Sql("IF OBJECT_ID('dbo.InventoryBatches', 'U') IS NOT NULL DROP TABLE dbo.InventoryBatches;");
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductInventories_BranchId_ProductId' AND object_id = OBJECT_ID('ProductInventories')) DROP INDEX IX_ProductInventories_BranchId_ProductId ON ProductInventories;");

            migrationBuilder.DropIndex(
                name: "IX_ProductInventories_BranchId",
                table: "ProductInventories");

            migrationBuilder.DropColumn(
                name: "EquippedVip",
                table: "CustomerProfiles");

            migrationBuilder.Sql("DELETE FROM CustomerVouchers WHERE [Type] = 'Vip' OR [Key] LIKE 'vip-moc-%' OR [Key] LIKE 'vip-badge-%'");

            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BatchCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBatchDeductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderDetailId = table.Column<int>(type: "int", nullable: true),
                    InventoryBatchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRestored = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatchDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryBatchDeductions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatchDeductions_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatchDeductions_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatchDeductions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatchDeductions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventories_BranchId_ProductId",
                table: "ProductInventories",
                columns: new[] { "BranchId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatchDeductions_BranchId",
                table: "InventoryBatchDeductions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatchDeductions_InventoryBatchId",
                table: "InventoryBatchDeductions",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatchDeductions_OrderDetailId",
                table: "InventoryBatchDeductions",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatchDeductions_OrderId",
                table: "InventoryBatchDeductions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatchDeductions_ProductId",
                table: "InventoryBatchDeductions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_BranchId",
                table: "InventoryBatches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_ProductId_BranchId_ExpiryDate",
                table: "InventoryBatches",
                columns: new[] { "ProductId", "BranchId", "ExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBatchDeductions");

            migrationBuilder.DropTable(
                name: "InventoryBatches");

            migrationBuilder.DropIndex(
                name: "IX_ProductInventories_BranchId_ProductId",
                table: "ProductInventories");

            migrationBuilder.AddColumn<string>(
                name: "EquippedVip",
                table: "CustomerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductInventories_BranchId",
                table: "ProductInventories",
                column: "BranchId");
        }
    }
}
