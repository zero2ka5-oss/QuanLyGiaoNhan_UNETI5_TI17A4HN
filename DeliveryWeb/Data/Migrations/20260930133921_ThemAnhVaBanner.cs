using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemAnhVaBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhBia",
                table: "TinTucs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhDaiDien",
                table: "TaiKhoans",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhXe",
                table: "PhuongTiens",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhGiaoHang",
                table: "DonGiaoHangs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BannerTrangChus",
                columns: table => new
                {
                    MaBanner = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Anh = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ChuNut = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LienKet = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    HienThi = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerTrangChus", x => x.MaBanner);
                });

            migrationBuilder.InsertData(
                table: "BannerTrangChus",
                columns: new[] { "MaBanner", "Anh", "ChuNut", "HienThi", "LienKet", "MoTa", "NgayTao", "ThuTu", "TieuDe" },
                values: new object[,]
                {
                    { 1, "/img/banner/banner-1.svg", "Tạo đơn ngay", true, "/DangNhap/DangKy", "Lấy hàng tận nơi trong ngày, phí hiển thị ngay khi tạo đơn.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Giao hàng nhanh, theo dõi minh bạch từng bước" },
                    { 2, "/img/banner/banner-2.svg", "Tìm hiểu thêm", true, "/TrangChu/ChiTietTinTuc/2", "Tiền thu hộ được nộp về công ty và trả lại người gửi sau khi đối soát.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Thu hộ tiền hàng an toàn, đối soát rõ ràng" },
                    { 3, "/img/banner/banner-3.svg", "Tra cứu đơn", true, "/TrangChu/TraCuu", "Chỉ cần mã đơn và số điện thoại người nhận.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Tra cứu hành trình đơn hàng mọi lúc" }
                });

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 1,
                column: "AnhBia",
                value: null);

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 2,
                column: "AnhBia",
                value: null);

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 3,
                column: "AnhBia",
                value: null);

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 4,
                column: "AnhBia",
                value: null);

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 5,
                column: "AnhBia",
                value: null);

            migrationBuilder.UpdateData(
                table: "TinTucs",
                keyColumn: "MaTinTuc",
                keyValue: 6,
                column: "AnhBia",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_BannerTrangChus_HienThi_ThuTu",
                table: "BannerTrangChus",
                columns: new[] { "HienThi", "ThuTu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerTrangChus");

            migrationBuilder.DropColumn(
                name: "AnhBia",
                table: "TinTucs");

            migrationBuilder.DropColumn(
                name: "AnhDaiDien",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "AnhXe",
                table: "PhuongTiens");

            migrationBuilder.DropColumn(
                name: "AnhGiaoHang",
                table: "DonGiaoHangs");
        }
    }
}
