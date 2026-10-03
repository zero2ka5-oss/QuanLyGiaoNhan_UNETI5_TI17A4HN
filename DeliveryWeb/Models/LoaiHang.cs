// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Entity LoaiHang (loại hàng và hệ số phụ thu).

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng LoaiHang
 * - TenLoaiHang bắt buộc, không trùng.
 * - HeSoPhuThu >= 0: phụ phí loại hàng = (phí cơ bản + phụ phí khối lượng) × HeSoPhuThu (xem TinhPhiXuLy).
 * - Loại hàng ngừng hoạt động không được chọn cho đơn mới.
 */
public class LoaiHang
{
    [Key]
    [Display(Name = "Mã loại hàng")]
    public int MaLoaiHang { get; set; }

    [Required(ErrorMessage = "Nhập tên loại hàng")]
    [StringLength(100)]
    [Display(Name = "Tên loại hàng")]
    public string TenLoaiHang { get; set; } = "";

    [Range(0, 10, ErrorMessage = "Hệ số phụ thu từ 0 đến 10")]
    [Display(Name = "Hệ số phụ thu")]
    public decimal HeSoPhuThu { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = [];
}
