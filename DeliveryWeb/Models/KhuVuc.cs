// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Entity KhuVuc (khu vực giao và phí cơ bản).

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng KhuVuc
 * - TenKhuVuc bắt buộc, không trùng; PhiCoBan >= 0 (thành phần đầu tiên của phí vận chuyển).
 * - Khu vực ngừng hoạt động không nhận đơn mới.
 * - 1 khu vực – n DonGiaoHang; là khu vực phụ trách của nhiều NhanVienGiaoHang.
 */
public class KhuVuc
{
    [Key]
    [Display(Name = "Mã khu vực")]
    public int MaKhuVuc { get; set; }

    [Required(ErrorMessage = "Nhập tên khu vực")]
    [StringLength(100)]
    [Display(Name = "Tên khu vực")]
    public string TenKhuVuc { get; set; } = "";

    [Range(0, 100_000_000, ErrorMessage = "Phí cơ bản phải >= 0")]
    [Display(Name = "Phí cơ bản (đ)")]
    public decimal PhiCoBan { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = [];
    public ICollection<NhanVienGiaoHang> NhanViens { get; set; } = [];
}
