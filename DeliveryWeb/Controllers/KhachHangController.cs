// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Controller Quản lý khách hàng

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class KhachHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index()
    {
        var danhSach = await context.KhachHangs
            .AsNoTracking()
            .Include(x => x.TaiKhoan)
            .OrderByDescending(x => x.MaKhachHang)
            .ToListAsync();

        return View(danhSach);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
            return NotFound();

        var khachHang = await context.KhachHangs
            .AsNoTracking()
            .Include(x => x.TaiKhoan)
            .FirstOrDefaultAsync(x => x.MaKhachHang == id);

        if (khachHang is null)
            return NotFound();

        return View(khachHang);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.TaiKhoans = await context.TaiKhoans
            .AsNoTracking()
            .Where(x => x.KhachHang == null)
            .OrderBy(x => x.HoTen)
            .ToListAsync();

        return View(new KhachHang());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KhachHang model)
    {
        var taiKhoan = await context.TaiKhoans
            .FirstOrDefaultAsync(x => x.MaTaiKhoan == model.MaTaiKhoan);

        if (taiKhoan is null)
        {
            ModelState.AddModelError(
                nameof(model.MaTaiKhoan),
                "Tài khoản không tồn tại.");
        }
        else if (await context.KhachHangs
            .AnyAsync(x => x.MaTaiKhoan == model.MaTaiKhoan))
        {
            ModelState.AddModelError(
                nameof(model.MaTaiKhoan),
                "Tài khoản này đã được gán cho khách hàng.");
        }

        if (await context.KhachHangs
            .AnyAsync(x => x.Email == model.Email))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email khách hàng đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TaiKhoans = await context.TaiKhoans
                .AsNoTracking()
                .Where(x => x.KhachHang == null ||
                            x.MaTaiKhoan == model.MaTaiKhoan)
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            return View(model);
        }

        context.KhachHangs.Add(model);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return NotFound();

        var khachHang = await context.KhachHangs.FindAsync(id);

        if (khachHang is null)
            return NotFound();

        ViewBag.TaiKhoans = await context.TaiKhoans
            .AsNoTracking()
            .Where(x => x.KhachHang == null ||
                        x.MaTaiKhoan == khachHang.MaTaiKhoan)
            .OrderBy(x => x.HoTen)
            .ToListAsync();

        return View(khachHang);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KhachHang model)
    {
        if (id != model.MaKhachHang)
            return NotFound();

        var taiKhoan = await context.TaiKhoans
            .FirstOrDefaultAsync(x => x.MaTaiKhoan == model.MaTaiKhoan);

        if (taiKhoan is null)
        {
            ModelState.AddModelError(
                nameof(model.MaTaiKhoan),
                "Tài khoản không tồn tại.");
        }
        else if (await context.KhachHangs.AnyAsync(
            x => x.MaTaiKhoan == model.MaTaiKhoan &&
                 x.MaKhachHang != model.MaKhachHang))
        {
            ModelState.AddModelError(
                nameof(model.MaTaiKhoan),
                "Tài khoản này đã được gán cho khách hàng khác.");
        }

        if (await context.KhachHangs.AnyAsync(
            x => x.Email == model.Email &&
                 x.MaKhachHang != model.MaKhachHang))
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email khách hàng đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.TaiKhoans = await context.TaiKhoans
                .AsNoTracking()
                .Where(x => x.KhachHang == null ||
                            x.MaTaiKhoan == model.MaTaiKhoan)
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            return View(model);
        }

        context.KhachHangs.Update(model);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
            return NotFound();

        var khachHang = await context.KhachHangs
            .AsNoTracking()
            .Include(x => x.TaiKhoan)
            .FirstOrDefaultAsync(x => x.MaKhachHang == id);

        if (khachHang is null)
            return NotFound();

        return View(khachHang);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var khachHang = await context.KhachHangs.FindAsync(id);

        if (khachHang is not null)
        {
            var coDonHang = await context.DonGiaoHangs
                .AnyAsync(x => x.MaKhachHang == id);

            if (coDonHang)
            {
                TempData["Error"] =
                    "Không thể xóa khách hàng vì khách hàng đã có đơn giao hàng.";

                return RedirectToAction(nameof(Index));
            }

            context.KhachHangs.Remove(khachHang);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}