using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng PhanCongGiaoHang
 * - Mỗi chặng (liên tỉnh: bưu cục gửi → bưu cục nhận; khu vực: bưu cục nhận → người nhận) / mỗi lần giao lại / đổi người
 *   là MỘT bản ghi mới – không sửa đè để giữ lịch sử. MaPhuongTien = phương tiện được gán cho shipper tại lúc phân công.
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

    [Display(Name = "Loại chặng")]
    public LoaiShipper LoaiChang { get; set; } = LoaiShipper.KhuVuc;

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

    /// <summary>
    /// Hoa hồng shipper hưởng khi giao thành công = phí vận chuyển × DonHangXuLy.TiLeHoaHongShipper.
    /// Chốt số tiền tại lúc giao (không tính lại) để đổi tỷ lệ sau này không làm lệch thu nhập / ví cũ. 0 khi chưa giao thành công.
    /// </summary>
    [Display(Name = "Thu nhập shipper")]
    public decimal ThuNhapShipper { get; set; }

    public bool DangHieuLuc => TrangThaiHieuLuc.Contains(TrangThai);
}
