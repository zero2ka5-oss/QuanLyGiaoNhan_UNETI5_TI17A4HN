using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;
[SessionAuthorize]
public class PhanCongGiaoHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index()
    {
        IQueryable<PhanCongGiaoHang> query = context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).Include(x => x.NhanVienGiaoHang).Include(x => x.PhuongTien).AsNoTracking();
        if (!IsCoordinator) query = query.Where(x => x.NhanVienGiaoHang!.MaTaiKhoan == CurrentAccountId);
        return View(await query.ToListAsync());
    }
    public async Task<IActionResult> Details(int? id) {
        if (id is null) return NotFound();
        var item = await context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).Include(x => x.NhanVienGiaoHang).Include(x => x.PhuongTien).AsNoTracking().FirstOrDefaultAsync(x => x.MaPhanCong == id);
        return item is null ? NotFound() : View(item);
    }
    public async Task<IActionResult> Create() {
        if (!IsCoordinator) return Forbid();
        await LoadSelections();
        return View(new PhanCongGiaoHang());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhanCongGiaoHang model) {
        if (!IsCoordinator) return Forbid();
        if (!ModelState.IsValid) {
            await LoadSelections();
            return View(model);
        }
        context.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int? id) {
        if (!IsCoordinator) return Forbid();
        if (id is null) return NotFound();
        var item = await context.PhanCongGiaoHangs.FindAsync(id);
        if (item is null) return NotFound();
        await LoadSelections();
        return View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PhanCongGiaoHang model) {
        if (!IsCoordinator) return Forbid();
        if (id != model.MaPhanCong) return NotFound();
        if (!ModelState.IsValid) {
            await LoadSelections();
            return View(model);
        }
        context.Update(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int? id) {
        if (!IsCoordinator) return Forbid();
        if (id is null) return NotFound();
        var item = await context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).AsNoTracking().FirstOrDefaultAsync(x => x.MaPhanCong == id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) {
        if (!IsCoordinator) return Forbid();
        var item = await context.PhanCongGiaoHangs.FindAsync(id);
        if (item is not null) {
            context.Remove(item);
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    private async Task LoadSelections() {
        ViewBag.DonGiaoHangs = await context.DonGiaoHangs.Where(x => x.TrangThai == "Chờ phân công").ToListAsync();
        ViewBag.NhanVienGiaoHangs = await context.NhanVienGiaoHangs.Where(x => x.TrangThai == "Sẵn sàng").ToListAsync();
        ViewBag.PhuongTiens = await context.PhuongTiens.Where(x => x.TrangThai == "Sẵn sàng").ToListAsync();
    }
}
