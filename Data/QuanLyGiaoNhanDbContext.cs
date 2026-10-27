using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;

/*
 * QuanLyGiaoNhanDbContext — Entity → DbContext → Migration → SQL Server
 * ---------------------------------------------------------------------
 * Quan hệ giữa các bảng:
 *   TaiKhoan 1 – 0..1 KhachHang, TaiKhoan 1 – 0..1 NhanVienGiaoHang
 *   KhachHang / LoaiHang / KhuVuc 1 – n DonGiaoHang
 *   DonGiaoHang / NhanVienGiaoHang / PhuongTien 1 – n PhanCongGiaoHang
 *   DonGiaoHang 1 – n LichSuGiaoNhan (tracking event: + bưu cục, shipper, phương tiện, toạ độ)
 *   BuuCuc 1 – n KhuVuc / NhanVienGiaoHang / TaiKhoan (nhân viên bưu cục); DonGiaoHang → bưu cục gửi / nhận / đang giữ
 *   NhanVienGiaoHang 0..1 – 0..1 PhuongTien (1 shipper ↔ 1 phương tiện), lịch sử đổi xe ở LichSuPhuongTien
 * Mọi khóa ngoại DeleteBehavior.Restrict: không xóa dây chuyền làm mất lịch sử.
 */
public class QuanLyGiaoNhanDbContext(DbContextOptions<QuanLyGiaoNhanDbContext> options) : DbContext(options)
{
    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<LoaiHang> LoaiHangs => Set<LoaiHang>();
    public DbSet<KhuVuc> KhuVucs => Set<KhuVuc>();
    public DbSet<DonGiaoHang> DonGiaoHangs => Set<DonGiaoHang>();
    public DbSet<NhanVienGiaoHang> NhanVienGiaoHangs => Set<NhanVienGiaoHang>();
    public DbSet<PhuongTien> PhuongTiens => Set<PhuongTien>();
    public DbSet<PhanCongGiaoHang> PhanCongGiaoHangs => Set<PhanCongGiaoHang>();
    public DbSet<LichSuGiaoNhan> LichSuGiaoNhans => Set<LichSuGiaoNhan>();
    public DbSet<GiaoDichVi> GiaoDichVis => Set<GiaoDichVi>();
    public DbSet<TinTuc> TinTucs => Set<TinTuc>();
    public DbSet<BannerTrangChu> BannerTrangChus => Set<BannerTrangChu>();
    public DbSet<BuuCuc> BuuCucs => Set<BuuCuc>();
    public DbSet<LichSuPhuongTien> LichSuPhuongTiens => Set<LichSuPhuongTien>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<YeuCauLienHe> YeuCauLienHes => Set<YeuCauLienHe>();
    public DbSet<TinNhanLienHe> TinNhanLienHes => Set<TinNhanLienHe>();
    public DbSet<GiaoDichViKhachHang> GiaoDichViKhachHangs => Set<GiaoDichViKhachHang>();
    public DbSet<ChuyenXe> ChuyenXes => Set<ChuyenXe>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<TaiKhoan>(e =>
        {
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.Property(x => x.TenDangNhap).IsUnicode(false);
            e.HasOne(x => x.BuuCuc).WithMany().HasForeignKey(x => x.MaBuuCuc);
        });

        mb.Entity<BuuCuc>(e =>
        {
            e.HasIndex(x => x.TenBuuCuc).IsUnique();
        });

        mb.Entity<KhachHang>(e =>
        {
            e.Property(x => x.SoDuVi).HasColumnType("decimal(18,0)");
            e.HasIndex(x => x.MaTaiKhoan).IsUnique().HasFilter("[MaTaiKhoan] IS NOT NULL");
            e.HasIndex(x => x.SoDienThoai).IsUnique();   // mỗi số điện thoại một khách hàng (in trên nhãn người gửi)
            e.HasOne(x => x.TaiKhoan).WithOne(t => t.KhachHang).HasForeignKey<KhachHang>(x => x.MaTaiKhoan);
        });

        mb.Entity<LoaiHang>(e =>
        {
            e.HasIndex(x => x.TenLoaiHang).IsUnique();
            e.Property(x => x.HeSoPhuThu).HasColumnType("decimal(5,2)");
            e.ToTable(t => t.HasCheckConstraint("CK_LoaiHang_HeSoPhuThu", "[HeSoPhuThu] >= 0"));
        });

        mb.Entity<KhuVuc>(e =>
        {
            e.HasIndex(x => x.TenKhuVuc).IsUnique();
            e.Property(x => x.PhiCoBan).HasColumnType("decimal(18,0)");
            e.ToTable(t => t.HasCheckConstraint("CK_KhuVuc_PhiCoBan", "[PhiCoBan] >= 0"));
            e.HasOne(x => x.BuuCuc).WithMany(b => b.KhuVucs).HasForeignKey(x => x.MaBuuCuc);
        });

