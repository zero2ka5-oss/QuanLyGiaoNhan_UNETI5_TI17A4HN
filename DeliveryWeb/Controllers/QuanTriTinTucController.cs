// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Quản lý tin tức trang chủ (đăng, sửa, ảnh bìa, ẩn / hiện, xóa, lọc theo chuyên mục và trạng thái).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriTinTucController — URL: /QuanTriTinTuc/{Index|TaoMoi|ChinhSua|DoiHienThi|Xoa}
 * Quyền: chỉ Quản trị (nội dung công khai của công ty).
 * Trạng thái hiển thị của bài: "hien" = đang hiển thị, "hen" = hẹn ngày đăng (ngày đăng sau hôm nay), "an" = đã ẩn.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriTinTucController(QuanLyGiaoNhanDbContext db, AnhXuLy anhXuLy) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, ChuyenMucTinTuc? chuyenMuc, string? trangThai, int trang = 1)
    {
        var homNay = DateTime.Today;
        var truyVan = db.TinTucs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(t => EF.Functions.Collate(t.TieuDe, "Latin1_General_100_CI_AI").Contains(tuKhoa)
                                      || EF.Functions.Collate(t.TomTat, "Latin1_General_100_CI_AI").Contains(tuKhoa));
        }
        if (chuyenMuc.HasValue) truyVan = truyVan.Where(t => t.ChuyenMuc == chuyenMuc);
        truyVan = trangThai switch
        {
            "hien" => truyVan.Where(t => t.HienThi && t.NgayDang <= homNay),
            "hen" => truyVan.Where(t => t.HienThi && t.NgayDang > homNay),
            "an" => truyVan.Where(t => !t.HienThi),
            _ => truyVan
        };

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.ChuyenMuc = chuyenMuc;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoDangHien = await db.TinTucs.CountAsync(t => t.HienThi && t.NgayDang <= homNay);
        ViewBag.SoHenDang = await db.TinTucs.CountAsync(t => t.HienThi && t.NgayDang > homNay);
        ViewBag.SoDaAn = await db.TinTucs.CountAsync(t => !t.HienThi);
        return View(await DanhSachTrang<TinTuc>.TaoAsync(
            truyVan.OrderByDescending(t => t.NgayDang).ThenByDescending(t => t.MaTinTuc), trang, 10));
    }

    public IActionResult TaoMoi() => View("BieuMau", new TinTuc());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(TinTuc tin, IFormFile? anhBia)
    {
        ChuanHoa(tin);
        tin.AnhBia = null;
        if (!ModelState.IsValid) return View("BieuMau", tin);
        if (anhBia is { Length: > 0 })
        {
            var (duongDan, loi) = await anhXuLy.LuuAsync(anhBia, AnhXuLy.ThuMucTinTuc);
            if (duongDan is null) { ModelState.AddModelError("AnhBia", loi!); return View("BieuMau", tin); }
            tin.AnhBia = duongDan;
        }
        tin.NguoiDang = HttpContext.Session.HoTen();
        db.TinTucs.Add(tin);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã đăng bài \"{tin.TieuDe}\"" + (tin.HienThi ? "" : " (đang ẩn)");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var tin = await db.TinTucs.FindAsync(id);
        return tin is null ? NotFound() : View("BieuMau", tin);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, TinTuc tin, IFormFile? anhBia, bool xoaAnhBia = false)
    {
        var cu = await db.TinTucs.FindAsync(id);
        if (cu is null || id != tin.MaTinTuc) return NotFound();
        ChuanHoa(tin);
        tin.AnhBia = cu.AnhBia;
        if (!ModelState.IsValid) return View("BieuMau", tin);
        if (anhBia is { Length: > 0 })
        {
            var (duongDan, loi) = await anhXuLy.LuuAsync(anhBia, AnhXuLy.ThuMucTinTuc);
            if (duongDan is null) { ModelState.AddModelError("AnhBia", loi!); return View("BieuMau", tin); }
            anhXuLy.Xoa(cu.AnhBia);
            cu.AnhBia = duongDan;
        }
        else if (xoaAnhBia) { anhXuLy.Xoa(cu.AnhBia); cu.AnhBia = null; }
        cu.TieuDe = tin.TieuDe;
        cu.TomTat = tin.TomTat;
        cu.NoiDung = tin.NoiDung;
        cu.ChuyenMuc = tin.ChuyenMuc;
        cu.BieuTuong = tin.BieuTuong;
        cu.NgayDang = tin.NgayDang;
        cu.HienThi = tin.HienThi;
        cu.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật bài \"{cu.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiHienThi(int id)
    {
        var tin = await db.TinTucs.FindAsync(id);
        if (tin is null) return NotFound();
        tin.HienThi = !tin.HienThi;
        tin.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{(tin.HienThi ? "Đã hiện" : "Đã ẩn")} bài \"{tin.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var tin = await db.TinTucs.FindAsync(id);
        if (tin is null) return NotFound();
        db.TinTucs.Remove(tin);
        await db.SaveChangesAsync();
        anhXuLy.Xoa(tin.AnhBia);
        TempData["Success"] = $"Đã xóa bài \"{tin.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Cắt khoảng trắng, chuẩn hóa xuống dòng, biểu tượng ngoài danh sách thì về mặc định.</summary>
    private static void ChuanHoa(TinTuc tin)
    {
        tin.TieuDe = tin.TieuDe?.Trim() ?? "";
        tin.TomTat = tin.TomTat?.Trim() ?? "";
        tin.NoiDung = (tin.NoiDung ?? "").Replace("\r\n", "\n").Trim();
        if (!TinTucXuLy.DsBieuTuong.Contains(tin.BieuTuong)) tin.BieuTuong = TinTucXuLy.DsBieuTuong[0];
    }
}
