using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng TaiKhoan
 * - TenDangNhap bắt buộc, không trùng (unique index cấu hình trong QuanLyGiaoNhanDbContext).
 * - MatKhau lưu dạng đã băm (MatKhauHelper) – không lưu mật khẩu gốc.
 * - VaiTro: QuanTri | NhanVien | GiaoHang | KhachHang (VaiTroNguoiDung).
 * - TrangThai = BiKhoa thì không được đăng nhập, Session cũ bị hủy ở YeuCauVaiTroAttribute.
 * - Quan hệ: 1 – 0..1 KhachHang, 1 – 0..1 NhanVienGiaoHang.
 */
public class TaiKhoan
{
    [Key]
    [Display(Name = "Mã tài khoản")]
    public int MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu _ và .")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [StringLength(200)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(20)]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = VaiTroNguoiDung.KhachHang;

    [Display(Name = "Trạng thái")]
    public TrangThaiTaiKhoan TrangThai { get; set; } = TrangThaiTaiKhoan.HoatDong;

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    /// <summary>Đường dẫn ảnh đại diện (/uploads/anh-dai-dien/...); null = hiện chữ cái đầu của họ tên.</summary>
    [StringLength(300)]
    [Display(Name = "Ảnh đại diện")]
    public string? AnhDaiDien { get; set; }

    /// <summary>true khi mật khẩu do Quản trị đặt / đặt lại → bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp (YeuCauVaiTroAttribute).</summary>
    [Display(Name = "Phải đổi mật khẩu")]
    public bool PhaiDoiMatKhau { get; set; }

    /// <summary>Bưu cục làm việc của tài khoản Nhân viên (VaiTro = NhanVien).</summary>
    [Display(Name = "Bưu cục")]
    public int? MaBuuCuc { get; set; }
    public BuuCuc? BuuCuc { get; set; }

    public KhachHang? KhachHang { get; set; }
    public NhanVienGiaoHang? NhanVienGiaoHang { get; set; }
}
