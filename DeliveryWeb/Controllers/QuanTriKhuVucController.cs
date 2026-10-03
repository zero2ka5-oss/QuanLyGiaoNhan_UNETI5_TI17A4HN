// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Quản lý khu vực giao (CRUD, kiểm tra trùng tên, phí cơ bản >= 0, ngừng nhận đơn).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriKhuVucController — URL: /QuanTriKhuVuc/{Index|ChiTiet|TaoMoi|ChinhSua|DoiTrangThai|Xoa}
 * Quyền: Quản trị, Điều phối.
 * Thêm / sửa / đổi trạng thái / xóa khu vực: chỉ Quản trị (Điều phối chỉ xem).
 * Khu vực ngừng hoạt động không nhận đơn mới; xóa chỉ khi chưa có đơn và chưa có nhân viên phụ trách.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriKhuVucController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiHoatDong? trangThai, int trang = 1)
    {
        var truyVan = db.KhuVucs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) truyVan = truyVan.Where(k => k.TenKhuVuc.Contains(tuKhoa.Trim()));
        if (trangThai.HasValue) truyVan = truyVan.Where(k => k.TrangThai == trangThai);
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoDon = await db.DonGiaoHangs.GroupBy(d => d.MaKhuVuc).Select(g => new { g.Key, SoLuong = g.Count() })
                                             .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.SoNhanVien = await db.NhanVienGiaoHangs.Where(n => n.MaKhuVucPhuTrach != null)
                                     .GroupBy(n => n.MaKhuVucPhuTrach!.Value).Select(g => new { g.Key, SoLuong = g.Count() })
                                     .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<KhuVuc>.TaoAsync(truyVan.OrderBy(k => k.PhiCoBan), trang, 10));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var khuVuc = await db.KhuVucs.AsNoTracking().Include(k => k.NhanViens).FirstOrDefaultAsync(k => k.MaKhuVuc == id);
        if (khuVuc is null) return NotFound();
        ViewBag.DonGanDay = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.LoaiHang)
                                    .Where(d => d.MaKhuVuc == id).OrderByDescending(d => d.NgayTao).Take(10).ToListAsync();
        ViewBag.TongDon = await db.DonGiaoHangs.CountAsync(d => d.MaKhuVuc == id);
        ViewBag.TongPhi = await db.DonGiaoHangs.Where(d => d.MaKhuVuc == id && d.TrangThai != TrangThaiDon.DaHuy)
                                  .SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0;
        return View(khuVuc);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public IActionResult TaoMoi() => View("BieuMau", new KhuVuc());

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(KhuVuc khuVuc)
    {
        khuVuc.TenKhuVuc = khuVuc.TenKhuVuc?.Trim() ?? "";
        if (await db.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc))
            ModelState.AddModelError(nameof(khuVuc.TenKhuVuc), "Tên khu vực đã tồn tại");
        if (!ModelState.IsValid) return View("BieuMau", khuVuc);
        db.KhuVucs.Add(khuVuc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khu vực {khuVuc.TenKhuVuc}";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        return khuVuc is null ? NotFound() : View("BieuMau", khuVuc);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, KhuVuc khuVuc)
    {
        if (id != khuVuc.MaKhuVuc) return BadRequest();
        khuVuc.TenKhuVuc = khuVuc.TenKhuVuc?.Trim() ?? "";
        if (await db.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc && k.MaKhuVuc != id))
            ModelState.AddModelError(nameof(khuVuc.TenKhuVuc), "Tên khu vực đã tồn tại");
        if (!ModelState.IsValid) return View("BieuMau", khuVuc);
        db.KhuVucs.Update(khuVuc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật {khuVuc.TenKhuVuc}. Phí các đơn đã tạo không thay đổi.";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        if (khuVuc is null) return NotFound();
        khuVuc.TrangThai = khuVuc.TrangThai == TrangThaiHoatDong.HoatDong ? TrangThaiHoatDong.NgungHoatDong : TrangThaiHoatDong.HoatDong;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{khuVuc.TenKhuVuc}: {khuVuc.TrangThai.TenHienThi().ToLower()}";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var khuVuc = await db.KhuVucs.FindAsync(id);
        if (khuVuc is null) return NotFound();
        if (await db.DonGiaoHangs.AnyAsync(d => d.MaKhuVuc == id) || await db.NhanVienGiaoHangs.AnyAsync(n => n.MaKhuVucPhuTrach == id))
            TempData["Error"] = $"Không thể xóa '{khuVuc.TenKhuVuc}' vì đã có đơn hoặc nhân viên phụ trách. Hãy chuyển sang Ngừng hoạt động.";
        else
        {
            db.KhuVucs.Remove(khuVuc);
            await db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa khu vực {khuVuc.TenKhuVuc}";
        }
        return RedirectToAction(nameof(Index));
    }
}
