using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize]
public class LichSuGiaoNhanController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(await context.LichSuGiaoNhans.Include(x => x.DonGiaoHang).AsNoTracking().OrderByDescending(x => x.ThoiGian).ToListAsync());
    public async Task<IActionResult> Details(int? id) { if (id is null) return NotFound(); var item = await context.LichSuGiaoNhans.Include(x => x.DonGiaoHang).AsNoTracking().FirstOrDefaultAsync(x => x.MaLichSu == id); return item is null ? NotFound() : View(item); }
    public async Task<IActionResult> Create() { if (!IsCoordinator) return Forbid(); ViewBag.DonGiaoHangs = await context.DonGiaoHangs.ToListAsync(); return View(new LichSuGiaoNhan()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LichSuGiaoNhan model) { if (!IsCoordinator) return Forbid(); if (!ModelState.IsValid) { ViewBag.DonGiaoHangs = await context.DonGiaoHangs.ToListAsync(); return View(model); } context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.LichSuGiaoNhans.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LichSuGiaoNhan model) { if (!IsCoordinator) return Forbid(); if (id != model.MaLichSu) return NotFound(); if (!ModelState.IsValid) return View(model); context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.LichSuGiaoNhans.AsNoTracking().FirstOrDefaultAsync(x => x.MaLichSu == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { if (!IsCoordinator) return Forbid(); var item = await context.LichSuGiaoNhans.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
