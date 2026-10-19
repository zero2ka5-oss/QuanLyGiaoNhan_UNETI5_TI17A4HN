using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>
/// Bảng GiaoDichVi – ví của shipper trên app giao hàng (nộp tiền COD, rút tiền thu nhập).
/// </summary>
public class GiaoDichVi
{
    [Key]
    [Display(Name = "Mã giao dịch")]
    public int MaGiaoDich { get; set; }

    [Display(Name = "Nhân viên")]
    public int MaNhanVien { get; set; }
    public NhanVienGiaoHang? NhanVien { get; set; }

    [Display(Name = "Loại giao dịch")]
    public LoaiGiaoDich Loai { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    [Display(Name = "Số tiền")]
    public decimal SoTien { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiGiaoDich TrangThai { get; set; } = TrangThaiGiaoDich.ChoXacNhan;

    [Required, StringLength(50)]
    [Display(Name = "Phương thức")]
    public string PhuongThuc { get; set; } = "";

    /// <summary>Nộp tiền: mã tham chiếu chuyển khoản / biên nhận. Rút tiền: ngân hàng – số tài khoản – chủ tài khoản.</summary>
    [StringLength(255)]
    [Display(Name = "Thông tin thanh toán")]
    public string? ThongTinThanhToan { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [Display(Name = "Ngày xử lý")]
    public DateTime? NgayXuLy { get; set; }

    [StringLength(150)]
    [Display(Name = "Người xử lý")]
    public string? NguoiXuLy { get; set; }

    [StringLength(255)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    /// <summary>Các đơn được đối soát trong lần nộp tiền này.</summary>
    public ICollection<DonGiaoHang> DonNop { get; set; } = [];

    [NotMapped]
    public string MaHienThi => $"GD{MaGiaoDich:D6}";
}
