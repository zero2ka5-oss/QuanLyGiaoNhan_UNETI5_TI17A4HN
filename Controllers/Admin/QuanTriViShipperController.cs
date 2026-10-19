using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriViShipperController — URL: /QuanTriViShipper/{Index|ChiTiet|XacNhan|TuChoi|TatToan}
 * Quyền: Quản trị (kế toán đối soát).
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriViShipperController(QuanLyGiaoNhanDbContext db, ViXuLy viXuLy) : Controller
{
    public async Task<IActionResult> Index(LoaiGiaoDich? loai, TrangThaiGiaoDich? trangThai, int? maNhanVien, int trang = 1)
    {
        var truyVan = db.GiaoDichVis.AsNoTracking().Include(g => g.NhanVien).AsQueryable();
        if (loai.HasValue) truyVan = truyVan.Where(g => g.Loai == loai);
        if (trangThai.HasValue) truyVan = truyVan.Where(g => g.TrangThai == trangThai);
        if (maNhanVien.HasValue) truyVan = truyVan.Where(g => g.MaNhanVien == maNhanVien);

        var dsVi = await viXuLy.TinhTatCaViAsync();
        var cho = db.GiaoDichVis.Where(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan);
        ViewBag.DsNhanVien = dsVi.Select(v => v.NhanVien).ToList();

        return View(new QuanTriViVM
        {
            DsVi = dsVi,
            GiaoDich = await DanhSachTrang<GiaoDichVi>.TaoAsync(
                truyVan.OrderBy(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan ? 0 : 1).ThenByDescending(g => g.NgayTao).ThenByDescending(g => g.MaGiaoDich), trang, 25),
            Loai = loai, TrangThai = trangThai, MaNhanVien = maNhanVien,
            SoChoXacNhan = await cho.CountAsync(),
            TienNopChoXacNhan = await cho.Where(g => g.Loai == LoaiGiaoDich.NopTienThuHo).SumAsync(g => (decimal?)g.SoTien) ?? 0,
            TienRutChoXacNhan = await cho.Where(g => g.Loai == LoaiGiaoDich.RutTien).SumAsync(g => (decimal?)g.SoTien) ?? 0
        });
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var gd = await db.GiaoDichVis.AsNoTracking().Include(g => g.NhanVien)
                         .Include(g => g.DonNop).ThenInclude(d => d.KhachHang)
                         .FirstOrDefaultAsync(g => g.MaGiaoDich == id);
        return gd is null ? NotFound() : View(gd);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id, string? quayLai)
    {
        var kq = await viXuLy.XacNhanAsync(id, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? lyDo)
    {
        var kq = await viXuLy.TuChoiAsync(id, lyDo, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TatToan(int maNhanVien, string? quayLai)
    {
        var kq = await viXuLy.TatToanAsync(maNhanVien, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(Index));
    }
}
