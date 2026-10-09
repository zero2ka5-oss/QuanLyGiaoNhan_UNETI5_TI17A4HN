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
    private const string KhoaMaBuuCuc = "TK.MaBuuCuc";

    public static void GhiPhien(this ISession phien, TaiKhoan tk, int? maKhachHang, int? maNhanVien)
    {
        phien.SetInt32(KhoaMaTaiKhoan, tk.MaTaiKhoan);
        phien.SetString(KhoaHoTen, tk.HoTen);
        phien.SetString(KhoaVaiTro, tk.VaiTro);
        if (maKhachHang.HasValue) phien.SetInt32(KhoaMaKhachHang, maKhachHang.Value); else phien.Remove(KhoaMaKhachHang);
        if (maNhanVien.HasValue) phien.SetInt32(KhoaMaNhanVien, maNhanVien.Value); else phien.Remove(KhoaMaNhanVien);
        if ((tk.VaiTro is VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc") && tk.MaBuuCuc.HasValue) phien.SetInt32(KhoaMaBuuCuc, tk.MaBuuCuc.Value); else phien.Remove(KhoaMaBuuCuc);
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
    /// <summary>Bưu cục của nhân viên; null với các vai trò khác hoặc nhân viên điều hành chung.</summary>
    public static int? MaBuuCuc(this ISession phien) => phien.GetInt32(KhoaMaBuuCuc);

    public static bool LaQuanTri(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.QuanTri;
    public static bool LaKhachHang(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.KhachHang;
    public static bool LaShipper(this ISession phien) => phien.VaiTro() == VaiTroNguoiDung.GiaoHang;
    public static bool LaNhanVien(this ISession phien) => phien.VaiTro() is VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc";
    public static bool LaNhanVienBuuCuc(this ISession phien) => phien.LaNhanVien();

    /// <summary>Chuỗi ghi vào LichSuGiaoNhan.NguoiThucHien, VD: "Trần Thu Hà (Điều phối)".</summary>
    /// <summary>"Họ tên (Vai trò)" ghi vào lịch sử / phân công. Họ tên cho phép tới 100 ký tự nhưng cột NguoiThucHien chỉ 100 ký tự
    /// và còn ghép thêm vai trò, mã giao dịch → rút gọn phần tên còn 70 ký tự.</summary>
    public static string NguoiThucHien(this ISession phien) => $"{DinhDang.Cat(phien.HoTen(), 70)} ({HienThi.TenVaiTroNgan(phien.VaiTro())})";

    /// <summary>
    /// Người thao tác cho DonHangXuLy: tên ghi tracking event, bưu cục (nhân viên bưu cục bị giới hạn theo bưu cục của mình),
    /// toạ độ trình duyệt gửi kèm khi quét (bỏ qua nếu không hợp lệ).
    /// </summary>
    public static ThaoTac ThaoTac(this ISession phien, double? viDo = null, double? kinhDo = null)
    {
        bool hopLe = viDo is >= -90 and <= 90 && kinhDo is >= -180 and <= 180;
        return new ThaoTac(phien.NguoiThucHien(), phien.MaBuuCuc(), hopLe ? viDo : null, hopLe ? kinhDo : null);
    }
}
