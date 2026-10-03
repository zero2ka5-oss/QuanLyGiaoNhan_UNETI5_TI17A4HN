// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 2 - Entity DonGiaoHang (đơn giao hàng, bảng trung tâm của hệ thống).

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng DonGiaoHang
 * - MaDon: khóa chính tự tăng (không trùng); hiển thị dạng DH000012 (MaHienThi).
 * - NgayTao do hệ thống gán; NgayGiaoDuKien không trước ngày tạo.
 * - PhiVanChuyen = PhiCoBan + PhuPhiKhoiLuong + PhuPhiLoaiHang, do TinhPhiXuLy tính – khách không nhập.
 * - TrangThai thay đổi qua DonHangXuLy; mỗi lần đổi ghi một dòng LichSuGiaoNhan.
 * - 1 đơn – n PhanCongGiaoHang (lịch sử phân công), 1 đơn – n LichSuGiaoNhan.
 * - Thu hộ (COD): TienThuHo = tiền hàng shipper thu của người nhận để trả lại người gửi.
 *   Người nhận trả tổng = TienThuHo + PhiVanChuyen (nếu NguoiTraPhi = NguoiNhan).
 *   Hoàn tất đơn = đối soát: shipper nộp tiền, hệ thống trả TienThuHo cho khách (ghi NgayDoiSoat).
 */
public class DonGiaoHang
{
    [Key]
    [Display(Name = "Mã đơn")]
    public int MaDon { get; set; }

    [Display(Name = "Khách hàng")]
    public int MaKhachHang { get; set; }
    public KhachHang? KhachHang { get; set; }

    [Display(Name = "Loại hàng")]
    public int MaLoaiHang { get; set; }
    public LoaiHang? LoaiHang { get; set; }

    [Display(Name = "Khu vực giao")]
    public int MaKhuVuc { get; set; }
    public KhuVuc? KhuVuc { get; set; }

    [Required(ErrorMessage = "Nhập tên người nhận")]
    [StringLength(100)]
    [Display(Name = "Người nhận")]
    public string TenNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại người nhận")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại người nhận")]
    public string SoDienThoaiNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập địa chỉ nhận")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ nhận")]
    public string DiaChiNhan { get; set; } = "";

    [Range(0.01, 100_000, ErrorMessage = "Khối lượng phải lớn hơn 0")]
    [Display(Name = "Khối lượng (kg)")]
    public decimal KhoiLuong { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày giao dự kiến")]
    public DateTime NgayGiaoDuKien { get; set; }

    // ----- Thành phần phí (lưu lại để hiển thị rõ cách tính) -----
    [Display(Name = "Phí cơ bản")] public decimal PhiCoBan { get; set; }
    [Display(Name = "Phụ phí khối lượng")] public decimal PhuPhiKhoiLuong { get; set; }
    [Display(Name = "Phụ phí loại hàng")] public decimal PhuPhiLoaiHang { get; set; }
    [Display(Name = "Phí vận chuyển")] public decimal PhiVanChuyen { get; set; }

    // ----- Thu hộ (COD) và người trả phí -----
    [Range(0, 50_000_000, ErrorMessage = "Tiền thu hộ từ 0 đến 50.000.000 đ")]
    [Display(Name = "Tiền thu hộ")]
    public decimal TienThuHo { get; set; }

    [Display(Name = "Người trả phí")]
    public NguoiTraPhi NguoiTraPhi { get; set; } = NguoiTraPhi.NguoiGui;

    [Display(Name = "Ngày đối soát")]
    public DateTime? NgayDoiSoat { get; set; }

    /// <summary>Lần nộp tiền (ví shipper) chứa đơn này – null khi shipper chưa nộp tiền của đơn.</summary>
    [Display(Name = "Giao dịch nộp tiền")]
    public int? MaGiaoDichNop { get; set; }
    public GiaoDichVi? GiaoDichNop { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiDon TrangThai { get; set; } = TrangThaiDon.ChoPhanCong;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Display(Name = "Ngày hoàn tất")]
    public DateTime? NgayHoanTat { get; set; }

    /// <summary>Ảnh xác nhận giao hàng do nhân viên chụp khi giao thành công (không bắt buộc).</summary>
    [StringLength(300)]
    [Display(Name = "Ảnh giao hàng")]
    public string? AnhGiaoHang { get; set; }

    public ICollection<PhanCongGiaoHang> PhanCongs { get; set; } = [];
    public ICollection<LichSuGiaoNhan> LichSus { get; set; } = [];

    /// <summary>Số tiền shipper phải thu của người nhận khi giao = tiền thu hộ + phí ship (nếu người nhận trả phí).</summary>
    [NotMapped]
    public decimal TongThuNguoiNhan => TienThuHo + (NguoiTraPhi == NguoiTraPhi.NguoiNhan ? PhiVanChuyen : 0);

    /// <summary>Tiền trả lại người gửi khi đối soát = tiền thu hộ − phí ship (nếu người gửi trả phí thì trừ vào tiền thu hộ).</summary>
    [NotMapped]
    public decimal TienTraNguoiGui => TienThuHo - (NguoiTraPhi == NguoiTraPhi.NguoiGui ? PhiVanChuyen : 0);

    /// <summary>Mã hiển thị cho người dùng, VD: DH000012.</summary>
    [NotMapped]
    public string MaHienThi => $"DH{MaDon:D6}";
}
