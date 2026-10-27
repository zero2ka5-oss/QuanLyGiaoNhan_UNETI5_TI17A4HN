using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class ChuyenXeIndexVM
{
    public BuuCuc BuuCucHienTai { get; set; } = null!;
    public List<BuuCuc> DsBuuCuc { get; set; } = [];
    public List<NhomDonTheoDichDenVM> DonChoTheoDichDen { get; set; } = [];
    public List<NhanVienGiaoHang> XeLienTinhDangDau { get; set; } = [];
    public List<ChuyenXe> DsChuyenXe { get; set; } = [];
    public string Tab { get; set; } = "dangxep";
    public int SoDangXep { get; set; }
    public int SoSapDen { get; set; }
    public int SoLichSu { get; set; }

    /// <summary>Bưu cục hiện tại có phải là 1 trong 3 Hub trung tâm (Bắc Ninh, Đà Nẵng, TP.HCM) không</summary>
    public bool LaHubTrungTam { get; set; }
    /// <summary>Tên Hub vùng phụ trách bưu cục này (Ví dụ: Hub Miền Bắc (Bắc Ninh))</summary>
    public string TenHubVung { get; set; } = "";
    public BuuCuc? HubVung { get; set; }
    public string TenMien { get; set; } = "";
}

public class NhomDonTheoDichDenVM
{
    public int MaBuuCucNhan { get; set; }
    public string TenBuuCucNhan { get; set; } = "";
    public int SoLuongDon { get; set; }
    public decimal TongKhoiLuong { get; set; }
    public List<DonGiaoHang> DsDonHang { get; set; } = [];

    /// <summary>Đánh dấu tuyến gom hàng về Hub trung tâm vùng</summary>
    public bool LaTuyenVeHub { get; set; }
    public string MoTaTuyen { get; set; } = "";
}

public class TaoChuyenXeVM
{
    [Required]
    public int MaBuuCucGui { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn bưu cục đến")]
    public int MaBuuCucNhan { get; set; }

    [Display(Name = "Trạm dừng trung gian (Tùy chọn)")]
    public int? MaBuuCucTrungGian { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tài xế / shipper liên tỉnh")]
    public int MaNhanVien { get; set; }

    public int? MaPhuongTien { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    public List<int> DsMaDon { get; set; } = [];
}
