using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public enum LoaiGiaoDichKhach : byte
{
    [Display(Name = "Cộng tiền COD")]
    CongTienCOD = 0,

    [Display(Name = "Rút tiền về ngân hàng")]
    RutTien = 1,

    [Display(Name = "Thanh toán cước vận chuyển")]
    ThanhToanCuoc = 2
}

public class GiaoDichViKhachHang
{
    [Key]
    public int MaGiaoDich { get; set; }

    public int MaKhachHang { get; set; }
    public KhachHang? KhachHang { get; set; }

    public LoaiGiaoDichKhach Loai { get; set; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal SoTien { get; set; }

    public decimal SoDuSauGiaoDich { get; set; }

    public int? MaDonHang { get; set; }
    public DonGiaoHang? DonHang { get; set; }

    public TrangThaiGiaoDich TrangThai { get; set; } = TrangThaiGiaoDich.ChoXacNhan;

    [StringLength(100)]
    public string? TenNganHang { get; set; }

    [StringLength(50)]
    public string? SoTaiKhoan { get; set; }

    [StringLength(100)]
    public string? ChuTaiKhoan { get; set; }

    [StringLength(100)]
    public string? MaThamChieu { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public DateTime? NgayXuLy { get; set; }

    [StringLength(150)]
    public string? NguoiXuLy { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [NotMapped]
    public string MaHienThi => $"VKH{MaGiaoDich:D6}";
}
