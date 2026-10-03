using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemChiMucThoiGianLichSu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_ThoiGian",
                table: "LichSuGiaoNhans",
                column: "ThoiGian");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LichSuGiaoNhans_ThoiGian",
                table: "LichSuGiaoNhans");
        }
    }
}
