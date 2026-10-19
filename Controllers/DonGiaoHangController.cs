using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;
[SessionAuthorize]
public class DonGiaoHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index(string? search)
    {
        IQueryable<DonGiaoHang> query = context.DonGiaoHangs.Include(x => x.KhachHang).Include(x => x.LoaiHang).Include(x => x.KhuVuc).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.TenNguoiNhan.Contains(search) || x.DiaChiNhan.Contains(search));
        if (!IsCoordinator) {
            var customerId = await context.KhachHangs.Where(x => x.MaTaiKhoan == CurrentAccountId).Select(x => x.MaKhachHang).FirstOrDefaultAsync();
            query = query.Where(x => x.MaKhachHang == customerId);
        }
        ViewBag.Search = search;
        return View(await query.OrderByDescending(x => x.NgayTao).ToListAsync());
    }
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.DonGiaoHangs.Include(x => x.KhachHang).Include(x => x.LoaiHang).Include(x => x.KhuVuc).Include(x => x.LichSuGiaoNhans).AsNoTracking().FirstOrDefaultAsync(x => x.MaDon == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.KhachHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        return View(item);
    }
    public async Task<IActionResult> Create()
    {
        await LoadSelections();
        var model = new DonGiaoHang();
        if (!IsCoordinator) model.MaKhachHang = await context.KhachHangs.Where(x => x.MaTaiKhoan == CurrentAccountId).Select(x => x.MaKhachHang).FirstOrDefaultAsync();
        return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DonGiaoHang model)
    {
        if (!IsCoordinator) model.MaKhachHang = await context.KhachHangs.Where(x => x.MaTaiKhoan == CurrentAccountId).Select(x => x.MaKhachHang).FirstOrDefaultAsync();
        if (!ModelState.IsValid) {
            await LoadSelections();
            return View(model);
        }
        context.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.DonGiaoHangs.Include(x => x.KhachHang).FirstOrDefaultAsync(x => x.MaDon == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.KhachHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        await LoadSelections();
        return View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DonGiaoHang model)
    {
        if (id != model.MaDon) return NotFound();
        var existing = await context.DonGiaoHangs.Include(x => x.KhachHang).FirstOrDefaultAsync(x => x.MaDon == id);
        if (existing is null) return NotFound();
        if (!IsCoordinator && existing.KhachHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        if (!ModelState.IsValid) {
            await LoadSelections();
            return View(model);
        }
        context.Entry(existing).CurrentValues.SetValues(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.DonGiaoHangs.Include(x => x.KhachHang).AsNoTracking().FirstOrDefaultAsync(x => x.MaDon == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.KhachHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        return View(item);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await context.DonGiaoHangs.Include(x => x.KhachHang).FirstOrDefaultAsync(x => x.MaDon == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.KhachHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        context.Remove(item);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    private async Task LoadSelections()
    {
        ViewBag.KhachHangs = await context.KhachHangs.AsNoTracking().ToListAsync();
        ViewBag.LoaiHangs = await context.LoaiHangs.AsNoTracking().ToListAsync();
        ViewBag.KhuVucs = await context.KhuVucs.AsNoTracking().ToListAsync();
    }
}