        mb.Entity<DonGiaoHang>(e =>
        {
            e.Property(x => x.KhoiLuong).HasColumnType("decimal(10,2)");
            e.Property(x => x.NgayGiaoDuKien).HasColumnType("date");
            foreach (var cot in new[] { nameof(DonGiaoHang.PhiCoBan), nameof(DonGiaoHang.PhuPhiKhoiLuong),
                                        nameof(DonGiaoHang.PhuPhiLoaiHang), nameof(DonGiaoHang.PhiVanChuyen), nameof(DonGiaoHang.TienThuHo),
                                        nameof(DonGiaoHang.PhuPhiKichThuoc) })
                e.Property(cot).HasColumnType("decimal(18,0)");
            e.Property(x => x.KichThuocM3).HasColumnType("decimal(10,4)");
            e.HasIndex(x => x.TrangThai);
            e.HasIndex(x => x.NgayTao);
            e.HasOne(x => x.KhachHang).WithMany(k => k.DonGiaoHangs).HasForeignKey(x => x.MaKhachHang);
            e.HasOne(x => x.LoaiHang).WithMany(l => l.DonGiaoHangs).HasForeignKey(x => x.MaLoaiHang);
            e.HasOne(x => x.KhuVuc).WithMany(k => k.DonGiaoHangs).HasForeignKey(x => x.MaKhuVuc);
            e.HasOne(x => x.BuuCucGui).WithMany().HasForeignKey(x => x.MaBuuCucGui);
            e.HasOne(x => x.BuuCucNhan).WithMany().HasForeignKey(x => x.MaBuuCucNhan);
            e.HasOne(x => x.BuuCucHienTai).WithMany().HasForeignKey(x => x.MaBuuCucHienTai);
            e.HasOne(x => x.NhanVienDangGiu).WithMany().HasForeignKey(x => x.MaNhanVienDangGiu);
            e.HasIndex(x => x.MaBuuCucHienTai);
            e.HasIndex(x => x.MaNhanVienDangGiu);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DonGiaoHang_KhoiLuong", "[KhoiLuong] > 0");
                t.HasCheckConstraint("CK_DonGiaoHang_Phi", "[PhiVanChuyen] >= 0");
                t.HasCheckConstraint("CK_DonGiaoHang_TienThuHo", "[TienThuHo] >= 0");
            });
        });

        mb.Entity<NhanVienGiaoHang>(e =>
        {
            e.HasIndex(x => x.MaTaiKhoan).IsUnique().HasFilter("[MaTaiKhoan] IS NOT NULL");
            e.HasIndex(x => x.SoDienThoai).IsUnique();   // số điện thoại liên hệ của shipper không trùng
            e.HasOne(x => x.TaiKhoan).WithOne(t => t.NhanVienGiaoHang).HasForeignKey<NhanVienGiaoHang>(x => x.MaTaiKhoan);
            e.HasOne(x => x.KhuVucPhuTrach).WithMany(k => k.NhanViens).HasForeignKey(x => x.MaKhuVucPhuTrach);
            e.HasOne(x => x.BuuCuc).WithMany(b => b.NhanViens).HasForeignKey(x => x.MaBuuCuc);
            // 1 shipper ↔ 1 phương tiện: một phương tiện không được gán cho hai shipper
            e.HasOne(x => x.PhuongTien).WithOne(p => p.NhanVien).HasForeignKey<NhanVienGiaoHang>(x => x.MaPhuongTien);
            e.HasIndex(x => x.MaPhuongTien).IsUnique().HasFilter("[MaPhuongTien] IS NOT NULL");
        });

        mb.Entity<PhuongTien>(e =>
        {
            e.HasIndex(x => x.BienSo).IsUnique();
            e.Property(x => x.TaiTrongToiDa).HasColumnType("decimal(10,2)");
            e.ToTable(t => t.HasCheckConstraint("CK_PhuongTien_TaiTrong", "[TaiTrongToiDa] > 0"));
        });

        mb.Entity<PhanCongGiaoHang>(e =>
        {
            e.HasIndex(x => new { x.MaDon, x.TrangThai });
            // Mỗi đơn tối đa MỘT phân công đang hiệu lực (Đã phân công / Đã nhận hàng / Đang giao) – chặn ở mức CSDL
            e.HasIndex(x => x.MaDon).IsUnique().HasFilter("[TrangThai] IN (0, 1, 2)").HasDatabaseName("UX_PhanCong_MotHieuLucMoiDon");
            e.Property(x => x.ThuNhapShipper).HasColumnType("decimal(18,0)");
            e.ToTable(t => t.HasCheckConstraint("CK_PhanCong_ThuNhapShipper", "[ThuNhapShipper] >= 0"));
            e.HasOne(x => x.DonGiaoHang).WithMany(d => d.PhanCongs).HasForeignKey(x => x.MaDon);
            e.HasOne(x => x.NhanVien).WithMany(n => n.PhanCongs).HasForeignKey(x => x.MaNhanVien);
            e.HasOne(x => x.PhuongTien).WithMany(p => p.PhanCongs).HasForeignKey(x => x.MaPhuongTien);
        });

        mb.Entity<LichSuGiaoNhan>(e =>
        {
            e.HasIndex(x => new { x.MaDon, x.ThoiGian });
            e.HasIndex(x => x.ThoiGian);   // trang lịch sử + "hoạt động gần đây" sắp xếp toàn bảng theo thời gian
            e.HasOne(x => x.DonGiaoHang).WithMany(d => d.LichSus).HasForeignKey(x => x.MaDon);
            e.HasOne(x => x.BuuCuc).WithMany().HasForeignKey(x => x.MaBuuCuc);
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNhanVien);
            e.HasOne(x => x.PhuongTien).WithMany().HasForeignKey(x => x.MaPhuongTien);
        });

        mb.Entity<LichSuPhuongTien>(e =>
        {
            e.HasIndex(x => new { x.MaNhanVien, x.ThoiGian });
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNhanVien);
            e.HasOne(x => x.PhuongTienCu).WithMany().HasForeignKey(x => x.MaPhuongTienCu);
            e.HasOne(x => x.PhuongTienMoi).WithMany().HasForeignKey(x => x.MaPhuongTienMoi);
        });

        mb.Entity<GiaoDichVi>(e =>
        {
            e.Property(x => x.SoTien).HasColumnType("decimal(18,0)");
            e.HasIndex(x => new { x.MaNhanVien, x.NgayTao });
            e.HasIndex(x => x.TrangThai);
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNhanVien);
            e.ToTable(t => t.HasCheckConstraint("CK_GiaoDichVi_SoTien", "[SoTien] >= 0"));
        });
        mb.Entity<DonGiaoHang>().HasOne(x => x.GiaoDichNop).WithMany(g => g.DonNop).HasForeignKey(x => x.MaGiaoDichNop);

        // Tin tức trang chủ – 6 bài mẫu nạp một lần qua migration, sau đó Quản trị tự quản lý
        mb.Entity<TinTuc>(e =>
        {
            e.Property(x => x.NoiDung).HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.HienThi, x.NgayDang });
            e.HasData(TinTucXuLy.TinTucMau);
        });

        // Banner trang chủ – 3 banner mẫu dùng ảnh minh họa trong wwwroot/img/banner/
        mb.Entity<BannerTrangChu>(e =>
        {
            e.HasIndex(x => new { x.HienThi, x.ThuTu });
            e.HasData(
                new BannerTrangChu { MaBanner = 1, ThuTu = 1, Anh = "/img/banner/banner-1.svg", NgayTao = new DateTime(2026, 9, 1),
                    TieuDe = "Giao hàng nhanh, theo dõi minh bạch từng bước", MoTa = "Gửi tại bưu cục gần nhất, quét mã ở mọi chặng, phí hiển thị ngay khi tạo đơn.",
                    ChuNut = "Tạo đơn ngay", LienKet = "/DangNhap/DangKy" },
                new BannerTrangChu { MaBanner = 2, ThuTu = 2, Anh = "/img/banner/banner-2.svg", NgayTao = new DateTime(2026, 9, 1),
                    TieuDe = "Thu hộ tiền hàng an toàn, đối soát rõ ràng", MoTa = "Tiền thu hộ được nộp về công ty và trả lại người gửi sau khi đối soát.",
                    ChuNut = "Tìm hiểu thêm", LienKet = "/TrangChu/ChiTietTinTuc/2" },
                new BannerTrangChu { MaBanner = 3, ThuTu = 3, Anh = "/img/banner/banner-3.svg", NgayTao = new DateTime(2026, 9, 1),
                    TieuDe = "Tra cứu hành trình đơn hàng mọi lúc", MoTa = "Chỉ cần mã đơn và số điện thoại người nhận.",
                    ChuNut = "Tra cứu đơn", LienKet = "/TrangChu/TraCuu" });
        });

        mb.Entity<ThongBao>(e =>
        {
            e.HasIndex(x => new { x.MaTaiKhoan, x.DaDoc, x.NgayTao });
            e.HasOne(x => x.TaiKhoan).WithMany().HasForeignKey(x => x.MaTaiKhoan);
        });

        mb.Entity<YeuCauLienHe>(e =>
        {
            e.HasIndex(x => new { x.MaKhachHang, x.NgayGui });
            e.HasIndex(x => x.TrangThai);
            e.HasOne(x => x.KhachHang).WithMany().HasForeignKey(x => x.MaKhachHang);
            e.HasOne(x => x.DonHang).WithMany().HasForeignKey(x => x.MaDonHang);
            e.HasOne(x => x.BuuCuc).WithMany().HasForeignKey(x => x.MaBuuCuc);
            e.HasMany(x => x.TinNhans).WithOne(t => t.LienHe).HasForeignKey(t => t.MaLienHe).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<TinNhanLienHe>(e =>
        {
            e.HasIndex(x => new { x.MaLienHe, x.NgayGui });
            e.HasOne(x => x.TaiKhoan).WithMany().HasForeignKey(x => x.MaTaiKhoan);
        });

        mb.Entity<ChuyenXe>(e =>
        {
            e.HasKey(x => x.MaChuyen);
            e.HasIndex(x => x.TrangThai);
            e.HasIndex(x => x.MaBuuCucGui);
            e.HasIndex(x => x.MaBuuCucNhan);
            e.HasOne(x => x.BuuCucGui).WithMany().HasForeignKey(x => x.MaBuuCucGui).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BuuCucNhan).WithMany().HasForeignKey(x => x.MaBuuCucNhan).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BuuCucTrungGian).WithMany().HasForeignKey(x => x.MaBuuCucTrungGian).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PhuongTien).WithMany().HasForeignKey(x => x.MaPhuongTien).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.DonHangs).WithOne(d => d.ChuyenXe).HasForeignKey(d => d.MaChuyenXe).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<GiaoDichViKhachHang>(e =>
        {
            e.Property(x => x.SoTien).HasColumnType("decimal(18,0)");
            e.Property(x => x.SoDuSauGiaoDich).HasColumnType("decimal(18,0)");
            e.HasIndex(x => new { x.MaKhachHang, x.NgayTao });
            e.HasIndex(x => x.TrangThai);
            e.HasOne(x => x.KhachHang).WithMany(k => k.GiaoDichVis).HasForeignKey(x => x.MaKhachHang);
            e.HasOne(x => x.DonHang).WithMany().HasForeignKey(x => x.MaDonHang).IsRequired(false);
            e.ToTable(t => t.HasCheckConstraint("CK_GiaoDichViKhachHang_SoTien", "[SoTien] >= 0"));
        });

        // Không xóa dây chuyền – bảo toàn lịch sử giao nhận
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            if (fk.DeclaringEntityType.ClrType != typeof(TinNhanLienHe))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    // ===================== DỮ LIỆU MẪU =====================

    public record DuLieuTinhThanhMau(
        string Ma, string TenTinh, string TenBuuCuc, string DiaChi, string SoDienThoai,
        double ViDo, double KinhDo, string TenNv,
        string BienSoXeTai, string TenSpTai,
        string BienSoXeMay, string TenSpMay
    );

    /// <summary>34 bưu cục tương ứng với 34 tỉnh thành trọng điểm tại Việt Nam.</summary>
    public static readonly DuLieuTinhThanhMau[] DanhSach34Tinh =
    [
        // Miền Bắc (15 tỉnh/thành)
        new("hanoi", "Hà Nội", "Bưu cục Hà Nội", "88 Xuân Thủy, Cầu Giấy, Hà Nội", "0243888001", 21.0368, 105.7825, "Trần Thu Hà", "29C-101.01", "Lâm Văn Khoa", "29B1-102.02", "Nguyễn Đức Anh"),
        new("haiphong", "Hải Phòng", "Bưu cục Hải Phòng", "15 Điện Biên Phủ, Hồng Bàng, Hải Phòng", "0225388002", 20.8606, 106.6818, "Vũ Thị Lan", "15C-101.01", "Trần Đình Trọng", "15B1-102.02", "Bùi Tiến Dũng"),
        new("quangninh", "Quảng Ninh", "Bưu cục Quảng Ninh", "56 Lê Thánh Tông, Hạ Long, Quảng Ninh", "0203388003", 20.9500, 107.0800, "Nguyễn Văn Cường", "14C-101.01", "Đào Văn Tuấn", "14B1-102.02", "Hoàng Thế Vinh"),
        new("haiduong", "Hải Dương", "Bưu cục Hải Dương", "1 Trương Mỹ, TP. Hải Dương", "0220388004", 20.9380, 106.3150, "Đỗ Thị Quỳnh", "34C-101.01", "Phạm Văn Bình", "34B1-102.02", "Vũ Quốc Đạt"),
        new("hungyen", "Hưng Yên", "Bưu cục Hưng Yên", "2 Điện Biên, TP. Hưng Yên", "0221388005", 20.6464, 106.0511, "Phạm Ngọc Mai", "89C-101.01", "Trần Đình Toàn", "89B1-102.02", "Ngô Đức Duy"),
        new("bacninh", "Bắc Ninh", "Bưu cục Bắc Ninh", "102 Lý Thái Tổ, TP. Bắc Ninh", "0222388006", 21.1861, 106.0763, "Lê Thị Mai", "99C-101.01", "Hoàng Văn Em", "99B1-102.02", "Nguyễn Tiến Đạt"),
        new("bacgiang", "Bắc Giang", "Bưu cục Bắc Giang", "82 Hùng Vương, TP. Bắc Giang", "0204388007", 21.2731, 106.1946, "Dương Hồng Nhung", "98C-101.01", "Lê Văn Hậu", "98B1-102.02", "Trần Trọng Hiếu"),
        new("thainguyen", "Thái Nguyên", "Bưu cục Thái Nguyên", "10 Lương Ngọc Quyến, TP. Thái Nguyên", "0208388008", 21.5928, 105.8442, "Hoàng Thị Thảo", "20C-101.01", "Bùi Văn Nam", "20B1-102.02", "Đặng Văn Lâm"),
        new("vinhphuc", "Vĩnh Phúc", "Bưu cục Vĩnh Phúc", "39 Mê Linh, TP. Vĩnh Yên, Vĩnh Phúc", "0211388009", 21.3090, 105.6049, "Nguyễn Thị Yến", "88C-101.01", "Phùng Anh Dũng", "88B1-102.02", "Lý Thanh Hải"),
        new("phutho", "Phú Thọ", "Bưu cục Phú Thọ", "1220 Hùng Vương, TP. Việt Trì, Phú Thọ", "0210388010", 21.3228, 105.4019, "Nguyễn Khánh Ly", "19C-101.01", "Cao Văn Thắng", "19B1-102.02", "Đinh Hữu Phước"),
        new("namdinh", "Nam Định", "Bưu cục Nam Định", "4 Hà Huy Tập, TP. Nam Định", "0228388011", 20.4344, 106.1773, "Vũ Thùy Linh", "18C-101.01", "Tạ Đình Hưng", "18B1-102.02", "Nguyễn Văn Chiến"),
        new("thaibinh", "Thái Bình", "Bưu cục Thái Bình", "358 Lê Thái Tổ, TP. Thái Bình", "0227388012", 20.4463, 106.3366, "Đoàn Mỹ Duyên", "17C-101.01", "Bùi Quang Minh", "17B1-102.02", "Lê Hồng Quân"),
        new("ninhbinh", "Ninh Bình", "Bưu cục Ninh Bình", "18 Tràng An, TP. Ninh Bình", "0229388013", 20.2506, 105.9745, "Trần Diệu Hương", "35C-101.01", "Trịnh Xuân Bách", "35B1-102.02", "Lê Văn Tuyên"),
        new("langson", "Lạng Sơn", "Bưu cục Lạng Sơn", "12 Lê Lợi, TP. Lạng Sơn", "0205388014", 21.8537, 106.7615, "Lộc Thị Nga", "12C-101.01", "Hoàng Văn Thái", "12B1-102.02", "Vi Văn Khiêm"),
        new("laocai", "Lào Cai", "Bưu cục Lào Cai", "68 Hoàng Liên, TP. Lào Cai", "0214388015", 22.4856, 103.9707, "Nông Thị Hằng", "24C-101.01", "Vàng Seo Sùng", "24B1-102.02", "Đào Duy Kiên"),

        // Miền Trung & Tây Nguyên (11 tỉnh/thành)
        new("thanhhoa", "Thanh Hóa", "Bưu cục Thanh Hóa", "28 Trần Phú, TP. Thanh Hóa", "0237388016", 19.8067, 105.7852, "Lê Thị Bích", "36C-101.01", "Lê Xuân Trường", "36B1-102.02", "Trần Bá Đạt"),
        new("nghean", "Nghệ An", "Bưu cục Nghệ An", "45 Lê Lợi, TP. Vinh, Nghệ An", "0238388017", 18.6796, 105.6813, "Nguyễn Thị Hà", "37C-101.01", "Nguyễn Cảnh Toàn", "37B1-102.02", "Phan Hữu Thọ"),
        new("hatinh", "Hà Tĩnh", "Bưu cục Hà Tĩnh", "16 Trần Phú, TP. Hà Tĩnh", "0239388018", 18.3435, 105.9058, "Phan Thị Thu", "38C-101.01", "Võ Văn Kiên", "38B1-102.02", "Trần Trọng Nghĩa"),
        new("hue", "Thừa Thiên Huế", "Bưu cục Thừa Thiên Huế", "8 Hoàng Hoa Thám, TP. Huế", "0234388019", 16.4637, 107.5909, "Hồ Thị Hương", "75C-101.01", "Trần Vĩnh Phước", "75B1-102.02", "Nguyễn Đắc Huy"),
        new("danang", "Đà Nẵng", "Bưu cục Đà Nẵng", "18 Bạch Đằng, Hải Châu, Đà Nẵng", "0236388020", 16.0718, 108.2240, "Hồ Văn Sơn", "43C-101.01", "Võ Quốc Tuấn", "43C1-102.02", "Đinh Công Phúc"),
        new("quangnam", "Quảng Nam", "Bưu cục Quảng Nam", "56 Hùng Vương, TP. Tam Kỳ, Quảng Nam", "0235388021", 15.5684, 108.4812, "Huỳnh Thị Vân", "92C-101.01", "Lương Viết Hùng", "92B1-102.02", "Đặng Văn Phát"),
        new("quangngai", "Quảng Ngãi", "Bưu cục Quảng Ngãi", "82 Hùng Vương, TP. Quảng Ngãi", "0255388022", 15.1213, 108.7923, "Nguyễn Thị Mơ", "76C-101.01", "Võ Thành Long", "76B1-102.02", "Bùi Đức Thịnh"),
        new("binhdinh", "Bình Định", "Bưu cục Bình Định", "127 Hai Bà Trưng, TP. Quy Nhơn, Bình Định", "0256388023", 13.7820, 109.2197, "Đặng Thúy Nga", "77C-101.01", "Trương Quang Hải", "77B1-102.02", "Nguyễn Phi Long"),
        new("khanhhoa", "Khánh Hòa", "Bưu cục Khánh Hòa", "2 Trần Phú, TP. Nha Trang, Khánh Hòa", "0258388024", 12.2388, 109.1967, "Võ Thanh Trúc", "79C-101.01", "Nguyễn Minh Thông", "79B1-102.02", "Lê Tuấn Kiệt"),
        new("daklak", "Đắk Lắk", "Bưu cục Đắk Lắk", "14 Hai Bà Trưng, TP. Buôn Ma Thuột", "0262388025", 12.6800, 108.0378, "H'Hen Niê", "47C-101.01", "Y Krăk Êban", "47B1-102.02", "Nguyễn Quốc Anh"),
        new("lamdong", "Lâm Đồng", "Bưu cục Lâm Đồng", "14 Trần Phú, TP. Đà Lạt, Lâm Đồng", "0263388026", 11.9404, 108.4377, "Phan Bích Thủy", "49C-101.01", "Đoàn Minh Trí", "49B1-102.02", "Vũ Hoàng Sơn"),

        // Miền Nam (8 tỉnh/thành)
        new("hcm", "TP. Hồ Chí Minh", "Bưu cục TP. Hồ Chí Minh", "250 Nguyễn Thị Minh Khai, Quận 3, TP.HCM", "0283888027", 10.7769, 106.6889, "Nguyễn Thanh Tùng", "51C-101.01", "Nguyễn Văn Hùng", "59E1-102.02", "Trương Minh Nhật"),
        new("binhduong", "Bình Dương", "Bưu cục Bình Dương", "324 Đại lộ Bình Dương, TP. Thủ Dầu Một", "0274388028", 10.9805, 106.6519, "Trần Kim Oanh", "61C-101.01", "Trần Trọng Hữu", "61B1-102.02", "Phạm Văn Lộc"),
        new("dongnai", "Đồng Nai", "Bưu cục Đồng Nai", "48 Đồng Khởi, TP. Biên Hòa, Đồng Nai", "0251388029", 10.9574, 106.8427, "Nguyễn Diễm My", "60C-101.01", "Lê Thanh Điền", "60B1-102.02", "Võ Tấn Thành"),
        new("vungtau", "Bà Rịa - Vũng Tàu", "Bưu cục Bà Rịa - Vũng Tàu", "40 Lê Ngọc Hân, TP. Vũng Tàu", "0254388030", 10.3460, 107.0843, "Lê Thảo My", "72C-101.01", "Nguyễn Hải Đăng", "72B1-102.02", "Huỳnh Văn Sang"),
        new("longan", "Long An", "Bưu cục Long An", "26 Trương Định, TP. Tân An, Long An", "0272388031", 10.5362, 106.4114, "Đỗ Hoài Phương", "62C-101.01", "Mai Văn Được", "62B1-102.02", "Nguyễn Phúc Thịnh"),
        new("tiengiang", "Tiền Giang", "Bưu cục Tiền Giang", "15 Nam Kỳ Khởi Nghĩa, TP. Mỹ Tho", "0273388032", 10.3545, 106.3639, "Huỳnh Ngọc Trâm", "63C-101.01", "Trần Hữu Danh", "63B1-102.02", "Dương Tấn Đạt"),
        new("cantho", "Cần Thơ", "Bưu cục Cần Thơ", "2 Hòa Bình, Ninh Kiều, Cần Thơ", "0292388033", 10.0452, 105.7469, "Phạm Thúy Hằng", "65C-101.01", "Võ Minh Khang", "65B1-102.02", "Lâm Quốc Cường"),
        new("angiang", "An Giang", "Bưu cục An Giang", "10 Hai Bà Trưng, TP. Long Xuyên, An Giang", "0296388034", 10.3759, 105.4387, "Trần Kim Ngân", "67C-101.01", "Nguyễn Tấn Bửu", "67B1-102.02", "Lý Thanh Bình")
    ];

    public static async Task NapDuLieuMauAsync(QuanLyGiaoNhanDbContext db, bool coDonMau = true)
    {
        if (await db.TaiKhoans.AnyAsync())
        {
            await BoSungDuLieuConThieuAsync(db);
            return;
        }

        var bayGio = DateTime.Now;
        string matKhau = MatKhauHelper.Bam("123456");
        TaiKhoan TaoTk(string ten, string hoTen, string email, string vaiTro, int ngayTruoc) => new()
        {
            TenDangNhap = ten, MatKhau = matKhau, HoTen = hoTen, Email = email, VaiTro = vaiTro, NgayTao = bayGio.AddDays(-ngayTruoc)
        };

        // 1. Tài khoản quản lý
        db.TaiKhoans.Add(TaoTk("admin", "Quản trị hệ thống", "admin@giaonhan.vn", VaiTroNguoiDung.QuanTri, 90));

        // 2. Loại hàng
        var dsLoai = new List<LoaiHang>
        {
            new() { TenLoaiHang = "Hàng thông thường", HeSoPhuThu = 0m, MoTa = "Quần áo, giấy tờ, đồ gia dụng thông thường" },
            new() { TenLoaiHang = "Hàng dễ vỡ", HeSoPhuThu = 0.30m, MoTa = "Gốm sứ, thủy tinh – cần chèn lót, dán nhãn dễ vỡ" },
            new() { TenLoaiHang = "Thực phẩm khô", HeSoPhuThu = 0.10m, MoTa = "Đồ khô đóng gói kín, không cần bảo quản lạnh" },
            new() { TenLoaiHang = "Điện tử – giá trị cao", HeSoPhuThu = 0.20m, MoTa = "Điện thoại, laptop, thiết bị điện tử" },
            new() { TenLoaiHang = "Hàng đông lạnh", HeSoPhuThu = 0.50m, MoTa = "Tạm ngừng nhận do chưa có xe lạnh", TrangThai = TrangThaiHoatDong.NgungHoatDong },
        };
        db.LoaiHangs.AddRange(dsLoai);

        // 3. 34 Bưu cục tương ứng với 34 tỉnh thành
        var dsBuuCuc = DanhSach34Tinh.Select(t => new BuuCuc
        {
            TenBuuCuc = t.TenBuuCuc, TinhThanh = t.TenTinh, DiaChi = t.DiaChi, SoDienThoai = t.SoDienThoai,
            ViDo = t.ViDo, KinhDo = t.KinhDo, TrangThai = TrangThaiHoatDong.HoatDong
        }).ToList();
        db.BuuCucs.AddRange(dsBuuCuc);

        // 34 Khu vực phụ trách phát hàng (mỗi khu vực do bưu cục tỉnh đó đảm nhận)
        var dsKhuVuc = DanhSach34Tinh.Select((t, i) => new KhuVuc
        {
            TenKhuVuc = $"Khu vực {t.TenTinh}", PhiCoBan = 18_000m,
            MoTa = $"Khu vực giao nhận tỉnh {t.TenTinh}", BuuCuc = dsBuuCuc[i],
            TrangThai = TrangThaiHoatDong.HoatDong
        }).ToList();
        db.KhuVucs.AddRange(dsKhuVuc);

        // 4. Nhân viên bưu cục (mã dạng nv.{tinh}, vd: nv.hanoi, nv.bacninh, nv.danang...)
        var dsTkBuuCuc = DanhSach34Tinh.Select((t, i) =>
        {
            var tk = TaoTk($"nv.{t.Ma}", t.TenNv, $"nv.{t.Ma}@giaonhan.vn", VaiTroNguoiDung.NhanVien, 88);
            tk.BuuCuc = dsBuuCuc[i];
            return tk;
        }).ToList();
        db.TaiKhoans.AddRange(dsTkBuuCuc);

        // 5. Khách hàng (có tài khoản đăng nhập)
        string[] hoTenKh = ["Nguyễn Văn An", "Trần Thị Bình", "Lê Minh Châu", "Phạm Quốc Dũng", "Hoàng Thu Giang",
                            "Võ Thanh Hải", "Đặng Mỹ Hạnh", "Bùi Gia Khánh", "Đỗ Ngọc Lan", "Huỳnh Tấn Phát",
                            "Ngô Bảo Quyên", "Dương Nhật Tân", "Lý Hoài Thương", "Trịnh Công Vinh", "Mai Anh Thư",
                            "Phan Đức Long", "Vũ Hồng Nhung", "Tạ Quang Minh", "Kiều Thanh Tâm", "Lương Văn Sơn"];
        string[] duong = ["Nguyễn Trãi", "Lê Lợi", "Trần Hưng Đạo", "Hai Bà Trưng", "Phạm Văn Đồng", "Láng Hạ",
                          "Xuân Thủy", "Nguyễn Chí Thanh", "Giải Phóng", "Kim Mã"];
        var dsKhach = new List<KhachHang>();
        for (int i = 0; i < 20; i++)
        {
            var tk = TaoTk($"kh{i + 1:D2}", hoTenKh[i], $"kh{i + 1:D2}@gmail.com", VaiTroNguoiDung.KhachHang, 80 - i * 3);
            if (i == 19) tk.TrangThai = TrangThaiTaiKhoan.BiKhoa;
            dsKhach.Add(new KhachHang
            {
                TaiKhoan = tk, HoTen = hoTenKh[i], SoDienThoai = $"09{12345600 + i * 137:D8}", Email = tk.Email,
                DiaChi = $"{(i * 7) % 200 + 1} {duong[i % duong.Length]}, Hà Nội", NgayDangKy = tk.NgayTao
            });
        }
        db.KhachHangs.AddRange(dsKhach);

        // 6. Phương tiện: 68 phương tiện cho 34 bưu cục (mỗi bưu cục 1 xe tải và 1 xe máy)
        var dsPhuongTien = new List<PhuongTien>();
        foreach (var t in DanhSach34Tinh)
        {
            dsPhuongTien.Add(new PhuongTien { BienSo = t.BienSoXeTai, LoaiPhuongTien = "Xe tải 2 tấn", TaiTrongToiDa = 2000, TrangThai = TrangThaiPhuongTien.SanSang, GhiChu = $"Tuyến liên tỉnh {t.TenTinh}" });
            dsPhuongTien.Add(new PhuongTien { BienSo = t.BienSoXeMay, LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 100, TrangThai = TrangThaiPhuongTien.SanSang, GhiChu = $"Giao hàng {t.TenTinh}" });
        }
        db.PhuongTiens.AddRange(dsPhuongTien);

        // 7. Shippers: mỗi bưu cục 2 shipper (1 xe tải sp.{tinh}01, 1 xe máy sp.{tinh}02)
        var dsNhanVien = new List<NhanVienGiaoHang>();
        for (int i = 0; i < DanhSach34Tinh.Length; i++)
        {
            var t = DanhSach34Tinh[i];
            var bc = dsBuuCuc[i];
            var kv = dsKhuVuc[i];
            var ptTai = dsPhuongTien[i * 2];
            var ptMay = dsPhuongTien[i * 2 + 1];

            // 1 xe tải: mã sp.{tinh}01
            var tkTai = TaoTk($"sp.{t.Ma}01", t.TenSpTai, $"sp.{t.Ma}01@giaonhan.vn", VaiTroNguoiDung.GiaoHang, 85);
            dsNhanVien.Add(new NhanVienGiaoHang
            {
                TaiKhoan = tkTai, HoTen = t.TenSpTai, SoDienThoai = $"08{61000000 + i * 200 + 1:D8}",
                Email = $"sp.{t.Ma}01@giaonhan.vn", LoaiShipper = LoaiShipper.LienTinh, BuuCuc = bc,
                PhuongTien = ptTai, TrangThai = TrangThaiNhanVien.SanSang
            });

            // 1 xe máy: mã sp.{tinh}02
            var tkMay = TaoTk($"sp.{t.Ma}02", t.TenSpMay, $"sp.{t.Ma}02@giaonhan.vn", VaiTroNguoiDung.GiaoHang, 85);
            dsNhanVien.Add(new NhanVienGiaoHang
            {
                TaiKhoan = tkMay, HoTen = t.TenSpMay, SoDienThoai = $"08{61000000 + i * 200 + 2:D8}",
                Email = $"sp.{t.Ma}02@giaonhan.vn", LoaiShipper = LoaiShipper.KhuVuc, BuuCuc = bc,
                KhuVucPhuTrach = kv, PhuongTien = ptMay, TrangThai = TrangThaiNhanVien.SanSang
            });
        }
        db.NhanVienGiaoHangs.AddRange(dsNhanVien);
        await db.SaveChangesAsync();

        foreach (var nv in dsNhanVien.Where(n => n.MaPhuongTien is not null))
            db.LichSuPhuongTiens.Add(new LichSuPhuongTien
            {
                MaNhanVien = nv.MaNhanVien, MaPhuongTienMoi = nv.MaPhuongTien, ThoiGian = bayGio.AddDays(-85),
                NguoiThucHien = "Quản trị hệ thống (Quản trị)", LyDo = "Gán phương tiện khi nhận việc"
            });
        await db.SaveChangesAsync();

        await BoSungDuLieuConThieuAsync(db);

        if (coDonMau)
        {
            await TaoDonMauAsync(db, dsKhach, dsLoai.Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).ToList(),
                                 dsKhuVuc.Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).ToList(), dsBuuCuc, dsTkBuuCuc,
                                 $"{dsTkBuuCuc[0].HoTen} (Nhân viên)");
            await TaoGiaoDichViMauAsync(db, $"{dsTkBuuCuc[0].HoTen} (Nhân viên)");
            await TaoViTriMauAsync(db);
        }
    }

    /// <summary>
    /// Lịch sử ví shipper mẫu: mỗi shipper có 1 lệnh nộp đã xác nhận cho các đơn đã hoàn tất (đối soát),
    /// 1 lần rút tiền đã chuyển khoản; thêm 1 lệnh nộp chờ xác nhận, 1 yêu cầu rút chờ duyệt và 1 yêu cầu bị từ chối.
    /// </summary>
    private static async Task TaoGiaoDichViMauAsync(QuanLyGiaoNhanDbContext db, string nguoiXuLy)
    {
        string[] dsNganHang = ["Vietcombank", "Techcombank", "MB Bank", "BIDV", "VietinBank", "ACB"];
        string KhongDau(string s) => new string(s.Normalize(System.Text.NormalizationForm.FormD)
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray())
            .Replace('đ', 'd').Replace('Đ', 'D').ToUpper();

        var dsNv = await db.NhanVienGiaoHangs.OrderBy(n => n.MaNhanVien).ToListAsync();
        var donThanhCong = await db.PhanCongGiaoHangs.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong)
            .Select(p => new { p.MaNhanVien, Don = p.DonGiaoHang! }).ToListAsync();
        // Thu nhập = hoa hồng các chặng hoàn thành (giao thành công + bàn giao liên tỉnh)
        var thuNhapNv = await db.PhanCongGiaoHangs.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc)
            .GroupBy(p => p.MaNhanVien).Select(g => new { g.Key, Tien = g.Sum(p => p.ThuNhapShipper) }).ToDictionaryAsync(x => x.Key, x => x.Tien);
        bool daTaoNopCho = false, daTaoRutCho = false, daTaoTuChoi = false;

        foreach (var nv in dsNv)
        {
            var cuaNv = donThanhCong.Where(x => x.MaNhanVien == nv.MaNhanVien).Select(x => x.Don).ToList();
            decimal thuNhap = thuNhapNv.GetValueOrDefault(nv.MaNhanVien);
            if (thuNhap == 0) continue;
            string taiKhoan = $"{dsNganHang[nv.MaNhanVien % dsNganHang.Length]} – {1_000_000_000L + nv.MaNhanVien * 7_654_321L} – {KhongDau(nv.HoTen)}";

            // 1. Đơn đã hoàn tất → lệnh nộp tiền đã được xác nhận vào ngày hoàn tất gần nhất
            var daHoanTat = cuaNv.Where(d => d.TrangThai == TrangThaiDon.HoanTat).ToList();
            if (daHoanTat.Count > 0)
            {
                var ngay = daHoanTat.Max(d => d.NgayHoanTat ?? d.NgayTao);
                var gd = new GiaoDichVi
                {
                    MaNhanVien = nv.MaNhanVien, Loai = LoaiGiaoDich.NopTienThuHo, SoTien = daHoanTat.Sum(d => d.TongThuNguoiNhan),
                    PhuongThuc = "Chuyển khoản ngân hàng", ThongTinThanhToan = $"FT{ngay:yyMMdd}{nv.MaNhanVien:D3}", GhiChu = $"Nộp tiền {daHoanTat.Count} đơn",
                    NgayTao = ngay.AddHours(-2), NgayXuLy = ngay, NguoiXuLy = nguoiXuLy, TrangThai = TrangThaiGiaoDich.DaXacNhan
                };
                db.GiaoDichVis.Add(gd);
                foreach (var d in daHoanTat) d.GiaoDichNop = gd;
            }

            // 2. Đã rút khoảng một nửa thu nhập (hoa hồng) về ngân hàng
            decimal daRut = Math.Floor(thuNhap / 2 / 10_000) * 10_000;
            if (daRut >= 50_000)
                db.GiaoDichVis.Add(new GiaoDichVi
                {
                    MaNhanVien = nv.MaNhanVien, Loai = LoaiGiaoDich.RutTien, SoTien = daRut, PhuongThuc = "Chuyển khoản ngân hàng",
                    ThongTinThanhToan = taiKhoan, NgayTao = DateTime.Now.AddDays(-6).AddHours(-nv.MaNhanVien),
                    NgayXuLy = DateTime.Now.AddDays(-5), NguoiXuLy = nguoiXuLy, TrangThai = TrangThaiGiaoDich.DaXacNhan
                });

            var dangGiu = cuaNv.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong).ToList();
            decimal conLai = thuNhap - daRut;
            // 3. Một shipper (không phải người đầu tiên) đã tạo lệnh nộp, đang chờ công ty xác nhận
            if (!daTaoNopCho && dangGiu.Count > 0 && nv.MaNhanVien != dsNv[0].MaNhanVien)
            {
                var gd = new GiaoDichVi
                {
                    MaNhanVien = nv.MaNhanVien, Loai = LoaiGiaoDich.NopTienThuHo, SoTien = dangGiu.Sum(d => d.TongThuNguoiNhan),
                    PhuongThuc = "Ví MoMo", ThongTinThanhToan = $"MOMO{DateTime.Now:yyMMdd}{nv.MaNhanVien:D3}", GhiChu = $"Nộp tiền {dangGiu.Count} đơn",
                    NgayTao = DateTime.Now.AddHours(-1)
                };
                db.GiaoDichVis.Add(gd);
                foreach (var d in dangGiu) d.GiaoDichNop = gd;
                daTaoNopCho = true;
            }
            // 4. Yêu cầu rút đang chờ chuyển khoản (chỉ shipper đã nộp hết tiền thu hộ) và một yêu cầu bị từ chối (sai tài khoản)
            else if (conLai >= 60_000 && nv.MaNhanVien != dsNv[0].MaNhanVien)
            {
                if (!daTaoRutCho && dangGiu.Count == 0)
                {
                    db.GiaoDichVis.Add(new GiaoDichVi
                    {
                        MaNhanVien = nv.MaNhanVien, Loai = LoaiGiaoDich.RutTien, SoTien = Math.Max(50_000, Math.Floor(conLai / 2 / 10_000) * 10_000),   // tối thiểu = ViXuLy.RutToiThieu
                        PhuongThuc = "Chuyển khoản ngân hàng", ThongTinThanhToan = taiKhoan, NgayTao = DateTime.Now.AddHours(-3)
                    });
                    daTaoRutCho = true;
                }
                else if (!daTaoTuChoi)
                {
                    db.GiaoDichVis.Add(new GiaoDichVi
                    {
                        MaNhanVien = nv.MaNhanVien, Loai = LoaiGiaoDich.RutTien, SoTien = 50_000, PhuongThuc = "Chuyển khoản ngân hàng",
                        ThongTinThanhToan = $"Agribank – 12345 – {KhongDau(nv.HoTen)}", NgayTao = DateTime.Now.AddDays(-2),
                        NgayXuLy = DateTime.Now.AddDays(-2).AddHours(1), NguoiXuLy = nguoiXuLy, TrangThai = TrangThaiGiaoDich.TuChoi,
                        GhiChu = "Từ chối: Số tài khoản không tồn tại – vui lòng kiểm tra lại"
                    });
                    daTaoTuChoi = true;
                }
            }
        }

        // 5. Phí người gửi (không trừ hết vào thu hộ): đơn hoàn tất quá 7 ngày coi như khách đã thanh toán, đơn gần đây còn công nợ
        var hanThu = DateTime.Now.AddDays(-7);
        foreach (var don in await DonHangXuLy.DonConNoPhi(db.DonGiaoHangs).Where(d => d.NgayHoanTat < hanThu).ToListAsync())
            don.NgayThuPhiNguoiGui = don.NgayHoanTat!.Value.AddDays(1);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Vị trí GPS mẫu: shipper đang giữ hàng ở gần bưu cục của mình; shipper liên tỉnh đang chạy thì ở giữa tuyến
    /// (khoảng 40% quãng đường từ bưu cục gửi tới bưu cục nhận của đơn đầu tiên đang trung chuyển).
    /// </summary>
    private static async Task TaoViTriMauAsync(QuanLyGiaoNhanDbContext db)
    {
        var ngauNhien = new Random(7);
        var dsNv = await db.NhanVienGiaoHangs.Include(n => n.BuuCuc).Where(n => n.MaPhuongTien != null).ToListAsync();
        foreach (var nv in dsNv)
        {
            var tuyen = await db.DonGiaoHangs.Where(d => d.MaNhanVienDangGiu == nv.MaNhanVien && d.TrangThai == TrangThaiDon.DangTrungChuyen)
                .Select(d => new { Di = d.BuuCucGui, Den = d.BuuCucNhan }).FirstOrDefaultAsync();
            bool dangGiu = await db.DonGiaoHangs.AnyAsync(d => d.MaNhanVienDangGiu == nv.MaNhanVien);
            if (!dangGiu || nv.BuuCuc?.ViDo is null) continue;
            if (tuyen is { Di.ViDo: not null, Den.ViDo: not null })
            {
                nv.ViDo = tuyen.Di.ViDo + (tuyen.Den.ViDo - tuyen.Di.ViDo) * 0.4;
                nv.KinhDo = tuyen.Di.KinhDo + (tuyen.Den.KinhDo - tuyen.Di.KinhDo) * 0.4;
            }
            else
            {
                nv.ViDo = nv.BuuCuc.ViDo + (ngauNhien.NextDouble() - 0.5) * 0.04;
                nv.KinhDo = nv.BuuCuc.KinhDo + (ngauNhien.NextDouble() - 0.5) * 0.04;
            }
            nv.ThoiGianViTri = DateTime.Now.AddMinutes(-ngauNhien.Next(1, 12));
        }
        await NapThongBaoMauAsync(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Chặng cuối cùng mỗi đơn mẫu dừng lại.</summary>
    private enum KichBan
    {
        ChoTiepNhan, TaiBuuCucGui, ChoLayLienTinh, DangTrungChuyen, TaiBuuCucNhan, ChoLayGiaoHang, DoiShipper, DangGiao,
        ThatBai, HenGiaoLai, ThatBaiTraVeRoiGiao, ThanhCong, HoanTat, HuyNgay, HuyTaiBuuCuc, HuyDaHoanHang, ThatLac
    }

    private static async Task TaoDonMauAsync(QuanLyGiaoNhanDbContext db, List<KhachHang> dsKhach, List<LoaiHang> dsLoai,
                                             List<KhuVuc> dsKhuVuc, List<BuuCuc> dsBuuCuc, List<TaiKhoan> dsTkBuuCuc, string nhanVien)
    {
        var xuLy = new DonHangXuLy(db);
        var ngauNhien = new Random(2026);
        var bayGioThat = DateTime.Now;
        DateTime dongHo = bayGioThat;
        xuLy.BayGio = () => dongHo < bayGioThat ? dongHo : bayGioThat;

        // Người thao tác: nhân viên của từng bưu cục (quét tại bưu cục, toạ độ = bưu cục), shipper (theo tài khoản)
        ThaoTac TaiBuuCuc(int? maBuuCuc)
        {
            var bc = dsBuuCuc.First(b => b.MaBuuCuc == maBuuCuc);
            var tk = dsTkBuuCuc.FirstOrDefault(t => t.MaBuuCuc == maBuuCuc);
            return new ThaoTac(tk is null ? nhanVien : $"{tk.HoTen} (Nhân viên)", tk is null ? null : maBuuCuc, bc.ViDo, bc.KinhDo);
        }
        var dsShipper = await db.NhanVienGiaoHangs.ToListAsync();
        ThaoTac Shipper(int maNhanVien) => new($"{dsShipper.First(n => n.MaNhanVien == maNhanVien).HoTen} (Nhân viên giao hàng)");

        // ~40 đơn: đơn cũ đã xong, đơn mới đang ở các chặng
        KichBan[] kichBan =
        [
            .. Enumerable.Repeat(KichBan.HoanTat, 8), KichBan.ThatBaiTraVeRoiGiao, .. Enumerable.Repeat(KichBan.ThanhCong, 4),
            KichBan.HuyNgay, KichBan.HuyNgay, KichBan.HuyDaHoanHang, KichBan.ThatLac,
            .. Enumerable.Repeat(KichBan.ThatBai, 2), KichBan.HenGiaoLai, KichBan.HuyTaiBuuCuc,
            .. Enumerable.Repeat(KichBan.DangGiao, 3), KichBan.DoiShipper, .. Enumerable.Repeat(KichBan.ChoLayGiaoHang, 2),
            .. Enumerable.Repeat(KichBan.TaiBuuCucNhan, 2), .. Enumerable.Repeat(KichBan.DangTrungChuyen, 3),
            .. Enumerable.Repeat(KichBan.ChoLayLienTinh, 2), .. Enumerable.Repeat(KichBan.TaiBuuCucGui, 2),
            .. Enumerable.Repeat(KichBan.ChoTiepNhan, 5),
        ];
        string[] nguoiNhan = ["Nguyễn Hữu Thắng", "Trần Mai Anh", "Lê Thị Hoa", "Phạm Gia Bảo", "Đỗ Minh Khoa", "Vũ Thị Ngọc",
                              "Hoàng Anh Tuấn", "Bùi Thị Thu", "Ngô Quang Huy", "Đặng Thùy Linh", "Phan Văn Toàn", "Lý Mỹ Duyên"];
        // Địa chỉ nhận theo khu vực giao (dsKhuVuc: nội thành HN, ngoại thành HN, tỉnh lân cận, miền Trung, miền Nam)
        string[][] diaChiNhan =
        [
            ["12 Tràng Tiền, Hoàn Kiếm", "45 Tôn Đức Thắng, Đống Đa", "88 Cầu Giấy, Cầu Giấy"],
            ["21 Quang Trung, Hà Đông", "5 Nguyễn Văn Cừ, Long Biên", "16 Ngô Xuân Quảng, Gia Lâm"],
            ["102 Lý Thái Tổ, Bắc Ninh", "7 Trần Phú, Hải Dương", "40 Điện Biên, Hưng Yên"],
            ["33 Lê Lợi, Thanh Hóa", "18 Bạch Đằng, Đà Nẵng", "60 Nguyễn Huệ, Huế"],
            ["250 Nguyễn Thị Minh Khai, TP.HCM", "9 Hùng Vương, Cần Thơ", "120 Cách Mạng Tháng 8, Biên Hòa"],
        ];
        decimal[] khoiLuong = [0.5m, 0.8m, 1.2m, 1.5m, 2m, 2.5m, 3.2m, 4m, 5.5m, 7m, 9.5m, 12m, 15m, 25m];

        for (int i = 0; i < kichBan.Length; i++)
        {
            var kb = kichBan[i];
            bool dangXuLy = kb is not (KichBan.HoanTat or KichBan.ThanhCong or KichBan.ThatBaiTraVeRoiGiao or KichBan.HuyNgay
                                       or KichBan.HuyDaHoanHang or KichBan.ThatLac);
            // Đơn đang xử lý: 0–2 ngày trước; đơn đã xong: rải trong ~5 tháng để thống kê theo tháng có dữ liệu
            int soNgayTruoc = dangXuLy ? ngauNhien.Next(1, 3) : ngauNhien.Next(4, 150);
            dongHo = bayGioThat.Date.AddDays(-soNgayTruoc).AddHours(7).AddMinutes(ngauNhien.Next(0, 120));

            var khach = dsKhach[i % 19];                                       // kh20 bị khóa – không tạo đơn
            int bcGui = i % 3 == 0 ? 1 : 0;                                    // khách gửi tại Cầu Giấy / Hà Đông
            int kv = ngauNhien.Next(100) < 45 ? bcGui : ngauNhien.Next(dsKhuVuc.Count);
            var khuVuc = dsKhuVuc[kv];
            decimal kg = i == 17 ? 65m : khoiLuong[ngauNhien.Next(khoiLuong.Length)];
            if (kg > 60 && kv != 1) kv = 1;                                    // hàng nặng giao ngoại thành bằng xe ba gác
            // Kịch bản dừng ở chặng liên tỉnh: bắt buộc gửi đi tỉnh khác (bưu cục nhận ≠ bưu cục gửi)
            if (kb is KichBan.TaiBuuCucGui or KichBan.ChoLayLienTinh or KichBan.DangTrungChuyen or KichBan.ThatLac) { kv = 2 + i % 3; kg = Math.Min(kg, 25m); }
            var kq = await xuLy.TaoDonAsync(new DonGiaoHangFormVM
            {
                MaLoaiHang = dsLoai[ngauNhien.Next(dsLoai.Count)].MaLoaiHang, MaKhuVuc = dsKhuVuc[kv].MaKhuVuc, MaBuuCucGui = dsBuuCuc[bcGui].MaBuuCuc,
                TenNguoiNhan = nguoiNhan[ngauNhien.Next(nguoiNhan.Length)], SoDienThoaiNguoiNhan = $"09{ngauNhien.Next(10_000_000, 99_999_999)}",
                DiaChiNhan = diaChiNhan[kv % diaChiNhan.Length][ngauNhien.Next(3)], KhoiLuong = kg,
                NgayGiaoDuKien = dongHo.Date.AddDays(kv <= 1 ? ngauNhien.Next(1, 3) : ngauNhien.Next(2, 5)),
                GhiChu = ngauNhien.Next(3) == 0 ? "Gọi trước khi giao" : null,
                // ~2/3 đơn có thu hộ (tiền hàng 150.000 – 2.500.000 đ, làm tròn nghìn); ~40% đơn người nhận trả phí ship
                TienThuHo = ngauNhien.Next(3) == 0 ? 0 : ngauNhien.Next(15, 251) * 10_000m,
                NguoiTraPhi = ngauNhien.Next(5) < 2 ? NguoiTraPhi.NguoiNhan : NguoiTraPhi.NguoiGui
            }, khach.MaKhachHang, $"{khach.HoTen} (Khách hàng)");
            if (!kq.ThanhCong) continue;
            int ma = kq.DuLieu!.MaDon;
            string maQuet = kq.DuLieu.MaHienThi;
            Task<DonGiaoHang> Don() => db.DonGiaoHangs.Include(d => d.PhanCongs).FirstAsync(d => d.MaDon == ma);

            if (kb == KichBan.ChoTiepNhan) continue;
            if (kb == KichBan.HuyNgay)
            {
                dongHo = dongHo.AddHours(1);
                await xuLy.HuyDonAsync(ma, "Khách đổi ý, không gửi nữa", new ThaoTac($"{khach.HoTen} (Khách hàng)"), khach.MaKhachHang);
                continue;
            }

            // Bưu cục gửi tiếp nhận (đơn nội vùng chuyển thẳng tới chờ phân shipper khu vực)
            dongHo = dongHo.AddHours(2);
            var don = await Don();
            await xuLy.TiepNhanAsync(ma, maQuet, TaiBuuCuc(don.MaBuuCucGui));
            if (kb == KichBan.TaiBuuCucGui && (await Don()).TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui) continue;
            if (kb is KichBan.HuyTaiBuuCuc or KichBan.HuyDaHoanHang)
            {
                dongHo = dongHo.AddHours(3);
                await xuLy.HuyDonAsync(ma, "Người gửi yêu cầu hủy, nhận lại hàng tại bưu cục", TaiBuuCuc(don.MaBuuCucGui), null);
                if (kb == KichBan.HuyDaHoanHang) { dongHo = dongHo.AddHours(20); await xuLy.XacNhanHoanHangAsync(ma, TaiBuuCuc(don.MaBuuCucGui)); }
                continue;
            }

            // Chặng liên tỉnh
            if ((await Don()).TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui)
            {
                var nvLt = await ChonShipperAsync(xuLy, await Don(), LoaiShipper.LienTinh, null);
                if (nvLt is null) continue;
                dongHo = dongHo.AddHours(2);
                await xuLy.PhanCongAsync(ma, nvLt.Value, null, TaiBuuCuc(don.MaBuuCucGui));
                if (kb == KichBan.ChoLayLienTinh) continue;
                dongHo = dongHo.AddHours(1);
                await xuLy.LayHangAsync(ma, maQuet, nvLt.Value, Shipper(nvLt.Value));
                dongHo = dongHo.AddMinutes(40);
                int maPcLt = await xuLy.PhanCongHieuLuc().Where(p => p.MaDon == ma).Select(p => p.MaPhanCong).FirstAsync();
                await xuLy.BatDauAsync(maPcLt, nvLt.Value, Shipper(nvLt.Value));
                if (kb == KichBan.DangTrungChuyen) continue;
                if (kb == KichBan.ThatLac)
                {
                    dongHo = dongHo.AddHours(10);
                    await xuLy.BaoSuCoAsync(ma, TrangThaiDon.ThatLac, "Kiện hàng không có trên xe khi kiểm đếm tại trạm dừng", new ThaoTac(nhanVien));
                    continue;
                }
                dongHo = dongHo.AddHours(don.MaKhuVuc == dsKhuVuc[0].MaKhuVuc || don.MaKhuVuc == dsKhuVuc[1].MaKhuVuc ? 3 : 20);
                await xuLy.NhanHangDenAsync(ma, maQuet, TaiBuuCuc(don.MaBuuCucNhan));
            }
            if (kb is KichBan.TaiBuuCucNhan or KichBan.TaiBuuCucGui) continue;

            // Chặng khu vực
            var nvKv = await ChonShipperAsync(xuLy, await Don(), LoaiShipper.KhuVuc, null);
            if (nvKv is null) continue;
            dongHo = dongHo.AddHours(1);
            await xuLy.PhanCongAsync(ma, nvKv.Value, null, TaiBuuCuc(don.MaBuuCucNhan));
            if (kb == KichBan.DoiShipper)
            {
                var nvMoi = await ChonShipperAsync(xuLy, await Don(), LoaiShipper.KhuVuc, nvKv.Value);
                if (nvMoi is not null)
                {
                    dongHo = dongHo.AddMinutes(30);
                    await xuLy.DoiPhanCongAsync(ma, nvMoi.Value, "Shipper cũ báo ốm, điều người khác", TaiBuuCuc(don.MaBuuCucNhan));
                }
                continue;
            }
            if (kb == KichBan.ChoLayGiaoHang) continue;
            bool thatBai = kb is KichBan.ThatBai or KichBan.HenGiaoLai or KichBan.ThatBaiTraVeRoiGiao;
            await GiaoKhuVucAsync(xuLy, ma, maQuet, nvKv.Value, Shipper(nvKv.Value), () => dongHo = dongHo.AddHours(1.5),
                                  kb == KichBan.DangGiao, thatBai ? (LyDoThatBai)(i % 5) : null);
            if (kb == KichBan.HenGiaoLai)
            {
                dongHo = dongHo.AddHours(1);
                await xuLy.HenGiaoLaiAsync(ma, nvKv.Value, dongHo.Date.AddDays(1), "Người nhận hẹn chiều mai", Shipper(nvKv.Value));
                continue;
            }
            if (kb == KichBan.ThatBaiTraVeRoiGiao)
            {
                // Shipper trả hàng về bưu cục cuối ngày → hôm sau phân shipper khác giao lại thành công
                dongHo = dongHo.AddHours(4);
                await xuLy.NhanLaiTuShipperAsync(ma, maQuet, TaiBuuCuc(don.MaBuuCucNhan));
                dongHo = dongHo.AddDays(1).Date.AddHours(8);
                var nvLai = await ChonShipperAsync(xuLy, await Don(), LoaiShipper.KhuVuc, nvKv.Value)
                            ?? await ChonShipperAsync(xuLy, await Don(), LoaiShipper.KhuVuc, null);
                if (nvLai is null) continue;
                await xuLy.PhanCongAsync(ma, nvLai.Value, "Giao lại theo lịch hẹn với người nhận", TaiBuuCuc(don.MaBuuCucNhan));
                await GiaoKhuVucAsync(xuLy, ma, maQuet, nvLai.Value, Shipper(nvLai.Value), () => dongHo = dongHo.AddHours(1.5), false, null);
            }
            if (kb is KichBan.HoanTat or KichBan.ThatBaiTraVeRoiGiao)
            {
                dongHo = dongHo.AddDays(1);
                // Đối soát qua lệnh nộp tiền: TaoGiaoDichViMauAsync tạo lệnh nộp "đã xác nhận" chứa các đơn hoàn tất này
                await xuLy.HoanTatAsync(ma, nhanVien, tuGiaoDichVi: true);
            }
        }
    }

    /// <summary>Shipper khu vực: quét nhận hàng tại bưu cục → bắt đầu giao → thành công / thất bại (dừng sau khi bắt đầu nếu dangGiao).</summary>
    private static async Task GiaoKhuVucAsync(DonHangXuLy xuLy, int maDon, string maQuet, int maNhanVien, ThaoTac shipper,
                                              Action tangGio, bool dungKhiDangGiao, LyDoThatBai? thatBai)
    {
        tangGio();
        await xuLy.LayHangAsync(maDon, maQuet, maNhanVien, shipper);
        int maPc = await xuLy.PhanCongHieuLuc().Where(p => p.MaDon == maDon).Select(p => p.MaPhanCong).FirstAsync();
        tangGio();
        await xuLy.BatDauAsync(maPc, maNhanVien, shipper);
        if (dungKhiDangGiao) return;
        tangGio();
        if (thatBai is not null)
            await xuLy.GiaoThatBaiAsync(maPc, maNhanVien, thatBai, thatBai == LyDoThatBai.Khac ? "Cổng khu tập thể khóa, bảo vệ không cho vào" : null, shipper);
        else
            await xuLy.GiaoThanhCongAsync(maPc, maNhanVien, "Người nhận đã ký nhận", shipper);
    }

    /// <summary>
    /// Đồng bộ và đảm bảo đủ 34 bưu cục (34 tỉnh thành), 34 nhân viên bưu cục nv.{tinh},
    /// và 68 shipper (mỗi bưu cục có ít nhất 1 xe tải sp.{tinh}01 và 1 xe máy sp.{tinh}02)
    /// khi CSDL đã tồn tại dữ liệu trước đó.
    /// </summary>
    /// <summary>
    /// Äáº£m báº£o cÃ¡c báº£ng vÃ  cá»™t bá»• sung luÃ´n tá»“n táº¡i trong CSDL trÆ°á»›c khi thá»±c hiá»‡n báº¥t ká»³ thao tÃ¡c nÃ o,
    /// trÃ¡nh lá»—i Invalid column name khi khá»Ÿi cháº¡y trÃªn mÃ¡y má»›i / CSDL má»›i khá»Ÿi táº¡o.
    /// </summary>
    public static async Task DamBaoCauTrucBangVaCotAsync(QuanLyGiaoNhanDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;
                IF COL_LENGTH('DonGiaoHangs', 'KichThuocM3') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD KichThuocM3 decimal(10,4) NOT NULL DEFAULT 0.005;
                IF COL_LENGTH('DonGiaoHangs', 'PhuPhiKichThuoc') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD PhuPhiKichThuoc decimal(18,0) NOT NULL DEFAULT 0;
                IF COL_LENGTH('DonGiaoHangs', 'PhiChuyenHoan') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD PhiChuyenHoan decimal(18,2) NOT NULL DEFAULT 0;
                IF COL_LENGTH('DonGiaoHangs', 'NgayThuPhiHoan') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD NgayThuPhiHoan datetime2 NULL;

                IF OBJECT_ID('YeuCauLienHes', 'U') IS NULL
                BEGIN
                    CREATE TABLE YeuCauLienHes (
                        MaLienHe int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaKhachHang int NOT NULL,
                        MaDonHang int NULL,
                        MaBuuCuc int NULL,
                        TieuDe nvarchar(200) NOT NULL,
                        ChuDe int NOT NULL DEFAULT 2,
                        NoiDung nvarchar(2000) NOT NULL,
                        NgayGui datetime2 NOT NULL DEFAULT GETDATE(),
                        TrangThai int NOT NULL DEFAULT 0,
                        NgayCapNhat datetime2 NULL,
                        CONSTRAINT FK_YeuCauLienHes_KhachHangs FOREIGN KEY (MaKhachHang) REFERENCES KhachHangs(MaKhachHang),
                        CONSTRAINT FK_YeuCauLienHes_DonGiaoHangs FOREIGN KEY (MaDonHang) REFERENCES DonGiaoHangs(MaDon),
                        CONSTRAINT FK_YeuCauLienHes_BuuCucs FOREIGN KEY (MaBuuCuc) REFERENCES BuuCucs(MaBuuCuc)
                    );
                    CREATE INDEX IX_YeuCauLienHes_KhachHang_NgayGui ON YeuCauLienHes(MaKhachHang, NgayGui);
                    CREATE INDEX IX_YeuCauLienHes_TrangThai ON YeuCauLienHes(TrangThai);
                END;

                IF OBJECT_ID('TinNhanLienHes', 'U') IS NULL
                BEGIN
                    CREATE TABLE TinNhanLienHes (
                        MaTinNhan int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaLienHe int NOT NULL,
                        MaTaiKhoan int NULL,
                        NguoiGui nvarchar(100) NOT NULL,
                        LaNhanVien bit NOT NULL DEFAULT 0,
                        NoiDung nvarchar(2000) NOT NULL,
                        NgayGui datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_TinNhanLienHes_YeuCauLienHes FOREIGN KEY (MaLienHe) REFERENCES YeuCauLienHes(MaLienHe) ON DELETE CASCADE,
                        CONSTRAINT FK_TinNhanLienHes_TaiKhoans FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoans(MaTaiKhoan)
                    );
                    CREATE INDEX IX_TinNhanLienHes_LienHe_NgayGui ON TinNhanLienHes(MaLienHe, NgayGui);
                END;

                IF COL_LENGTH('KhachHangs', 'SoDuVi') IS NULL 
                    ALTER TABLE KhachHangs ADD SoDuVi decimal(18,0) NOT NULL DEFAULT 0;
                IF COL_LENGTH('KhachHangs', 'TenNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD TenNganHang nvarchar(100) NULL;
                IF COL_LENGTH('KhachHangs', 'SoTaiKhoanNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD SoTaiKhoanNganHang nvarchar(50) NULL;
                IF COL_LENGTH('KhachHangs', 'ChuTaiKhoanNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD ChuTaiKhoanNganHang nvarchar(100) NULL;
                IF COL_LENGTH('KhachHangs', 'PhienBan') IS NULL 
                    ALTER TABLE KhachHangs ADD PhienBan rowversion;

                IF OBJECT_ID('GiaoDichViKhachHangs', 'U') IS NULL
                BEGIN
                    CREATE TABLE GiaoDichViKhachHangs (
                        MaGiaoDich int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaKhachHang int NOT NULL,
                        Loai tinyint NOT NULL,
                        SoTien decimal(18,0) NOT NULL,
                        SoDuSauGiaoDich decimal(18,0) NOT NULL,
                        MaDonHang int NULL,
                        TrangThai tinyint NOT NULL DEFAULT 0,
                        TenNganHang nvarchar(100) NULL,
                        SoTaiKhoan nvarchar(50) NULL,
                        ChuTaiKhoan nvarchar(100) NULL,
                        MaThamChieu nvarchar(100) NULL,
                        NgayTao datetime2 NOT NULL DEFAULT GETDATE(),
                        NgayXuLy datetime2 NULL,
                        NguoiXuLy nvarchar(150) NULL,
                        GhiChu nvarchar(500) NULL,
                        CONSTRAINT FK_GiaoDichViKhachHangs_KhachHangs FOREIGN KEY (MaKhachHang) REFERENCES KhachHangs(MaKhachHang),
                        CONSTRAINT FK_GiaoDichViKhachHangs_DonGiaoHangs FOREIGN KEY (MaDonHang) REFERENCES DonGiaoHangs(MaDon)
                    );
                    CREATE INDEX IX_GiaoDichViKhachHangs_KhachHang_NgayTao ON GiaoDichViKhachHangs(MaKhachHang, NgayTao);
                    CREATE INDEX IX_GiaoDichViKhachHangs_TrangThai ON GiaoDichViKhachHangs(TrangThai);
                END;

                IF COL_LENGTH('GiaoDichViKhachHangs', 'PhienBan') IS NULL 
                    ALTER TABLE GiaoDichViKhachHangs ADD PhienBan rowversion;

                IF OBJECT_ID('ChuyenXes', 'U') IS NULL
                BEGIN
                    CREATE TABLE ChuyenXes (
                        MaChuyen int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaBuuCucGui int NOT NULL,
                        MaBuuCucNhan int NOT NULL,
                        MaBuuCucTrungGian int NULL,
                        DaGheTram bit NOT NULL DEFAULT 0,
                        NgayGheTram datetime2 NULL,
                        NgayRoiTram datetime2 NULL,
                        MaNhanVien int NOT NULL,
                        MaPhuongTien int NULL,
                        TrangThai tinyint NOT NULL DEFAULT 0,
                        NgayTao datetime2 NOT NULL DEFAULT GETDATE(),
                        NgayXuatBen datetime2 NULL,
                        NgayDenNoi datetime2 NULL,
                        GhiChu nvarchar(500) NULL,
                        CONSTRAINT FK_ChuyenXes_BuuCucGui FOREIGN KEY (MaBuuCucGui) REFERENCES BuuCucs(MaBuuCuc),
                        CONSTRAINT FK_ChuyenXes_BuuCucNhan FOREIGN KEY (MaBuuCucNhan) REFERENCES BuuCucs(MaBuuCuc),
                        CONSTRAINT FK_ChuyenXes_BuuCucTrungGian FOREIGN KEY (MaBuuCucTrungGian) REFERENCES BuuCucs(MaBuuCuc),
                        CONSTRAINT FK_ChuyenXes_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVienGiaoHangs(MaNhanVien),
                        CONSTRAINT FK_ChuyenXes_PhuongTien FOREIGN KEY (MaPhuongTien) REFERENCES PhuongTiens(MaPhuongTien)
                    );
                    CREATE INDEX IX_ChuyenXes_TrangThai ON ChuyenXes(TrangThai);
                    CREATE INDEX IX_ChuyenXes_BuuCucGui ON ChuyenXes(MaBuuCucGui);
                    CREATE INDEX IX_ChuyenXes_BuuCucNhan ON ChuyenXes(MaBuuCucNhan);
                    CREATE INDEX IX_ChuyenXes_BuuCucTrungGian ON ChuyenXes(MaBuuCucTrungGian);
                END;

                IF COL_LENGTH('ChuyenXes', 'MaBuuCucTrungGian') IS NULL
                BEGIN
                    ALTER TABLE ChuyenXes ADD MaBuuCucTrungGian int NULL;
                    ALTER TABLE ChuyenXes ADD CONSTRAINT FK_ChuyenXes_BuuCucTrungGian FOREIGN KEY (MaBuuCucTrungGian) REFERENCES BuuCucs(MaBuuCuc);
                END;

                IF COL_LENGTH('ChuyenXes', 'DaGheTram') IS NULL
                    ALTER TABLE ChuyenXes ADD DaGheTram bit NOT NULL DEFAULT 0;

                IF COL_LENGTH('ChuyenXes', 'NgayGheTram') IS NULL
                    ALTER TABLE ChuyenXes ADD NgayGheTram datetime2 NULL;

                IF COL_LENGTH('ChuyenXes', 'NgayRoiTram') IS NULL
                    ALTER TABLE ChuyenXes ADD NgayRoiTram datetime2 NULL;

                IF COL_LENGTH('DonGiaoHangs', 'MaChuyenXe') IS NULL 
                BEGIN
                    ALTER TABLE DonGiaoHangs ADD MaChuyenXe int NULL;
                    ALTER TABLE DonGiaoHangs ADD CONSTRAINT FK_DonGiaoHangs_ChuyenXes FOREIGN KEY (MaChuyenXe) REFERENCES ChuyenXes(MaChuyen);
                END;
            ");
        }
        catch
        {
            // Bá» qua náº¿u cáº¥u trÃºc báº£ng hoáº·c cá»™t Ä‘Ã£ tá»“n táº¡i
        }
    }

    public static async Task BoSungDuLieuConThieuAsync(QuanLyGiaoNhanDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                IF COL_LENGTH('DonGiaoHangs', 'KichThuocM3') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD KichThuocM3 decimal(10,4) NOT NULL DEFAULT 0.005;
                IF COL_LENGTH('DonGiaoHangs', 'PhuPhiKichThuoc') IS NULL 
                    ALTER TABLE DonGiaoHangs ADD PhuPhiKichThuoc decimal(18,0) NOT NULL DEFAULT 0;

                IF OBJECT_ID('YeuCauLienHes', 'U') IS NULL
                BEGIN
                    CREATE TABLE YeuCauLienHes (
                        MaLienHe int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaKhachHang int NOT NULL,
                        MaDonHang int NULL,
                        MaBuuCuc int NULL,
                        TieuDe nvarchar(200) NOT NULL,
                        ChuDe int NOT NULL DEFAULT 2,
                        NoiDung nvarchar(2000) NOT NULL,
                        NgayGui datetime2 NOT NULL DEFAULT GETDATE(),
                        TrangThai int NOT NULL DEFAULT 0,
                        NgayCapNhat datetime2 NULL,
                        CONSTRAINT FK_YeuCauLienHes_KhachHangs FOREIGN KEY (MaKhachHang) REFERENCES KhachHangs(MaKhachHang),
                        CONSTRAINT FK_YeuCauLienHes_DonGiaoHangs FOREIGN KEY (MaDonHang) REFERENCES DonGiaoHangs(MaDon),
                        CONSTRAINT FK_YeuCauLienHes_BuuCucs FOREIGN KEY (MaBuuCuc) REFERENCES BuuCucs(MaBuuCuc)
                    );
                    CREATE INDEX IX_YeuCauLienHes_KhachHang_NgayGui ON YeuCauLienHes(MaKhachHang, NgayGui);
                    CREATE INDEX IX_YeuCauLienHes_TrangThai ON YeuCauLienHes(TrangThai);
                END;

                IF OBJECT_ID('TinNhanLienHes', 'U') IS NULL
                BEGIN
                    CREATE TABLE TinNhanLienHes (
                        MaTinNhan int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaLienHe int NOT NULL,
                        MaTaiKhoan int NULL,
                        NguoiGui nvarchar(100) NOT NULL,
                        LaNhanVien bit NOT NULL DEFAULT 0,
                        NoiDung nvarchar(2000) NOT NULL,
                        NgayGui datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_TinNhanLienHes_YeuCauLienHes FOREIGN KEY (MaLienHe) REFERENCES YeuCauLienHes(MaLienHe) ON DELETE CASCADE,
                        CONSTRAINT FK_TinNhanLienHes_TaiKhoans FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoans(MaTaiKhoan)
                    );
                    CREATE INDEX IX_TinNhanLienHes_LienHe_NgayGui ON TinNhanLienHes(MaLienHe, NgayGui);
                END;

                IF COL_LENGTH('KhachHangs', 'SoDuVi') IS NULL 
                    ALTER TABLE KhachHangs ADD SoDuVi decimal(18,0) NOT NULL DEFAULT 0;
                IF COL_LENGTH('KhachHangs', 'TenNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD TenNganHang nvarchar(100) NULL;
                IF COL_LENGTH('KhachHangs', 'SoTaiKhoanNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD SoTaiKhoanNganHang nvarchar(50) NULL;
                IF COL_LENGTH('KhachHangs', 'ChuTaiKhoanNganHang') IS NULL 
                    ALTER TABLE KhachHangs ADD ChuTaiKhoanNganHang nvarchar(100) NULL;

                IF OBJECT_ID('GiaoDichViKhachHangs', 'U') IS NULL
                BEGIN
                    CREATE TABLE GiaoDichViKhachHangs (
                        MaGiaoDich int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        MaKhachHang int NOT NULL,
                        Loai tinyint NOT NULL,
                        SoTien decimal(18,0) NOT NULL,
                        SoDuSauGiaoDich decimal(18,0) NOT NULL,
                        MaDonHang int NULL,
                        TrangThai tinyint NOT NULL DEFAULT 0,
                        TenNganHang nvarchar(100) NULL,
                        SoTaiKhoan nvarchar(50) NULL,
                        ChuTaiKhoan nvarchar(100) NULL,
                        MaThamChieu nvarchar(100) NULL,
                        NgayTao datetime2 NOT NULL DEFAULT GETDATE(),
                        NgayXuLy datetime2 NULL,
                        NguoiXuLy nvarchar(150) NULL,
                        GhiChu nvarchar(500) NULL,
                        CONSTRAINT FK_GiaoDichViKhachHangs_KhachHangs FOREIGN KEY (MaKhachHang) REFERENCES KhachHangs(MaKhachHang),
                        CONSTRAINT FK_GiaoDichViKhachHangs_DonGiaoHangs FOREIGN KEY (MaDonHang) REFERENCES DonGiaoHangs(MaDon)
                    );
                    CREATE INDEX IX_GiaoDichViKhachHangs_KhachHang_NgayTao ON GiaoDichViKhachHangs(MaKhachHang, NgayTao);
                    CREATE INDEX IX_GiaoDichViKhachHangs_TrangThai ON GiaoDichViKhachHangs(TrangThai);
                END;
            ");
        }
        catch { /* Bỏ qua nếu môi trường DB không hỗ trợ cú pháp SQL Server */ }

        await DongBoViKhachHangMauAsync(db);

        bool coThayDoi = false;
        var bayGio = DateTime.Now;
        var matKhau = await db.TaiKhoans.Where(t => t.TenDangNhap == "admin").Select(t => t.MatKhau).FirstOrDefaultAsync() 
                      ?? MatKhauHelper.Bam("123456");

        TaiKhoan TaoTk(string ten, string hoTen, string email, string vaiTro, int ngayTruoc) => new()
        {
            TenDangNhap = ten, MatKhau = matKhau, HoTen = hoTen, Email = email, VaiTro = vaiTro, NgayTao = bayGio.AddDays(-ngayTruoc)
        };

        // 1. Đảm bảo đầy đủ 34 Bưu cục tương ứng 34 tỉnh thành
        var dsBuuCucHienTai = await db.BuuCucs.ToListAsync();
        foreach (var t in DanhSach34Tinh)
        {
            var bc = dsBuuCucHienTai.FirstOrDefault(b => b.TinhThanh == t.TenTinh || b.TenBuuCuc == t.TenBuuCuc
                                                         || (t.Ma == "hanoi" && (b.TenBuuCuc.Contains("Cầu Giấy") || b.TenBuuCuc.Contains("Hà Đông"))));
            if (bc is null)
            {
                bc = new BuuCuc
                {
                    TenBuuCuc = t.TenBuuCuc, TinhThanh = t.TenTinh, DiaChi = t.DiaChi, SoDienThoai = t.SoDienThoai,
                    ViDo = t.ViDo, KinhDo = t.KinhDo, TrangThai = TrangThaiHoatDong.HoatDong
                };
                db.BuuCucs.Add(bc);
                dsBuuCucHienTai.Add(bc);
                coThayDoi = true;
            }
            else
            {
                if (bc.ViDo != t.ViDo || bc.KinhDo != t.KinhDo || bc.TenBuuCuc != t.TenBuuCuc)
                {
                    bc.TenBuuCuc = t.TenBuuCuc;
                    bc.TinhThanh = t.TenTinh;
                    bc.ViDo = t.ViDo;
                    bc.KinhDo = t.KinhDo;
                    bc.DiaChi = t.DiaChi;
                    bc.SoDienThoai = t.SoDienThoai;
                    coThayDoi = true;
                }
            }
        }
        if (coThayDoi) { await db.SaveChangesAsync(); coThayDoi = false; }

        // 2. Đảm bảo mỗi bưu cục có ít nhất 1 KhuVuc tương ứng
        var dsKhuVucHienTai = await db.KhuVucs.ToListAsync();
        foreach (var t in DanhSach34Tinh)
        {
            var bc = dsBuuCucHienTai.First(b => b.TinhThanh == t.TenTinh || b.TenBuuCuc == t.TenBuuCuc);
            var kv = dsKhuVucHienTai.FirstOrDefault(k => k.MaBuuCuc == bc.MaBuuCuc);
            if (kv is null)
            {
                kv = new KhuVuc
                {
                    TenKhuVuc = $"Khu vực {t.TenTinh}", PhiCoBan = 18_000m,
                    MoTa = $"Khu vực giao nhận tỉnh {t.TenTinh}", BuuCuc = bc,
                    TrangThai = TrangThaiHoatDong.HoatDong
                };
                db.KhuVucs.Add(kv);
                dsKhuVucHienTai.Add(kv);
                coThayDoi = true;
            }
        }
        if (coThayDoi) { await db.SaveChangesAsync(); coThayDoi = false; }

        // 3. Chuẩn hóa mã nhân viên bưu cục: nv.{tinh} (vd: nv.hanoi, nv.bacninh, nv.danang...)
        var dsTkTatCa = await db.TaiKhoans.ToListAsync();
        var mapTkTheoTen = dsTkTatCa.ToDictionary(t => t.TenDangNhap.ToLowerInvariant(), t => t);

        // Chuẩn hóa vai trò nhân viên cũ
        foreach (var tk in dsTkTatCa.Where(t => t.VaiTro is "DieuPhoi" or "BuuCuc"))
        {
            tk.VaiTro = VaiTroNguoiDung.NhanVien;
            coThayDoi = true;
        }

        // Đảm bảo mỗi tỉnh trong 34 tỉnh có đúng 1 tài khoản nv.{t.Ma}
        foreach (var t in DanhSach34Tinh)
        {
            string maNvChuan = $"nv.{t.Ma}";
            var bc = dsBuuCucHienTai.First(b => b.TinhThanh == t.TenTinh || b.TenBuuCuc == t.TenBuuCuc);

            if (mapTkTheoTen.TryGetValue(maNvChuan, out var tkTonTai))
            {
                tkTonTai.VaiTro = VaiTroNguoiDung.NhanVien;
                tkTonTai.MaBuuCuc = bc.MaBuuCuc;
                coThayDoi = true;
            }
            else
            {
                // Chưa có -> tìm xem có tài khoản cũ chưa đổi tên không
                TaiKhoan? tkCu = null;
                if (t.Ma == "hanoi")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv01" or "dieuphoi" or "bc.caugiay");
                else if (t.Ma == "bacninh")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv02" or "nv04" or "bc.bacninh");
                else if (t.Ma == "haiphong")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv03" or "nv07" or "bc.haiphong");
                else if (t.Ma == "quangninh")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv04" or "bc.quangninh");
                else if (t.Ma == "danang")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv05" or "bc.danang");
                else if (t.Ma == "hcm")
                    tkCu = dsTkTatCa.FirstOrDefault(x => x.TenDangNhap is "nv06" or "bc.hcm");

                if (tkCu != null && !mapTkTheoTen.ContainsKey(maNvChuan))
                {
                    mapTkTheoTen.Remove(tkCu.TenDangNhap.ToLowerInvariant());
                    tkCu.TenDangNhap = maNvChuan;
                    tkCu.Email = $"{maNvChuan}@giaonhan.vn";
                    tkCu.MaBuuCuc = bc.MaBuuCuc;
                    tkCu.VaiTro = VaiTroNguoiDung.NhanVien;
                    mapTkTheoTen[maNvChuan] = tkCu;
                    coThayDoi = true;
                }
                else
                {
                    var tkMoi = TaoTk(maNvChuan, t.TenNv, $"{maNvChuan}@giaonhan.vn", VaiTroNguoiDung.NhanVien, 88);
                    tkMoi.MaBuuCuc = bc.MaBuuCuc;
                    db.TaiKhoans.Add(tkMoi);
                    dsTkTatCa.Add(tkMoi);
                    mapTkTheoTen[maNvChuan] = tkMoi;
                    coThayDoi = true;
                }
            }
        }

        // Dọn dẹp hoàn toàn các tài khoản nhân viên cũ (nv01, nv02..., dieuphoi, bc.*) còn sót lại
        var dsTkCuThua = await db.TaiKhoans
            .Include(t => t.KhachHang)
            .Include(t => t.NhanVienGiaoHang)
            .Where(t => t.KhachHang == null && t.NhanVienGiaoHang == null &&
                        (t.TenDangNhap.StartsWith("nv0") || t.TenDangNhap.StartsWith("bc.") || t.TenDangNhap == "dieuphoi"))
            .ToListAsync();
        if (dsTkCuThua.Count > 0)
        {
            var ids = dsTkCuThua.Select(x => x.MaTaiKhoan).ToList();
            var thongBaos = await db.ThongBaos.Where(tb => ids.Contains(tb.MaTaiKhoan)).ToListAsync();
            if (thongBaos.Count > 0) db.ThongBaos.RemoveRange(thongBaos);

            db.TaiKhoans.RemoveRange(dsTkCuThua);
            coThayDoi = true;
        }
        if (coThayDoi) { await db.SaveChangesAsync(); coThayDoi = false; }

        // 4. Phương tiện & Shippers (mỗi bưu cục 1 xe tải sp.{tinh}01 và 1 xe máy sp.{tinh}02)
        var dsPtHienTai = await db.PhuongTiens.ToListAsync();
        PhuongTien LayHoacTaoPt(string bienSo, string loai, decimal taiTrong, string ghiChu)
        {
            var pt = dsPtHienTai.FirstOrDefault(p => p.BienSo.Equals(bienSo, StringComparison.OrdinalIgnoreCase));
            if (pt is null)
            {
                pt = new PhuongTien { BienSo = bienSo, LoaiPhuongTien = loai, TaiTrongToiDa = taiTrong, TrangThai = TrangThaiPhuongTien.SanSang, GhiChu = ghiChu };
                db.PhuongTiens.Add(pt);
                dsPtHienTai.Add(pt);
                coThayDoi = true;
            }
            return pt;
        }

        for (int i = 0; i < DanhSach34Tinh.Length; i++)
        {
            var t = DanhSach34Tinh[i];
            LayHoacTaoPt(t.BienSoXeTai, "Xe tải 2 tấn", 2000, $"Tuyến liên tỉnh {t.TenTinh}");
            LayHoacTaoPt(t.BienSoXeMay, "Xe máy", 100, $"Giao hàng {t.TenTinh}");
        }
        if (coThayDoi) { await db.SaveChangesAsync(); coThayDoi = false; }

        var dsShipperHienTai = await db.NhanVienGiaoHangs
            .Include(n => n.TaiKhoan)
            .Include(n => n.PhuongTien)
            .Include(n => n.BuuCuc)
            .ToListAsync();

        var tapSoDt = new HashSet<string>(dsShipperHienTai.Select(s => s.SoDienThoai));
        string SinhSoDt(int index, int suffix)
        {
            for (int k = 0; k < 1000; k++)
            {
                string s = $"08{61000000 + index * 200 + suffix + k:D8}";
                if (tapSoDt.Add(s)) return s;
            }
            return $"0899{Random.Shared.Next(100000, 999999)}";
        }

        var tapXeDaGan = new HashSet<int>(dsShipperHienTai.Where(s => s.MaPhuongTien.HasValue).Select(s => s.MaPhuongTien!.Value));
        var tapTenShipperDaDung = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in dsShipperHienTai)
        {
            if (s.TaiKhoan != null && s.TaiKhoan.TenDangNhap.StartsWith("sp.", StringComparison.OrdinalIgnoreCase))
            {
                tapTenShipperDaDung.Add(s.TaiKhoan.TenDangNhap.ToLowerInvariant());
            }
        }

        // Chuẩn hóa mã shipper cũ (sp01, sp02, ...) theo bưu cục: xe tải là 01, xe máy từ 02 trở đi
        foreach (var s in dsShipperHienTai)
        {
            if (s.TaiKhoan == null || s.TaiKhoan.TenDangNhap.StartsWith("sp.", StringComparison.OrdinalIgnoreCase))
                continue;

            string maTinh = "hanoi";
            if (s.BuuCuc != null)
            {
                var tinhKhop = DanhSach34Tinh.FirstOrDefault(x => x.TenTinh == s.BuuCuc.TinhThanh || x.TenBuuCuc == s.BuuCuc.TenBuuCuc);
                if (tinhKhop != null) maTinh = tinhKhop.Ma;
            }

            string maMoi;
            if (s.LoaiShipper == LoaiShipper.LienTinh && !tapTenShipperDaDung.Contains($"sp.{maTinh}01"))
            {
                maMoi = $"sp.{maTinh}01";
            }
            else
            {
                int soThuTu = 2;
                while (tapTenShipperDaDung.Contains($"sp.{maTinh}{soThuTu:D2}"))
                {
                    soThuTu++;
                }
                maMoi = $"sp.{maTinh}{soThuTu:D2}";
            }

            s.TaiKhoan.TenDangNhap = maMoi;
            s.TaiKhoan.Email = $"{maMoi}@giaonhan.vn";
            s.TaiKhoan.VaiTro = VaiTroNguoiDung.GiaoHang;
            s.Email = s.TaiKhoan.Email;
            tapTenShipperDaDung.Add(maMoi);
            coThayDoi = true;
        }
        if (coThayDoi) { await db.SaveChangesAsync(); coThayDoi = false; }

        // Bổ sung đầy đủ cho 34 tỉnh: 1 xe tải sp.{tinh}01 và 1 xe máy sp.{tinh}02
        var dsNvMoiTao = new List<NhanVienGiaoHang>();
        for (int i = 0; i < DanhSach34Tinh.Length; i++)
        {
            var t = DanhSach34Tinh[i];
            var bc = dsBuuCucHienTai.First(b => b.TinhThanh == t.TenTinh || b.TenBuuCuc == t.TenBuuCuc);
            var kv = dsKhuVucHienTai.FirstOrDefault(k => k.MaBuuCuc == bc.MaBuuCuc);
            var ptTai = dsPtHienTai.First(p => p.BienSo == t.BienSoXeTai);
            var ptMay = dsPtHienTai.First(p => p.BienSo == t.BienSoXeMay);

            // 1. Xe tải: sp.{t.Ma}01
            string maSpTai = $"sp.{t.Ma}01";
            var spTai = dsShipperHienTai.FirstOrDefault(s => s.TaiKhoan != null && s.TaiKhoan.TenDangNhap.Equals(maSpTai, StringComparison.OrdinalIgnoreCase));
            if (spTai is null)
            {
                var tk = TaoTk(maSpTai, t.TenSpTai, $"{maSpTai}@giaonhan.vn", VaiTroNguoiDung.GiaoHang, 85);
                spTai = new NhanVienGiaoHang
                {
                    TaiKhoan = tk, HoTen = t.TenSpTai, SoDienThoai = SinhSoDt(i, 1),
                    Email = $"{maSpTai}@giaonhan.vn", LoaiShipper = LoaiShipper.LienTinh, BuuCuc = bc,
                    TrangThai = TrangThaiNhanVien.SanSang
                };
                if (!tapXeDaGan.Contains(ptTai.MaPhuongTien))
                {
                    spTai.PhuongTien = ptTai;
                    tapXeDaGan.Add(ptTai.MaPhuongTien);
                }
                db.NhanVienGiaoHangs.Add(spTai);
                dsShipperHienTai.Add(spTai);
                dsNvMoiTao.Add(spTai);
                tapTenShipperDaDung.Add(maSpTai);
                coThayDoi = true;
            }
            else
            {
                spTai.BuuCuc = bc;
                spTai.LoaiShipper = LoaiShipper.LienTinh;
                if (spTai.MaPhuongTien == null && !tapXeDaGan.Contains(ptTai.MaPhuongTien))
                {
                    spTai.PhuongTien = ptTai;
                    tapXeDaGan.Add(ptTai.MaPhuongTien);
                    coThayDoi = true;
                }
            }

            // 2. Xe máy: sp.{t.Ma}02
            string maSpMay = $"sp.{t.Ma}02";
            var spMay = dsShipperHienTai.FirstOrDefault(s => s.TaiKhoan != null && s.TaiKhoan.TenDangNhap.Equals(maSpMay, StringComparison.OrdinalIgnoreCase));
            if (spMay is null)
            {
                var tk = TaoTk(maSpMay, t.TenSpMay, $"{maSpMay}@giaonhan.vn", VaiTroNguoiDung.GiaoHang, 85);
                spMay = new NhanVienGiaoHang
                {
                    TaiKhoan = tk, HoTen = t.TenSpMay, SoDienThoai = SinhSoDt(i, 2),
                    Email = $"{maSpMay}@giaonhan.vn", LoaiShipper = LoaiShipper.KhuVuc, BuuCuc = bc,
                    KhuVucPhuTrach = kv, TrangThai = TrangThaiNhanVien.SanSang
                };
                if (!tapXeDaGan.Contains(ptMay.MaPhuongTien))
                {
                    spMay.PhuongTien = ptMay;
                    tapXeDaGan.Add(ptMay.MaPhuongTien);
                }
                db.NhanVienGiaoHangs.Add(spMay);
                dsShipperHienTai.Add(spMay);
                dsNvMoiTao.Add(spMay);
                tapTenShipperDaDung.Add(maSpMay);
                coThayDoi = true;
            }
            else
            {
                spMay.BuuCuc = bc;
                spMay.LoaiShipper = LoaiShipper.KhuVuc;
                if (kv != null) spMay.KhuVucPhuTrach = kv;
                if (spMay.MaPhuongTien == null && !tapXeDaGan.Contains(ptMay.MaPhuongTien))
                {
                    spMay.PhuongTien = ptMay;
                    tapXeDaGan.Add(ptMay.MaPhuongTien);
                    coThayDoi = true;
                }
            }
        }

        if (coThayDoi)
        {
            await db.SaveChangesAsync();
            foreach (var nv in dsNvMoiTao.Where(n => n.MaPhuongTien is not null))
            {
                db.LichSuPhuongTiens.Add(new LichSuPhuongTien
                {
                    MaNhanVien = nv.MaNhanVien, MaPhuongTienMoi = nv.MaPhuongTien, ThoiGian = bayGio.AddDays(-85),
                    NguoiThucHien = "Quản trị hệ thống (Quản trị)", LyDo = "Gán phương tiện khi nhận việc"
                });
            }
            await db.SaveChangesAsync();
        }

        await NapThongBaoMauAsync(db);
        await NapLienHeMauAsync(db);
        await TaoChuyenXeMauAsync(db);
    }

    /// <summary>Nạp 10 thông báo mẫu khớp 100% hình ảnh yêu cầu thiết kế.</summary>
    public static async Task NapThongBaoMauAsync(QuanLyGiaoNhanDbContext db)
    {
        if (await db.ThongBaos.AnyAsync()) return;

        var dsThongBao = new List<ThongBao>
        {
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Lệnh rút ví khách VK000031 chờ chuyển khoản",
                NoiDung = "Đỗ Ngọc Lan rút 420.000 đ về Vietcombank – 0123456789 – KIEM THU.",
                Loai = LoaiThongBao.Vi,
                DuongDan = "/QuanTriCongNo",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-10-08 21:41")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Yêu cầu xe: Bưu cục Thái Nguyên → Bưu cục Cầu Giấy",
                NoiDung = "Bưu cục Thái Nguyên cần xe tuyến Bưu cục Thái Nguyên → Bưu cục Cầu Giấy: 0 đơn (0 kg). Lập kế hoạch điều phối xe.",
                Loai = LoaiThongBao.DonHang,
                DuongDan = "/QuanTriPhuongTien",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-10-08 21:24")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Sự cố DH202610080040: hư hỏng tại bưu cục",
                NoiDung = "Thùng bị ướt và vỡ khi xếp kệ",
                Loai = LoaiThongBao.SuCo,
                DuongDan = "/QuanTriDonGiaoHang",
                DaDoc = true, // Đã đọc - nền trắng trong ảnh mẫu
                NgayTao = DateTime.Parse("2026-10-08 06:29")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Sự cố DH202609300032: thất lạc",
                NoiDung = "Thất lạc trên chuyến CX000003 Bưu cục Cầu Giấy → Bưu cục Đà Nẵng: Kiểm đếm khi xe tới thiếu 1 đơn hàng.",
                Loai = LoaiThongBao.SuCo,
                DuongDan = "/QuanTriDonGiaoHang",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-10-01 04:59")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Lệnh nộp GD000007 chờ xác nhận",
                NoiDung = "Đặng Thị Yến nộp 450.000 đ (nộp tiền 1 đơn) qua Chuyển khoản ngân hàng.",
                Loai = LoaiThongBao.Vi,
                DuongDan = "/QuanTriViShipper",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-30 01:59")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Lệnh nộp GD000006 chờ xác nhận",
                NoiDung = "Hồ Văn Trung nộp 59.000 đ (nộp tiền 2 đơn) qua Chuyển khoản ngân hàng.",
                Loai = LoaiThongBao.Vi,
                DuongDan = "/QuanTriViShipper",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-26 16:29")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Lệnh nộp GD000005 chờ xác nhận",
                NoiDung = "Lê Quang Cường nộp 1.250.000 đ (nộp tiền 5 đơn) qua Chuyển khoản VietinBank.",
                Loai = LoaiThongBao.Vi,
                DuongDan = "/QuanTriViShipper",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-25 18:12")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Chuyến xe liên tỉnh CX000004 đã khởi hành",
                NoiDung = "Tài xế Mạc Đình Long tiếp nhận 38 kiện hàng di chuyển về Bưu cục Hải Phòng.",
                Loai = LoaiThongBao.DonHang,
                DuongDan = "/QuanTriPhuongTien",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-24 08:30")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Yêu cầu rút ví shipper SP000003",
                NoiDung = "Phạm Minh Đức yêu cầu rút 680.000 đ tiền hoa hồng về ví MoMo.",
                Loai = LoaiThongBao.Vi,
                DuongDan = "/QuanTriViShipper",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-23 14:05")
            },
            new()
            {
                MaTaiKhoan = 1,
                TieuDe = "Sự cố DH202609220015: sai lệch cân nặng",
                NoiDung = "Bưu cục Hà Đông báo cân nặng thực tế 4.5kg, trên phiếu ghi 1.2kg.",
                Loai = LoaiThongBao.SuCo,
                DuongDan = "/QuanTriDonGiaoHang",
                DaDoc = false,
                NgayTao = DateTime.Parse("2026-09-22 10:15")
            }
        };

        db.ThongBaos.AddRange(dsThongBao);
        await db.SaveChangesAsync();
    }

    /// <summary>Nạp các yêu cầu liên hệ / hỗ trợ mẫu giữa khách hàng và nhân viên / admin.</summary>
    public static async Task NapLienHeMauAsync(QuanLyGiaoNhanDbContext db)
    {
        if (await db.YeuCauLienHes.AnyAsync()) return;

        var kh1 = await db.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == 1);
        var kh2 = await db.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == 2);
        if (kh1 == null) return;

        var don1 = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaKhachHang == kh1.MaKhachHang);
        var buuCucHn = await db.BuuCucs.FirstOrDefaultAsync();

        var lh1 = new YeuCauLienHe
        {
            MaKhachHang = kh1.MaKhachHang,
            MaDonHang = don1?.MaDon,
            MaBuuCuc = buuCucHn?.MaBuuCuc,
            ChuDe = ChuDeLienHe.CuocPhi,
            TieuDe = "Thắc mắc về cách tính phụ phí hàng cồng kềnh",
            NoiDung = "Chào bưu cục, kiện hàng của tôi kích thước khoảng 0.035 m³, cho tôi hỏi cách tính phụ phí cụ thể như thế nào ạ?",
            NgayGui = DateTime.Now.AddDays(-2),
            TrangThai = TrangThaiLienHe.DaPhanHoi,
            NgayCapNhat = DateTime.Now.AddDays(-1).AddHours(3)
        };
        db.YeuCauLienHes.Add(lh1);
        await db.SaveChangesAsync();

        db.TinNhanLienHes.AddRange(
            new TinNhanLienHe
            {
                MaLienHe = lh1.MaLienHe,
                MaTaiKhoan = kh1.MaTaiKhoan,
                NguoiGui = $"{kh1.HoTen} (Khách hàng)",
                LaNhanVien = false,
                NoiDung = "Chào bưu cục, kiện hàng của tôi kích thước khoảng 0.035 m³, cho tôi hỏi cách tính phụ phí cụ thể như thế nào ạ?",
                NgayGui = lh1.NgayGui
            },
            new TinNhanLienHe
            {
                MaLienHe = lh1.MaLienHe,
                MaTaiKhoan = 1,
                NguoiGui = "Quản trị hệ thống (CSKH)",
                LaNhanVien = true,
                NoiDung = "Chào bạn! Với kích thước 0.035 m³ thuộc nhóm 'Cồng kềnh lớn' (0.03 – 0.08 m³), mức phụ thu cồng kềnh là 20.000 đ. Kiện hàng dưới 0.01 m³ sẽ được miễn phụ thu hoàn toàn bạn nhé.",
                NgayGui = DateTime.Now.AddDays(-1).AddHours(3)
            }
        );

        if (kh2 != null)
        {
            var don2 = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaKhachHang == kh2.MaKhachHang);
            var lh2 = new YeuCauLienHe
            {
                MaKhachHang = kh2.MaKhachHang,
                MaDonHang = don2?.MaDon,
                MaBuuCuc = buuCucHn?.MaBuuCuc,
                ChuDe = ChuDeLienHe.ThayDoiThongTin,
                TieuDe = "Cần thay đổi số điện thoại người nhận đơn hàng",
                NoiDung = "Người nhận vừa đổi số điện thoại liên lạc mới là 0988776655, nhờ bưu cục cập nhật giúp tôi với shipper trước khi phát hàng nhé.",
                NgayGui = DateTime.Now.AddHours(-3),
                TrangThai = TrangThaiLienHe.ChoPhanHoi,
                NgayCapNhat = DateTime.Now.AddHours(-3)
            };
            db.YeuCauLienHes.Add(lh2);
            await db.SaveChangesAsync();

            db.TinNhanLienHes.Add(new TinNhanLienHe
            {
                MaLienHe = lh2.MaLienHe,
                MaTaiKhoan = kh2.MaTaiKhoan,
                NguoiGui = $"{kh2.HoTen} (Khách hàng)",
                LaNhanVien = false,
                NoiDung = "Người nhận vừa đổi số điện thoại liên lạc mới là 0988776655, nhờ bưu cục cập nhật giúp tôi với shipper trước khi phát hàng nhé.",
                NgayGui = lh2.NgayGui
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Shipper hợp lệ đầu tiên theo quy tắc của form phân công (ưu tiên đúng khu vực, giữ ít đơn nhất).</summary>
    private static async Task<int?> ChonShipperAsync(DonHangXuLy xuLy, DonGiaoHang don, LoaiShipper loai, int? boQuaNhanVien) =>
        (await xuLy.LayLuaChonAsync(don, loai, null)).FirstOrDefault(x => x.DuocChon && x.MaNhanVien != boQuaNhanVien)?.MaNhanVien;

    /// <summary>Đồng bộ số dư ví và khởi tạo lịch sử giao dịch COD mẫu cho khách hàng có đơn hoàn tất.</summary>
    private static async Task DongBoViKhachHangMauAsync(QuanLyGiaoNhanDbContext db)
    {
        try
        {
            var dsKhach = await db.KhachHangs.Include(k => k.DonGiaoHangs).ToListAsync();
            string[] nganHangs = ["Vietcombank", "MB Bank", "Techcombank", "BIDV", "ACB"];
            int nhIdx = 0;

            foreach (var kh in dsKhach)
            {
                if (string.IsNullOrEmpty(kh.TenNganHang))
                {
                    kh.TenNganHang = nganHangs[nhIdx % nganHangs.Length];
                    kh.SoTaiKhoanNganHang = $"1900{kh.MaKhachHang:D4}888";
                    kh.ChuTaiKhoanNganHang = kh.HoTen.ToUpperInvariant();
                    nhIdx++;
                }

                var donHoanTats = kh.DonGiaoHangs
                    .Where(d => d.TrangThai == TrangThaiDon.HoanTat && d.TienTraNguoiGui > 0)
                    .OrderBy(d => d.NgayHoanTat ?? d.NgayTao)
                    .ToList();

                var gdHienCo = await db.GiaoDichViKhachHangs.Where(g => g.MaKhachHang == kh.MaKhachHang).ToListAsync();

                if (gdHienCo.Count == 0 && donHoanTats.Count > 0)
                {
                    decimal soDu = 0;
                    foreach (var don in donHoanTats)
                    {
                        soDu += don.TienTraNguoiGui;
                        var gd = new GiaoDichViKhachHang
                        {
                            MaKhachHang = kh.MaKhachHang,
                            Loai = LoaiGiaoDichKhach.CongTienCOD,
                            SoTien = don.TienTraNguoiGui,
                            SoDuSauGiaoDich = soDu,
                            MaDonHang = don.MaDon,
                            TrangThai = TrangThaiGiaoDich.DaXacNhan,
                            NgayTao = don.NgayHoanTat ?? don.NgayTao,
                            NgayXuLy = don.NgayHoanTat ?? don.NgayTao,
                            NguoiXuLy = "Hệ thống tự động",
                            GhiChu = $"Cộng tiền thu hộ COD đơn {don.MaHienThi} (sau khi đối soát trừ cước)"
                        };
                        db.GiaoDichViKhachHangs.Add(gd);
                    }

                    if (soDu >= 500000)
                    {
                        decimal tienRut = 300000;
                        soDu -= tienRut;
                        var gdRut = new GiaoDichViKhachHang
                        {
                            MaKhachHang = kh.MaKhachHang,
                            Loai = LoaiGiaoDichKhach.RutTien,
                            SoTien = tienRut,
                            SoDuSauGiaoDich = soDu,
                            TrangThai = TrangThaiGiaoDich.DaXacNhan,
                            TenNganHang = kh.TenNganHang,
                            SoTaiKhoan = kh.SoTaiKhoanNganHang,
                            ChuTaiKhoan = kh.ChuTaiKhoanNganHang,
                            MaThamChieu = $"FT{DateTime.Now:yyyyMMdd}9988",
                            NgayTao = DateTime.Now.AddDays(-1),
                            NgayXuLy = DateTime.Now.AddDays(-1),
                            NguoiXuLy = "Quản trị hệ thống",
                            GhiChu = "Rút tiền COD về tài khoản ngân hàng"
                        };
                        db.GiaoDichViKhachHangs.Add(gdRut);
                    }

                    kh.SoDuVi = soDu;
                }
            }

            await db.SaveChangesAsync();
        }
        catch { /* Bỏ qua nếu có ngoại lệ đồng bộ */ }
    }
    /// <summary>Náº¡p 1-2 chuyáº¿n xe liÃªn tá»‰nh máº«u ban Ä‘áº§u náº¿u há»‡ thá»‘ng chÆ°a cÃ³ chuyáº¿n nÃ o.</summary>
    public static async Task TaoChuyenXeMauAsync(QuanLyGiaoNhanDbContext db)
    {
        try
        {
            if (await db.ChuyenXes.AnyAsync()) return;

            var bcBn = await db.BuuCucs.FirstOrDefaultAsync(b => b.TinhThanh.Contains("Báº¯c Ninh") || b.TenBuuCuc.Contains("Báº¯c Ninh"))
                     ?? await db.BuuCucs.FirstOrDefaultAsync(b => b.TinhThanh.Contains("HÃ  Ná»™i") || b.TenBuuCuc.Contains("HÃ  Ná»™i"));
            var bcDn = await db.BuuCucs.FirstOrDefaultAsync(b => b.TinhThanh.Contains("ÄÃ  Náºµng") || b.TenBuuCuc.Contains("ÄÃ  Náºµng"));
            var bcHcm = await db.BuuCucs.FirstOrDefaultAsync(b => b.TinhThanh.Contains("Há»“ ChÃ­ Minh") || b.TenBuuCuc.Contains("Há»“ ChÃ­ Minh"));
            if (bcBn == null || bcDn == null) return;

            var shipperTai = await db.NhanVienGiaoHangs.Include(s => s.PhuongTien)
                .FirstOrDefaultAsync(s => s.MaBuuCuc == bcBn.MaBuuCuc && s.LoaiShipper == LoaiShipper.LienTinh);
            if (shipperTai == null) return;

            int maDich = bcHcm?.MaBuuCuc ?? bcDn.MaBuuCuc;
            int? maTram = bcHcm != null ? bcDn.MaBuuCuc : null;

            var chuyen1 = new ChuyenXe
            {
                MaBuuCucGui = bcBn.MaBuuCuc,
                MaBuuCucNhan = maDich,
                MaBuuCucTrungGian = maTram,
                MaNhanVien = shipperTai.MaNhanVien,
                MaPhuongTien = shipperTai.MaPhuongTien,
                TrangThai = TrangThaiChuyenXe.DangXepHang,
                NgayTao = DateTime.Now.AddHours(-2),
                GhiChu = maTram.HasValue ? "Chuyáº¿n trá»¥c Báº¯c - Nam (Báº¯c Ninh âž” ghÃ© Hub ÄÃ  Náºµng âž” Hub TP.HCM)" : "Chuyáº¿n ghÃ©p liÃªn tá»‰nh Báº¯c Ninh - ÄÃ  Náºµng trong ngÃ y"
            };
            db.ChuyenXes.Add(chuyen1);
            await db.SaveChangesAsync();

            // GÃ¡n cÃ¡c Ä‘Æ¡n liÃªn tá»‰nh náº¿u cÃ³
            var donLienTinh = await db.DonGiaoHangs
                .Where(d => d.MaBuuCucGui == bcBn.MaBuuCuc && d.MaChuyenXe == null)
                .Take(2)
                .ToListAsync();

            foreach (var d in donLienTinh)
            {
                d.MaChuyenXe = chuyen1.MaChuyen;
                if (d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || d.TrangThai == TrangThaiDon.ChoLayLienTinh)
                {
                    d.TrangThai = TrangThaiDon.DaLayLienTinh;
                    d.MaNhanVienDangGiu = shipperTai.MaNhanVien;
                }
            }
            if (donLienTinh.Count > 0)
                await db.SaveChangesAsync();
        }
        catch
        {
            // Bá» qua náº¿u cÃ³ lá»—i náº¡p chuyáº¿n xe máº«u
        }
    }
}
