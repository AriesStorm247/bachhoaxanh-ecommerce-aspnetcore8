using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBanHang.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyFramesRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyFrameResetsUsed",
                table: "CustomerProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DailyFramesJson",
                table: "CustomerProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DailyFramesLastResetDate",
                table: "CustomerProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyFrameResetsUsed",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "DailyFramesJson",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "DailyFramesLastResetDate",
                table: "CustomerProfiles");
        }
    }
}
