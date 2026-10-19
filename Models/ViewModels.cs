using System.ComponentModel.DataAnnotations;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
public class LoginViewModel
{
    [Required, Display(Name = "Tên đăng nhập")] public string TenDangNhap {
        get;
        set;
    }
    = string.Empty;
    [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string MatKhau {
        get;
        set;
    }
    = string.Empty;
    public string? ReturnUrl {
        get;
        set;
    }
}
public class DashboardViewModel
{
    public int TongDonHang {
        get;
        set;
    }
    public int DonChoPhanCong {
        get;
        set;
    }
    public int DonDangGiao {
        get;
        set;
    }
    public int DonHoanTat {
        get;
        set;
    }
    public decimal TongPhiVanChuyen {
        get;
        set;
    }
}
