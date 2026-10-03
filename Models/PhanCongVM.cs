// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - ViewModel phân công, đổi phân công, giao không thành công, công việc nhân viên.

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Form phân công / đổi phân công một đơn.</summary>
public class PhanCongFormVM
{
    public int MaDon { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn nhân viên giao hàng")]
    [Display(Name = "Nhân viên giao hàng")]
    public int MaNhanVien { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn phương tiện")]
    [Display(Name = "Phương tiện")]
    public int MaPhuongTien { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú / lý do")]
    public string? GhiChu { get; set; }

    // ----- Dữ liệu hiển thị -----
    public DonGiaoHang? Don { get; set; }
    public PhanCongGiaoHang? PhanCongHienTai { get; set; }
    public List<LuaChonNhanVien> DsNhanVien { get; set; } = [];
    public List<LuaChonPhuongTien> DsPhuongTien { get; set; } = [];
    public bool LaDoiPhanCong { get; set; }
}

public record LuaChonNhanVien(int MaNhanVien, string HoTen, string? KhuVuc, bool CungKhuVuc,
                              TrangThaiNhanVien TrangThai, int SoDonDangGiu, bool DuocChon, string? LyDoKhongChon);

public record LuaChonPhuongTien(int MaPhuongTien, string BienSo, string Loai, decimal TaiTrong, decimal DangChoKg,
                                string? NhanVienDangDung, TrangThaiPhuongTien TrangThai, bool DuocChon, string? LyDoKhongChon);

/// <summary>Nhân viên ghi nhận giao không thành công – bắt buộc lý do.</summary>
public class GiaoThatBaiVM
{
    public int MaPhanCong { get; set; }

    [Required(ErrorMessage = "Chọn lý do giao không thành công")]
    [Display(Name = "Lý do")]
    public LyDoThatBai? LyDo { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả chi tiết")]
    public string? ChiTiet { get; set; }

    public DonGiaoHang? Don { get; set; }
}

/// <summary>Màn hình "Công việc" của nhân viên giao hàng.</summary>
public class CongViecNhanVienVM
{
    public NhanVienGiaoHang NhanVien { get; set; } = null!;
    public List<PhanCongGiaoHang> CanNhanHang { get; set; } = [];
    public List<PhanCongGiaoHang> DaNhanHang { get; set; } = [];
    public List<PhanCongGiaoHang> DangGiao { get; set; } = [];
    public int GiaoThanhCongHomNay { get; set; }
    public int ThatBaiHomNay { get; set; }
    /// <summary>Tổng cước (phí vận chuyển) các đơn giao thành công hôm nay / trong tháng.</summary>
    public decimal TienHomNay { get; set; }
    public decimal TienThangNay { get; set; }
    /// <summary>Tổng tiền phải thu người nhận của các đơn đang giữ.</summary>
    public decimal TienCanThu { get; set; }
}

/// <summary>Hồ sơ nhân viên giao hàng: thông tin cá nhân, nguồn lực đang dùng và thống kê kết quả giao.</summary>
public class HoSoNhanVienVM
{
    public NhanVienGiaoHang NhanVien { get; set; } = null!;
    public string? TenDangNhap { get; set; }
    public PhuongTien? XeDangDung { get; set; }
    public int SoDonDangGiu { get; set; }
    public int TongThanhCong { get; set; }
    public int TongThatBai { get; set; }
    public double TiLeThanhCong { get; set; }
    public decimal TongTien { get; set; }
    public decimal TienThangNay { get; set; }
    public int DonThangNay { get; set; }
    /// <summary>Tiền đã thu của người nhận (đơn Giao thành công) chưa nộp / đối soát.</summary>
    public decimal TienDangGiu { get; set; }
    public int SoDonDangGiuTien { get; set; }
    public decimal TongKhoiLuong { get; set; }
    /// <summary>6 tháng gần nhất: Nhan = "MM/yyyy", GiaTri = số đơn thành công, GiaTriPhu = tổng cước.</summary>
    public List<DongThongKe> TheoThang { get; set; } = [];
    public CapNhatLienHeVM LienHe { get; set; } = new();
}

/// <summary>Nhân viên tự cập nhật thông tin liên hệ (họ tên, khu vực, trạng thái do điều phối quản lý).</summary>
public class CapNhatLienHeVM
{
    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";
}
