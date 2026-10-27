using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriBuuCucController — URL: /QuanTriBuuCuc/{Index|ChiTiet|TaoMoi|ChinhSua|DoiTrangThai|Xoa}
 * Quyền: Quản trị.
 * Bưu cục: nơi tiếp nhận, tập kết, phân loại, bàn giao đơn.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriBuuCucController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa = null, int trang = 1)
    {
        var truyVan = db.BuuCucs.AsNoTracking().Include(b => b.KhuVucs).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(b => b.TenBuuCuc.Contains(tk) || b.TinhThanh.Contains(tk) || (b.DiaChi != null && b.DiaChi.Contains(tk)));
        }
        truyVan = truyVan.OrderBy(b => b.TinhThanh).ThenBy(b => b.TenBuuCuc);
        var ketQua = await DanhSachTrang<BuuCuc>.TaoAsync(truyVan, trang, 25);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.SoShipper = await db.NhanVienGiaoHangs.Where(n => n.MaBuuCuc != null && n.TrangThai != TrangThaiNhanVien.NgungHoatDong)
            .GroupBy(n => new { n.MaBuuCuc, n.LoaiShipper }).Select(g => new { g.Key.MaBuuCuc, g.Key.LoaiShipper, SoLuong = g.Count() })
            .ToDictionaryAsync(x => (x.MaBuuCuc!.Value, x.LoaiShipper), x => x.SoLuong);
        ViewBag.SoDonTaiBuuCuc = await db.DonGiaoHangs.Where(d => d.MaBuuCucHienTai != null)
            .GroupBy(d => d.MaBuuCucHienTai!.Value).Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.SoNhanVienBuuCuc = await db.TaiKhoans.Where(t => t.VaiTro == VaiTroNguoiDung.BuuCuc && t.MaBuuCuc != null)
            .GroupBy(t => t.MaBuuCuc!.Value).Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(ketQua);
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var bc = await db.BuuCucs.AsNoTracking().Include(b => b.KhuVucs).FirstOrDefaultAsync(b => b.MaBuuCuc == id);
        if (bc is null) return NotFound();
        ViewBag.DsShipper = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.PhuongTien).Include(n => n.KhuVucPhuTrach)
                                    .Where(n => n.MaBuuCuc == id).OrderBy(n => n.LoaiShipper).ThenBy(n => n.HoTen).ToListAsync();
        ViewBag.DsNhanVienBuuCuc = await db.TaiKhoans.AsNoTracking().Where(t => t.VaiTro == VaiTroNguoiDung.BuuCuc && t.MaBuuCuc == id)
                                           .OrderBy(t => t.HoTen).ToListAsync();
        ViewBag.SoDonTaiBuuCuc = await db.DonGiaoHangs.CountAsync(d => d.MaBuuCucHienTai == id);
        ViewBag.SoDonDangDen = await db.DonGiaoHangs.CountAsync(d => d.MaBuuCucNhan == id
            && (d.TrangThai == TrangThaiDon.DaLayLienTinh || d.TrangThai == TrangThaiDon.DangTrungChuyen));
        return View(bc);
    }

    public IActionResult TaoMoi() => View("BieuMau", new BuuCuc());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(BuuCuc bc)
    {
        bc.TenBuuCuc = bc.TenBuuCuc?.Trim() ?? "";
        if (await db.BuuCucs.AnyAsync(b => b.TenBuuCuc == bc.TenBuuCuc)) ModelState.AddModelError(nameof(bc.TenBuuCuc), "Tên bưu cục đã tồn tại");
        if ((bc.ViDo is null) != (bc.KinhDo is null)) ModelState.AddModelError(nameof(bc.KinhDo), "Nhập đủ cả vĩ độ và kinh độ (hoặc bỏ trống cả hai)");
        if (!ModelState.IsValid) return View("BieuMau", bc);
        db.BuuCucs.Add(bc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm {bc.TenBuuCuc}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var bc = await db.BuuCucs.FindAsync(id);
        return bc is null ? NotFound() : View("BieuMau", bc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, BuuCuc bc)
    {
        if (id != bc.MaBuuCuc) return BadRequest();
        bc.TenBuuCuc = bc.TenBuuCuc?.Trim() ?? "";
        if (await db.BuuCucs.AnyAsync(b => b.TenBuuCuc == bc.TenBuuCuc && b.MaBuuCuc != id))
            ModelState.AddModelError(nameof(bc.TenBuuCuc), "Tên bưu cục đã tồn tại");
        if ((bc.ViDo is null) != (bc.KinhDo is null)) ModelState.AddModelError(nameof(bc.KinhDo), "Nhập đủ cả vĩ độ và kinh độ (hoặc bỏ trống cả hai)");
        if (!ModelState.IsValid) return View("BieuMau", bc);
        db.BuuCucs.Update(bc);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật {bc.TenBuuCuc}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var bc = await db.BuuCucs.FindAsync(id);
        if (bc is null) return NotFound();
        bc.TrangThai = bc.TrangThai == TrangThaiHoatDong.HoatDong ? TrangThaiHoatDong.NgungHoatDong : TrangThaiHoatDong.HoatDong;
        await db.SaveChangesAsync();
        int soDon = await db.DonGiaoHangs.CountAsync(d => d.MaBuuCucHienTai == id);
        TempData["Success"] = $"{bc.TenBuuCuc}: {bc.TrangThai.TenHienThi().ToLower()}" +
                              (bc.TrangThai == TrangThaiHoatDong.NgungHoatDong && soDon > 0 ? $" – còn {soDon} đơn đang ở bưu cục cần xử lý tiếp" : "");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var bc = await db.BuuCucs.FindAsync(id);
        if (bc is null) return NotFound();
        bool coLienKet = await db.KhuVucs.AnyAsync(k => k.MaBuuCuc == id) || await db.NhanVienGiaoHangs.AnyAsync(n => n.MaBuuCuc == id)
                      || await db.TaiKhoans.AnyAsync(t => t.MaBuuCuc == id)
                      || await db.DonGiaoHangs.AnyAsync(d => d.MaBuuCucGui == id || d.MaBuuCucNhan == id || d.MaBuuCucHienTai == id)
                      || await db.LichSuGiaoNhans.AnyAsync(l => l.MaBuuCuc == id)
                      || await db.ChuyenXes.AnyAsync(c => c.MaBuuCucGui == id || c.MaBuuCucNhan == id || c.MaBuuCucTrungGian == id);
        if (coLienKet)
            TempData["Error"] = $"Không thể xóa {bc.TenBuuCuc} vì đã gắn khu vực / shipper / nhân viên / đơn hàng / chuyến xe. Hãy chuyển sang Ngừng hoạt động.";
        else
        {
            try
            {
                db.BuuCucs.Remove(bc);
                await db.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa {bc.TenBuuCuc}";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = $"Không thể xóa {bc.TenBuuCuc} do có ràng buộc dữ liệu liên kết. Hãy chuyển sang Ngừng hoạt động.";
            }
        }
        return RedirectToAction(nameof(Index));
    }
}
