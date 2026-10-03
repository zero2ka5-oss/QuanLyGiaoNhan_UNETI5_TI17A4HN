// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 3 - Entity PhuongTien.

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng PhuongTien
 * - BienSo bắt buộc, không trùng; TaiTrongToiDa > 0 (kg).
 * - TrangThai: SanSang | DangSuDung | BaoTri | NgungHoatDong.
 *   Phương tiện Bảo trì / Ngừng hoạt động không được phân công.
 *   DangSuDung do hệ thống cập nhật khi có phân công đang hiệu lực.
 */
public class PhuongTien
{
    /// <summary>Danh sách loại phương tiện gợi ý cho ô chọn.</summary>
    public static readonly string[] DanhSachLoai = ["Xe máy", "Xe ba gác", "Xe tải 500 kg", "Xe tải 1 tấn", "Xe tải 1,5 tấn", "Xe tải 2,5 tấn"];

    [Key]
    [Display(Name = "Mã phương tiện")]
    public int MaPhuongTien { get; set; }

    [Required(ErrorMessage = "Nhập biển số")]
    [StringLength(15)]
    [RegularExpression(@"^[0-9]{2}[A-Z]{1,2}[0-9]?-[0-9]{3}\.?[0-9]{2}$", ErrorMessage = "Biển số dạng 29B1-123.45 hoặc 29C-456.78")]
    [Display(Name = "Biển số")]
    public string BienSo { get; set; } = "";

    [Required(ErrorMessage = "Chọn loại phương tiện")]
    [StringLength(50)]
    [Display(Name = "Loại phương tiện")]
    public string LoaiPhuongTien { get; set; } = "";

    [Range(0.01, 100_000, ErrorMessage = "Tải trọng tối đa phải lớn hơn 0")]
    [Display(Name = "Tải trọng tối đa (kg)")]
    public decimal TaiTrongToiDa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiPhuongTien TrangThai { get; set; } = TrangThaiPhuongTien.SanSang;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [StringLength(300)]
    [Display(Name = "Ảnh phương tiện")]
    public string? AnhXe { get; set; }

    public ICollection<PhanCongGiaoHang> PhanCongs { get; set; } = [];
}
