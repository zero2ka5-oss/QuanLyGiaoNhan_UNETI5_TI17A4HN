using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienVanHanhController — URL: /NhanVienVanHanh (Bàn làm việc vận hành của Nhân viên)
 * Quyền: Nhân viên (vận hành bưu cục), Quản trị viên.
 * 2 Nhiệm vụ trọng tâm:
 *   1. DUYỆT ĐƠN TẠI BƯU CỤC: Quét mã tiếp nhận, xuất kho, kiểm kho hàng lưu trú, chuyển hoàn.
 *   2. ĐIỀU HÀNH VẬN CHUYỂN: Phân công shipper lấy hàng tận nơi, chạy tuyến liên tỉnh, phát hàng khu vực.
 */
[YeuCauVaiTro(VaiTroNguoiDung.NhanVien, VaiTroNguoiDung.QuanTri)]
public class NhanVienVanHanhController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> Index(int? maBuuCuc)
    {
        int? ma = HttpContext.Session.MaBuuCuc() ?? maBuuCuc;
        var bc = ma is null
            ? await db.BuuCucs.AsNoTracking().OrderBy(b => b.MaBuuCuc).FirstOrDefaultAsync()
            : await db.BuuCucs.AsNoTracking().FirstOrDefaultAsync(b => b.MaBuuCuc == ma);

        if (bc is null) return View("ChuaCoBuuCuc");

        ViewBag.BuuCuc = bc;
        ViewBag.KhoaBuuCuc = HttpContext.Session.MaBuuCuc() is not null;
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();

        int idBc = bc.MaBuuCuc;
        var don = db.DonGiaoHangs.AsNoTracking();

        // 1. Nhóm Duyệt đơn bưu cục
        ViewBag.SoChoKhachMangToi = await don.CountAsync(d => d.TrangThai == TrangThaiDon.DaTao && d.MaBuuCucGui == idBc);
        ViewBag.SoChoShipperLayVe = await don.CountAsync(d => d.TrangThai == TrangThaiDon.DaLayTanNoi && d.MaBuuCucGui == idBc);
        ViewBag.SoDangOBuuCuc = await don.CountAsync(d => d.MaBuuCucHienTai == idBc && d.TrangThai != TrangThaiDon.DaHuy);
        ViewBag.SoDangChuyenDen = await don.CountAsync(d => d.MaBuuCucNhan == idBc && (d.TrangThai == TrangThaiDon.DaLayLienTinh || d.TrangThai == TrangThaiDon.DangTrungChuyen));
        ViewBag.SoChoHoanHang = await DonHangXuLy.DonChoHoanHang(don).CountAsync(d => d.MaBuuCucHienTai == idBc);

        // 2. Nhóm Điều hành & Phân công vận chuyển
        var taiBuuCuc = don.Where(d => d.MaBuuCucHienTai == idBc);
        ViewBag.SoChoPhanLayHang = await don.CountAsync(d => d.TrangThai == TrangThaiDon.ChoLayTanNoi && d.MaBuuCucGui == idBc && !d.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)));
        ViewBag.SoChoPhanLienTinh = await taiBuuCuc.CountAsync(d => d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui && d.MaBuuCucGui != d.MaBuuCucNhan);
        ViewBag.SoChoPhanKhuVuc = await taiBuuCuc.CountAsync(d => d.TrangThai == TrangThaiDon.DaDenBuuCucNhan);
        ViewBag.SoDangVanChuyen = await xuLy.PhanCongHieuLuc().CountAsync(p => p.NhanVien!.MaBuuCuc == idBc);

        // Đội ngũ Shipper tại bưu cục
        ViewBag.SoShipperSanSang = await db.NhanVienGiaoHangs.CountAsync(n => n.MaBuuCuc == idBc && n.TrangThai == TrangThaiNhanVien.SanSang);
        ViewBag.SoShipperDangGiao = await db.NhanVienGiaoHangs.CountAsync(n => n.MaBuuCuc == idBc && n.TrangThai == TrangThaiNhanVien.DangGiaoHang);

        // Các đơn hàng cần xử lý gấp
        var donCanXuLy = await don
            .Include(d => d.KhachHang)
            .Include(d => d.KhuVuc)
            .Include(d => d.BuuCucGui)
            .Include(d => d.BuuCucNhan)
            .Where(d => (d.MaBuuCucHienTai == idBc || d.MaBuuCucGui == idBc || d.MaBuuCucNhan == idBc) &&
                        (d.TrangThai == TrangThaiDon.ChoLayTanNoi ||
                         d.TrangThai == TrangThaiDon.DaLayTanNoi ||
                         d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui ||
                         d.TrangThai == TrangThaiDon.DaDenBuuCucNhan ||
                         d.TrangThai == TrangThaiDon.GiaoKhongThanhCong ||
                         d.TrangThai == TrangThaiDon.HenGiaoLai))
            .OrderByDescending(d => d.NgayTao)
            .Take(8)
            .ToListAsync();

        return View(donCanXuLy);
    }
}
