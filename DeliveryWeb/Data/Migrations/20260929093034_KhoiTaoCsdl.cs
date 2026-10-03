using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTaoCsdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhuVucs",
                columns: table => new
                {
                    MaKhuVuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuVuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhiCoBan = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuVucs", x => x.MaKhuVuc);
                    table.CheckConstraint("CK_KhuVuc_PhiCoBan", "[PhiCoBan] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "LoaiHangs",
                columns: table => new
                {
                    MaLoaiHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HeSoPhuThu = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiHangs", x => x.MaLoaiHang);
                    table.CheckConstraint("CK_LoaiHang_HeSoPhuThu", "[HeSoPhuThu] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "PhuongTiens",
                columns: table => new
                {
                    MaPhuongTien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BienSo = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    LoaiPhuongTien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaiTrongToiDa = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuongTiens", x => x.MaPhuongTien);
                    table.CheckConstraint("CK_PhuongTien_TaiTrong", "[TaiTrongToiDa] > 0");
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoans",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "KhachHangs",
                columns: table => new
                {
                    MaKhachHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangs", x => x.MaKhachHang);
                    table.ForeignKey(
                        name: "FK_KhachHangs_TaiKhoans_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhanVienGiaoHangs",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaKhuVucPhuTrach = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVienGiaoHangs", x => x.MaNhanVien);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_KhuVucs_MaKhuVucPhuTrach",
                        column: x => x.MaKhuVucPhuTrach,
                        principalTable: "KhuVucs",
                        principalColumn: "MaKhuVuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_TaiKhoans_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonGiaoHangs",
                columns: table => new
                {
                    MaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhachHang = table.Column<int>(type: "int", nullable: false),
                    MaLoaiHang = table.Column<int>(type: "int", nullable: false),
                    MaKhuVuc = table.Column<int>(type: "int", nullable: false),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoaiNguoiNhan = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DiaChiNhan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    KhoiLuong = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayGiaoDuKien = table.Column<DateTime>(type: "date", nullable: false),
                    PhiCoBan = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PhuPhiKhoiLuong = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PhuPhiLoaiHang = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PhiVanChuyen = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayHoanTat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonGiaoHangs", x => x.MaDon);
                    table.CheckConstraint("CK_DonGiaoHang_KhoiLuong", "[KhoiLuong] > 0");
                    table.CheckConstraint("CK_DonGiaoHang_Phi", "[PhiVanChuyen] >= 0");
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_KhachHangs_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "KhachHangs",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_KhuVucs_MaKhuVuc",
                        column: x => x.MaKhuVuc,
                        principalTable: "KhuVucs",
                        principalColumn: "MaKhuVuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_LoaiHangs_MaLoaiHang",
                        column: x => x.MaLoaiHang,
                        principalTable: "LoaiHangs",
                        principalColumn: "MaLoaiHang",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuGiaoNhans",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDon = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThaiCu = table.Column<byte>(type: "tinyint", nullable: true),
                    TrangThaiMoi = table.Column<byte>(type: "tinyint", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuGiaoNhans", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuGiaoNhans_DonGiaoHangs_MaDon",
                        column: x => x.MaDon,
                        principalTable: "DonGiaoHangs",
                        principalColumn: "MaDon",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhanCongGiaoHangs",
                columns: table => new
                {
                    MaPhanCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDon = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaPhuongTien = table.Column<int>(type: "int", nullable: false),
                    NgayPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayNhanHang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayBatDauGiao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    KetQua = table.Column<byte>(type: "tinyint", nullable: true),
                    LyDoThatBai = table.Column<byte>(type: "tinyint", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiPhanCong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongGiaoHangs", x => x.MaPhanCong);
                    table.ForeignKey(
                        name: "FK_PhanCongGiaoHangs_DonGiaoHangs_MaDon",
                        column: x => x.MaDon,
                        principalTable: "DonGiaoHangs",
                        principalColumn: "MaDon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCongGiaoHangs_NhanVienGiaoHangs_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVienGiaoHangs",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCongGiaoHangs_PhuongTiens_MaPhuongTien",
                        column: x => x.MaPhuongTien,
                        principalTable: "PhuongTiens",
                        principalColumn: "MaPhuongTien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaKhachHang",
                table: "DonGiaoHangs",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaKhuVuc",
                table: "DonGiaoHangs",
                column: "MaKhuVuc");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaLoaiHang",
                table: "DonGiaoHangs",
                column: "MaLoaiHang");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_NgayTao",
                table: "DonGiaoHangs",
                column: "NgayTao");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_TrangThai",
                table: "DonGiaoHangs",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangs_MaTaiKhoan",
                table: "KhachHangs",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangs_SoDienThoai",
                table: "KhachHangs",
                column: "SoDienThoai");

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucs_TenKhuVuc",
                table: "KhuVucs",
                column: "TenKhuVuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_MaDon_ThoiGian",
                table: "LichSuGiaoNhans",
                columns: new[] { "MaDon", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_LoaiHangs_TenLoaiHang",
                table: "LoaiHangs",
                column: "TenLoaiHang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaKhuVucPhuTrach",
                table: "NhanVienGiaoHangs",
                column: "MaKhuVucPhuTrach");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaTaiKhoan",
                table: "NhanVienGiaoHangs",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiaoHangs_MaDon_TrangThai",
                table: "PhanCongGiaoHangs",
                columns: new[] { "MaDon", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiaoHangs_MaNhanVien",
                table: "PhanCongGiaoHangs",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongGiaoHangs_MaPhuongTien",
                table: "PhanCongGiaoHangs",
                column: "MaPhuongTien");

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_BienSo",
                table: "PhuongTiens",
                column: "BienSo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_TenDangNhap",
                table: "TaiKhoans",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuGiaoNhans");

            migrationBuilder.DropTable(
                name: "PhanCongGiaoHangs");

            migrationBuilder.DropTable(
                name: "DonGiaoHangs");

            migrationBuilder.DropTable(
                name: "NhanVienGiaoHangs");

            migrationBuilder.DropTable(
                name: "PhuongTiens");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "LoaiHangs");

            migrationBuilder.DropTable(
                name: "KhuVucs");

            migrationBuilder.DropTable(
                name: "TaiKhoans");
        }
    }
}
