// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Đọc/ghi Session đăng nhập (mã tài khoản, họ tên, vai trò).

using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * MoRongPhien — extension cho ISession
 * Sau khi DangNhapController.Index (POST) xác thực thành công gọi GhiPhien(...).
 * Các Controller/View lấy thông tin qua HttpContext.Session.MaTaiKhoan(), .VaiTro(), .MaKhachHang()...
 * Không lưu mật khẩu trong Session. Đăng xuất gọi Session.Clear().
 */
public static class MoRongPhien
{
    private const string KhoaMaTaiKhoan = "TK.MaTaiKhoan";
    private const string KhoaHoTen = "TK.HoTen";
    private const string KhoaVaiTro = "TK.VaiTro";
    private const string KhoaMaKhachHang = "TK.MaKhachHang";
    private const string KhoaMaNhanVien = "TK.MaNhanVien";
    private const string KhoaAnhDaiDien = "TK.AnhDaiDien";

    public static void GhiPhien(this ISession phien, TaiKhoan tk, int? maKhachHang, int? maNhanVien)
    {
        phien.SetInt32(KhoaMaTaiKhoan, tk.MaTaiKhoan);
        phien.SetString(KhoaHoTen, tk.HoTen);
        phien.SetString(KhoaVaiTro, tk.VaiTro);
        if (maKhachHang.HasValue) phien.SetInt32(KhoaMaKhachHang, maKhachHang.Value); else phien.Remove(KhoaMaKhachHang);
        if (maNhanVien.HasValue) phien.SetInt32(KhoaMaNhanVien, maNhanVien.Value); else phien.Remove(KhoaMaNhanVien);
        phien.GhiAnhDaiDien(tk.AnhDaiDien);
    }

    /// <summary>Cập nhật ảnh đại diện trong Session ngay sau khi người dùng đổi / xóa ảnh.</summary>
    public static void GhiAnhDaiDien(this ISession phien, string? anh)
    {
        if (string.IsNullOrEmpty(anh)) phien.Remove(KhoaAnhDaiDien); else phien.SetString(KhoaAnhDaiDien, anh);
    }

    public static bool DaDangNhap(this ISession phien) => phien.GetInt32(KhoaMaTaiKhoan).HasValue;
    public static int? MaTaiKhoan(this ISession phien) => phien.GetInt32(KhoaMaTaiKhoan);
    public static string HoTen(this ISession phien) => phien.GetString(KhoaHoTen) ?? "";
    public static string? VaiTro(this ISession phien) => phien.GetString(KhoaVaiTro);
    public static int? MaKhachHang(this ISession phien) => phien.GetInt32(KhoaMaKhachHang);
    public static int? MaNhanVien(this ISession phien) => phien.GetInt32(KhoaMaNhanVien);
    public static string? AnhDaiDien(this ISession phien) => phien.GetString(KhoaAnhDaiDien);

    public static bool LaQuanLy(this ISession phien) => phien.VaiTro() is VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.DieuPhoi;
    public static bool LaQuanTri(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.QuanTri;
    public static bool LaKhachHang(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.KhachHang;
    public static bool LaNhanVienGiaoHang(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.GiaoHang;

    /// <summary>Chuỗi ghi vào LichSuGiaoNhan.NguoiThucHien, VD: "Trần Thu Hà (Điều phối)".</summary>
    public static string NguoiThucHien(this ISession phien) => $"{phien.HoTen()} ({HienThi.TenVaiTroNgan(phien.VaiTro())})";
}
