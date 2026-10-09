using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Form phân công / đổi shipper cho một chặng (phương tiện là xe được gán cố định cho shipper).</summary>
public class PhanCongFormVM
{
    public int MaDon { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn shipper")]
    [Display(Name = "Shipper")]
    public int MaNhanVien { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú / lý do")]
    public string? GhiChu { get; set; }

    // ----- Dữ liệu hiển thị -----
    public DonGiaoHang? Don { get; set; }
    public PhanCongGiaoHang? PhanCongHienTai { get; set; }
    public List<LuaChonNhanVien> DsNhanVien { get; set; } = [];
    public bool LaDoiPhanCong { get; set; }
    public LoaiShipper LoaiChang { get; set; }
}

/// <summary>Một shipper trên form phân công kèm phương tiện được gán và lý do không chọn được.</summary>
public record LuaChonNhanVien(int MaNhanVien, string HoTen, string MaHienThi, string? KhuVuc, bool CungKhuVuc,
                              TrangThaiNhanVien TrangThai, int SoDonDangGiu, string? MaXe, string? BienSo, string? LoaiXe,
                              decimal TaiTrong, decimal DangChoKg, bool DuocChon, string? LyDoKhongChon);

/// <summary>Vị trí hiện tại của đơn (Loai: buucuc | shipper | dagiao | khach | loi | khac).</summary>
public record ViTriDon(string Loai, string TieuDe, string MoTa, double? ViDo, double? KinhDo, DateTime? CapNhatLuc, string? PhuongTien);
