using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

public class LoginViewModel
{
    [Required, Display(Name = "Tên đăng nhập")] public string TenDangNhap { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string MatKhau { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(50), Display(Name = "Tên đăng nhập")] public string TenDangNhap { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 4), DataType(DataType.Password), Display(Name = "Mật khẩu")] public string MatKhau { get; set; } = string.Empty;
    [Required, DataType(DataType.Password), Compare(nameof(MatKhau)), Display(Name = "Nhập lại mật khẩu")] public string XacNhanMatKhau { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Họ tên")] public string HoTen { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, Phone, StringLength(20), Display(Name = "Số điện thoại")] public string SoDienThoai { get; set; } = string.Empty;
    [Required, StringLength(250), Display(Name = "Địa chỉ")] public string DiaChi { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public int TongDonHang { get; set; }
    public int DonChoPhanCong { get; set; }
    public int DonDangGiao { get; set; }
    public int DonHoanTat { get; set; }
    public decimal TongPhiVanChuyen { get; set; }
}

public class CustomerDashboardViewModel
{
    public string HoTen { get; set; } = string.Empty;
    public int TongDonHang { get; set; }
    public int DonChoPhanCong { get; set; }
    public int DonDangGiao { get; set; }
    public int DonHoanTat { get; set; }
    public IReadOnlyList<DonGiaoHang> DonGanDay { get; set; } = [];
}

public class ShipperDashboardViewModel
{
    public string HoTen { get; set; } = string.Empty;
    public int TongPhanCong { get; set; }
    public int DonDangGiao { get; set; }
    public int DonHoanTat { get; set; }
    public IReadOnlyList<PhanCongGiaoHang> PhanCongGanDay { get; set; } = [];
}

public class UpdateDeliveryStatusViewModel
{
    public int MaPhanCong { get; set; }
    [Required, StringLength(50)] public string TrangThai { get; set; } = string.Empty;
    [StringLength(500), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }
}
