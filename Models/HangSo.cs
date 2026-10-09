namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Vai trò người dùng – lưu ở cột TaiKhoan.VaiTro và trong Session.</summary>
public static class VaiTroNguoiDung
{
    public const string QuanTri = "QuanTri";      // Admin - Quản trị hệ thống
    public const string NhanVien = "NhanVien";    // Nhân viên: điều hành & duyệt đơn tại bưu cục
    public const string GiaoHang = "GiaoHang";    // Shipper - Nhân viên giao hàng
    public const string KhachHang = "KhachHang";  // Khách hàng

    // Hỗ trợ tương thích dữ liệu cũ
    public const string DieuPhoi = "NhanVien";
    public const string BuuCuc = "NhanVien";

    public static readonly string[] TatCa = [QuanTri, NhanVien, GiaoHang, KhachHang];
}

public enum TrangThaiTaiKhoan : byte { BiKhoa = 0, HoatDong = 1 }

/// <summary>Trạng thái chung cho Loại hàng, Khu vực, Khách hàng.</summary>
public enum TrangThaiHoatDong : byte { NgungHoatDong = 0, HoatDong = 1 }

/// <summary>Hình thức gửi hàng: người gửi tự mang ra bưu cục gửi hoặc yêu cầu shipper đến tận nhà/kho lấy.</summary>
public enum HinhThucGui : byte
{
    GuiTaiBuuCuc = 0,   // DROP_OFF – khách tự mang hàng ra bưu cục gửi
    LayTanNoi = 1       // PICK_UP – shipper khu vực đến địa chỉ người gửi lấy hàng
}

/// <summary>
/// Vòng đời đơn: Khách hàng → Bưu cục gửi → Shipper liên tỉnh → Bưu cục nhận → Shipper khu vực → Người nhận.
/// Nếu lấy hàng tận nơi: Khách tạo đơn → Chờ lấy tận nơi → Shipper đã lấy → Tiếp nhận tại bưu cục gửi.
/// Mỗi lần đổi trạng thái là một Tracking Event (LichSuGiaoNhan). Tên tiếng Anh theo đặc tả ở chú thích từng dòng.
/// Đơn có bưu cục gửi = bưu cục nhận bỏ qua chặng liên tỉnh (2 → 4).
/// </summary>
public enum TrangThaiDon : byte
{
    DaTao = 0,                 // CREATED – khách đã tạo đơn, chưa mang hàng tới bưu cục gửi
    DaNhanTaiBuuCucGui = 1,    // RECEIVED_AT_ORIGIN_HUB
    ChoLayLienTinh = 2,        // ASSIGNED_TO_INTERPROVINCIAL_COURIER
    DaLayLienTinh = 3,         // PICKED_UP_FOR_INTERPROVINCIAL
    DangTrungChuyen = 4,       // IN_TRANSIT
    DaDenBuuCucNhan = 5,       // ARRIVED_AT_DESTINATION_HUB
    ChoLayGiaoHang = 6,        // ASSIGNED_TO_LOCAL_COURIER
    DaLayGiaoHang = 7,         // PICKED_UP_BY_LOCAL_COURIER
    DangGiao = 8,              // OUT_FOR_DELIVERY
    GiaoThanhCong = 9,         // DELIVERED
    GiaoKhongThanhCong = 10,   // DELIVERY_FAILED – shipper khu vực vẫn đang giữ hàng
    HenGiaoLai = 11,           // RESCHEDULED – shipper giữ hàng, chờ đi giao lại
    HoanTat = 12,              // đã đối soát tiền (sau DELIVERED)
    DaHuy = 13,                // hủy; hàng đã ở bưu cục thì chờ chuyển hoàn (NgayHoanHang)
    ThatLac = 14,              // LOST
    HuHong = 15,               // DAMAGED
    ChoLayTanNoi = 16,         // ASSIGNED_FOR_PICKUP – chờ shipper đến lấy hàng tận nơi
    DaLayTanNoi = 17           // PICKED_UP_FROM_SENDER – shipper đã lấy hàng, đang mang về bưu cục gửi
}

/// <summary>Loại shipper / loại chặng: liên tỉnh chạy giữa các bưu cục, khu vực nhận ở bưu cục nhận và giao tới người nhận.</summary>
public enum LoaiShipper : byte { KhuVuc = 0, LienTinh = 1 }

public enum TrangThaiNhanVien : byte { SanSang = 0, DangGiaoHang = 1, TamNghi = 2, NgungHoatDong = 3 }

public enum TrangThaiPhuongTien : byte { SanSang = 0, DangSuDung = 1, BaoTri = 2, NgungHoatDong = 3 }

/// <summary>Trạng thái một lần phân công. 3 trạng thái đầu là "đang hiệu lực".</summary>
public enum TrangThaiPhanCong : byte { DaPhanCong = 0, DaNhanHang = 1, DangGiao = 2, KetThuc = 3, DaThayDoi = 4 }

/// <summary>Kết quả khi phân công (một chặng / một lần giao) kết thúc.</summary>
public enum KetQuaGiao : byte
{
    GiaoThanhCong = 0,        // chặng khu vực: giao được cho người nhận
    GiaoKhongThanhCong = 1,   // chặng khu vực: một lần giao thất bại
    DaHuyDon = 2,
    DaBanGiaoBuuCuc = 3,      // chặng liên tỉnh: đã bàn giao cho bưu cục nhận
    TraVeBuuCuc = 4,          // chặng khu vực: shipper trả hàng lại bưu cục (không giao tiếp)
    SuCo = 5,                 // thất lạc / hư hỏng khi đang giữ hàng
    DaLayHang = 6             // chặng lấy hàng tận nơi: shipper đã lấy và bàn giao lại bưu cục gửi
}

/// <summary>Lý do giao không thành công – dùng cho thống kê theo lý do.</summary>
public enum LyDoThatBai : byte { NguoiNhanVangMat = 0, SaiDiaChi = 1, TuChoiNhan = 2, KhongLienLacDuoc = 3, Khac = 4 }

/// <summary>Ai trả phí vận chuyển: người gửi (trừ vào tiền đối soát / trả trước) hay người nhận (shipper thu khi giao).</summary>
public enum NguoiTraPhi : byte { NguoiGui = 0, NguoiNhan = 1 }

/// <summary>Giao dịch ví shipper: nộp tiền thu hộ về công ty / rút tiền ship về tài khoản.</summary>
public enum LoaiGiaoDich : byte { NopTienThuHo = 0, RutTien = 1 }

public enum TrangThaiGiaoDich : byte { ChoXacNhan = 0, DaXacNhan = 1, TuChoi = 2 }
