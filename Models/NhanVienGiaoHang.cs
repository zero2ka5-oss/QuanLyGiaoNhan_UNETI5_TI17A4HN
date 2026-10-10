using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng NhanVienGiaoHang
 * - MaTaiKhoan: tài khoản vai trò GiaoHang để nhân viên đăng nhập xem công việc.
 * - LoaiShipper: LienTinh (chạy giữa các bưu cục, xuất phát từ MaBuuCuc) | KhuVuc (nhận đơn ở MaBuuCuc, giao trong MaKhuVucPhuTrach).
 * - MaPhuongTien: phương tiện được gán – 1 shipper ↔ 1 phương tiện (unique index); đổi phương tiện ghi LichSuPhuongTien.
 * - ViDo / KinhDo / ThoiGianViTri: vị trí GPS gần nhất do trình duyệt của shipper gửi lên – vị trí của các đơn shipper đang giữ.
 * - MaKhuVucPhuTrach: khu vực shipper khu vực được giao hàng.
 * - TrangThai: SanSang | DangGiaoHang | TamNghi | NgungHoatDong.
 *   DangGiaoHang do hệ thống tự cập nhật theo phân công (DonHangXuLy.CapNhatTrangThaiNguonLuc),
 *   người dùng không chọn tay trạng thái này.
 */
public class NhanVienGiaoHang
{
    [Key]
    [Display(Name = "Mã nhân viên")]
    public int MaNhanVien { get; set; }

    [Display(Name = "Tài khoản")]
    public int? MaTaiKhoan { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    [Required(ErrorMessage = "Nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Nhập email")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Display(Name = "Khu vực phụ trách")]
    public int? MaKhuVucPhuTrach { get; set; }
    public KhuVuc? KhuVucPhuTrach { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiNhanVien TrangThai { get; set; } = TrangThaiNhanVien.SanSang;

    [Display(Name = "Loại shipper")]
    public LoaiShipper LoaiShipper { get; set; } = LoaiShipper.KhuVuc;

    [Display(Name = "Bưu cục")]
    public int? MaBuuCuc { get; set; }
    public BuuCuc? BuuCuc { get; set; }

    [Display(Name = "Phương tiện")]
    public int? MaPhuongTien { get; set; }
    public PhuongTien? PhuongTien { get; set; }

    [Display(Name = "Vĩ độ")] public double? ViDo { get; set; }
    [Display(Name = "Kinh độ")] public double? KinhDo { get; set; }
    [Display(Name = "Cập nhật vị trí lúc")] public DateTime? ThoiGianViTri { get; set; }

    public ICollection<PhanCongGiaoHang> PhanCongs { get; set; } = [];

    /// <summary>Mã shipper hiển thị, VD: SP025.</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string MaHienThi => $"SP{MaNhanVien:D3}";
}
