// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Entity GiaoDichVi (giao dịch ví của nhân viên giao hàng: nộp tiền thu hộ, rút tiền ship).

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng GiaoDichVi – ví của shipper trên app giao hàng
 * - NopTienThuHo: shipper nộp lại tiền đã thu của người nhận (COD + phí ship người nhận trả) về công ty.
 *   Mỗi lần nộp gom các đơn Giao thành công chưa đối soát (DonGiaoHang.MaGiaoDichNop);
 *   chủ quản xác nhận → các đơn đó được đối soát & hoàn tất; từ chối → đơn trở lại "đang giữ tiền".
 * - RutTien: shipper rút thu nhập (tiền ship các đơn giao thành công) về tài khoản ngân hàng;
 *   chủ quản xác nhận đã chuyển khoản hoặc từ chối (tiền trở lại số dư).
 * - Số dư không lưu cứng mà tính từ đơn đã giao và các giao dịch → luôn khớp dữ liệu.
 */
public class GiaoDichVi
{
    [Key]
    [Display(Name = "Mã giao dịch")]
    public int MaGiaoDich { get; set; }

    [Display(Name = "Nhân viên")]
    public int MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Loại giao dịch")]
    public LoaiGiaoDich Loai { get; set; }

    [Range(0, 1_000_000_000)]
    [Display(Name = "Số tiền")]
    public decimal SoTien { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiGiaoDich TrangThai { get; set; } = TrangThaiGiaoDich.ChoXacNhan;

    [Required, StringLength(50)]
    [Display(Name = "Phương thức")]
    public string PhuongThuc { get; set; } = "";

    /// <summary>Nộp tiền: mã tham chiếu chuyển khoản / biên nhận. Rút tiền: ngân hàng – số tài khoản – chủ tài khoản.</summary>
    [StringLength(255)]
    [Display(Name = "Thông tin thanh toán")]
    public string? ThongTinThanhToan { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Display(Name = "Ngày xử lý")]
    public DateTime? NgayXuLy { get; set; }

    [StringLength(150)]
    [Display(Name = "Người xử lý")]
    public string? NguoiXuLy { get; set; }

    [StringLength(255)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    /// <summary>Các đơn được đối soát trong lần nộp tiền này.</summary>
    public ICollection<DonGiaoHang> DonNop { get; set; } = [];

    [NotMapped]
    public string MaHienThi => $"GD{MaGiaoDich:D6}";
}
