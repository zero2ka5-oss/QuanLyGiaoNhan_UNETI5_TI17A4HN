using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối", "Nhân viên giao hàng" })]
public class PhanCongGiaoHangController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index()
    {
        IQueryable<PhanCongGiaoHang> query = context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).Include(x => x.NhanVienGiaoHang).Include(x => x.PhuongTien).AsNoTracking();
        if (!IsCoordinator) query = query.Where(x => x.NhanVienGiaoHang!.MaTaiKhoan == CurrentAccountId);
        return View(await query.ToListAsync());
    }
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).Include(x => x.NhanVienGiaoHang).Include(x => x.PhuongTien).AsNoTracking().FirstOrDefaultAsync(x => x.MaPhanCong == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.NhanVienGiaoHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        return View(item);
    }
    public async Task<IActionResult> Create() { if (!IsCoordinator) return Forbid(); await LoadSelections(); return View(new PhanCongGiaoHang()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhanCongGiaoHang model)
    {
        if (!IsCoordinator) return Forbid();
        if (!ModelState.IsValid) { await LoadSelections(); return View(model); }
        var order = await context.DonGiaoHangs.FindAsync(model.MaDon);
        if (order is null || order.TrangThai != "Chờ phân công") return NotFound();
        context.Add(model);
        var oldStatus = order.TrangThai;
        order.TrangThai = "Đã phân công";
        context.LichSuGiaoNhans.Add(new LichSuGiaoNhan { MaDon = order.MaDon, TrangThaiCu = oldStatus, TrangThaiMoi = order.TrangThai, NoiDung = "Đơn đã được phân công cho nhân viên giao hàng.", NguoiThucHien = HttpContext.Session.GetString(SessionHelper.FullName) ?? "Điều phối viên" });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.PhanCongGiaoHangs.FindAsync(id); if (item is null) return NotFound(); await LoadSelections(); return View(item); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PhanCongGiaoHang model) { if (!IsCoordinator) return Forbid(); if (id != model.MaPhanCong) return NotFound(); if (!ModelState.IsValid) { await LoadSelections(); return View(model); } context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateDeliveryStatusViewModel model)
    {
        if (!HttpContext.Session.HasRole("Nhân viên giao hàng")) return Forbid();
        var assignment = await context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).Include(x => x.NhanVienGiaoHang).FirstOrDefaultAsync(x => x.MaPhanCong == model.MaPhanCong);
        if (assignment is null) return NotFound();
        if (assignment.NhanVienGiaoHang?.MaTaiKhoan != CurrentAccountId) return Forbid();

        var nextStatuses = assignment.TrangThai switch
        {
            "Đã phân công" => new[] { "Đã nhận hàng" },
            "Đã nhận hàng" => new[] { "Đang giao" },
            "Đang giao" => new[] { "Hoàn tất", "Giao thất bại" },
            _ => Array.Empty<string>()
        };
        if (!nextStatuses.Contains(model.TrangThai, StringComparer.Ordinal))
        {
            TempData["Error"] = "Trạng thái giao hàng không hợp lệ hoặc đã được cập nhật.";
            return RedirectToAction(nameof(Details), new { id = model.MaPhanCong });
        }

        assignment.TrangThai = model.TrangThai;
        assignment.GhiChu = model.GhiChu;
        if (model.TrangThai == "Đã nhận hàng") assignment.NgayNhanHang ??= DateTime.Now;
        if (model.TrangThai == "Đang giao") assignment.NgayBatDauGiao ??= DateTime.Now;
        if (model.TrangThai is "Hoàn tất" or "Giao thất bại") assignment.NgayKetThuc = DateTime.Now;
        if (assignment.DonGiaoHang is not null)
        {
            var orderOldStatus = assignment.DonGiaoHang.TrangThai;
            assignment.DonGiaoHang.TrangThai = model.TrangThai;
            context.LichSuGiaoNhans.Add(new LichSuGiaoNhan { MaDon = assignment.MaDon, TrangThaiCu = orderOldStatus, TrangThaiMoi = model.TrangThai, NoiDung = model.GhiChu ?? $"Nhân viên cập nhật trạng thái: {model.TrangThai}.", NguoiThucHien = HttpContext.Session.GetString(SessionHelper.FullName) ?? "Nhân viên giao hàng" });
        }
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = model.MaPhanCong });
    }
    public async Task<IActionResult> Delete(int? id) { if (!IsCoordinator) return Forbid(); if (id is null) return NotFound(); var item = await context.PhanCongGiaoHangs.Include(x => x.DonGiaoHang).AsNoTracking().FirstOrDefaultAsync(x => x.MaPhanCong == id); return item is null ? NotFound() : View(item); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id) { if (!IsCoordinator) return Forbid(); var item = await context.PhanCongGiaoHangs.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index)); }
    private async Task LoadSelections() { ViewBag.DonGiaoHangs = await context.DonGiaoHangs.Where(x => x.TrangThai == "Chờ phân công").ToListAsync(); ViewBag.NhanVienGiaoHangs = await context.NhanVienGiaoHangs.Where(x => x.TrangThai == "Sẵn sàng").ToListAsync(); ViewBag.PhuongTiens = await context.PhuongTiens.Where(x => x.TrangThai == "Sẵn sàng").ToListAsync(); }
}
