using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng BuuCuc
 * - MaBuuCuc: khóa chính tự tăng; hiển thị BC001, BC002... (MaHienThi).
 * - Tọa độ ViDo, KinhDo dùng để hiển thị bản đồ trạm / bưu cục (BuuCucController).
 * - Bưu cục ngừng hoạt động thì không nhận đơn gửi mới, không thể gán làm bưu cục của shipper mới.
 * - Quan hệ: 1 bưu cục – n KhuVuc (phụ trách), 1 bưu cục – n NhanVienGiaoHang (xuất phát).
 */
public class BuuCuc
{
    [Key]
    [Display(Name = "Mã bưu cục")]
    public int MaBuuCuc { get; set; }

    [Required(ErrorMessage = "Nhập tên bưu cục")]
    [StringLength(100)]
    [Display(Name = "Tên bưu cục")]
    public string TenBuuCuc { get; set; } = "";

    [Required(ErrorMessage = "Nhập tỉnh / thành phố")]
    [StringLength(100)]
    [Display(Name = "Tỉnh / thành phố")]
    public string TinhThanh { get; set; } = "";

    [Required(ErrorMessage = "Nhập địa chỉ")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = "";

    [StringLength(15)]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Range(-90, 90, ErrorMessage = "Vĩ độ từ -90 đến 90")]
    [Display(Name = "Vĩ độ")]
    public double? ViDo { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ từ -180 đến 180")]
    [Display(Name = "Kinh độ")]
    public double? KinhDo { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiHoatDong TrangThai { get; set; } = TrangThaiHoatDong.HoatDong;

    public ICollection<KhuVuc> KhuVucs { get; set; } = [];
    public ICollection<NhanVienGiaoHang> NhanViens { get; set; } = [];

    /// <summary>Mã bưu cục hiển thị, VD: BC025.</summary>
    [NotMapped]
    public string MaHienThi => $"BC{MaBuuCuc:D3}";
}

/*
 * Bảng LichSuPhuongTien
 * Ghi vết mỗi lần gán, đổi hoặc gỡ phương tiện của nhân viên giao hàng.
 */
public class LichSuPhuongTien
{
    [Key]
    public int MaLichSu { get; set; }

    [Display(Name = "Nhân viên")]
    public int MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Phương tiện cũ")]
    public int? MaPhuongTienCu { get; set; }
    public PhuongTien? PhuongTienCu { get; set; }

    [Display(Name = "Phương tiện mới")]
    public int? MaPhuongTienMoi { get; set; }
    public PhuongTien? PhuongTienMoi { get; set; }

    [Display(Name = "Thời gian")]
    public DateTime ThoiGian { get; set; } = DateTime.Now;

    [Required, StringLength(150)]
    [Display(Name = "Người thực hiện")]
    public string NguoiThucHien { get; set; } = "";

    [StringLength(255)]
    [Display(Name = "Lý do")]
    public string? LyDo { get; set; }
}
