
using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class PhanCongGiaoHang
{
    public static readonly TrangThaiPhanCong[] TrangThaiHieuLuc =
        [TrangThaiPhanCong.DaPhanCong, TrangThaiPhanCong.DaNhanHang, TrangThaiPhanCong.DangGiao];

    [Key]
    [Display(Name = "Mã phân công")]
    public int MaPhanCong { get; set; }

    [Display(Name = "Đơn hàng")]
    public int MaDon { get; set; }
    public DonGiaoHang? DonGiaoHang { get; set; }

    [Display(Name = "Nhân viên")]
    public int MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Phương tiện")]
    public int MaPhuongTien { get; set; }
    public PhuongTien? PhuongTien { get; set; }

    [Display(Name = "Ngày phân công")] public DateTime NgayPhanCong { get; set; }
    [Display(Name = "Ngày nhận hàng")] public DateTime? NgayNhanHang { get; set; }
    [Display(Name = "Bắt đầu giao")] public DateTime? NgayBatDauGiao { get; set; }
    [Display(Name = "Ngày kết thúc")] public DateTime? NgayKetThuc { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiPhanCong TrangThai { get; set; } = TrangThaiPhanCong.DaPhanCong;

    [Display(Name = "Kết quả")]
    public KetQuaGiao? KetQua { get; set; }

    [Display(Name = "Lý do thất bại")]
    public LyDoThatBai? LyDoThatBai { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [StringLength(100)]
    [Display(Name = "Người phân công")]
    public string? NguoiPhanCong { get; set; }

    public bool DangHieuLuc => TrangThaiHieuLuc.Contains(TrangThai);
}
