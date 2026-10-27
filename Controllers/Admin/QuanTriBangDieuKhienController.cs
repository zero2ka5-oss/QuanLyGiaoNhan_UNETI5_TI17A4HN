using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriBangDieuKhienController — URL: /QuanTriBangDieuKhien (trang chủ của Quản trị hệ thống)
 * Quyền: Quản trị.
 * Dashboard: số đơn theo trạng thái, nguồn lực sẵn sàng, doanh thu, đơn 14 ngày, việc cần xử lý, hoạt động gần đây.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriBangDieuKhienController(QuanLyGiaoNhanDbContext db) : Controller
{
    // ===================== DASHBOARD =====================

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
            // Hàng đang ở bưu cục chờ phân chặng (liên tỉnh / khu vực)
            ChoPhanCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.DaNhanTaiBuuCucGui) + theoTrangThai.GetValueOrDefault(TrangThaiDon.DaDenBuuCucNhan),
            DangTrungChuyen = theoTrangThai.GetValueOrDefault(TrangThaiDon.DangTrungChuyen) + theoTrangThai.GetValueOrDefault(TrangThaiDon.DaLayLienTinh),
            ChoTiepNhan = theoTrangThai.GetValueOrDefault(TrangThaiDon.DaTao),
            DangGiao = theoTrangThai.GetValueOrDefault(TrangThaiDon.DangGiao),
            GiaoThanhCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoThanhCong),
            GiaoKhongThanhCong = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoKhongThanhCong),
            HoanTat = theoTrangThai.GetValueOrDefault(TrangThaiDon.HoanTat),
            NhanVienSanSang = nvTheoTrangThai.GetValueOrDefault(TrangThaiNhanVien.SanSang),
            NhanVienDangGiao = nvTheoTrangThai.GetValueOrDefault(TrangThaiNhanVien.DangGiaoHang),
            PhuongTienSanSang = ptTheoTrangThai.GetValueOrDefault(TrangThaiPhuongTien.SanSang),
            PhuongTienDangSuDung = ptTheoTrangThai.GetValueOrDefault(TrangThaiPhuongTien.DangSuDung),
            TongPhiHopLe = await db.DonGiaoHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy).SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0,
            // Tiền shipper đã thu của người nhận (thu hộ + phí nếu người nhận trả) – chờ đối soát khi hoàn tất
            TienChoDoiSoat = await db.DonGiaoHangs.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong)
                .SumAsync(d => (decimal?)(d.TienThuHo + (d.NguoiTraPhi == NguoiTraPhi.NguoiNhan ? d.PhiVanChuyen : 0))) ?? 0,
            SoDonChoDoiSoat = theoTrangThai.GetValueOrDefault(TrangThaiDon.GiaoThanhCong),
            SoDonChoXacNhanNop = await db.DonGiaoHangs.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoThanhCong && d.MaGiaoDichNop != null),
            NhanNgay = [.. Enumerable.Range(0, 14).Select(i => tuNgay.AddDays(i).ToString("dd/MM"))],
            SoDonTheoNgay = [.. Enumerable.Range(0, 14).Select(i => theoNgay.FirstOrDefault(x => x.Ngay == tuNgay.AddDays(i))?.SoLuong ?? 0)],
            TheoTrangThai = theoTrangThai,
            // Việc cần xử lý: đơn chờ phân công, giao thất bại cần xử lý lại, đơn giao thành công chờ hoàn tất, đơn hủy chờ chuyển hoàn
            DonCanXuLy = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.KhachHang).Include(d => d.BuuCucHienTai)
                .Where(d => d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || d.TrangThai == TrangThaiDon.DaDenBuuCucNhan
                         || d.TrangThai == TrangThaiDon.GiaoKhongThanhCong
                         || d.TrangThai == TrangThaiDon.GiaoThanhCong
                         || (d.TrangThai == TrangThaiDon.DaHuy && d.NgayHoanHang == null && d.MaBuuCucHienTai != null))
                .OrderByDescending(d => d.TrangThai == TrangThaiDon.GiaoKhongThanhCong || d.TrangThai == TrangThaiDon.DaHuy).ThenBy(d => d.NgayGiaoDuKien)
                .Take(8).ToListAsync(),
            HoatDongGanDay = await db.LichSuGiaoNhans.AsNoTracking().Include(l => l.DonGiaoHang).OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu)
                                     .Take(8).ToListAsync()
        };
        return View(vm);
    }
}
