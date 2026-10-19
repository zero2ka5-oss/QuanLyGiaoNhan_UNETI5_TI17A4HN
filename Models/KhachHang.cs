using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng KhachHang
 * - MaTaiKhoan: liên kết 0..1 với TaiKhoan (khách tự đăng ký hoặc do điều phối tạo).
 * - NgayDangKy do hệ thống gán khi tạo; khách chỉ xem/sửa hồ sơ của chính mình (KhachHangHoSoController).
 * - 1 khách hàng – n DonGiaoHang.
 */
public class KhachHang
{
    [Key]
    [Display(Name = "Mã khách hàng")]
    public int MaKhachHang { get; set; }

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

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Nhập địa chỉ")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = "";

    [Display(Name = "Ngày đăng ký")]
    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "Số dư ví")]
    public decimal SoDuVi { get; set; } = 0;

    [StringLength(100)]
    [Display(Name = "Tên ngân hàng")]
    public string? TenNganHang { get; set; }

    [StringLength(50)]
    [Display(Name = "Số tài khoản ngân hàng")]
    public string? SoTaiKhoanNganHang { get; set; }

    [StringLength(100)]
    [Display(Name = "Chủ tài khoản ngân hàng")]
    public string? ChuTaiKhoanNganHang { get; set; }

    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = [];
    public ICollection<GiaoDichViKhachHang> GiaoDichVis { get; set; } = [];
}
