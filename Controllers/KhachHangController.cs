using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize]
public class KhachHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(IsCoordinator ? await context.KhachHangs.AsNoTracking().ToListAsync() : await context.KhachHangs.AsNoTracking().Where(x => x.MaTaiKhoan == CurrentAccountId).ToListAsync());
    public async Task<IActionResult> Details(int? id) { if (id is null) return NotFound(); var item = await context.KhachHangs.FindAsync(id); return item is null || (!IsCoordinator && item.MaTaiKhoan != CurrentAccountId) ? Forbid() : View(item); }
    public async Task<IActionResult> Create() { if (!IsCoordinator) return Forbid(); ViewBag.TaiKhoans = await context.TaiKhoans.Where(x => x.VaiTro == "Khách hàng").ToListAsync(); return View(new KhachHang()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhachHang model) { if (!IsCoordinator) return Forbid(); if (!ModelState.IsValid) { ViewBag.TaiKhoans = await context.TaiKhoans.Where(x => x.VaiTro == "Khách hàng").ToListAsync(); return View(model); } context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.KhachHangs.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KhachHang model) { if (!IsCoordinator) return Forbid(); if (id != model.MaKhachHang) return NotFound(); if (!ModelState.IsValid) return View(model); context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhachHang == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { if (!IsCoordinator) return Forbid(); var item = await context.KhachHangs.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
