// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 3 - Entity NhanVienGiaoHang.

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng NhanVienGiaoHang
 * - MaTaiKhoan: tài khoản vai trò GiaoHang để nhân viên đăng nhập xem công việc.
 * - MaKhuVucPhuTrach: khu vực nhân viên phụ trách (ưu tiên khi phân công).
 * - TrangThai: SanSang | DangGiaoHang | TamNghi | NgungHoatDong.
 *   DangGiaoHang do hệ thống tự cập nhật theo phân công (DonHangXuLy.CapNhatTrangThaiNguonLuc),
 *   người dùng không chọn tay trạng thái này.
 */
public class NhanVienGiaoHang
{
    [Key]
    [Display(Name = "Mã nhân viên")]
    public int MaNhanVien { get; set; }

    [Display(Name = "Tài khoản")]
    public int? MaTaiKhoan { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Display(Name = "Khu vực phụ trách")]
    public int? MaKhuVucPhuTrach { get; set; }
    public KhuVuc? KhuVucPhuTrach { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiNhanVien TrangThai { get; set; } = TrangThaiNhanVien.SanSang;

    public ICollection<PhanCongGiaoHang> PhanCongs { get; set; } = [];
}
