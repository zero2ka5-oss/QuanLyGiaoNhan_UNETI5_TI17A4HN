
namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public static class VaiTroNguoiDung
{
    public const string QuanTri = "QuanTri";      
    public const string DieuPhoi = "DieuPhoi";    
    public const string GiaoHang = "GiaoHang";    
    public const string KhachHang = "KhachHang";  

    public static readonly string[] TatCa = [QuanTri, DieuPhoi, GiaoHang, KhachHang];
}

public enum TrangThaiTaiKhoan : byte { BiKhoa = 0, HoatDong = 1 }

public enum TrangThaiHoatDong : byte { NgungHoatDong = 0, HoatDong = 1 }

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

public enum TrangThaiPhanCong : byte { DaPhanCong = 0, DaNhanHang = 1, DangGiao = 2, KetThuc = 3, DaThayDoi = 4 }

public enum KetQuaGiao : byte { GiaoThanhCong = 0, GiaoKhongThanhCong = 1, DaHuyDon = 2 }

public enum LyDoThatBai : byte { NguoiNhanVangMat = 0, SaiDiaChi = 1, TuChoiNhan = 2, KhongLienLacDuoc = 3, Khac = 4 }

public enum NguoiTraPhi : byte { NguoiGui = 0, NguoiNhan = 1 }

public enum LoaiGiaoDich : byte { NopTienThuHo = 0, RutTien = 1 }

public enum TrangThaiGiaoDich : byte { ChoXacNhan = 0, DaXacNhan = 1, TuChoi = 2 }
