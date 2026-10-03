// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Banner trình chiếu trên trang chủ công khai – Quản trị thêm ảnh, tiêu đề, nút bấm, thứ tự, ẩn / hiện.

using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng BannerTrangChu
 * - Anh: đường dẫn ảnh (ảnh tải lên nằm trong /uploads/banner/, 3 banner mẫu nằm trong /img/banner/).
 * - Trang chủ hiện các banner HienThi = true, sắp theo ThuTu tăng dần; không có banner nào thì dùng khối giới thiệu mặc định.
 */
public class BannerTrangChu
{
    [Key]
    public int MaBanner { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề"), StringLength(150, ErrorMessage = "Tiêu đề tối đa 150 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = "";

    [StringLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự")]
    [Display(Name = "Mô tả ngắn")]
    public string? MoTa { get; set; }

    [StringLength(300)]
    [Display(Name = "Ảnh banner")]
    public string Anh { get; set; } = "";

    [StringLength(50, ErrorMessage = "Chữ trên nút tối đa 50 ký tự")]
    [Display(Name = "Chữ trên nút")]
    public string? ChuNut { get; set; }

    [StringLength(300)]
    [Display(Name = "Liên kết của nút")]
    public string? LienKet { get; set; }

    [Range(0, 999, ErrorMessage = "Thứ tự từ 0 đến 999")]
    [Display(Name = "Thứ tự hiển thị")]
    public int ThuTu { get; set; }

    [Display(Name = "Hiển thị trên trang chủ")]
    public bool HienThi { get; set; } = true;

    public DateTime NgayTao { get; set; } = DateTime.Now;
}
