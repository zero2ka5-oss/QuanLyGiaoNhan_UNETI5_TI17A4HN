using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        if (await context.TaiKhoans.AnyAsync()) return;

        var admin = new TaiKhoan { TenDangNhap = "admin", MatKhau = "Admin@123", HoTen = "Quản trị viên", Email = "admin@uneti.edu.vn", VaiTro = "Admin" };
        var shipperAccount = new TaiKhoan { TenDangNhap = "shipper01", MatKhau = "Shipper@123", HoTen = "Nguyễn Văn Giao", Email = "shipper01@uneti.edu.vn", VaiTro = "Nhân viên giao hàng" };
        var customerAccount = new TaiKhoan { TenDangNhap = "khachhang01", MatKhau = "Khach@123", HoTen = "Trần Thị Khách", Email = "khach01@example.com", VaiTro = "Khách hàng" };
        context.TaiKhoans.AddRange(admin, shipperAccount, customerAccount);

        var types = new[]
        {
            new LoaiHang { TenLoaiHang = "Hàng tiêu chuẩn", HeSoPhuThu = 1, MoTa = "Hàng hóa thông thường" },
            new LoaiHang { TenLoaiHang = "Hàng dễ vỡ", HeSoPhuThu = 1.2m, MoTa = "Cần đóng gói cẩn thận" },
            new LoaiHang { TenLoaiHang = "Tài liệu", HeSoPhuThu = 0.8m },
            new LoaiHang { TenLoaiHang = "Thực phẩm khô", HeSoPhuThu = 1.1m }
        };
        var areas = new[]
        {
            new KhuVuc { TenKhuVuc = "Hoàng Mai", PhiCoBan = 25000 }, new KhuVuc { TenKhuVuc = "Hai Bà Trưng", PhiCoBan = 20000 },
            new KhuVuc { TenKhuVuc = "Cầu Giấy", PhiCoBan = 25000 }, new KhuVuc { TenKhuVuc = "Thanh Xuân", PhiCoBan = 22000 }
        };
        context.LoaiHangs.AddRange(types);
        context.KhuVucs.AddRange(areas);
        await context.SaveChangesAsync();

        var customer = new KhachHang { MaTaiKhoan = customerAccount.MaTaiKhoan, HoTen = customerAccount.HoTen, SoDienThoai = "0900000001", Email = customerAccount.Email, DiaChi = "Hà Nội" };
        var employees = new[]
        {
            new NhanVienGiaoHang { MaTaiKhoan = shipperAccount.MaTaiKhoan, HoTen = shipperAccount.HoTen, SoDienThoai = "0900000002", Email = shipperAccount.Email, KhuVucPhuTrach = "Hoàng Mai" },
            new NhanVienGiaoHang { MaTaiKhoan = admin.MaTaiKhoan, HoTen = "Lê Văn Điều phối", SoDienThoai = "0900000003", Email = "dieuphoi@uneti.edu.vn", KhuVucPhuTrach = "Cầu Giấy" }
        };
        context.KhachHangs.Add(customer);
        context.NhanVienGiaoHangs.AddRange(employees);
        var vehicles = new[]
        {
            new PhuongTien { BienSo = "29A-12345", LoaiPhuongTien = "Xe máy", TaiTrongToiDa = 30 },
            new PhuongTien { BienSo = "29B-67890", LoaiPhuongTien = "Xe tải nhỏ", TaiTrongToiDa = 500 }
        };
        context.PhuongTiens.AddRange(vehicles);
        await context.SaveChangesAsync();

        context.DonGiaoHangs.AddRange(
            new DonGiaoHang { MaKhachHang = customer.MaKhachHang, MaLoaiHang = types[0].MaLoaiHang, MaKhuVuc = areas[0].MaKhuVuc, TenNguoiNhan = "Phạm Văn A", SoDienThoaiNguoiNhan = "0911111111", DiaChiNhan = "Hoàng Mai, Hà Nội", KhoiLuong = 2, PhiVanChuyen = 25000 },
            new DonGiaoHang { MaKhachHang = customer.MaKhachHang, MaLoaiHang = types[1].MaLoaiHang, MaKhuVuc = areas[1].MaKhuVuc, TenNguoiNhan = "Nguyễn Thị B", SoDienThoaiNguoiNhan = "0922222222", DiaChiNhan = "Hai Bà Trưng, Hà Nội", KhoiLuong = 1, PhiVanChuyen = 24000 });
        await context.SaveChangesAsync();
    }
}
