using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriKhuVucController — URL: /QuanTriKhuVuc/{Index|ChiTiet|TaoMoi|ChinhSua|DoiTrangThai|Xoa}
 * Quyền: Quản trị.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriKhuVucController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiHoatDong? trangThai, int trang = 1)
    {
        var truyVan = db.KhuVucs.AsNoTracking().Include(k => k.BuuCuc).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) truyVan = truyVan.Where(k => k.TenKhuVuc.Contains(tuKhoa.Trim()));
        if (trangThai.HasValue) truyVan = truyVan.Where(k => k.TrangThai == trangThai);
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoDon = await db.DonGiaoHangs.GroupBy(d => d.MaKhuVuc).Select(g => new { g.Key, SoLuong = g.Count() })
                                             .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.SoNhanVien = await db.NhanVienGiaoHangs.Where(n => n.MaKhuVucPhuTrach != null)
                                     .GroupBy(n => n.MaKhuVucPhuTrach!.Value).Select(g => new { g.Key, SoLuong = g.Count() })
                                     .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<KhuVuc>.TaoAsync(truyVan.OrderBy(k => k.TenKhuVuc), trang, 20));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var khuVuc = await db.KhuVucs.AsNoTracking().Include(k => k.NhanViens).FirstOrDefaultAsync(k => k.MaKhuVuc == id);
        if (khuVuc is null) return NotFound();
        ViewBag.DonGanDay = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.LoaiHang)
                                    .Where(d => d.MaKhuVuc == id).OrderByDescending(d => d.NgayTao).Take(10).ToListAsync();
        ViewBag.TongDon = await db.DonGiaoHangs.CountAsync(d => d.MaKhuVuc == id);
        ViewBag.TongPhi = await db.DonGiaoHangs.Where(d => d.MaKhuVuc == id && d.TrangThai != TrangThaiDon.DaHuy)
                                  .SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0;
        return View(khuVuc);
    }

    public async Task<IActionResult> TaoMoi()
    {
        await NapBuuCucAsync();
        return View("BieuMau", new KhuVuc());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(KhuVuc khuVuc)
    {
        khuVuc.TenKhuVuc = khuVuc.TenKhuVuc?.Trim() ?? "";
        if (await db.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc))
            ModelState.AddModelError(nameof(khuVuc.TenKhuVuc), "Tên khu vực đã tồn tại");
        if (khuVuc.MaBuuCuc is null) ModelState.AddModelError(nameof(khuVuc.MaBuuCuc), "Chọn bưu cục phụ trách giao hàng cho khu vực");
        if (!ModelState.IsValid) { await NapBuuCucAsync(); return View("BieuMau", khuVuc); }
        db.KhuVucs.Add(khuVuc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khu vực {khuVuc.TenKhuVuc}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        if (khuVuc is null) return NotFound();
        await NapBuuCucAsync();
        return View("BieuMau", khuVuc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, KhuVuc khuVuc)
    {
        if (id != khuVuc.MaKhuVuc) return BadRequest();
        khuVuc.TenKhuVuc = khuVuc.TenKhuVuc?.Trim() ?? "";
        if (await db.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc && k.MaKhuVuc != id))
            ModelState.AddModelError(nameof(khuVuc.TenKhuVuc), "Tên khu vực đã tồn tại");
        if (khuVuc.MaBuuCuc is null) ModelState.AddModelError(nameof(khuVuc.MaBuuCuc), "Chọn bưu cục phụ trách giao hàng cho khu vực");
        if (!ModelState.IsValid) { await NapBuuCucAsync(); return View("BieuMau", khuVuc); }
        db.KhuVucs.Update(khuVuc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật {khuVuc.TenKhuVuc}. Phí các đơn đã tạo giữ nguyên (chỉ báo giá lại khi đơn được sửa khu vực / loại hàng / khối lượng).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        if (khuVuc is null) return NotFound();
        khuVuc.TrangThai = khuVuc.TrangThai == TrangThaiHoatDong.HoatDong ? TrangThaiHoatDong.NgungHoatDong : TrangThaiHoatDong.HoatDong;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{khuVuc.TenKhuVuc}: {khuVuc.TrangThai.TenHienThi().ToLower()}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        if (khuVuc is null) return NotFound();
        if (await db.DonGiaoHangs.AnyAsync(d => d.MaKhuVuc == id) || await db.NhanVienGiaoHangs.AnyAsync(n => n.MaKhuVucPhuTrach == id))
            TempData["Error"] = $"Không thể xóa '{khuVuc.TenKhuVuc}' vì đã có đơn hoặc nhân viên phụ trách. Hãy chuyển sang Ngừng hoạt động.";
        else
        {
            db.KhuVucs.Remove(khuVuc);
            await db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa khu vực {khuVuc.TenKhuVuc}";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task NapBuuCucAsync() =>
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong)
                                           .OrderBy(b => b.TinhThanh).ThenBy(b => b.TenBuuCuc).ToListAsync();
}
