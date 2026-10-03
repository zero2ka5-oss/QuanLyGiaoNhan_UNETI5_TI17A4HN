// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: DbContext EF Core 10 – khai báo 9 DbSet, khóa, quan hệ, unique index, ràng buộc CHECK
//                     và nạp dữ liệu mẫu (mục 16 của đề).

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;

/*
 * QuanLyGiaoNhanDbContext — Entity → DbContext → Migration → SQL Server
 * ---------------------------------------------------------------------
 * Quan hệ (mục 10 của đề):
 *   TaiKhoan 1 – 0..1 KhachHang, TaiKhoan 1 – 0..1 NhanVienGiaoHang
 *   KhachHang / LoaiHang / KhuVuc 1 – n DonGiaoHang
 *   DonGiaoHang / NhanVienGiaoHang / PhuongTien 1 – n PhanCongGiaoHang
 *   DonGiaoHang 1 – n LichSuGiaoNhan
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

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<TaiKhoan>(e =>
        {
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.Property(x => x.TenDangNhap).IsUnicode(false);
        });

        mb.Entity<KhachHang>(e =>
        {
            e.HasIndex(x => x.MaTaiKhoan).IsUnique().HasFilter("[MaTaiKhoan] IS NOT NULL");
            e.HasOne(x => x.TaiKhoan).WithOne(t => t.KhachHang).HasForeignKey<KhachHang>(x => x.MaTaiKhoan);
            e.HasIndex(x => x.SoDienThoai);
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
        });

        mb.Entity<DonGiaoHang>(e =>
        {
            e.Property(x => x.KhoiLuong).HasColumnType("decimal(10,2)");
            e.Property(x => x.NgayGiaoDuKien).HasColumnType("date");
            foreach (var cot in new[] { nameof(DonGiaoHang.PhiCoBan), nameof(DonGiaoHang.PhuPhiKhoiLuong),
                                        nameof(DonGiaoHang.PhuPhiLoaiHang), nameof(DonGiaoHang.PhiVanChuyen), nameof(DonGiaoHang.TienThuHo) })
                e.Property(cot).HasColumnType("decimal(18,0)");
            e.HasIndex(x => x.TrangThai);
            e.HasIndex(x => x.NgayTao);
            e.HasOne(x => x.KhachHang).WithMany(k => k.DonGiaoHangs).HasForeignKey(x => x.MaKhachHang);
            e.HasOne(x => x.LoaiHang).WithMany(l => l.DonGiaoHangs).HasForeignKey(x => x.MaLoaiHang);
            e.HasOne(x => x.KhuVuc).WithMany(k => k.DonGiaoHangs).HasForeignKey(x => x.MaKhuVuc);
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
            e.HasOne(x => x.TaiKhoan).WithOne(t => t.NhanVienGiaoHang).HasForeignKey<NhanVienGiaoHang>(x => x.MaTaiKhoan);
            e.HasOne(x => x.KhuVucPhuTrach).WithMany(k => k.NhanViens).HasForeignKey(x => x.MaKhuVucPhuTrach);
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
            e.HasOne(x => x.DonGiaoHang).WithMany(d => d.PhanCongs).HasForeignKey(x => x.MaDon);
            e.HasOne(x => x.NhanVien).WithMany(n => n.PhanCongs).HasForeignKey(x => x.MaNhanVien);
            e.HasOne(x => x.PhuongTien).WithMany(p => p.PhanCongs).HasForeignKey(x => x.MaPhuongTien);
        });

        mb.Entity<LichSuGiaoNhan>(e =>
        {
            e.HasIndex(x => new { x.MaDon, x.ThoiGian });
            e.HasIndex(x => x.ThoiGian);   // trang lịch sử + "hoạt động gần đây" sắp xếp toàn bảng theo thời gian
            e.HasOne(x => x.DonGiaoHang).WithMany(d => d.LichSus).HasForeignKey(x => x.MaDon);
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
                    TieuDe = "Giao hàng nhanh, theo dõi minh bạch từng bước", MoTa = "Lấy hàng tận nơi trong ngày, phí hiển thị ngay khi tạo đơn.",
                    ChuNut = "Tạo đơn ngay", LienKet = "/DangNhap/DangKy" },
                new BannerTrangChu { MaBanner = 2, ThuTu = 2, Anh = "/img/banner/banner-2.svg", NgayTao = new DateTime(2026, 9, 1),
                    TieuDe = "Thu hộ tiền hàng an toàn, đối soát rõ ràng", MoTa = "Tiền thu hộ được nộp về công ty và trả lại người gửi sau khi đối soát.",
                    ChuNut = "Tìm hiểu thêm", LienKet = "/TrangChu/ChiTietTinTuc/2" },
                new BannerTrangChu { MaBanner = 3, ThuTu = 3, Anh = "/img/banner/banner-3.svg", NgayTao = new DateTime(2026, 9, 1),
                    TieuDe = "Tra cứu hành trình đơn hàng mọi lúc", MoTa = "Chỉ cần mã đơn và số điện thoại người nhận.",
                    ChuNut = "Tra cứu đơn", LienKet = "/TrangChu/TraCuu" });
        });

        // Không xóa dây chuyền – bảo toàn lịch sử giao nhận
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    // ===================== CHỌN MÁY CHỦ SQL SERVER =====================

    /*
     * Mỗi máy cài SQL Server khác nhau: SQL Server đầy đủ (.), SQL Server Express (.\SQLEXPRESS)
     * hoặc chỉ có LocalDB đi kèm Visual Studio. Hàm thử lần lượt các máy chủ trong appsettings.json
     * ("MayChuSqlThuLanLuot") và dùng máy chủ đầu tiên kết nối được, nên mang dự án sang máy khác vẫn chạy.
     * Với LocalDB: bật sẵn instance bằng "sqllocaldb start" vì LocalDB tự tắt khi rảnh.
     */
    public static string ChonChuoiKetNoi(string chuoiGoc, IEnumerable<string> dsMayChu, Action<string>? ghiLog = null)
    {
        var dsThu = dsMayChu.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var goc = new SqlConnectionStringBuilder(chuoiGoc);
        if (dsThu.Count == 0) dsThu.Add(goc.DataSource);
        var loi = new List<string>();

        foreach (var mayChu in dsThu)
        {
            bool laLocalDb = mayChu.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase);
            if (laLocalDb) BatLocalDb(mayChu);

            // Kiểm tra bằng CSDL master: CSDL của ứng dụng có thể chưa tồn tại (Migrate sẽ tạo)
            var thu = new SqlConnectionStringBuilder(goc.ConnectionString)
            {
                DataSource = mayChu, InitialCatalog = "master", ConnectTimeout = laLocalDb ? 30 : 5, Pooling = false
            };
            try
            {
                using var ketNoi = new SqlConnection(thu.ConnectionString);
                ketNoi.Open();
                ghiLog?.Invoke($"Dùng SQL Server: {mayChu}");
                return new SqlConnectionStringBuilder(goc.ConnectionString) { DataSource = mayChu }.ConnectionString;
            }
            catch (Exception ex)
            {
                loi.Add($"  - {mayChu}: {ex.Message.Split('\n')[0]}");
            }
        }
        throw new InvalidOperationException(
            "Không kết nối được SQL Server nào. Hãy cài SQL Server / SQL Server Express hoặc LocalDB (đi kèm Visual Studio), " +
            "hoặc sửa \"MayChuSqlThuLanLuot\" trong appsettings.json.\n" + string.Join("\n", loi));
    }

    private static void BatLocalDb(string mayChu)
    {
        var tenInstance = mayChu[(mayChu.IndexOf(')') + 1)..].TrimStart('\\');
        try
        {
            using var tienTrinh = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sqllocaldb", $"start \"{tenInstance}\"")
            {
                CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
            });
            tienTrinh?.WaitForExit(60_000);
        }
        catch (Exception) { /* máy không có sqllocaldb → để SqlClient tự xử lý */ }
    }

    // ===================== DỮ LIỆU MẪU =====================

    /*
     * DỮ LIỆU MẪU — NapDuLieuMauAsync gọi trong Program.cs sau db.Database.Migrate()
     * -----------------------------------------------------------
     *  - 2 tài khoản quản lý: admin (Quản trị), dieuphoi (Điều phối)          – mật khẩu 123456
     *  - 5 loại hàng (1 ngừng), 6 khu vực (1 ngừng)
     *  - 20 khách hàng kh01..kh20 (kh20 bị khóa để kiểm thử)                  – mật khẩu 123456
     *  - 8 nhân viên nv01..nv08 (1 tạm nghỉ, 1 ngừng), 8 phương tiện (1 bảo trì, 1 ngừng, tải trọng 80 kg → 1,5 tấn)
     *  - 40 đơn đi qua ĐÚNG nghiệp vụ DonHangXuLy (đồng hồ giả lập) nên có đủ: chờ phân công, đang giao, giao thất bại,
     *    đổi phân công, giao lại sau thất bại, hoàn tất, hủy – kèm phân công và lịch sử giao nhận nhất quán.
     */

    public static async Task NapDuLieuMauAsync(QuanLyGiaoNhanDbContext db, bool coDonMau = true)
    {
        if (await db.TaiKhoans.AnyAsync()) return;

        var bayGio = DateTime.Now;
        string matKhau = MatKhauHelper.Bam("123456");
        TaiKhoan TaoTk(string ten, string hoTen, string email, string vaiTro, int ngayTruoc) => new()
        {
            TenDangNhap = ten, MatKhau = matKhau, HoTen = hoTen, Email = email, VaiTro = vaiTro, NgayTao = bayGio.AddDays(-ngayTruoc)
        };

        // 1. Tài khoản quản lý
        var tkDieuPhoi = TaoTk("dieuphoi", "Trần Thu Hà", "dieuphoi@giaonhan.vn", VaiTroNguoiDung.DieuPhoi, 90);
        db.TaiKhoans.AddRange(TaoTk("admin", "Quản trị hệ thống", "admin@giaonhan.vn", VaiTroNguoiDung.QuanTri, 90), tkDieuPhoi);

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

        // 3. Khu vực giao
        var dsKhuVuc = new List<KhuVuc>
        {
            new() { TenKhuVuc = "Nội thành Hà Nội", PhiCoBan = 18_000, MoTa = "Hoàn Kiếm, Ba Đình, Đống Đa, Hai Bà Trưng, Cầu Giấy, Thanh Xuân" },
            new() { TenKhuVuc = "Ngoại thành Hà Nội", PhiCoBan = 25_000, MoTa = "Hà Đông, Long Biên, Gia Lâm, Đông Anh, Hoài Đức..." },
            new() { TenKhuVuc = "Tỉnh lân cận", PhiCoBan = 32_000, MoTa = "Bắc Ninh, Hưng Yên, Hải Dương, Vĩnh Phúc, Hà Nam" },
            new() { TenKhuVuc = "Miền Trung", PhiCoBan = 45_000, MoTa = "Thanh Hóa đến Khánh Hòa" },
            new() { TenKhuVuc = "Miền Nam", PhiCoBan = 55_000, MoTa = "TP.HCM và các tỉnh phía Nam" },
            new() { TenKhuVuc = "Hải đảo", PhiCoBan = 90_000, MoTa = "Tạm ngừng nhận đơn", TrangThai = TrangThaiHoatDong.NgungHoatDong },
        };
        db.KhuVucs.AddRange(dsKhuVuc);

        // 4. Khách hàng (có tài khoản đăng nhập)
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

        // 5. Nhân viên giao hàng (có tài khoản)
        (string Ten, int KhuVuc, TrangThaiNhanVien TrangThai)[] nvData =
        [
            ("Nguyễn Đức Anh", 0, TrangThaiNhanVien.SanSang), ("Trần Văn Bảo", 0, TrangThaiNhanVien.SanSang),
            ("Lê Quang Cường", 1, TrangThaiNhanVien.SanSang), ("Phạm Minh Đức", 1, TrangThaiNhanVien.SanSang),
            ("Hoàng Văn Em", 2, TrangThaiNhanVien.SanSang), ("Đinh Công Phúc", 3, TrangThaiNhanVien.SanSang),
            ("Vũ Thành Giang", 0, TrangThaiNhanVien.TamNghi), ("Bùi Văn Hùng", 1, TrangThaiNhanVien.NgungHoatDong),
        ];
        var dsNhanVien = nvData.Select((n, i) => new NhanVienGiaoHang
        {
            TaiKhoan = TaoTk($"nv{i + 1:D2}", n.Ten, $"nv{i + 1:D2}@giaonhan.vn", VaiTroNguoiDung.GiaoHang, 85),
            HoTen = n.Ten, SoDienThoai = $"08{61230000 + i * 111:D8}", Email = $"nv{i + 1:D2}@giaonhan.vn",
            KhuVucPhuTrach = dsKhuVuc[n.KhuVuc], TrangThai = n.TrangThai
        }).ToList();
        db.NhanVienGiaoHangs.AddRange(dsNhanVien);

        // 6. Phương tiện
        var dsPhuongTien = new List<PhuongTien>
        {
            new() { BienSo = "29B1-123.45", LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 80, GhiChu = "Thùng sau 60 lít" },
            new() { BienSo = "29B1-234.56", LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 80 },
            new() { BienSo = "29H1-345.67", LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 100, GhiChu = "Xe có giá chở hàng" },
            new() { BienSo = "30F-456.78", LoaiPhuongTien = "Xe ba gác", TaiTrongToiDa = 300 },
            new() { BienSo = "29C-567.89", LoaiPhuongTien = "Xe tải 500 kg", TaiTrongToiDa = 500 },
            new() { BienSo = "29C-678.90", LoaiPhuongTien = "Xe tải 1 tấn", TaiTrongToiDa = 1000, TrangThai = TrangThaiPhuongTien.BaoTri, GhiChu = "Bảo dưỡng định kỳ" },
            new() { BienSo = "29C-789.01", LoaiPhuongTien = "Xe tải 1,5 tấn", TaiTrongToiDa = 1500, GhiChu = "Chạy tuyến liên tỉnh" },
            new() { BienSo = "29D-890.12", LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 80, TrangThai = TrangThaiPhuongTien.NgungHoatDong, GhiChu = "Đã thanh lý" },
        };
        db.PhuongTiens.AddRange(dsPhuongTien);
        await db.SaveChangesAsync();

        if (coDonMau)
            await TaoDonMauAsync(db, dsKhach, dsLoai.Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).ToList(),
                                 dsKhuVuc.Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).ToList(), dsNhanVien, dsPhuongTien,
                                 $"{tkDieuPhoi.HoTen} (Điều phối)");
        if (coDonMau)
            await TaoGiaoDichViMauAsync(db, $"{tkDieuPhoi.HoTen} (Điều phối)");
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
        bool daTaoNopCho = false, daTaoRutCho = false, daTaoTuChoi = false;

        foreach (var nv in dsNv)
        {
            var cuaNv = donThanhCong.Where(x => x.MaNhanVien == nv.MaNhanVien).Select(x => x.Don).ToList();
            if (cuaNv.Count == 0) continue;
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

            // 2. Đã rút khoảng một nửa tiền ship về ngân hàng
            decimal thuNhap = cuaNv.Sum(d => d.PhiVanChuyen);
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
        await db.SaveChangesAsync();
    }

    /// <summary>Kịch bản cuối cùng mong muốn của mỗi đơn mẫu.</summary>
    private enum KichBan { ChoPhanCong, DaPhanCong, DaNhanHang, DangGiao, ThanhCong, HoanTat, ThatBai, HuyNgay, HuySauPhanCong, ThatBaiRoiGiaoLai, DoiPhanCong, DoiRoiHoanTat }

    private static async Task TaoDonMauAsync(QuanLyGiaoNhanDbContext db, List<KhachHang> dsKhach, List<LoaiHang> dsLoai,
                                             List<KhuVuc> dsKhuVuc, List<NhanVienGiaoHang> dsNhanVien, List<PhuongTien> dsPhuongTien,
                                             string dieuPhoi)
    {
        var xuLy = new DonHangXuLy(db);
        var ngauNhien = new Random(2026);
        var bayGioThat = DateTime.Now;
        DateTime dongHo = bayGioThat;
        xuLy.BayGio = () => dongHo < bayGioThat ? dongHo : bayGioThat;

        // Nhân viên i dùng phương tiện cố định (nv01→xe 1, ..., nv05→tải 500 kg, nv06→tải 1,5 tấn)
        var xeCuaNhanVien = new Dictionary<int, PhuongTien>
        {
            [dsNhanVien[0].MaNhanVien] = dsPhuongTien[0], [dsNhanVien[1].MaNhanVien] = dsPhuongTien[1],
            [dsNhanVien[2].MaNhanVien] = dsPhuongTien[2], [dsNhanVien[3].MaNhanVien] = dsPhuongTien[3],
            [dsNhanVien[4].MaNhanVien] = dsPhuongTien[4], [dsNhanVien[5].MaNhanVien] = dsPhuongTien[6],
        };

        // 40 đơn: đơn cũ đã xong, đơn mới đang xử lý
        KichBan[] kichBan =
        [
            .. Enumerable.Repeat(KichBan.HoanTat, 8), KichBan.ThatBaiRoiGiaoLai, KichBan.ThatBaiRoiGiaoLai, KichBan.DoiRoiHoanTat,
            .. Enumerable.Repeat(KichBan.ThanhCong, 5), .. Enumerable.Repeat(KichBan.ThatBai, 3),
            KichBan.HuyNgay, KichBan.HuyNgay, KichBan.HuySauPhanCong,
            .. Enumerable.Repeat(KichBan.DangGiao, 4), .. Enumerable.Repeat(KichBan.DaNhanHang, 3),
            .. Enumerable.Repeat(KichBan.DaPhanCong, 4), KichBan.DoiPhanCong,
            .. Enumerable.Repeat(KichBan.ChoPhanCong, 6),
        ];
        string[] nguoiNhan = ["Nguyễn Hữu Thắng", "Trần Mai Anh", "Lê Thị Hoa", "Phạm Gia Bảo", "Đỗ Minh Khoa", "Vũ Thị Ngọc",
                              "Hoàng Anh Tuấn", "Bùi Thị Thu", "Ngô Quang Huy", "Đặng Thùy Linh", "Phan Văn Toàn", "Lý Mỹ Duyên"];
        string[] diaChiNhan = ["12 Tràng Tiền", "45 Tôn Đức Thắng", "88 Cầu Giấy", "21 Quang Trung, Hà Đông", "5 Nguyễn Văn Cừ, Long Biên",
                               "102 Lý Thái Tổ, Bắc Ninh", "7 Trần Phú, Hải Dương", "33 Lê Lợi, Thanh Hóa", "18 Bạch Đằng, Đà Nẵng",
                               "250 Nguyễn Thị Minh Khai, TP.HCM", "9 Hùng Vương, Cần Thơ", "60 Nguyễn Huệ, Huế"];
        decimal[] khoiLuong = [0.5m, 0.8m, 1.2m, 1.5m, 2m, 2.5m, 3.2m, 4m, 5.5m, 7m, 9.5m, 12m, 15m, 25m];

        for (int i = 0; i < kichBan.Length; i++)
        {
            var kb = kichBan[i];
            bool dangXuLy = kb is KichBan.DangGiao or KichBan.DaNhanHang or KichBan.DaPhanCong or KichBan.DoiPhanCong or KichBan.ChoPhanCong;
            // Đơn đang xử lý: 0–2 ngày trước; đơn đã xong: rải trong ~5 tháng để thống kê theo tháng có dữ liệu
            int soNgayTruoc = dangXuLy ? ngauNhien.Next(0, 3) : ngauNhien.Next(3, 150);
            dongHo = bayGioThat.Date.AddDays(-soNgayTruoc).AddHours(7).AddMinutes(ngauNhien.Next(0, 180));

            var khach = dsKhach[i % 19];                                  // kh20 bị khóa – không tạo đơn
            var khuVuc = dsKhuVuc[ngauNhien.Next(100) < 55 ? 0 : ngauNhien.Next(dsKhuVuc.Count)];
            decimal kg = i is 5 or 17 ? 65m : i == 26 ? 150m : khoiLuong[ngauNhien.Next(khoiLuong.Length)];   // vài đơn nặng cần xe lớn
            var kq = await xuLy.TaoDonAsync(new DonGiaoHangFormVM
            {
                MaLoaiHang = dsLoai[ngauNhien.Next(dsLoai.Count)].MaLoaiHang, MaKhuVuc = khuVuc.MaKhuVuc,
                TenNguoiNhan = nguoiNhan[ngauNhien.Next(nguoiNhan.Length)], SoDienThoaiNguoiNhan = $"09{ngauNhien.Next(10_000_000, 99_999_999)}",
                DiaChiNhan = diaChiNhan[ngauNhien.Next(diaChiNhan.Length)], KhoiLuong = kg,
                NgayGiaoDuKien = dongHo.Date.AddDays(ngauNhien.Next(1, 4)),
                GhiChu = ngauNhien.Next(3) == 0 ? "Gọi trước khi giao" : null,
                // ~2/3 đơn có thu hộ (tiền hàng 150.000 – 2.500.000 đ, làm tròn nghìn); ~40% đơn người nhận trả phí ship
                TienThuHo = ngauNhien.Next(3) == 0 ? 0 : ngauNhien.Next(15, 251) * 10_000m,
                NguoiTraPhi = ngauNhien.Next(5) < 2 ? NguoiTraPhi.NguoiNhan : NguoiTraPhi.NguoiGui
            }, khach.MaKhachHang, $"{khach.HoTen} (Khách hàng)");
            var don = kq.DuLieu!;

            if (kb == KichBan.ChoPhanCong) continue;
            if (kb == KichBan.HuyNgay)
            {
                dongHo = dongHo.AddHours(1);
                await xuLy.HuyDonAsync(don.MaDon, "Khách đổi ý, không gửi nữa", $"{khach.HoTen} (Khách hàng)", khach.MaKhachHang);
                continue;
            }

            // Phân công nhân viên phù hợp: ưu tiên cùng khu vực, đủ tải trọng
            var nv = await ChonNhanVienAsync(xuLy, don, dsNhanVien, xeCuaNhanVien, null);
            if (nv is null) continue;
            dongHo = dongHo.AddHours(2);
            await xuLy.PhanCongAsync(don.MaDon, nv.MaNhanVien, xeCuaNhanVien[nv.MaNhanVien].MaPhuongTien, null, dieuPhoi);

            if (kb is KichBan.DoiPhanCong or KichBan.DoiRoiHoanTat)
            {
                var nvMoi = await ChonNhanVienAsync(xuLy, don, dsNhanVien, xeCuaNhanVien, nv.MaNhanVien);
                if (nvMoi is not null)
                {
                    dongHo = dongHo.AddHours(1);
                    await xuLy.DoiPhanCongAsync(don.MaDon, nvMoi.MaNhanVien, xeCuaNhanVien[nvMoi.MaNhanVien].MaPhuongTien,
                                                "Nhân viên cũ báo ốm, điều người khác", dieuPhoi);
                    nv = nvMoi;
                }
            }
            if (kb is KichBan.DaPhanCong or KichBan.DoiPhanCong) continue;
            if (kb == KichBan.HuySauPhanCong)
            {
                dongHo = dongHo.AddHours(1);
                await xuLy.HuyDonAsync(don.MaDon, "Khách gọi tổng đài yêu cầu hủy", dieuPhoi, null);
                continue;
            }

            if (!await GiaoAsync(xuLy, db, don.MaDon, nv, () => dongHo = dongHo.AddHours(2), kb == KichBan.DaNhanHang, kb == KichBan.DangGiao,
                                 kb is KichBan.ThatBai or KichBan.ThatBaiRoiGiaoLai ? (LyDoThatBai)(i % 5) : null))
                continue;

            if (kb == KichBan.ThatBaiRoiGiaoLai)
            {
                dongHo = dongHo.AddDays(1).Date.AddHours(8);
                var nvLai = await ChonNhanVienAsync(xuLy, don, dsNhanVien, xeCuaNhanVien, null);
                if (nvLai is null) continue;
                await xuLy.PhanCongAsync(don.MaDon, nvLai.MaNhanVien, xeCuaNhanVien[nvLai.MaNhanVien].MaPhuongTien,
                                         "Giao lại theo lịch hẹn với người nhận", dieuPhoi);
                await GiaoAsync(xuLy, db, don.MaDon, nvLai, () => dongHo = dongHo.AddHours(2), false, false, null);
            }
            if (kb is KichBan.HoanTat or KichBan.ThatBaiRoiGiaoLai or KichBan.DoiRoiHoanTat)
            {
                dongHo = dongHo.AddDays(1);
                await xuLy.HoanTatAsync(don.MaDon, dieuPhoi);
            }
        }
    }

    /// <summary>Chạy các bước của nhân viên: nhận hàng → bắt đầu giao → thành công / thất bại. Trả false nếu dừng giữa chừng.</summary>
    private static async Task<bool> GiaoAsync(DonHangXuLy xuLy, QuanLyGiaoNhanDbContext db, int maDon, NhanVienGiaoHang nv,
                                              Action tangGio, bool dungSauNhan, bool dungSauBatDau, LyDoThatBai? thatBai)
    {
        string ten = $"{nv.HoTen} (NV giao hàng)";
        int maPc = await xuLy.PhanCongHieuLuc().Where(p => p.MaDon == maDon).Select(p => p.MaPhanCong).FirstAsync();
        tangGio();
        await xuLy.NhanHangAsync(maPc, nv.MaNhanVien, ten);
        if (dungSauNhan) return false;
        tangGio();
        await xuLy.BatDauGiaoAsync(maPc, nv.MaNhanVien, ten);
        if (dungSauBatDau) return false;
        tangGio();
        if (thatBai is not null)
        {
            await xuLy.GiaoThatBaiAsync(maPc, nv.MaNhanVien, thatBai,
                thatBai == LyDoThatBai.Khac ? "Cổng khu tập thể khóa, bảo vệ không cho vào" : null, ten);
            return true;
        }
        await xuLy.GiaoThanhCongAsync(maPc, nv.MaNhanVien, "Người nhận đã ký nhận", ten);
        return true;
    }

    private static async Task<NhanVienGiaoHang?> ChonNhanVienAsync(DonHangXuLy xuLy, DonGiaoHang don, List<NhanVienGiaoHang> dsNhanVien,
                                                                   Dictionary<int, PhuongTien> xeCuaNhanVien, int? boQuaNhanVien)
    {
        // Ưu tiên cùng khu vực, rồi nhân viên đang giữ ít đơn nhất (chia đều công việc)
        var soDonDangGiu = await xuLy.PhanCongHieuLuc().GroupBy(p => p.MaNhanVien)
                                     .Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        var ungVien = dsNhanVien.Where(n => xeCuaNhanVien.ContainsKey(n.MaNhanVien) && n.MaNhanVien != boQuaNhanVien)
                                .OrderByDescending(n => n.MaKhuVucPhuTrach == don.MaKhuVuc)
                                .ThenBy(n => soDonDangGiu.GetValueOrDefault(n.MaNhanVien))
                                .ThenBy(n => n.MaNhanVien);
        foreach (var nv in ungVien)
        {
            int maXe = xeCuaNhanVien[nv.MaNhanVien].MaPhuongTien;
            if (await xuLy.KiemTraNhanVienAsync(nv.MaNhanVien, maXe) is null &&
                await xuLy.KiemTraPhuongTienAsync(maXe, nv.MaNhanVien, don.KhoiLuong) is null)
                return nv;
        }
        return null;
    }
}
