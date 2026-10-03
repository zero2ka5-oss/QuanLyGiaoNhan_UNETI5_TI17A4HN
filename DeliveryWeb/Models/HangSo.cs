// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Các kiểu liệt kê (enum) trạng thái và hằng số vai trò dùng chung cho 5 module.

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Vai trò người dùng – lưu ở cột TaiKhoan.VaiTro và trong Session.</summary>
public static class VaiTroNguoiDung
{
    public const string QuanTri = "QuanTri";      // Admin
    public const string DieuPhoi = "DieuPhoi";    // Nhân viên điều phối
    public const string GiaoHang = "GiaoHang";    // Nhân viên giao hàng
    public const string KhachHang = "KhachHang";  // Khách hàng

    public static readonly string[] TatCa = [QuanTri, DieuPhoi, GiaoHang, KhachHang];
}

public enum TrangThaiTaiKhoan : byte { BiKhoa = 0, HoatDong = 1 }

/// <summary>Trạng thái chung cho Loại hàng, Khu vực, Khách hàng.</summary>
public enum TrangThaiHoatDong : byte { NgungHoatDong = 0, HoatDong = 1 }

/// <summary>Vòng đời đơn giao hàng (mục 6.2 của đề).</summary>
public enum TrangThaiDon : byte
{
    ChoPhanCong = 0,
    DaPhanCong = 1,
    DaNhanHang = 2,
    DangGiao = 3,
    GiaoThanhCong = 4,
    GiaoKhongThanhCong = 5,
    HoanTat = 6,
    DaHuy = 7
}

public enum TrangThaiNhanVien : byte { SanSang = 0, DangGiaoHang = 1, TamNghi = 2, NgungHoatDong = 3 }

public enum TrangThaiPhuongTien : byte { SanSang = 0, DangSuDung = 1, BaoTri = 2, NgungHoatDong = 3 }

/// <summary>Trạng thái một lần phân công (mục 8.1). 3 trạng thái đầu là "đang hiệu lực".</summary>
public enum TrangThaiPhanCong : byte { DaPhanCong = 0, DaNhanHang = 1, DangGiao = 2, KetThuc = 3, DaThayDoi = 4 }

/// <summary>Kết quả khi phân công kết thúc.</summary>
public enum KetQuaGiao : byte { GiaoThanhCong = 0, GiaoKhongThanhCong = 1, DaHuyDon = 2 }

/// <summary>Lý do giao không thành công (mục 8.7) – dùng cho thống kê theo lý do.</summary>
public enum LyDoThatBai : byte { NguoiNhanVangMat = 0, SaiDiaChi = 1, TuChoiNhan = 2, KhongLienLacDuoc = 3, Khac = 4 }

/// <summary>Ai trả phí vận chuyển: người gửi (trừ vào tiền đối soát / trả trước) hay người nhận (shipper thu khi giao).</summary>
public enum NguoiTraPhi : byte { NguoiGui = 0, NguoiNhan = 1 }

/// <summary>Giao dịch ví shipper: nộp tiền thu hộ về công ty / rút tiền ship về tài khoản.</summary>
public enum LoaiGiaoDich : byte { NopTienThuHo = 0, RutTien = 1 }

public enum TrangThaiGiaoDich : byte { ChoXacNhan = 0, DaXacNhan = 1, TuChoi = 2 }
