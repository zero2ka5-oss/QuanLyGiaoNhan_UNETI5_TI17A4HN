using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class ViKhachHangVM
{
    public KhachHang KhachHang { get; set; } = null!;
    public decimal SoDuVi { get; set; }
    public decimal TongCODDaNhan { get; set; }
    public decimal TongDaRut { get; set; }
    public decimal TienDangChoRut { get; set; }
    public decimal SoDuKhaDung => Math.Max(0, SoDuVi);
    public int SoDonCODHoanTat { get; set; }
    public List<GiaoDichViKhachHang> LichSuGiaoDich { get; set; } = [];
    public List<DonGiaoHang> DonCODChoDoiSoat { get; set; } = [];
}

public class RutTienKhachHangVM
{
    [Required(ErrorMessage = "Vui lòng nhập số tiền muốn rút")]
    [Range(typeof(decimal), "50000", "1000000000", ErrorMessage = "Số tiền rút tối thiểu là 50.000 đ")]
    [Display(Name = "Số tiền cần rút")]
    public decimal SoTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngân hàng nhận tiền")]
    [StringLength(100)]
    [Display(Name = "Tên ngân hàng")]
    public string TenNganHang { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
    [RegularExpression(@"^\d{6,25}$", ErrorMessage = "Số tài khoản từ 6 đến 25 chữ số")]
    [StringLength(50)]
    [Display(Name = "Số tài khoản")]
    public string SoTaiKhoan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
    [StringLength(100)]
    [Display(Name = "Chủ tài khoản (viết hoa không dấu)")]
    public string ChuTaiKhoan { get; set; } = "";

    [StringLength(255)]
    [Display(Name = "Ghi chú rút tiền")]
    public string? GhiChu { get; set; }
}

public class CapNhatNganHangVM
{
    [Required(ErrorMessage = "Vui lòng chọn ngân hàng")]
    [StringLength(100)]
    [Display(Name = "Tên ngân hàng")]
    public string TenNganHang { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
    [RegularExpression(@"^\d{6,25}$", ErrorMessage = "Số tài khoản từ 6 đến 25 chữ số")]
    [StringLength(50)]
    [Display(Name = "Số tài khoản")]
    public string SoTaiKhoan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
    [StringLength(100)]
    [Display(Name = "Chủ tài khoản (viết hoa không dấu)")]
    public string ChuTaiKhoan { get; set; } = "";
}

public class QuanTriViKhachHangVM
{
    public DanhSachTrang<GiaoDichViKhachHang> GiaoDich { get; set; } = new();
    public TrangThaiGiaoDich? TrangThai { get; set; }
    public LoaiGiaoDichKhach? Loai { get; set; }
    public string? TuKhoa { get; set; }
    public int Trang { get; set; } = 1;
    public int SoChoXacNhan { get; set; }
    public decimal TienChoXacNhan { get; set; }
    public int SoDaXacNhan { get; set; }
    public decimal TienDaXacNhan { get; set; }
    public int SoTuChoi { get; set; }
}

public class DuyetRutTienVM
{
    public int MaGiaoDich { get; set; }

    [StringLength(100)]
    [Display(Name = "Mã giao dịch ngân hàng / Ủy nhiệm chi")]
    public string? MaThamChieu { get; set; }

    [StringLength(255)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
}

public class TuChoiRutTienVM
{
    public int MaGiaoDich { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối")]
    [StringLength(255, ErrorMessage = "Lý do không quá 255 ký tự")]
    [Display(Name = "Lý do từ chối")]
    public string LyDo { get; set; } = "";
}
