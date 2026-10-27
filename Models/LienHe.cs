using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public enum ChuDeLienHe
{
    [Display(Name = "Hỏi cước phí & thanh toán")]
    CuocPhi = 1,

    [Display(Name = "Tra cứu & tiến độ đơn hàng")]
    TienDoDonHang = 2,

    [Display(Name = "Thay đổi thông tin người nhận")]
    ThayDoiThongTin = 3,

    [Display(Name = "Khiếu nại hư hỏng / thất lạc")]
    KhieuNaiSuCo = 4,

    [Display(Name = "Góp ý dịch vụ & shipper")]
    GopYThaiDo = 5,

    [Display(Name = "Yêu cầu khác")]
    Khac = 6
}

public enum TrangThaiLienHe
{
    [Display(Name = "Chờ phản hồi")]
    ChoPhanHoi = 0,

    [Display(Name = "Đang xử lý")]
    DangXuLy = 1,

    [Display(Name = "Đã phản hồi")]
    DaPhanHoi = 2,

    [Display(Name = "Đã giải quyết & Đóng")]
    DaDong = 3
}

public class YeuCauLienHe
{
    [Key]
    public int MaLienHe { get; set; }

    [Display(Name = "Khách hàng")]
    public int MaKhachHang { get; set; }
    public KhachHang? KhachHang { get; set; }

    [Display(Name = "Đơn hàng liên quan")]
    public int? MaDonHang { get; set; }
    public DonGiaoHang? DonHang { get; set; }

    [Display(Name = "Bưu cục tiếp nhận")]
    public int? MaBuuCuc { get; set; }
    public BuuCuc? BuuCuc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề thắc mắc")]
    [StringLength(200, ErrorMessage = "Tiêu đề không quá 200 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = "";

    [Display(Name = "Chủ đề")]
    public ChuDeLienHe ChuDe { get; set; } = ChuDeLienHe.TienDoDonHang;

    [Required(ErrorMessage = "Vui lòng nhập nội dung cần hỗ trợ")]
    [StringLength(2000, ErrorMessage = "Nội dung không quá 2.000 ký tự")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = "";

    [Display(Name = "Thời gian gửi")]
    public DateTime NgayGui { get; set; } = DateTime.Now;

    [Display(Name = "Trạng thái")]
    public TrangThaiLienHe TrangThai { get; set; } = TrangThaiLienHe.ChoPhanHoi;

    [Display(Name = "Cập nhật gần nhất")]
    public DateTime? NgayCapNhat { get; set; }

    public ICollection<TinNhanLienHe> TinNhans { get; set; } = [];

    [NotMapped]
    public string MaHienThi => $"LH{NgayGui:yyyyMMdd}{MaLienHe:D4}";

    [NotMapped]
    public int SoTinNhan => TinNhans?.Count ?? 0;
}

public class TinNhanLienHe
{
    [Key]
    public int MaTinNhan { get; set; }

    public int MaLienHe { get; set; }
    public YeuCauLienHe? LienHe { get; set; }

    public int? MaTaiKhoan { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    [Required]
    [StringLength(100)]
    public string NguoiGui { get; set; } = "";

    public bool LaNhanVien { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn")]
    [StringLength(2000, ErrorMessage = "Nội dung không quá 2.000 ký tự")]
    public string NoiDung { get; set; } = "";

    public DateTime NgayGui { get; set; } = DateTime.Now;
}
