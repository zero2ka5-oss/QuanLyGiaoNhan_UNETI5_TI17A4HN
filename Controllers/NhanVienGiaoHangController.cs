using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class NhanVienGiaoHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(await context.NhanVienGiaoHangs.Include(x => x.TaiKhoan).AsNoTracking().ToListAsync());
    public async Task<IActionResult> Details(int? id) { if (id is null) return NotFound(); var item = await context.NhanVienGiaoHangs.Include(x => x.TaiKhoan).AsNoTracking().FirstOrDefaultAsync(x => x.MaNhanVien == id); return item is null ? NotFound() : View(item); }
    public async Task<IActionResult> Create() { ViewBag.TaiKhoans = await context.TaiKhoans.Where(x => x.VaiTro == "Nhân viên giao hàng" || x.VaiTro == "Admin").ToListAsync(); return View(new NhanVienGiaoHang()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NhanVienGiaoHang model) { if (!ModelState.IsValid) { ViewBag.TaiKhoans = await context.TaiKhoans.ToListAsync(); return View(model); } context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Edit(int? id) { if (id is null) return NotFound(); var item = await context.NhanVienGiaoHangs.FindAsync(id); return item is null ? NotFound() : View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NhanVienGiaoHang model) { if (id != model.MaNhanVien) return NotFound(); if (!ModelState.IsValid) return View(model); context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
    public async Task<IActionResult> Delete(int? id) { if (id is null) return NotFound(); var item = await context.NhanVienGiaoHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNhanVien == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { var item = await context.NhanVienGiaoHangs.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
}
