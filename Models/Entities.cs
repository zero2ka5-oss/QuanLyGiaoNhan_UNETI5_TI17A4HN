using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

public class TaiKhoan
{
    [Key] public int MaTaiKhoan { get; set; }
    [Required, StringLength(50)] [Display(Name = "Tên đăng nhập")] public string TenDangNhap { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 4)] [DataType(DataType.Password)] [Display(Name = "Mật khẩu")] public string MatKhau { get; set; } = string.Empty;
    [Required, StringLength(100)] [Display(Name = "Họ tên")] public string HoTen { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(50)] public string VaiTro { get; set; } = "Khách hàng";
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Hoạt động";
    public KhachHang? KhachHang { get; set; }
    public NhanVienGiaoHang? NhanVienGiaoHang { get; set; }
}

public class KhachHang
{
    [Key] public int MaKhachHang { get; set; }
    [Display(Name = "Tài khoản")] public int MaTaiKhoan { get; set; }
    [Required, StringLength(100)] [Display(Name = "Họ tên")] public string HoTen { get; set; } = string.Empty;
    [Required, Phone, StringLength(20)] [Display(Name = "Số điện thoại")] public string SoDienThoai { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(250)] public string DiaChi { get; set; } = string.Empty;
    [DataType(DataType.Date)] public DateTime NgayDangKy { get; set; } = DateTime.Today;
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Hoạt động";
    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = new List<DonGiaoHang>();
}

public class LoaiHang
{
    [Key] public int MaLoaiHang { get; set; }
    [Required, StringLength(100)] [Display(Name = "Tên loại hàng")] public string TenLoaiHang { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "999999999")] [Display(Name = "Hệ số phụ thu")] public decimal HeSoPhuThu { get; set; }
    [StringLength(500)] public string? MoTa { get; set; }
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Hoạt động";
    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = new List<DonGiaoHang>();
}

public class KhuVuc
{
    [Key] public int MaKhuVuc { get; set; }
    [Required, StringLength(100)] [Display(Name = "Tên khu vực")] public string TenKhuVuc { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "999999999")] [Display(Name = "Phí cơ bản")] public decimal PhiCoBan { get; set; }
    [StringLength(500)] public string? MoTa { get; set; }
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Hoạt động";
    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = new List<DonGiaoHang>();
}

public class DonGiaoHang : IValidatableObject
{
    [Key] public int MaDon { get; set; }
    public int MaKhachHang { get; set; }
    public int MaLoaiHang { get; set; }
    public int MaKhuVuc { get; set; }
    [Required, StringLength(100)] [Display(Name = "Người nhận")] public string TenNguoiNhan { get; set; } = string.Empty;
    [Required, Phone, StringLength(20)] [Display(Name = "SĐT người nhận")] public string SoDienThoaiNguoiNhan { get; set; } = string.Empty;
    [Required, StringLength(250)] [Display(Name = "Địa chỉ nhận")] public string DiaChiNhan { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999")] [Display(Name = "Khối lượng")] public decimal KhoiLuong { get; set; }
    [DataType(DataType.DateTime)] [Display(Name = "Ngày tạo")] public DateTime NgayTao { get; set; } = DateTime.Now;
    [DataType(DataType.DateTime)] [Display(Name = "Ngày giao dự kiến")] public DateTime NgayGiaoDuKien { get; set; } = DateTime.Now.AddDays(1);
    [Range(typeof(decimal), "0", "999999999")] [Display(Name = "Phí vận chuyển")] public decimal PhiVanChuyen { get; set; }
    [Required, StringLength(50)] public string TrangThai { get; set; } = "Chờ phân công";
    [StringLength(500)] public string? GhiChu { get; set; }
    public KhachHang? KhachHang { get; set; }
    public LoaiHang? LoaiHang { get; set; }
    public KhuVuc? KhuVuc { get; set; }
    public ICollection<PhanCongGiaoHang> PhanCongGiaoHangs { get; set; } = new List<PhanCongGiaoHang>();
    public ICollection<LichSuGiaoNhan> LichSuGiaoNhans { get; set; } = new List<LichSuGiaoNhan>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NgayGiaoDuKien < NgayTao)
            yield return new ValidationResult("Ngày giao dự kiến không được trước ngày tạo.", new[] { nameof(NgayGiaoDuKien) });
    }
}

public class NhanVienGiaoHang
{
    [Key] public int MaNhanVien { get; set; }
    public int MaTaiKhoan { get; set; }
    [Required, StringLength(100)] [Display(Name = "Họ tên")] public string HoTen { get; set; } = string.Empty;
    [Required, Phone, StringLength(20)] [Display(Name = "Số điện thoại")] public string SoDienThoai { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100)] [Display(Name = "Khu vực phụ trách")] public string KhuVucPhuTrach { get; set; } = string.Empty;
    [Required, StringLength(40)] public string TrangThai { get; set; } = "Sẵn sàng";
    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<PhanCongGiaoHang> PhanCongGiaoHangs { get; set; } = new List<PhanCongGiaoHang>();
}

public class PhuongTien
{
    [Key] public int MaPhuongTien { get; set; }
    [Required, StringLength(20)] [Display(Name = "Biển số")] public string BienSo { get; set; } = string.Empty;
    [Required, StringLength(50)] [Display(Name = "Loại phương tiện")] public string LoaiPhuongTien { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999999")] [Display(Name = "Tải trọng tối đa")] public decimal TaiTrongToiDa { get; set; }
    [Required, StringLength(40)] public string TrangThai { get; set; } = "Sẵn sàng";
    [StringLength(500)] public string? GhiChu { get; set; }
    public ICollection<PhanCongGiaoHang> PhanCongGiaoHangs { get; set; } = new List<PhanCongGiaoHang>();
}

public class PhanCongGiaoHang
{
    [Key] public int MaPhanCong { get; set; }
    public int MaDon { get; set; }
    public int MaNhanVien { get; set; }
    public int MaPhuongTien { get; set; }
    [DataType(DataType.DateTime)] public DateTime NgayPhanCong { get; set; } = DateTime.Now;
    [DataType(DataType.DateTime)] public DateTime? NgayNhanHang { get; set; }
    [DataType(DataType.DateTime)] public DateTime? NgayBatDauGiao { get; set; }
    [DataType(DataType.DateTime)] public DateTime? NgayKetThuc { get; set; }
    [Required, StringLength(50)] public string TrangThai { get; set; } = "Đã phân công";
    [StringLength(500)] public string? GhiChu { get; set; }
    public DonGiaoHang? DonGiaoHang { get; set; }
    public NhanVienGiaoHang? NhanVienGiaoHang { get; set; }
    public PhuongTien? PhuongTien { get; set; }
}

public class LichSuGiaoNhan
{
    [Key] public int MaLichSu { get; set; }
    public int MaDon { get; set; }
    [DataType(DataType.DateTime)] public DateTime ThoiGian { get; set; } = DateTime.Now;
    [Required, StringLength(50)] public string TrangThaiCu { get; set; } = string.Empty;
    [Required, StringLength(50)] public string TrangThaiMoi { get; set; } = string.Empty;
    [Required, StringLength(500)] public string NoiDung { get; set; } = string.Empty;
    [Required, StringLength(100)] public string NguoiThucHien { get; set; } = string.Empty;
    public DonGiaoHang? DonGiaoHang { get; set; }
}
