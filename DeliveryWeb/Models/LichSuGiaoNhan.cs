// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Entity LichSuGiaoNhan (truy vết mọi thay đổi trạng thái đơn).

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng LichSuGiaoNhan
 * - Ghi mỗi thay đổi trạng thái quan trọng của đơn: tạo, phân công, đổi phân công, nhận hàng,
 *   bắt đầu giao, giao thành công/không thành công (kèm lý do), hoàn tất, hủy.
 * - Không bao giờ xóa – kể cả khi đổi nhân viên, giao lại hay hoàn tất đơn.
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
}
