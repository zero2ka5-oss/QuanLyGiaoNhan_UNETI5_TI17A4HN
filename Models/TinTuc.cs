using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public enum ChuyenMucTinTuc : byte { ThongBao = 0, CamNang = 1, HuongDan = 2 }

/*
 * Bảng TinTuc
 * - NoiDung: mỗi dòng là một đoạn văn; dòng bắt đầu bằng "- " hiển thị thành gạch đầu dòng.
 * - Trang công khai chỉ hiện bài HienThi = true và NgayDang <= hôm nay (đặt ngày tương lai = hẹn ngày đăng).
 */
public class TinTuc
{
    [Key]
    public int MaTinTuc { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề"), StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = "";

    [Required(ErrorMessage = "Nhập tóm tắt"), StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự")]
    [Display(Name = "Tóm tắt")]
    public string TomTat { get; set; } = "";

    [Required(ErrorMessage = "Nhập nội dung bài viết")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = "";

    [Display(Name = "Chuyên mục")]
    public ChuyenMucTinTuc ChuyenMuc { get; set; }

    /// <summary>Ảnh bìa bài viết; không có ảnh thì thẻ tin hiện biểu tượng theo chuyên mục.</summary>
    [StringLength(300)]
    [Display(Name = "Ảnh bìa")]
    public string? AnhBia { get; set; }

    [StringLength(50)]
    [Display(Name = "Biểu tượng")]
    public string BieuTuong { get; set; } = "bi-newspaper";

    [Required(ErrorMessage = "Chọn ngày đăng"), DataType(DataType.Date)]
    [Display(Name = "Ngày đăng")]
    public DateTime NgayDang { get; set; } = DateTime.Today;

    [Display(Name = "Hiển thị trên trang chủ")]
    public bool HienThi { get; set; } = true;

    [StringLength(100)]
    public string? NguoiDang { get; set; }

    public DateTime? NgayCapNhat { get; set; }
}
