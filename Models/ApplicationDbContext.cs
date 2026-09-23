using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TaiKhoan>().HasIndex(x => x.TenDangNhap).IsUnique();
        modelBuilder.Entity<LoaiHang>().HasIndex(x => x.TenLoaiHang).IsUnique();
        modelBuilder.Entity<KhuVuc>().HasIndex(x => x.TenKhuVuc).IsUnique();
        modelBuilder.Entity<PhuongTien>().HasIndex(x => x.BienSo).IsUnique();
        modelBuilder.Entity<LoaiHang>().Property(x => x.HeSoPhuThu).HasPrecision(18, 2);
        modelBuilder.Entity<KhuVuc>().Property(x => x.PhiCoBan).HasPrecision(18, 2);
        modelBuilder.Entity<DonGiaoHang>().Property(x => x.KhoiLuong).HasPrecision(18, 2);
        modelBuilder.Entity<DonGiaoHang>().Property(x => x.PhiVanChuyen).HasPrecision(18, 2);
        modelBuilder.Entity<PhuongTien>().Property(x => x.TaiTrongToiDa).HasPrecision(18, 2);

        modelBuilder.Entity<KhachHang>().HasOne(x => x.TaiKhoan).WithOne(x => x.KhachHang).HasForeignKey<KhachHang>(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<NhanVienGiaoHang>().HasOne(x => x.TaiKhoan).WithOne(x => x.NhanVienGiaoHang).HasForeignKey<NhanVienGiaoHang>(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DonGiaoHang>().HasOne(x => x.KhachHang).WithMany(x => x.DonGiaoHangs).HasForeignKey(x => x.MaKhachHang).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DonGiaoHang>().HasOne(x => x.LoaiHang).WithMany(x => x.DonGiaoHangs).HasForeignKey(x => x.MaLoaiHang).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DonGiaoHang>().HasOne(x => x.KhuVuc).WithMany(x => x.DonGiaoHangs).HasForeignKey(x => x.MaKhuVuc).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PhanCongGiaoHang>().HasOne(x => x.DonGiaoHang).WithMany(x => x.PhanCongGiaoHangs).HasForeignKey(x => x.MaDon).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PhanCongGiaoHang>().HasOne(x => x.NhanVienGiaoHang).WithMany(x => x.PhanCongGiaoHangs).HasForeignKey(x => x.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PhanCongGiaoHang>().HasOne(x => x.PhuongTien).WithMany(x => x.PhanCongGiaoHangs).HasForeignKey(x => x.MaPhuongTien).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LichSuGiaoNhan>().HasOne(x => x.DonGiaoHang).WithMany(x => x.LichSuGiaoNhans).HasForeignKey(x => x.MaDon).OnDelete(DeleteBehavior.Cascade);
    }
}
