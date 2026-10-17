using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriCongNoController — URL: /QuanTriCongNo/{Index|Duyet|TuChoi|DuyetTatCa}
 * Quyền: Quản trị.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriCongNoController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> Index(string tab = "choduyet")
    {
        var truyVan = db.DonGiaoHangs.AsNoTracking()
            .Include(d => d.KhachHang)
            .Include(d => d.LoaiHang)
            .Include(d => d.KhuVuc)
            .Include(d => d.BuuCucGui)
            .Include(d => d.BuuCucNhan)
            .AsQueryable();

        var choDuyetRaw = await truyVan.Where(d => d.NgayYeuCauThanhToan != null && d.NgayThuPhiNguoiGui == null)
                                        .OrderByDescending(d => d.NgayYeuCauThanhToan)
                                        .ToListAsync();
        var choDuyet = choDuyetRaw.Where(d => d.PhiNguoiGuiPhaiTra > 0).ToList();

        var chuaYeuCauRaw = await truyVan.Where(d => d.NgayYeuCauThanhToan == null && (d.NgayThuPhiNguoiGui == null || d.NgayThuPhiHoan == null) &&
                                                     ((d.TrangThai == TrangThaiDon.HoanTat && d.NguoiTraPhi == NguoiTraPhi.NguoiGui && d.PhiVanChuyen > d.TienThuHo)
                                                      || (d.TrangThai == TrangThaiDon.DaHuy && d.NgayHoanHang != null && d.PhiVanChuyen > 0)
                                                      || (d.TrangThai == TrangThaiDon.DaHoanHang || d.TrangThai == TrangThaiDon.ChoChuyenHoan || d.TrangThai == TrangThaiDon.DangChuyenHoan || d.TrangThai == TrangThaiDon.DaVeBuuCucGui || d.TrangThai == TrangThaiDon.DangGiaoHoan)))
                                          .OrderByDescending(d => d.NgayHoanTat ?? d.NgayHoanHang ?? d.NgayTao)
                                          .ToListAsync();
        var chuaYeuCau = chuaYeuCauRaw.Where(d => d.PhiNguoiGuiPhaiTra > 0).ToList();

        var daThuRaw = await truyVan.Where(d => d.NgayThuPhiNguoiGui != null)
                                    .OrderByDescending(d => d.NgayThuPhiNguoiGui)
                                    .Take(100)
                                    .ToListAsync();
        var daThu = daThuRaw.Where(d => d.PhiNguoiGuiPhaiTra > 0).Take(50).ToList();

        ViewBag.Tab = tab;
        ViewBag.SoChoDuyet = choDuyet.Count;
        ViewBag.TienChoDuyet = choDuyet.Sum(d => d.PhiNguoiGuiPhaiTra);
        ViewBag.SoChuaYeuCau = chuaYeuCau.Count;
        ViewBag.TienChuaYeuCau = chuaYeuCau.Sum(d => d.PhiNguoiGuiPhaiTra);
        ViewBag.SoDaThu = daThu.Count;
        ViewBag.TienDaThu = daThu.Sum(d => d.PhiNguoiGuiPhaiTra);

        var danhSachHienThi = tab switch
        {
            "chuayeucau" => chuaYeuCau,
            "dathu" => daThu,
            _ => choDuyet
        };

        return View(danhSachHienThi);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id)
    {
        var don = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaDon == id);
        if (don is null) return NotFound();

        var kq = await xuLy.ThuPhiNguoiGuiAsync(id, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { tab = "choduyet" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? lyDo)
    {
        var kq = await xuLy.TuChoiThuPhiNguoiGuiAsync(id, HttpContext.Session.NguoiThucHien(), lyDo);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { tab = "choduyet" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DuyetTatCa()
    {
        var choDuyetRaw = await db.DonGiaoHangs.Where(d => d.NgayYeuCauThanhToan != null && d.NgayThuPhiNguoiGui == null).ToListAsync();
        var choDuyet = choDuyetRaw.Where(d => d.PhiNguoiGuiPhaiTra > 0).ToList();
        if (choDuyet.Count == 0)
        {
            TempData["Success"] = "Không có đơn hàng nào đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        decimal tongTien = choDuyet.Sum(d => d.PhiNguoiGuiPhaiTra);
        string nguoiThucHien = HttpContext.Session.NguoiThucHien();
        foreach (var don in choDuyet)
        {
            await xuLy.ThuPhiNguoiGuiAsync(don.MaDon, nguoiThucHien);
        }

        TempData["Success"] = $"Đã duyệt toàn bộ {choDuyet.Count} đơn chờ duyệt (tổng tiền cước {DinhDang.Tien(tongTien)}). Toàn bộ công nợ đã được gạch trừ!";
        return RedirectToAction(nameof(Index), new { tab = "choduyet" });
    }
}
