// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 3 - Quản lý phương tiện: CRUD, biển số không trùng, tải trọng > 0,
//                     trạng thái sẵn sàng / bảo trì, khối lượng đang chở và lịch sử sử dụng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriPhuongTienController — URL: /QuanTriPhuongTien/{Index|ChiTiet|TaoMoi|ChinhSua|Xoa}
 * Quyền: Quản trị, Điều phối.
 * Thêm / sửa / đổi trạng thái / xóa phương tiện: chỉ Quản trị (Điều phối chỉ xem).
 * Người dùng chỉ chọn Sẵn sàng / Bảo trì / Ngừng hoạt động; "Đang sử dụng" do hệ thống cập nhật theo phân công.
 * Không cho chuyển Bảo trì / Ngừng khi xe còn chở đơn đang hiệu lực.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriPhuongTienController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiPhuongTien? trangThai, string? loai, int trang = 1)
    {
        var truyVan = db.PhuongTiens.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) truyVan = truyVan.Where(p => p.BienSo.Contains(tuKhoa.Trim()));
        if (trangThai.HasValue) truyVan = truyVan.Where(p => p.TrangThai == trangThai);
        if (!string.IsNullOrEmpty(loai)) truyVan = truyVan.Where(p => p.LoaiPhuongTien == loai);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.Loai = loai;
        ViewBag.SoTheoTrangThai = await db.PhuongTiens.GroupBy(p => p.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                          .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        // Khối lượng đang chở trên từng xe = tổng khối lượng đơn của các phân công đang hiệu lực
        ViewBag.DangCho = await xuLy.PhanCongHieuLuc().GroupBy(p => p.MaPhuongTien)
                                    .Select(g => new { g.Key, KhoiLuong = g.Sum(p => p.DonGiaoHang!.KhoiLuong) })
                                    .ToDictionaryAsync(x => x.Key, x => x.KhoiLuong);
        ViewBag.NguoiDung = await xuLy.PhanCongHieuLuc().GroupBy(p => p.MaPhuongTien)
                                      .Select(g => new { g.Key, HoTen = g.Max(p => p.NhanVien!.HoTen) })
                                      .ToDictionaryAsync(x => x.Key, x => x.HoTen);
        return View(await DanhSachTrang<PhuongTien>.TaoAsync(truyVan.OrderBy(p => p.TrangThai).ThenBy(p => p.TaiTrongToiDa), trang, 10));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var pt = await db.PhuongTiens.AsNoTracking().FirstOrDefaultAsync(p => p.MaPhuongTien == id);
        if (pt is null) return NotFound();
        ViewBag.DsPhanCong = await db.PhanCongGiaoHangs.AsNoTracking().Include(p => p.DonGiaoHang).Include(p => p.NhanVien)
                                     .Where(p => p.MaPhuongTien == id).OrderByDescending(p => p.NgayPhanCong).Take(30).ToListAsync();
        ViewBag.DangCho = await xuLy.KhoiLuongDangChoAsync(id, null);
        return View(pt);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public IActionResult TaoMoi() => View("BieuMau", new PhuongTien());

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(PhuongTien pt, IFormFile? anhXe)
    {
        pt.AnhXe = null;
        pt.BienSo = pt.BienSo?.Trim().ToUpper() ?? "";
        if (await db.PhuongTiens.AnyAsync(p => p.BienSo == pt.BienSo)) ModelState.AddModelError(nameof(pt.BienSo), "Biển số đã tồn tại");
        if (pt.TrangThai == TrangThaiPhuongTien.DangSuDung)
            ModelState.AddModelError(nameof(pt.TrangThai), "Trạng thái 'Đang sử dụng' do hệ thống tự cập nhật");
        if (!ModelState.IsValid) return View("BieuMau", pt);
        if (anhXe is { Length: > 0 })
        {
            var (duongDan, loi) = await anhXuLy.LuuAsync(anhXe, AnhXuLy.ThuMucPhuongTien);
            if (duongDan is null) { ModelState.AddModelError(nameof(pt.AnhXe), loi!); return View("BieuMau", pt); }
            pt.AnhXe = duongDan;
        }
        db.PhuongTiens.Add(pt);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm phương tiện {pt.BienSo}";
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var pt = await db.PhuongTiens.FindAsync(id);
        if (pt is null) return NotFound();
        ViewBag.TrangThaiHienTai = pt.TrangThai;
        if (pt.TrangThai == TrangThaiPhuongTien.DangSuDung) pt.TrangThai = TrangThaiPhuongTien.SanSang;
        return View("BieuMau", pt);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, PhuongTien pt, IFormFile? anhXe, bool xoaAnhXe = false)
    {
        if (id != pt.MaPhuongTien) return BadRequest();
        var cu = await db.PhuongTiens.FindAsync(id);
        if (cu is null) return NotFound();
        pt.AnhXe = cu.AnhXe;
        pt.BienSo = pt.BienSo?.Trim().ToUpper() ?? "";
        if (await db.PhuongTiens.AnyAsync(p => p.BienSo == pt.BienSo && p.MaPhuongTien != id))
            ModelState.AddModelError(nameof(pt.BienSo), "Biển số đã tồn tại");
        if (pt.TrangThai == TrangThaiPhuongTien.DangSuDung)
            ModelState.AddModelError(nameof(pt.TrangThai), "Trạng thái 'Đang sử dụng' do hệ thống tự cập nhật");
        decimal dangCho = await xuLy.KhoiLuongDangChoAsync(id, null);
        bool coDonHieuLuc = await xuLy.PhanCongHieuLuc().AnyAsync(p => p.MaPhuongTien == id);
        if (pt.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong && coDonHieuLuc)
            ModelState.AddModelError(nameof(pt.TrangThai), "Xe đang chở đơn hiệu lực – hãy đổi phân công trước khi bảo trì / ngừng");
        if (pt.TaiTrongToiDa < dangCho)
            ModelState.AddModelError(nameof(pt.TaiTrongToiDa), $"Tải trọng không được nhỏ hơn khối lượng đang chở ({DinhDang.KhoiLuong(dangCho)})");
        if (!ModelState.IsValid) { ViewBag.TrangThaiHienTai = cu.TrangThai; return View("BieuMau", pt); }

        cu.BienSo = pt.BienSo;
        cu.LoaiPhuongTien = pt.LoaiPhuongTien;
        cu.TaiTrongToiDa = pt.TaiTrongToiDa;
        cu.TrangThai = pt.TrangThai;
        cu.GhiChu = pt.GhiChu;
        if (anhXe is { Length: > 0 })
        {
            var (duongDan, loi) = await anhXuLy.LuuAsync(anhXe, AnhXuLy.ThuMucPhuongTien);
            if (duongDan is null) { ModelState.AddModelError(nameof(pt.AnhXe), loi!); ViewBag.TrangThaiHienTai = cu.TrangThai; return View("BieuMau", pt); }
            anhXuLy.Xoa(cu.AnhXe);
            cu.AnhXe = duongDan;
        }
        else if (xoaAnhXe) { anhXuLy.Xoa(cu.AnhXe); cu.AnhXe = null; }
        await db.SaveChangesAsync();
        await xuLy.CapNhatNguonLucAsync([], [id]);
        TempData["Success"] = $"Đã cập nhật phương tiện {cu.BienSo}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var pt = await db.PhuongTiens.FindAsync(id);
        if (pt is null) return NotFound();
        if (await db.PhanCongGiaoHangs.AnyAsync(p => p.MaPhuongTien == id))
        {
            TempData["Error"] = $"Không thể xóa {pt.BienSo} vì đã có lịch sử phân công. Hãy chuyển sang Ngừng hoạt động.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        db.PhuongTiens.Remove(pt);
        await db.SaveChangesAsync();
        anhXuLy.Xoa(pt.AnhXe);
        TempData["Success"] = $"Đã xóa phương tiện {pt.BienSo}";
        return RedirectToAction(nameof(Index));
    }
}
