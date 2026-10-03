// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Entity PhanCongGiaoHang (lần phân công nhân viên + phương tiện cho đơn).

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng PhanCongGiaoHang
 * - Mỗi lần phân công / đổi người / giao lại là MỘT bản ghi mới – không sửa đè để giữ lịch sử.
 * - "Đang hiệu lực" = TrangThai thuộc { DaPhanCong, DaNhanHang, DangGiao }; một đơn chỉ có tối đa 1 bản ghi hiệu lực.
 * - Khi kết thúc: TrangThai = KetThuc (có KetQua) hoặc DaThayDoi (bị thay bằng phân công khác), ghi NgayKetThuc.
 * - LyDoThatBai + GhiChu bắt buộc khi KetQua = GiaoKhongThanhCong.
 */
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
