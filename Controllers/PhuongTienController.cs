using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class PhuongTienController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(await context.PhuongTiens.AsNoTracking().ToListAsync());
    public async Task<IActionResult> Details(int? id) { if (id is null) return NotFound(); var item = await context.PhuongTiens.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhuongTien == id); return item is null ? NotFound() : View(item); }
    public IActionResult Create() => View(new PhuongTien());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhuongTien model) { if (await context.PhuongTiens.AnyAsync(x => x.BienSo == model.BienSo)) ModelState.AddModelError(nameof(model.BienSo), "Biển số đã tồn tại."); if (!ModelState.IsValid) return View(model); context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int? id) { if (id is null) return NotFound(); var item = await context.PhuongTiens.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PhuongTien model) { if (id != model.MaPhuongTien) return NotFound(); if (await context.PhuongTiens.AnyAsync(x => x.BienSo == model.BienSo && x.MaPhuongTien != id)) ModelState.AddModelError(nameof(model.BienSo), "Biển số đã tồn tại."); if (!ModelState.IsValid) return View(model); context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int? id) { if (id is null) return NotFound(); var item = await context.PhuongTiens.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhuongTien == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var item = await context.PhuongTiens.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
