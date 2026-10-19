using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class KhachHangFormVM
{
    public int MaKhachHang { get; set; }

    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Nhập địa chỉ"), StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = "";

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    // Tạo kèm tài khoản đăng nhập (không bắt buộc)
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu _ và .")]
    [Display(Name = "Tên đăng nhập")]
    public string? TenDangNhap { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string? MatKhau { get; set; }

    public bool CoTaiKhoan { get; set; }
}

public class NhanVienFormVM
{
    public int MaNhanVien { get; set; }

    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Display(Name = "Loại shipper")]
    public LoaiShipper LoaiShipper { get; set; } = LoaiShipper.KhuVuc;

    /// <summary>Bưu cục: liên tỉnh xuất phát từ bưu cục này; khu vực nhận hàng ở bưu cục này để đi giao.</summary>
    [Display(Name = "Bưu cục")]
    public int? MaBuuCuc { get; set; }

    /// <summary>Khu vực giao (chỉ shipper khu vực) – phải là khu vực do bưu cục trên phát hàng.</summary>
    [Display(Name = "Khu vực phụ trách")]
    public int? MaKhuVucPhuTrach { get; set; }

    /// <summary>Phương tiện gán cố định (1 shipper ↔ 1 phương tiện); đổi phương tiện ghi lịch sử.</summary>
    [Display(Name = "Phương tiện")]
    public int? MaPhuongTien { get; set; }

    [StringLength(255)]
    [Display(Name = "Lý do đổi phương tiện")]
    public string? LyDoDoiXe { get; set; }

    /// <summary>Chỉ cho chọn SanSang / TamNghi / NgungHoatDong; "Đang giao hàng" do hệ thống tự cập nhật.</summary>
    [Display(Name = "Trạng thái")]
    public TrangThaiNhanVien TrangThai { get; set; } = TrangThaiNhanVien.SanSang;

    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu _ và .")]
    [Display(Name = "Tên đăng nhập")]
    public string? TenDangNhap { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu")]
    public string? MatKhau { get; set; }

    public bool CoTaiKhoan { get; set; }
}
