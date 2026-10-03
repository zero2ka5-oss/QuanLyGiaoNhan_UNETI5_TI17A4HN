// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Bảng điều khiển (Dashboard) của Quản trị / Điều phối.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriBangDieuKhienController — URL: /QuanTriBangDieuKhien (trang chủ của Quản trị / Điều phối)
 * Quyền: Quản trị, Điều phối.
 * Dashboard: số đơn theo trạng thái, nguồn lực sẵn sàng, doanh thu, đơn 14 ngày, việc cần xử lý, hoạt động gần đây.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriBangDieuKhienController(QuanLyGiaoNhanDbContext db) : Controller
{
    // ===================== DASHBOARD (mục 9.4) =====================

    public async Task<IActionResult> Index()
    {
        var theoTrangThai = await db.DonGiaoHangs.GroupBy(d => d.TrangThai)
                                    .Select(g => new { g.Key, SoLuong = g.Count() })
                                    .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        var nvTheoTrangThai = await db.NhanVienGiaoHangs.GroupBy(n => n.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                      .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        var ptTheoTrangThai = await db.PhuongTiens.GroupBy(p => p.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                      .ToDictionaryAsync(x => x.Key, x => x.SoLuong);

        var tuNgay = DateTime.Today.AddDays(-13);
        var theoNgay = await db.DonGiaoHangs.Where(d => d.NgayTao >= tuNgay)
                               .GroupBy(d => d.NgayTao.Date).Select(g => new { Ngay = g.Key, SoLuong = g.Count() }).ToListAsync();

        var vm = new DashboardVM
        {
            TongKhachHang = await db.KhachHangs.CountAsync(),
            TongDon = theoTrangThai.Values.Sum(),
            ChoPhanCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.ChoPhanCong),
            DangGiao = theoTrangThai.GetValueOrDefault(TrangThaiDon.DangGiao),
            GiaoThanhCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoThanhCong),
            GiaoKhongThanhCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoKhongThanhCong),
            HoanTat = theoTrangThai.GetValueOrDefault(TrangThaiDon.HoanTat),
            NhanVienSanSang = nvTheoTrangThai.GetValueOrDefault(TrangThaiNhanVien.SanSang),
            NhanVienDangGiao = nvTheoTrangThai.GetValueOrDefault(TrangThaiNhanVien.DangGiaoHang),
            PhuongTienSanSang = ptTheoTrangThai.GetValueOrDefault(TrangThaiPhuongTien.SanSang),
            PhuongTienDangSuDung = ptTheoTrangThai.GetValueOrDefault(TrangThaiPhuongTien.DangSuDung),
            TongPhiHopLe = HttpContext.Session.LaQuanTri()   // doanh thu: chỉ Quản trị xem
                ? await db.DonGiaoHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy).SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0 : 0,
            // Tiền shipper đã thu của người nhận (thu hộ + phí nếu người nhận trả) – chờ đối soát khi hoàn tất
            TienChoDoiSoat = await db.DonGiaoHangs.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong)
                .SumAsync(d => (decimal?)(d.TienThuHo + (d.NguoiTraPhi == NguoiTraPhi.NguoiNhan ? d.PhiVanChuyen : 0))) ?? 0,
            SoDonChoDoiSoat = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoThanhCong),
            NhanNgay = [.. Enumerable.Range(0, 14).Select(i => tuNgay.AddDays(i).ToString("dd/MM"))],
            SoDonTheoNgay = [.. Enumerable.Range(0, 14).Select(i => theoNgay.FirstOrDefault(x => x.Ngay == tuNgay.AddDays(i))?.SoLuong ?? 0)],
            TheoTrangThai = theoTrangThai,
            // Việc cần xử lý: đơn chờ phân công, giao thất bại cần xử lý lại, đơn giao thành công chờ hoàn tất
            DonCanXuLy = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.KhachHang)
                .Where(d => d.TrangThai == TrangThaiDon.ChoPhanCong || d.TrangThai == TrangThaiDon.GiaoKhongThanhCong
                         || d.TrangThai == TrangThaiDon.GiaoThanhCong)
                .OrderByDescending(d => d.TrangThai == TrangThaiDon.GiaoKhongThanhCong).ThenBy(d => d.NgayGiaoDuKien)
                .Take(8).ToListAsync(),
            HoatDongGanDay = await db.LichSuGiaoNhans.AsNoTracking().Include(l => l.DonGiaoHang).OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu)
                                     .Take(8).ToListAsync()
        };
        return View(vm);
    }
}
