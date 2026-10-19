using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class TaoLienHeVM
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề thắc mắc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không quá 200 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = "";

    [Display(Name = "Chủ đề cần hỗ trợ")]
    public ChuDeLienHe ChuDe { get; set; } = ChuDeLienHe.TienDoDonHang;

    [Display(Name = "Đơn hàng liên quan (nếu có)")]
    public int? MaDonHang { get; set; }

    [Display(Name = "Bưu cục tiếp nhận (tùy chọn)")]
    public int? MaBuuCuc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung chi tiết")]
    [StringLength(2000, ErrorMessage = "Nội dung không quá 2.000 ký tự")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = "";
}

public class GuiPhanHoiVM
{
    public int MaLienHe { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung phản hồi")]
    [StringLength(2000, ErrorMessage = "Nội dung không quá 2.000 ký tự")]
    [Display(Name = "Nội dung phản hồi")]
    public string NoiDung { get; set; } = "";

    [Display(Name = "Trạng thái mới")]
    public TrangThaiLienHe? TrangThaiMoi { get; set; }
}

public class BoLocLienHe
{
    public string? TuKhoa { get; set; }
    public TrangThaiLienHe? TrangThai { get; set; }
    public ChuDeLienHe? ChuDe { get; set; }
    public int? MaBuuCuc { get; set; }
    public int Trang { get; set; } = 1;
}

public class DanhSachLienHeVM
{
    public BoLocLienHe BoLoc { get; set; } = new();
    public DanhSachTrang<YeuCauLienHe> KetQua { get; set; } = new();
    public int TongSoChoPhanHoi { get; set; }
    public int TongSoDangXuLy { get; set; }
    public int TongSoDaPhanHoi { get; set; }
    public int TongSoDaDong { get; set; }
    public bool LaKhachHang { get; set; }
}
