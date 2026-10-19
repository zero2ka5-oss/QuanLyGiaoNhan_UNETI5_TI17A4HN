namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Dashboard quản trị.</summary>
public class DashboardVM
{
    public int TongKhachHang { get; set; }
    public int TongDon { get; set; }
    /// <summary>Hàng ở bưu cục chờ phân chặng.</summary>
    public int ChoPhanCong { get; set; }
    /// <summary>Đơn đang trên chặng liên tỉnh.</summary>
    public int DangTrungChuyen { get; set; }
    /// <summary>Đơn đã tạo, khách chưa mang hàng tới bưu cục.</summary>
    public int ChoTiepNhan { get; set; }
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
    /// <summary>Trong số đơn chờ đối soát: số đơn shipper đã tạo lệnh nộp, chờ xác nhận (còn lại là shipper đang giữ).</summary>
    public int SoDonChoXacNhanNop { get; set; }
    public List<string> NhanNgay { get; set; } = [];
    public List<int> SoDonTheoNgay { get; set; } = [];
    public Dictionary<TrangThaiDon, int> TheoTrangThai { get; set; } = [];
    public List<DonGiaoHang> DonCanXuLy { get; set; } = [];
    public List<LichSuGiaoNhan> HoatDongGanDay { get; set; } = [];
}

/// <summary>Một mục trên biểu đồ / bảng thống kê (nhãn + giá trị chính + giá trị phụ tùy chọn).</summary>
public record DongThongKe(string Nhan, decimal GiaTri, decimal? GiaTriPhu = null);

/// <summary>Shipper đang giữ tiền thu hộ / tiền ship chưa nộp về công ty.</summary>
public record DongGiuTien(string TenNhanVien, int SoDon, decimal TienThuHo, decimal PhiShipDaThu)
{
    public decimal TongPhaiNop => TienThuHo + PhiShipDaThu;
}

/// <summary>Dữ liệu trang Thống kê & Báo cáo quản trị (lọc theo khoảng ngày).</summary>
public class ThongKeVM
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }

    // ----- Vận hành đơn hàng -----
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

    // ----- Thu hộ (COD) -----
    public decimal CodTong { get; set; }            // tổng COD các đơn phát sinh trong kỳ
    public int CodSoDon { get; set; }               // số đơn có COD
    public double CodTiLeDon { get; set; }          // % đơn có COD
    public decimal CodDaThu { get; set; }           // đã thu của người nhận (Giao thành công + Hoàn tất)
    public decimal CodChoDoiSoat { get; set; }      // đã thu, shipper đang giữ, chưa đối soát
    public decimal CodDaDoiSoat { get; set; }       // đã đối soát (hoàn tất)
    public decimal CodThucTraShop { get; set; }     // số thực trả người gửi = COD − phí ship do người gửi trả
    public decimal CodChuaThu { get; set; }         // đơn chưa giao xong / giao thất bại
    public List<DongThongKe> CodTheoThang { get; set; } = [];   // GiaTri = COD phát sinh, GiaTriPhu = COD đã đối soát
    public List<DongThongKe> TopKhachThuHo { get; set; } = [];  // GiaTri = tổng COD, GiaTriPhu = số đơn

    // ----- Tài chính & Dòng tiền -----
    /// <summary>Shipper đang giữ tiền: đơn giao thành công chưa tạo lệnh nộp (cùng định nghĩa với Ví shipper).</summary>
    public List<DongGiuTien> ShipperGiuTien { get; set; } = [];
    /// <summary>Tiền shipper đã tạo lệnh nộp, chờ Quản trị xác nhận.</summary>
    public decimal TienChoXacNhanNop { get; set; }
    /// <summary>Thu nhập (hoa hồng) của từng shipper trong kỳ: GiaTri = hoa hồng, GiaTriPhu = số đơn giao thành công.</summary>
    public List<DongThongKe> TienShipTheoNhanVien { get; set; } = [];
    /// <summary>Cước các đơn giao thành công trong kỳ = ChiTraShipper + DoanhThuThuan.</summary>
    public decimal CuocGiaoThanhCong { get; set; }
    public decimal ChiTraShipper { get; set; }
    public decimal DoanhThuThuan => CuocGiaoThanhCong - ChiTraShipper;
    /// <summary>Công nợ phí người gửi hiện tại (đơn đã đối soát, chưa thu phí).</summary>
    public decimal CongNoPhi { get; set; }
    public int SoDonNoPhi { get; set; }
}

/// <summary>Trang tổng quan của khách hàng.</summary>
public class TongQuanKhachHangVM
{
    public KhachHang KhachHang { get; set; } = null!;
    public int TongDon { get; set; }
    public int DangXuLy { get; set; }
    public int DaGiao { get; set; }
    public int ThatBai { get; set; }
    public int DaHuy { get; set; }
    public decimal TongPhi { get; set; }
    public List<DonGiaoHang> DonGanDay { get; set; } = [];
    /// <summary>Đơn đang trên đường (chờ phân công → đang giao), mới nhất trước.</summary>
    public List<DonGiaoHang> DonDangVanChuyen { get; set; } = [];
    public Dictionary<TrangThaiDon, int> SoTheoTrangThai { get; set; } = [];
    /// <summary>Phí vận chuyển và số đơn 6 tháng gần nhất (cũ → mới).</summary>
    public List<(string Thang, int SoDon, decimal Phi)> TheoThang { get; set; } = [];
    /// <summary>Đơn đã hoàn tất nhưng người gửi chưa thanh toán phần phí không trừ được vào thu hộ (công nợ).</summary>
    public List<DonGiaoHang> DonNoPhi { get; set; } = [];
    public decimal CongNoPhi => DonNoPhi.Sum(d => d.PhiNguoiGuiPhaiTra);
}
