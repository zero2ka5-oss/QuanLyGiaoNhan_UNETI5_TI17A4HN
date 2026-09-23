using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class KhuVucController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(await context.KhuVucs.AsNoTracking().ToListAsync());
    public async Task<IActionResult> Details(int? id) { if (id is null) return NotFound(); var item = await context.KhuVucs.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhuVuc == id); return item is null ? NotFound() : View(item); }
    public IActionResult Create() => View(new KhuVuc());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhuVuc model) { if (await context.KhuVucs.AnyAsync(x => x.TenKhuVuc == model.TenKhuVuc)) ModelState.AddModelError(nameof(model.TenKhuVuc), "Tên khu vực đã tồn tại."); if (!ModelState.IsValid) return View(model); context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int? id) { if (id is null) return NotFound(); var item = await context.KhuVucs.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KhuVuc model) { if (id != model.MaKhuVuc) return NotFound(); if (await context.KhuVucs.AnyAsync(x => x.TenKhuVuc == model.TenKhuVuc && x.MaKhuVuc != id)) ModelState.AddModelError(nameof(model.TenKhuVuc), "Tên khu vực đã tồn tại."); if (!ModelState.IsValid) return View(model); context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int? id) { if (id is null) return NotFound(); var item = await context.KhuVucs.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhuVuc == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var item = await context.KhuVucs.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
