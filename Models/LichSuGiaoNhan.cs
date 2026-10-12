using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng LichSuGiaoNhan = Tracking Event
 * - Mỗi lần quét mã / bàn giao / đổi trạng thái của đơn tạo một bản ghi: loại sự kiện (TrangThaiMoi), bưu cục,
 *   shipper + phương tiện liên quan, toạ độ lúc quét, người thực hiện, ghi chú (NoiDung).
 * - Không bao giờ xóa – truy xuất được ai nhận / bàn giao, xe nào, lúc nào, ở đâu.
 */
public class LichSuGiaoNhan
{
    [Key]
    [Display(Name = "Mã lịch sử")]
    public int MaLichSu { get; set; }

    [Display(Name = "Đơn hàng")]
    public int MaDon { get; set; }
    public DonGiaoHang? DonGiaoHang { get; set; }

    [Display(Name = "Thời gian")]
    public DateTime ThoiGian { get; set; } = DateTime.Now;

    [Display(Name = "Trạng thái cũ")]
    public TrangThaiDon? TrangThaiCu { get; set; }

    [Display(Name = "Trạng thái mới")]
    public TrangThaiDon TrangThaiMoi { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Người thực hiện")]
    public string NguoiThucHien { get; set; } = "";

    [Display(Name = "Bưu cục")]
    public int? MaBuuCuc { get; set; }
    public BuuCuc? BuuCuc { get; set; }

    [Display(Name = "Shipper")]
    public int? MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Phương tiện")]
    public int? MaPhuongTien { get; set; }
    public PhuongTien? PhuongTien { get; set; }

    [Display(Name = "Vĩ độ")] public double? ViDo { get; set; }
    [Display(Name = "Kinh độ")] public double? KinhDo { get; set; }

    /// <summary>true = sự kiện tạo ra từ việc quét mã QR / barcode của đơn.</summary>
    [Display(Name = "Quét mã")]
    public bool QuetMa { get; set; }
}
