// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Áp phí vào đơn, ghi lịch sử, bảng giá (đã chạy);
//                     hoàn tất, tra cứu, thống kê theo tháng (khung, hoàn thiện ở tuần 3, 4, 6).

using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * DonHangXuLy (partial – Module 5)
 * Lớp DonHangXuLy được chia nhiều tệp theo module để mỗi thành viên commit đúng phần việc của mình;
 * khi biên dịch C# gộp lại thành một lớp duy nhất (cùng DbContext db, cùng hàm BayGio, DoiTrangThaiDonAsync...).
 * Các hàm khung trả kết quả tạm để các module khác của nhóm gọi được ngay từ đầu.
 */
public partial class DonHangXuLy
{
    // =====================================================================
    // TÍNH PHÍ
    // =====================================================================

    private static KetQuaTinhPhi ApDungPhi(DonGiaoHang don, KhuVuc khuVuc, LoaiHang loaiHang)
    {
        var phi = TinhPhiXuLy.TinhPhi(khuVuc.PhiCoBan, don.KhoiLuong, loaiHang.HeSoPhuThu);
        don.PhiCoBan = phi.PhiCoBan;
        don.PhuPhiKhoiLuong = phi.PhuPhiKhoiLuong;
        don.PhuPhiLoaiHang = phi.PhuPhiLoaiHang;
        don.PhiVanChuyen = phi.Tong;
        return phi;
    }

    // =====================================================================
    // HOÀN TẤT & ĐỐI SOÁT THU HỘ  (khung – hoàn thiện tuần 4)
    // =====================================================================

    public Task<KetQua> HoanTatAsync(int maDon, string nguoiThucHien, bool tuGiaoDichVi = false) =>
        Task.FromResult(KetQua.Loi("Chức năng hoàn tất đơn (Module 5) đang được phát triển"));

    // =====================================================================
    // LỊCH SỬ GIAO NHẬN
    // =====================================================================

    /// <summary>Một dòng lịch sử giao nhận: mọi thay đổi trạng thái đơn đều được ghi lại (ai, lúc nào, nội dung).</summary>
    private LichSuGiaoNhan TaoLichSu(TrangThaiDon? cu, TrangThaiDon moi, string noiDung, string nguoi, int maDon = 0) =>
        new() { MaDon = maDon, ThoiGian = BayGio(), TrangThaiCu = cu, TrangThaiMoi = moi, NoiDung = noiDung, NguoiThucHien = nguoi };

    // =====================================================================
    // THỐNG KÊ, BẢNG GIÁ, TRA CỨU  (khung – hoàn thiện tuần 2, 3, 6)
    // =====================================================================

    /// <summary>Khung: n tháng gần nhất, chưa có số liệu.</summary>
    private static Task<List<(string Thang, int SoDon, decimal Phi)>> PhiTheoThangAsync(IQueryable<DonGiaoHang> truyVan, int n)
    {
        var dau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(n - 1));
        return Task.FromResult(Enumerable.Range(0, n).Select(i => ($"T{dau.AddMonths(i).Month}", 0, 0m)).ToList());
    }

    public Task<(DonGiaoHang? Don, string? Loi)> TraCuuAsync(string? ma, string? sdt) =>
        Task.FromResult<(DonGiaoHang?, string?)>((null, "Chức năng tra cứu đơn đang được phát triển"));

    /// <summary>Dữ liệu khối ước lượng phí + bảng giá: khu vực, loại hàng đang hoạt động và khối lượng tối đa một đơn.</summary>
    public async Task<BangGiaVM> BangGiaAsync(bool laKhachHang) => new(
        await db.KhuVucs.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(k => k.PhiCoBan).ToListAsync(),
        await db.LoaiHangs.AsNoTracking().Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(l => l.HeSoPhuThu).ToListAsync(),
        await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong).MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0,
        laKhachHang);
}
