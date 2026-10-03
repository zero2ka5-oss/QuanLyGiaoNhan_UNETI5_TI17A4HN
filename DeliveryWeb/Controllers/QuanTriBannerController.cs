// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Quản lý banner trình chiếu trên trang chủ (thêm ảnh, sửa, sắp thứ tự, ẩn / hiện, xóa).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriBannerController — URL: /QuanTriBanner/{Index|TaoMoi|ChinhSua|DoiHienThi|DoiThuTu|Xoa}
 * Quyền: chỉ Quản trị.
 * - Thêm mới bắt buộc có ảnh; sửa có thể giữ ảnh cũ hoặc chọn ảnh khác (ảnh cũ bị xóa khỏi ổ đĩa).
 * - Liên kết của nút chỉ nhận đường dẫn trong trang ("/...") hoặc http(s):// để tránh "javascript:".
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriBannerController(QuanLyGiaoNhanDbContext db, AnhXuLy anhXuLy) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.BannerTrangChus.AsNoTracking().OrderBy(b => b.ThuTu).ThenBy(b => b.MaBanner).ToListAsync());

    public async Task<IActionResult> TaoMoi() =>
        View("BieuMau", new BannerTrangChu { ThuTu = (await db.BannerTrangChus.MaxAsync(b => (int?)b.ThuTu) ?? 0) + 1 });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(BannerTrangChu banner, IFormFile? anhBanner)
    {
        ChuanHoa(banner);
        banner.Anh = "";
        if (anhBanner is not { Length: > 0 }) ModelState.AddModelError(nameof(banner.Anh), "Chọn ảnh banner");
        if (!ModelState.IsValid) return View("BieuMau", banner);
        var (duongDan, loi) = await anhXuLy.LuuAsync(anhBanner, AnhXuLy.ThuMucBanner);
        if (duongDan is null) { ModelState.AddModelError(nameof(banner.Anh), loi!); return View("BieuMau", banner); }
        banner.Anh = duongDan;
        banner.NgayTao = DateTime.Now;
        db.BannerTrangChus.Add(banner);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm banner \"{banner.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var banner = await db.BannerTrangChus.FindAsync(id);
        return banner is null ? NotFound() : View("BieuMau", banner);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, BannerTrangChu banner, IFormFile? anhBanner)
    {
        var cu = await db.BannerTrangChus.FindAsync(id);
        if (cu is null || id != banner.MaBanner) return NotFound();
        ChuanHoa(banner);
        banner.Anh = cu.Anh;
        if (!ModelState.IsValid) return View("BieuMau", banner);
        if (anhBanner is { Length: > 0 })
        {
            var (duongDan, loi) = await anhXuLy.LuuAsync(anhBanner, AnhXuLy.ThuMucBanner);
            if (duongDan is null) { ModelState.AddModelError(nameof(banner.Anh), loi!); return View("BieuMau", banner); }
            anhXuLy.Xoa(cu.Anh);
            cu.Anh = duongDan;
        }
        cu.TieuDe = banner.TieuDe;
        cu.MoTa = banner.MoTa;
        cu.ChuNut = banner.ChuNut;
        cu.LienKet = banner.LienKet;
        cu.ThuTu = banner.ThuTu;
        cu.HienThi = banner.HienThi;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật banner \"{cu.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiHienThi(int id)
    {
        var banner = await db.BannerTrangChus.FindAsync(id);
        if (banner is null) return NotFound();
        banner.HienThi = !banner.HienThi;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{(banner.HienThi ? "Đã hiện" : "Đã ẩn")} banner \"{banner.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Đổi chỗ banner với banner đứng trước (len = true) hoặc đứng sau trong danh sách.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiThuTu(int id, bool len)
    {
        var ds = await db.BannerTrangChus.OrderBy(b => b.ThuTu).ThenBy(b => b.MaBanner).ToListAsync();
        int i = ds.FindIndex(b => b.MaBanner == id), j = len ? i - 1 : i + 1;
        if (i < 0) return NotFound();
        if (j >= 0 && j < ds.Count)
        {
            (ds[i], ds[j]) = (ds[j], ds[i]);
            for (int k = 0; k < ds.Count; k++) ds[k].ThuTu = k + 1;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var banner = await db.BannerTrangChus.FindAsync(id);
        if (banner is null) return NotFound();
        db.BannerTrangChus.Remove(banner);
        await db.SaveChangesAsync();
        anhXuLy.Xoa(banner.Anh);
        TempData["Success"] = $"Đã xóa banner \"{banner.TieuDe}\"";
        return RedirectToAction(nameof(Index));
    }

    private void ChuanHoa(BannerTrangChu b)
    {
        ModelState.Remove(nameof(b.Anh));   // ảnh lấy từ tệp tải lên, không lấy từ form
        b.TieuDe = b.TieuDe?.Trim() ?? "";
        b.MoTa = string.IsNullOrWhiteSpace(b.MoTa) ? null : b.MoTa.Trim();
        b.ChuNut = string.IsNullOrWhiteSpace(b.ChuNut) ? null : b.ChuNut.Trim();
        b.LienKet = string.IsNullOrWhiteSpace(b.LienKet) ? null : b.LienKet.Trim();
        if (b.LienKet is not null && !(b.LienKet.StartsWith('/') && !b.LienKet.StartsWith("//"))
            && !b.LienKet.StartsWith("http://") && !b.LienKet.StartsWith("https://"))
            ModelState.AddModelError(nameof(b.LienKet), "Liên kết phải bắt đầu bằng / hoặc http(s)://");
        if (b.ChuNut is not null && b.LienKet is null)
            ModelState.AddModelError(nameof(b.LienKet), "Nhập liên kết cho nút");
    }
}
