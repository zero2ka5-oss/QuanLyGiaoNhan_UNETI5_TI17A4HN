using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Form tạo / sửa đơn. Không có trường phí – phí do hệ thống tính.</summary>
public class DonGiaoHangFormVM
{
    public int MaDon { get; set; }

    /// <summary>Chỉ dùng khi điều phối tạo đơn thay khách; khách hàng tự tạo thì lấy từ Session.</summary>
    [Display(Name = "Khách hàng")]
    public int? MaKhachHang { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn loại hàng")]
    [Display(Name = "Loại hàng")]
    public int MaLoaiHang { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn khu vực giao")]
    [Display(Name = "Khu vực giao")]
    public int MaKhuVuc { get; set; }

    /// <summary>Bưu cục khách mang hàng tới gửi (bưu cục nhận suy ra từ khu vực giao).</summary>
    [Required(ErrorMessage = "Chọn bưu cục gửi hàng")]
    [Display(Name = "Bưu cục gửi")]
    public int? MaBuuCucGui { get; set; }

    [Display(Name = "Hình thức gửi")]
    public HinhThucGui HinhThucGui { get; set; } = HinhThucGui.GuiTaiBuuCuc;

    [StringLength(255)]
    [Display(Name = "Địa chỉ lấy hàng")]
    public string? DiaChiLayHang { get; set; }

    [Required(ErrorMessage = "Nhập tên người nhận"), StringLength(100)]
    [Display(Name = "Tên người nhận")]
    public string TenNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại người nhận")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại người nhận")]
    public string SoDienThoaiNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập địa chỉ nhận"), StringLength(255)]
    [Display(Name = "Địa chỉ nhận")]
    public string DiaChiNhan { get; set; } = "";

    [Range(0.01, 100_000, ErrorMessage = "Khối lượng từ 0,01 đến 100.000 kg")]
    [Display(Name = "Khối lượng (kg)")]
    public decimal KhoiLuong { get; set; }

    [Required(ErrorMessage = "Chọn ngày giao dự kiến")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày giao dự kiến")]
    public DateTime NgayGiaoDuKien { get; set; } = DateTime.Today.AddDays(2);

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Range(typeof(decimal), "0", "50000000", ErrorMessage = "Tiền thu hộ từ 0 đến 50.000.000 đ")]
    [Display(Name = "Tiền thu hộ (COD)")]
    public decimal TienThuHo { get; set; }

    [Display(Name = "Người trả phí ship")]
    public NguoiTraPhi NguoiTraPhi { get; set; } = NguoiTraPhi.NguoiGui;

    public static DonGiaoHangFormVM TuDon(DonGiaoHang don) => new()
    {
        MaDon = don.MaDon, MaKhachHang = don.MaKhachHang, MaLoaiHang = don.MaLoaiHang, MaKhuVuc = don.MaKhuVuc, MaBuuCucGui = don.MaBuuCucGui,
        HinhThucGui = don.HinhThucGui, DiaChiLayHang = don.DiaChiLayHang,
        TenNguoiNhan = don.TenNguoiNhan, SoDienThoaiNguoiNhan = don.SoDienThoaiNguoiNhan, DiaChiNhan = don.DiaChiNhan,
        KhoiLuong = don.KhoiLuong, NgayGiaoDuKien = don.NgayGiaoDuKien, GhiChu = don.GhiChu,
        TienThuHo = don.TienThuHo, NguoiTraPhi = don.NguoiTraPhi
    };
}

/// <summary>Các thành phần phí vận chuyển.</summary>
public record KetQuaTinhPhi(decimal PhiCoBan, decimal KhoiLuongVuot, decimal PhuPhiKhoiLuong,
                            decimal HeSoPhuThu, decimal PhuPhiLoaiHang, decimal Tong);

/// <summary>Tham số tìm kiếm + lọc + sắp xếp + phân trang của danh sách đơn.</summary>
public class BoLocDonHang
{
    public string? TuKhoa { get; set; }
    public TrangThaiDon? TrangThai { get; set; }
    public int? MaLoaiHang { get; set; }
    public int? MaKhuVuc { get; set; }
    /// <summary>Đơn liên quan tới bưu cục (gửi / nhận / đang giữ).</summary>
    public int? MaBuuCuc { get; set; }
    /// <summary>"tao" = lọc theo ngày tạo, "giao" = theo ngày giao dự kiến.</summary>
    public string LoaiNgay { get; set; } = "tao";
    [DataType(DataType.Date)] public DateTime? TuNgay { get; set; }
    [DataType(DataType.Date)] public DateTime? DenNgay { get; set; }
    /// <summary>ngay_moi | ngay_cu | kl_tang | kl_giam | phi_tang | phi_giam | ten_az | ten_za</summary>
    public string SapXep { get; set; } = "ngay_moi";
    public int Trang { get; set; } = 1;
}

public class DanhSachDonVM
{
    public BoLocDonHang BoLoc { get; set; } = new();
    public DanhSachTrang<DonGiaoHang> KetQua { get; set; } = new();
    public Dictionary<TrangThaiDon, int> SoDonTheoTrangThai { get; set; } = [];
    public bool LaKhachHang { get; set; }
}

public class ChiTietDonVM
{
    public DonGiaoHang Don { get; set; } = null!;
    public PhanCongGiaoHang? PhanCongHienTai { get; set; }
    public List<PhanCongGiaoHang> DsPhanCong { get; set; } = [];
    public List<LichSuGiaoNhan> DsLichSu { get; set; } = [];
    public bool CoTheSua { get; set; }
    public bool CoTheHuy { get; set; }
    public bool CoTheHoanTat { get; set; }
    public bool CoThePhanCong { get; set; }
    public bool CoTheDoiPhanCong { get; set; }
    public bool LaKhachHang { get; set; }
    public ViTriDon? ViTri { get; set; }
}

/// <summary>Vị trí hiện tại của đơn (Loai: buucuc | shipper | dagiao | khach | loi | khac).</summary>
public record ViTriDon(string Loai, string TieuDe, string MoTa, double? ViDo, double? KinhDo, DateTime? CapNhatLuc, string? PhuongTien);
