using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTaoCsdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "BuuCucs",
                columns: table => new
                {
                    MaBuuCuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenBuuCuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TinhThanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ViDo = table.Column<double>(type: "float", nullable: true),
                    KinhDo = table.Column<double>(type: "float", nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuuCucs", x => x.MaBuuCuc);
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
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnhXe = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuongTiens", x => x.MaPhuongTien);
                    table.CheckConstraint("CK_PhuongTien_TaiTrong", "[TaiTrongToiDa] > 0");
                });

            migrationBuilder.CreateTable(
                name: "TinTucs",
                columns: table => new
                {
                    MaTinTuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChuyenMuc = table.Column<byte>(type: "tinyint", nullable: false),
                    AnhBia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BieuTuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayDang = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HienThi = table.Column<bool>(type: "bit", nullable: false),
                    NguoiDang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinTucs", x => x.MaTinTuc);
                });

            migrationBuilder.CreateTable(
                name: "KhuVucs",
                columns: table => new
                {
                    MaKhuVuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuVuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhiCoBan = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    MaBuuCuc = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuVucs", x => x.MaKhuVuc);
                    table.CheckConstraint("CK_KhuVuc_PhiCoBan", "[PhiCoBan] >= 0");
                    table.ForeignKey(
                        name: "FK_KhuVucs_BuuCucs_MaBuuCuc",
                        column: x => x.MaBuuCuc,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
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
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhaiDoiMatKhau = table.Column<bool>(type: "bit", nullable: false),
                    MaBuuCuc = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.MaTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoans_BuuCucs_MaBuuCuc",
                        column: x => x.MaBuuCuc,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
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
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    LoaiShipper = table.Column<byte>(type: "tinyint", nullable: false),
                    MaBuuCuc = table.Column<int>(type: "int", nullable: true),
                    MaPhuongTien = table.Column<int>(type: "int", nullable: true),
                    ViDo = table.Column<double>(type: "float", nullable: true),
                    KinhDo = table.Column<double>(type: "float", nullable: true),
                    ThoiGianViTri = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVienGiaoHangs", x => x.MaNhanVien);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_BuuCucs_MaBuuCuc",
                        column: x => x.MaBuuCuc,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_KhuVucs_MaKhuVucPhuTrach",
                        column: x => x.MaKhuVucPhuTrach,
                        principalTable: "KhuVucs",
                        principalColumn: "MaKhuVuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_PhuongTiens_MaPhuongTien",
                        column: x => x.MaPhuongTien,
                        principalTable: "PhuongTiens",
                        principalColumn: "MaPhuongTien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhanVienGiaoHangs_TaiKhoans_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "LichSuPhuongTiens",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaPhuongTienCu = table.Column<int>(type: "int", nullable: true),
                    MaPhuongTienMoi = table.Column<int>(type: "int", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuPhuongTiens", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuPhuongTiens_NhanVienGiaoHangs_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVienGiaoHangs",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuPhuongTiens_PhuongTiens_MaPhuongTienCu",
                        column: x => x.MaPhuongTienCu,
                        principalTable: "PhuongTiens",
                        principalColumn: "MaPhuongTien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuPhuongTiens_PhuongTiens_MaPhuongTienMoi",
                        column: x => x.MaPhuongTienMoi,
                        principalTable: "PhuongTiens",
                        principalColumn: "MaPhuongTien",
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
                    MaBuuCucGui = table.Column<int>(type: "int", nullable: true),
                    MaBuuCucNhan = table.Column<int>(type: "int", nullable: true),
                    MaBuuCucHienTai = table.Column<int>(type: "int", nullable: true),
                    MaNhanVienDangGiu = table.Column<int>(type: "int", nullable: true),
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
                    TienThuHo = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    NguoiTraPhi = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayDoiSoat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayThuPhiNguoiGui = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayYeuCauThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaGiaoDichNop = table.Column<int>(type: "int", nullable: true),
                    HinhThucGui = table.Column<byte>(type: "tinyint", nullable: false),
                    DiaChiLayHang = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayHoanTat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHoanHang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhienBan = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    AnhGiaoHang = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonGiaoHangs", x => x.MaDon);
                    table.CheckConstraint("CK_DonGiaoHang_KhoiLuong", "[KhoiLuong] > 0");
                    table.CheckConstraint("CK_DonGiaoHang_Phi", "[PhiVanChuyen] >= 0");
                    table.CheckConstraint("CK_DonGiaoHang_TienThuHo", "[TienThuHo] >= 0");
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_BuuCucs_MaBuuCucGui",
                        column: x => x.MaBuuCucGui,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_BuuCucs_MaBuuCucHienTai",
                        column: x => x.MaBuuCucHienTai,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_BuuCucs_MaBuuCucNhan",
                        column: x => x.MaBuuCucNhan,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_GiaoDichVis_MaGiaoDichNop",
                        column: x => x.MaGiaoDichNop,
                        principalTable: "GiaoDichVis",
                        principalColumn: "MaGiaoDich",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_DonGiaoHangs_NhanVienGiaoHangs_MaNhanVienDangGiu",
                        column: x => x.MaNhanVienDangGiu,
                        principalTable: "NhanVienGiaoHangs",
                        principalColumn: "MaNhanVien",
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
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaBuuCuc = table.Column<int>(type: "int", nullable: true),
                    MaNhanVien = table.Column<int>(type: "int", nullable: true),
                    MaPhuongTien = table.Column<int>(type: "int", nullable: true),
                    ViDo = table.Column<double>(type: "float", nullable: true),
                    KinhDo = table.Column<double>(type: "float", nullable: true),
                    QuetMa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuGiaoNhans", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuGiaoNhans_BuuCucs_MaBuuCuc",
                        column: x => x.MaBuuCuc,
                        principalTable: "BuuCucs",
                        principalColumn: "MaBuuCuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuGiaoNhans_DonGiaoHangs_MaDon",
                        column: x => x.MaDon,
                        principalTable: "DonGiaoHangs",
                        principalColumn: "MaDon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuGiaoNhans_NhanVienGiaoHangs_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVienGiaoHangs",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuGiaoNhans_PhuongTiens_MaPhuongTien",
                        column: x => x.MaPhuongTien,
                        principalTable: "PhuongTiens",
                        principalColumn: "MaPhuongTien",
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
                    LoaiChang = table.Column<byte>(type: "tinyint", nullable: false),
                    NgayPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayNhanHang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayBatDauGiao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    KetQua = table.Column<byte>(type: "tinyint", nullable: true),
                    LyDoThatBai = table.Column<byte>(type: "tinyint", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiPhanCong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThuNhapShipper = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongGiaoHangs", x => x.MaPhanCong);
                    table.CheckConstraint("CK_PhanCong_ThuNhapShipper", "[ThuNhapShipper] >= 0");
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

            migrationBuilder.InsertData(
                table: "BannerTrangChus",
                columns: new[] { "MaBanner", "Anh", "ChuNut", "HienThi", "LienKet", "MoTa", "NgayTao", "ThuTu", "TieuDe" },
                values: new object[,]
                {
                    { 1, "/img/banner/banner-1.svg", "Tạo đơn ngay", true, "/DangNhap/DangKy", "Gửi tại bưu cục gần nhất, quét mã ở mọi chặng, phí hiển thị ngay khi tạo đơn.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Giao hàng nhanh, theo dõi minh bạch từng bước" },
                    { 2, "/img/banner/banner-2.svg", "Tìm hiểu thêm", true, "/TrangChu/ChiTietTinTuc/2", "Tiền thu hộ được nộp về công ty và trả lại người gửi sau khi đối soát.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Thu hộ tiền hàng an toàn, đối soát rõ ràng" },
                    { 3, "/img/banner/banner-3.svg", "Tra cứu đơn", true, "/TrangChu/TraCuu", "Chỉ cần mã đơn và số điện thoại người nhận.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Tra cứu hành trình đơn hàng mọi lúc" }
                });

            migrationBuilder.InsertData(
                table: "TinTucs",
                columns: new[] { "MaTinTuc", "AnhBia", "BieuTuong", "ChuyenMuc", "HienThi", "NgayCapNhat", "NgayDang", "NguoiDang", "NoiDung", "TieuDe", "TomTat" },
                values: new object[,]
                {
                    { 1, null, "bi-cloud-lightning-rain", (byte)0, true, null, new DateTime(2026, 9, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Do ảnh hưởng của mưa lớn kéo dài, một số tuyến giao hàng ngoại thành và liên tỉnh có thể chậm hơn ngày giao dự kiến từ 1 đến 2 ngày.\nTrong thời gian này, GiaoNhanh áp dụng các biện pháp sau:\n- Hàng hóa được bọc chống nước trước khi đưa lên xe.\n- Shipper gọi điện cho người nhận trước khi đến để hẹn thời gian phù hợp.\n- Đơn chưa giao được vì thời tiết sẽ được ghi rõ lý do trong lịch sử giao nhận và sắp xếp giao lại sớm nhất.\nQuý khách có thể theo dõi trạng thái đơn bất cứ lúc nào tại mục Tra cứu đơn bằng mã đơn và số điện thoại người nhận.", "Mùa mưa bão: một số đơn có thể giao chậm 1 – 2 ngày", "Tại các khu vực chịu ảnh hưởng của mưa lớn, thời gian giao hàng có thể kéo dài hơn dự kiến. Hàng hóa vẫn được bảo quản an toàn." },
                    { 2, null, "bi-cash-coin", (byte)1, true, null, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Khi tạo đơn, người gửi có thể nhập số tiền thu hộ – đây là tiền hàng mà shipper sẽ thu của người nhận.\nQuy trình đối soát tiền thu hộ tại GiaoNhanh:\n- Giao thành công: shipper thu tiền thu hộ (cộng phí ship nếu người nhận trả phí).\n- Shipper nộp số tiền đã thu về công ty qua ví trên ứng dụng.\n- Công ty xác nhận đã nhận tiền, đơn chuyển sang Hoàn tất và được đối soát.\n- Người gửi nhận lại tiền thu hộ, trừ phí ship nếu người gửi là người trả phí.\nShipper chỉ được hưởng hoa hồng trên phí vận chuyển của chặng mình chạy, không được hưởng tiền thu hộ.", "Thu hộ tiền hàng: người gửi nhận lại tiền khi nào?", "Tiền thu hộ là tiền hàng của người gửi. Shipper chỉ giữ hộ và nộp lại công ty để đối soát trả người gửi." },
                    { 3, null, "bi-calculator", (byte)2, true, null, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Phí vận chuyển của mỗi đơn gồm ba phần và được hiển thị trước khi quý khách xác nhận tạo đơn:\n- Phí cơ bản theo khu vực giao (xem Bảng giá trên trang chủ).\n- Phụ phí khối lượng: phần vượt 2 kg tính 5.000 đ cho mỗi kg.\n- Phụ phí loại hàng: hàng dễ vỡ, hàng giá trị cao… được cộng thêm theo hệ số của từng loại.\nQuý khách có thể chọn người gửi hoặc người nhận trả phí ship. Nếu người nhận trả, shipper sẽ thu phí ship cùng tiền thu hộ khi giao.", "Cách tính phí vận chuyển minh bạch", "Phí được hệ thống tự tính ngay khi tạo đơn theo khu vực giao, khối lượng và loại hàng – không phát sinh thêm khi giao." },
                    { 4, null, "bi-box-seam", (byte)1, true, null, new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Hàng dễ vỡ như đồ gốm sứ, thủy tinh, thiết bị điện tử cần được đóng gói kỹ trước khi mang tới bưu cục gửi:\n- Dùng thùng carton cứng, còn nguyên vẹn, kích thước vừa với hàng.\n- Bọc từng món bằng màng xốp hơi, chèn kín khoảng trống để hàng không xê dịch.\n- Dán băng keo chữ H ở mặt trên và mặt đáy thùng.\n- Chọn đúng loại hàng khi tạo đơn và ghi chú \"Hàng dễ vỡ, nhẹ tay\" để bưu cục và shipper lưu ý.\nKhai báo đúng khối lượng giúp hệ thống xếp xe phù hợp với tải trọng, tránh chèn ép hàng.", "Đóng gói hàng dễ vỡ đúng cách để giao an toàn", "Một vài lưu ý đơn giản khi đóng gói giúp hàng dễ vỡ đến tay người nhận nguyên vẹn." },
                    { 5, null, "bi-arrow-repeat", (byte)2, true, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Khi không liên lạc được người nhận hoặc người nhận hẹn lại, shipper sẽ cập nhật trạng thái Giao không thành công kèm lý do cụ thể.\nSau đó shipper hẹn ngày giao lại với người nhận, hoặc trả hàng về bưu cục để phân shipper khác giao:\n- Mỗi đơn được giao tối đa 3 lần (lần đầu và các lần giao lại).\n- Mỗi lần giao lại đều được ghi vào lịch sử giao nhận: shipper nào, lúc nào, kết quả ra sao.\n- Quá số lần giao, công ty sẽ liên hệ người gửi để hủy hoặc chuyển hoàn đơn.\nĐể đơn được giao thành công ngay lần đầu, quý khách vui lòng nhập đúng số điện thoại và địa chỉ người nhận.", "Đơn giao không thành công được xử lý thế nào?", "Mỗi đơn được giao tối đa 3 lần. Lý do không giao được luôn được ghi lại để quý khách theo dõi." },
                    { 6, null, "bi-pencil-square", (byte)2, true, null, new DateTime(2026, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Sau khi tạo đơn, quý khách có thể tự thay đổi thông tin người nhận, khối lượng, khu vực giao hoặc hủy đơn tại trang Đơn hàng của tôi.\n- Được sửa / hủy: khi đơn đang ở trạng thái Đã tạo đơn (chưa mang hàng tới bưu cục gửi).\n- Không được sửa / hủy: khi bưu cục đã quét mã tiếp nhận hàng – lúc này vui lòng liên hệ tổng đài 1900 1515 để được hỗ trợ.\nKhi sửa khối lượng, khu vực hoặc loại hàng, phí vận chuyển được hệ thống tính lại và ghi vào lịch sử đơn.", "Sửa hoặc hủy đơn: khi nào vẫn còn kịp?", "Quý khách tự sửa thông tin hoặc hủy đơn ngay trên tài khoản khi chưa mang hàng tới bưu cục gửi." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannerTrangChus_HienThi_ThuTu",
                table: "BannerTrangChus",
                columns: new[] { "HienThi", "ThuTu" });

            migrationBuilder.CreateIndex(
                name: "IX_BuuCucs_TenBuuCuc",
                table: "BuuCucs",
                column: "TenBuuCuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaBuuCucGui",
                table: "DonGiaoHangs",
                column: "MaBuuCucGui");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaBuuCucHienTai",
                table: "DonGiaoHangs",
                column: "MaBuuCucHienTai");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaBuuCucNhan",
                table: "DonGiaoHangs",
                column: "MaBuuCucNhan");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_MaGiaoDichNop",
                table: "DonGiaoHangs",
                column: "MaGiaoDichNop");

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
                name: "IX_DonGiaoHangs_MaNhanVienDangGiu",
                table: "DonGiaoHangs",
                column: "MaNhanVienDangGiu");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_NgayTao",
                table: "DonGiaoHangs",
                column: "NgayTao");

            migrationBuilder.CreateIndex(
                name: "IX_DonGiaoHangs_TrangThai",
                table: "DonGiaoHangs",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichVis_MaNhanVien_NgayTao",
                table: "GiaoDichVis",
                columns: new[] { "MaNhanVien", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichVis_TrangThai",
                table: "GiaoDichVis",
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
                column: "SoDienThoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucs_MaBuuCuc",
                table: "KhuVucs",
                column: "MaBuuCuc");

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucs_TenKhuVuc",
                table: "KhuVucs",
                column: "TenKhuVuc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_MaBuuCuc",
                table: "LichSuGiaoNhans",
                column: "MaBuuCuc");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_MaDon_ThoiGian",
                table: "LichSuGiaoNhans",
                columns: new[] { "MaDon", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_MaNhanVien",
                table: "LichSuGiaoNhans",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_MaPhuongTien",
                table: "LichSuGiaoNhans",
                column: "MaPhuongTien");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGiaoNhans_ThoiGian",
                table: "LichSuGiaoNhans",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuPhuongTiens_MaNhanVien_ThoiGian",
                table: "LichSuPhuongTiens",
                columns: new[] { "MaNhanVien", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuPhuongTiens_MaPhuongTienCu",
                table: "LichSuPhuongTiens",
                column: "MaPhuongTienCu");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuPhuongTiens_MaPhuongTienMoi",
                table: "LichSuPhuongTiens",
                column: "MaPhuongTienMoi");

            migrationBuilder.CreateIndex(
                name: "IX_LoaiHangs_TenLoaiHang",
                table: "LoaiHangs",
                column: "TenLoaiHang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaBuuCuc",
                table: "NhanVienGiaoHangs",
                column: "MaBuuCuc");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaKhuVucPhuTrach",
                table: "NhanVienGiaoHangs",
                column: "MaKhuVucPhuTrach");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaPhuongTien",
                table: "NhanVienGiaoHangs",
                column: "MaPhuongTien",
                unique: true,
                filter: "[MaPhuongTien] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_MaTaiKhoan",
                table: "NhanVienGiaoHangs",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVienGiaoHangs_SoDienThoai",
                table: "NhanVienGiaoHangs",
                column: "SoDienThoai",
                unique: true);

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
                name: "UX_PhanCong_MotHieuLucMoiDon",
                table: "PhanCongGiaoHangs",
                column: "MaDon",
                unique: true,
                filter: "[TrangThai] IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_PhuongTiens_BienSo",
                table: "PhuongTiens",
                column: "BienSo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_MaBuuCuc",
                table: "TaiKhoans",
                column: "MaBuuCuc");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_TenDangNhap",
                table: "TaiKhoans",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TinTucs_HienThi_NgayDang",
                table: "TinTucs",
                columns: new[] { "HienThi", "NgayDang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerTrangChus");

            migrationBuilder.DropTable(
                name: "LichSuGiaoNhans");

            migrationBuilder.DropTable(
                name: "LichSuPhuongTiens");

            migrationBuilder.DropTable(
                name: "PhanCongGiaoHangs");

            migrationBuilder.DropTable(
                name: "TinTucs");

            migrationBuilder.DropTable(
                name: "DonGiaoHangs");

            migrationBuilder.DropTable(
                name: "GiaoDichVis");

            migrationBuilder.DropTable(
                name: "KhachHangs");

            migrationBuilder.DropTable(
                name: "LoaiHangs");

            migrationBuilder.DropTable(
                name: "NhanVienGiaoHangs");

            migrationBuilder.DropTable(
                name: "KhuVucs");

            migrationBuilder.DropTable(
                name: "PhuongTiens");

            migrationBuilder.DropTable(
                name: "TaiKhoans");

            migrationBuilder.DropTable(
                name: "BuuCucs");
        }
    }
}
