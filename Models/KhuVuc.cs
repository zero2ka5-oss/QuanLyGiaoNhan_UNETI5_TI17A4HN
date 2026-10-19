using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng KhuVuc
 * - TenKhuVuc bắt buộc, không trùng; PhiCoBan >= 0 (thành phần đầu tiên của phí vận chuyển).
 * - Khu vực ngừng hoạt động không nhận đơn mới.
 * - 1 khu vực – n DonGiaoHang; là khu vực phụ trách của nhiều NhanVienGiaoHang (shipper khu vực).
 * - MaBuuCuc: bưu cục phụ trách giao hàng cho khu vực = bưu cục nhận của các đơn giao tới khu vực này.
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

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Phí cơ bản phải >= 0")]
    [Display(Name = "Phí cơ bản (đ)")]
    public decimal PhiCoBan { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    [Display(Name = "Bưu cục phụ trách")]
    public int? MaBuuCuc { get; set; }
    public BuuCuc? BuuCuc { get; set; }

    public ICollection<DonGiaoHang> DonGiaoHangs { get; set; } = [];
    public ICollection<NhanVienGiaoHang> NhanViens { get; set; } = [];
}
