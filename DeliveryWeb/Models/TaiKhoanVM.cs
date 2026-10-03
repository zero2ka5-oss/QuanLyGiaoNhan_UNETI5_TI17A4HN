// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - ViewModel đăng nhập, đăng ký, đổi mật khẩu, quản lý tài khoản.

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class DangNhapVM
{
    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";
}

/// <summary>Khách hàng tự đăng ký: tạo TaiKhoan (vai trò KhachHang) + KhachHang.</summary>
public class DangKyVM
{
    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu _ và .")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu")]
    public string NhapLaiMatKhau { get; set; } = "";

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

    [Required(ErrorMessage = "Nhập địa chỉ"), StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = "";
}

public class DoiMatKhauVM
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại"), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string MatKhauHienTai { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = "";

    [DataType(DataType.Password), Display(Name = "Nhập lại mật khẩu mới")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    public string NhapLaiMatKhau { get; set; } = "";
}

/// <summary>Quản trị tạo / sửa tài khoản.</summary>
public class TaiKhoanFormVM
{
    public int MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Chỉ dùng chữ không dấu, số, dấu _ và .")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string? MatKhau { get; set; }

    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string VaiTro { get; set; } = VaiTroNguoiDung.DieuPhoi;

    [Display(Name = "Trạng thái")]
    public TrangThaiTaiKhoan TrangThai { get; set; } = TrangThaiTaiKhoan.HoatDong;
}

/// <summary>Dữ liệu cho partial _DoiAnhDaiDien: ảnh hiện tại, họ tên (chữ cái đầu khi chưa có ảnh), lớp màu thêm.</summary>
public record AnhDaiDienVM(string? Anh, string HoTen, string Lop = "");
