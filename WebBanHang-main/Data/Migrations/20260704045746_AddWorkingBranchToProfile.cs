using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkingBranchToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkingBranchId",
                table: "CustomerProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_WorkingBranchId",
                table: "CustomerProfiles",
                column: "WorkingBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerProfiles_Branches_WorkingBranchId",
                table: "CustomerProfiles",
                column: "WorkingBranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerProfiles_Branches_WorkingBranchId",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_WorkingBranchId",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "WorkingBranchId",
                table: "CustomerProfiles");
        }
    }
}
