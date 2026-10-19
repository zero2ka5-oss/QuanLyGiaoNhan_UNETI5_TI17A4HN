namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Khung cự ly khoảng cách giữa các tỉnh thành và cước cơ bản tương ứng.</summary>
public record KhungCuLy(string TenKhung, double TuKm, double DenKm, decimal PhiCoBan, int SoNgayGiao, string MoTa);

/// <summary>Khu vực, loại hàng đang hoạt động, bưu cục, khung cự ly và khối lượng tối đa một đơn.</summary>
public record BangGiaVM(
    List<KhuVuc> DsKhuVuc,
    List<LoaiHang> DsLoaiHang,
    decimal KhoiLuongToiDa,
    bool LaKhachHang,
    List<BuuCuc>? DsBuuCuc = null,
    List<KhungCuLy>? DsCuLy = null
);
