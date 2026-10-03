using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemThuHoCod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDoiSoat",
                table: "DonGiaoHangs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "NguoiTraPhi",
                table: "DonGiaoHangs",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<decimal>(
                name: "TienThuHo",
                table: "DonGiaoHangs",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DonGiaoHang_TienThuHo",
                table: "DonGiaoHangs",
                sql: "[TienThuHo] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DonGiaoHang_TienThuHo",
                table: "DonGiaoHangs");

            migrationBuilder.DropColumn(
                name: "NgayDoiSoat",
                table: "DonGiaoHangs");

            migrationBuilder.DropColumn(
                name: "NguoiTraPhi",
                table: "DonGiaoHangs");

            migrationBuilder.DropColumn(
                name: "TienThuHo",
                table: "DonGiaoHangs");
        }
    }
}
