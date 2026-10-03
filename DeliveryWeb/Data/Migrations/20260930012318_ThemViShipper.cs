using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemViShipper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaGiaoDichNop",
                table: "DonGiaoHangs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GiaoDichVis",
                columns: table => new
                {
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<byte>(type: "tinyint", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThongTinThanhToan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiXuLy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichVis", x => x.MaGiaoDich);
                    table.CheckConstraint("CK_GiaoDichVi_SoTien", "[SoTien] >= 0");
                    table.ForeignKey(
                        name: "FK_GiaoDichVis_NhanVienGiaoHangs_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVienGiaoHangs",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaGiaoDichNop",
                table: "DonGiaoHangs",
                column: "MaGiaoDichNop");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichVis_MaNhanVien_NgayTao",
                table: "GiaoDichVis",
                columns: new[] { "MaNhanVien", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichVis_TrangThai",
                table: "GiaoDichVis",
                column: "TrangThai");

            migrationBuilder.AddForeignKey(
                name: "FK_DonGiaoHangs_GiaoDichVis_MaGiaoDichNop",
                table: "DonGiaoHangs",
                column: "MaGiaoDichNop",
                principalTable: "GiaoDichVis",
                principalColumn: "MaGiaoDich",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonGiaoHangs_GiaoDichVis_MaGiaoDichNop",
                table: "DonGiaoHangs");

            migrationBuilder.DropTable(
                name: "GiaoDichVis");

            migrationBuilder.DropIndex(
                name: "IX_DonGiaoHangs_MaGiaoDichNop",
                table: "DonGiaoHangs");

            migrationBuilder.DropColumn(
                name: "MaGiaoDichNop",
                table: "DonGiaoHangs");
        }
    }
}
