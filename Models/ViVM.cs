using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Các con số của ví một shipper – tính từ đơn đã giao và giao dịch.</summary>
public class ViShipperVM
{
    public NhanVienGiaoHang NhanVien { get; set; } = null!;
    public decimal TienDangGiu { get; set; }
    public int SoDonDangGiu { get; set; }
    public decimal TienChoXacNhanNop { get; set; }
    public decimal ThuNhap { get; set; }
    public int SoDonThanhCong { get; set; }
    public decimal DaRut { get; set; }
    public decimal DangChoRut { get; set; }
    public decimal SoDu => ThuNhap - DaRut - DangChoRut;
    public List<GiaoDichVi> LichSu { get; set; } = [];
    public List<DonGiaoHang> DonDangGiu { get; set; } = [];
}

/// <summary>Lệnh nộp tiền thu hộ về công ty.</summary>
public class NopTienVM
{
    [Required(ErrorMessage = "Chọn phương thức nộp tiền")]
    [Display(Name = "Phương thức")]
    public string PhuongThuc { get; set; } = "Chuyển khoản ngân hàng";

    [StringLength(100)]
    [Display(Name = "Mã giao dịch / biên nhận")]
    public string? MaThamChieu { get; set; }

    public List<int>? MaDonChon { get; set; }
}

/// <summary>Yêu cầu rút tiền ship về tài khoản ngân hàng.</summary>
public class RutTienVM
{
    [Range(typeof(decimal), "1", "1000000000", ErrorMessage = "Nhập số tiền cần rút")]
    [Display(Name = "Số tiền rút")]
    public decimal SoTien { get; set; }

    [Required(ErrorMessage = "Chọn ngân hàng"), StringLength(50)]
    [Display(Name = "Ngân hàng")]
    public string NganHang { get; set; } = "";

    [Required(ErrorMessage = "Nhập số tài khoản")]
    [RegularExpression(@"^\d{6,20}$", ErrorMessage = "Số tài khoản gồm 6–20 chữ số")]
    [Display(Name = "Số tài khoản")]
    public string SoTaiKhoan { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên chủ tài khoản"), StringLength(100)]
    [Display(Name = "Chủ tài khoản")]
    public string ChuTaiKhoan { get; set; } = "";
}

/// <summary>Trang quản trị ví: số dư từng shipper + danh sách giao dịch (lọc, phân trang).</summary>
public class QuanTriViVM
{
    public List<ViShipperVM> DsVi { get; set; } = [];
    public DanhSachTrang<GiaoDichVi> GiaoDich { get; set; } = new();
    public LoaiGiaoDich? Loai { get; set; }
    public TrangThaiGiaoDich? TrangThai { get; set; }
    public int? MaNhanVien { get; set; }
    public int SoChoXacNhan { get; set; }
    public decimal TienNopChoXacNhan { get; set; }
    public decimal TienRutChoXacNhan { get; set; }
}
