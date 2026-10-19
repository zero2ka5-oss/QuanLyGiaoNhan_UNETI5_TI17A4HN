using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public enum TrangThaiChuyenXe : byte
{
    DangXepHang = 0,    // Đang gom hàng / xếp hàng lên xe tải
    DangVanChuyen = 1,  // Xe đã xuất bến, đang chạy liên tỉnh
    DaDenNoi = 2,       // Đã tới bưu cục đích và hoàn tất tiếp nhận
    DaHuy = 3,          // Hủy chuyến xe
    DangGheTram = 4     // Xe đang dừng tại trạm trung gian (dỡ & bốc thêm hàng)
}

public class ChuyenXe
{
    [Key]
    public int MaChuyen { get; set; }

    [Display(Name = "Bưu cục gửi")]
    public int MaBuuCucGui { get; set; }
    public BuuCuc? BuuCucGui { get; set; }

    [Display(Name = "Bưu cục nhận")]
    public int MaBuuCucNhan { get; set; }
    public BuuCuc? BuuCucNhan { get; set; }

    /// <summary>Bưu cục trạm dừng trung gian giữa đường (Ví dụ Hub Đà Nẵng trên tuyến Bắc Ninh - TP.HCM)</summary>
    [Display(Name = "Trạm dừng trung gian")]
    public int? MaBuuCucTrungGian { get; set; }
    public BuuCuc? BuuCucTrungGian { get; set; }

    /// <summary>Đã hoàn tất ghé trạm và dỡ/bốc hàng tại trạm trung gian</summary>
    [Display(Name = "Đã ghé trạm")]
    public bool DaGheTram { get; set; }

    [Display(Name = "Thời gian ghé trạm")]
    public DateTime? NgayGheTram { get; set; }

    [Display(Name = "Thời gian rời trạm")]
    public DateTime? NgayRoiTram { get; set; }

    [Display(Name = "Tài xế / Shipper liên tỉnh")]
    public int MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Phương tiện xe tải")]
    public int? MaPhuongTien { get; set; }
    public PhuongTien? PhuongTien { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiChuyenXe TrangThai { get; set; } = TrangThaiChuyenXe.DangXepHang;

    [Display(Name = "Thời gian tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Display(Name = "Thời gian xuất bến")]
    public DateTime? NgayXuatBen { get; set; }

    [Display(Name = "Thời gian đến nơi")]
    public DateTime? NgayDenNoi { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public ICollection<DonGiaoHang> DonHangs { get; set; } = [];

    [NotMapped]
    public string MaHienThi => $"CX{NgayTao:yyyyMMdd}{MaChuyen:D4}";

    [NotMapped]
    public bool CoGheTram => MaBuuCucTrungGian.HasValue;

    [NotMapped]
    public int TongSoDon => DonHangs.Count;

    [NotMapped]
    public decimal TongKhoiLuong => DonHangs.Sum(d => d.KhoiLuong);
}
