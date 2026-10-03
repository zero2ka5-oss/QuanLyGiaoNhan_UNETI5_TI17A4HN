// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Quản lý loại hàng (CRUD, kiểm tra trùng tên, hệ số phụ thu >= 0, ngừng hoạt động).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriLoaiHangController — URL: /QuanTriLoaiHang/{Index|ChiTiet|TaoMoi|ChinhSua|DoiTrangThai|Xoa}
 * Quyền: Quản trị, Điều phối.
 * Thêm / sửa / đổi trạng thái / xóa loại hàng: chỉ Quản trị (Điều phối chỉ xem).
 * Xóa chỉ khi chưa có đơn dùng loại hàng; đã có đơn thì chuyển "Ngừng hoạt động" (không chọn được cho đơn mới).
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriLoaiHangController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiHoatDong? trangThai, int trang = 1)
    {
        var truyVan = db.LoaiHangs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) truyVan = truyVan.Where(l => l.TenLoaiHang.Contains(tuKhoa.Trim()));
        if (trangThai.HasValue) truyVan = truyVan.Where(l => l.TrangThai == trangThai);
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoDon = await db.DonGiaoHangs.GroupBy(d => d.MaLoaiHang).Select(g => new { g.Key, SoLuong = g.Count() })
                                             .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<LoaiHang>.TaoAsync(truyVan.OrderBy(l => l.HeSoPhuThu).ThenBy(l => l.TenLoaiHang), trang, 10));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var loai = await db.LoaiHangs.AsNoTracking().FirstOrDefaultAsync(l => l.MaLoaiHang == id);
        if (loai is null) return NotFound();
        ViewBag.DonGanDay = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.KhuVuc)
                                    .Where(d => d.MaLoaiHang == id).OrderByDescending(d => d.NgayTao).Take(10).ToListAsync();
        ViewBag.TongDon = await db.DonGiaoHangs.CountAsync(d => d.MaLoaiHang == id);
        ViewBag.TongKhoiLuong = await db.DonGiaoHangs.Where(d => d.MaLoaiHang == id).SumAsync(d => (decimal?)d.KhoiLuong) ?? 0;
        return View(loai);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public IActionResult TaoMoi() => View("BieuMau", new LoaiHang());

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(LoaiHang loai)
    {
        loai.TenLoaiHang = loai.TenLoaiHang?.Trim() ?? "";
        if (await db.LoaiHangs.AnyAsync(l => l.TenLoaiHang == loai.TenLoaiHang))
            ModelState.AddModelError(nameof(loai.TenLoaiHang), "Tên loại hàng đã tồn tại");
        if (!ModelState.IsValid) return View("BieuMau", loai);
        db.LoaiHangs.Add(loai);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm loại hàng {loai.TenLoaiHang}";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var loai = await db.LoaiHangs.FindAsync(id);
        return loai is null ? NotFound() : View("BieuMau", loai);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, LoaiHang loai)
    {
        if (id != loai.MaLoaiHang) return BadRequest();
        loai.TenLoaiHang = loai.TenLoaiHang?.Trim() ?? "";
        if (await db.LoaiHangs.AnyAsync(l => l.TenLoaiHang == loai.TenLoaiHang && l.MaLoaiHang != id))
            ModelState.AddModelError(nameof(loai.TenLoaiHang), "Tên loại hàng đã tồn tại");
        if (!ModelState.IsValid) return View("BieuMau", loai);
        db.LoaiHangs.Update(loai);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật {loai.TenLoaiHang}. Phí các đơn đã tạo không thay đổi.";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var loai = await db.LoaiHangs.FindAsync(id);
        if (loai is null) return NotFound();
        loai.TrangThai = loai.TrangThai == TrangThaiHoatDong.HoatDong ? TrangThaiHoatDong.NgungHoatDong : TrangThaiHoatDong.HoatDong;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{loai.TenLoaiHang}: {loai.TrangThai.TenHienThi().ToLower()}";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var loai = await db.LoaiHangs.FindAsync(id);
        if (loai is null) return NotFound();
        if (await db.DonGiaoHangs.AnyAsync(d => d.MaLoaiHang == id))
            TempData["Error"] = $"Không thể xóa '{loai.TenLoaiHang}' vì đã có đơn sử dụng. Hãy chuyển sang Ngừng hoạt động.";
        else
        {
            db.LoaiHangs.Remove(loai);
            await db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa loại hàng {loai.TenLoaiHang}";
        }
        return RedirectToAction(nameof(Index));
    }
}
