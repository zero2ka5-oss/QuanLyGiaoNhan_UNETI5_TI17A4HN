// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Tài khoản, đăng nhập/đăng xuất, phân quyền, quản lý loại hàng và khu vực giao.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;
[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class LoaiHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index() => View(await context.LoaiHangs.AsNoTracking().ToListAsync());
    public async Task<IActionResult> Details(int? id) {
        if (id is null) return NotFound();
        var item = await context.LoaiHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaLoaiHang == id);
        return item is null ? NotFound() : View(item);
    }
    public IActionResult Create() => View(new LoaiHang());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LoaiHang model) {
        if (await context.LoaiHangs.AnyAsync(x => x.TenLoaiHang == model.TenLoaiHang)) ModelState.AddModelError(nameof(model.TenLoaiHang), "Tên loại hàng đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        context.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int? id) {
        if (id is null) return NotFound();
        var item = await context.LoaiHangs.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LoaiHang model) {
        if (id != model.MaLoaiHang) return NotFound();
        if (await context.LoaiHangs.AnyAsync(x => x.TenLoaiHang == model.TenLoaiHang && x.MaLoaiHang != id)) ModelState.AddModelError(nameof(model.TenLoaiHang), "Tên loại hàng đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        context.Update(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Delete(int? id) {
        if (id is null) return NotFound();
        var item = await context.LoaiHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaLoaiHang == id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await context.LoaiHangs.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));
        if (await context.DonGiaoHangs.AnyAsync(x => x.MaLoaiHang == id))
        {
            TempData["ErrorMessage"] = "Không thể xóa loại hàng đã được sử dụng trong đơn giao. Hãy chuyển trạng thái sang Ngừng hoạt động.";
            return RedirectToAction(nameof(Index));
        }
        context.LoaiHangs.Remove(item);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa loại hàng.";
        return RedirectToAction(nameof(Index));
    }
}
