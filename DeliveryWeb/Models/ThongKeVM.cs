// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - ViewModel Dashboard, thống kê LINQ và trang tổng quan khách hàng.

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Dashboard quản trị (mục 9.4).</summary>
public class DashboardVM
{
    public int TongKhachHang { get; set; }
    public int TongDon { get; set; }
    public int ChoPhanCong { get; set; }
    public int DangGiao { get; set; }
    public int GiaoThanhCong { get; set; }
    public int GiaoKhongThanhCong { get; set; }
    public int HoanTat { get; set; }
    public int NhanVienSanSang { get; set; }
    public int NhanVienDangGiao { get; set; }
    public int PhuongTienSanSang { get; set; }
    public int PhuongTienDangSuDung { get; set; }
    public decimal TongPhiHopLe { get; set; }
    /// <summary>Tiền shipper đã thu của người nhận (đơn Giao thành công) nhưng chưa đối soát.</summary>
    public decimal TienChoDoiSoat { get; set; }
    public int SoDonChoDoiSoat { get; set; }
    public List<string> NhanNgay { get; set; } = [];
    public List<int> SoDonTheoNgay { get; set; } = [];
    public Dictionary<TrangThaiDon, int> TheoTrangThai { get; set; } = [];
    public List<DonGiaoHang> DonCanXuLy { get; set; } = [];
    public List<LichSuGiaoNhan> HoatDongGanDay { get; set; } = [];
}

public record DongThongKe(string Nhan, decimal GiaTri, decimal? GiaTriPhu = null);

/// <summary>Tiền shipper đã thu của người nhận nhưng chưa nộp (đơn Giao thành công, chưa đối soát).</summary>
public record DongGiuTien(string TenNhanVien, int SoDon, decimal TienThuHo, decimal PhiShipDaThu)
{
    public decimal TongPhaiNop => TienThuHo + PhiShipDaThu;
}

/// <summary>Trang thống kê LINQ (mục 9.5).</summary>
public class ThongKeVM
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }
    public List<DongThongKe> TheoTrangThai { get; set; } = [];
    public List<DongThongKe> TheoKhuVuc { get; set; } = [];            // GiaTri = số đơn, GiaTriPhu = tổng khối lượng
    public List<DongThongKe> TheoLoaiHang { get; set; } = [];
    public List<DongThongKe> DonDaGiaoTheoNhanVien { get; set; } = []; // GiaTri = thành công, GiaTriPhu = thất bại
    public DongThongKe? NhanVienXuatSac { get; set; }
    public List<DongThongKe> ThatBaiTheoLyDo { get; set; } = [];
    public double TiLeThanhCong { get; set; }
    public int SoDonCoKetQua { get; set; }
    public List<DongThongKe> TheoThang { get; set; } = [];             // GiaTri = số đơn, GiaTriPhu = tổng phí
    public decimal KhoiLuongTrungBinh { get; set; }
    public decimal PhiTrungBinh { get; set; }

    // ----- Thu hộ (COD): tiền hàng của người gửi, shipper thu hộ rồi nộp lại để công ty trả người gửi -----
    public decimal CodTong { get; set; }            // tổng tiền thu hộ của các đơn hợp lệ trong kỳ
    public int CodSoDon { get; set; }
    public double CodTiLeDon { get; set; }          // % đơn có thu hộ
    public decimal CodDaThu { get; set; }           // shipper đã thu của người nhận (Giao thành công + Hoàn tất)
    public decimal CodChoDoiSoat { get; set; }      // đã thu, shipper đang giữ, chưa đối soát
    public decimal CodDaDoiSoat { get; set; }       // đã đối soát (hoàn tất)
    public decimal CodThucTraShop { get; set; }     // số thực trả người gửi = COD − phí ship do người gửi trả
    public decimal CodChuaThu { get; set; }         // đơn chưa giao xong / giao thất bại
    public List<DongThongKe> CodTheoThang { get; set; } = [];   // GiaTri = COD phát sinh, GiaTriPhu = COD đã đối soát
    public List<DongThongKe> TopKhachThuHo { get; set; } = [];  // GiaTri = tổng COD, GiaTriPhu = số đơn
    public List<DongGiuTien> ShipperGiuTien { get; set; } = [];
    /// <summary>Tiền ship (thu nhập) của từng shipper trong kỳ: GiaTri = tiền ship, GiaTriPhu = số đơn giao thành công.</summary>
    public List<DongThongKe> TienShipTheoNhanVien { get; set; } = [];
}

/// <summary>Trang tổng quan của khách hàng.</summary>
public class TongQuanKhachHangVM
{
    public KhachHang KhachHang { get; set; } = null!;
    public int TongDon { get; set; }
    public int DangXuLy { get; set; }
    public int DaGiao { get; set; }
    public int ThatBai { get; set; }
    public decimal TongPhi { get; set; }
    public List<DonGiaoHang> DonGanDay { get; set; } = [];
    /// <summary>Đơn đang trên đường (chờ phân công → đang giao), mới nhất trước.</summary>
    public List<DonGiaoHang> DonDangVanChuyen { get; set; } = [];
    public Dictionary<TrangThaiDon, int> SoTheoTrangThai { get; set; } = [];
    /// <summary>Phí vận chuyển và số đơn 6 tháng gần nhất (cũ → mới).</summary>
    public List<(string Thang, int SoDon, decimal Phi)> TheoThang { get; set; } = [];
}

/// <summary>Dữ liệu cho khối ước lượng phí + bảng giá (trang chủ công khai và trang Bảng giá của khách hàng).</summary>
public record BangGiaVM(List<KhuVuc> DsKhuVuc, List<LoaiHang> DsLoaiHang, decimal KhoiLuongToiDa, bool LaKhachHang);
