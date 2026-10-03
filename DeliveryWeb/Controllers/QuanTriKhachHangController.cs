// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 2 - Quản lý khách hàng: tìm kiếm, lọc, sắp xếp, phân trang, thêm/sửa, ngừng hoạt động, xóa.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriKhachHangController — URL: /QuanTriKhachHang/{Index|ChiTiet|TaoMoi|ChinhSua|DoiTrangThai|Xoa}
 * Quyền: Quản trị, Điều phối.
 * NgayDangKy do hệ thống gán; có thể tạo kèm tài khoản đăng nhập; xóa khách hàng chỉ khi chưa có đơn.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriKhachHangController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiHoatDong? trangThai, string sapXep = "moi", int trang = 1)
    {
        var truyVan = db.KhachHangs.AsNoTracking().Include(k => k.TaiKhoan).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(k => k.HoTen.Contains(tuKhoa) || k.SoDienThoai.Contains(tuKhoa)
                                      || (k.Email != null && k.Email.Contains(tuKhoa)) || k.DiaChi.Contains(tuKhoa));
        }
        if (trangThai.HasValue) truyVan = truyVan.Where(k => k.TrangThai == trangThai);
        truyVan = sapXep switch
        {
            "cu" => truyVan.OrderBy(k => k.NgayDangKy),
            "ten" => truyVan.OrderBy(k => k.HoTen),
            "nhieudon" => truyVan.OrderByDescending(k => k.DonGiaoHangs.Count),
            _ => truyVan.OrderByDescending(k => k.NgayDangKy)
        };
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.SapXep = sapXep;
        ViewBag.SoDon = await db.DonGiaoHangs.GroupBy(d => d.MaKhachHang).Select(g => new { g.Key, SoLuong = g.Count() })
                                             .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<KhachHang>.TaoAsync(truyVan, trang, 12));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var kh = await db.KhachHangs.AsNoTracking().Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == id);
        if (kh is null) return NotFound();
        return View(await xuLy.TongQuanKhachHangAsync(kh, 15));
    }

    public IActionResult TaoMoi() => View("BieuMau", new KhachHangFormVM());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(KhachHangFormVM vm)
    {
        var tk = await KiemTraTaiKhoanMoiAsync(vm.TenDangNhap, vm.MatKhau, vm.HoTen, vm.Email);
        if (!ModelState.IsValid) return View("BieuMau", vm);

        db.KhachHangs.Add(new KhachHang
        {
            TaiKhoan = tk, HoTen = vm.HoTen.Trim(), SoDienThoai = vm.SoDienThoai, Email = vm.Email?.Trim(),
            DiaChi = vm.DiaChi.Trim(), TrangThai = vm.TrangThai, NgayDangKy = DateTime.Now
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm khách hàng {vm.HoTen}" + (tk is null ? "" : $" (tài khoản {tk.TenDangNhap})");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var kh = await db.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == id);
        if (kh is null) return NotFound();
        return View("BieuMau", new KhachHangFormVM
        {
            MaKhachHang = kh.MaKhachHang, HoTen = kh.HoTen, SoDienThoai = kh.SoDienThoai, Email = kh.Email, DiaChi = kh.DiaChi,
            TrangThai = kh.TrangThai, CoTaiKhoan = kh.TaiKhoan is not null, TenDangNhap = kh.TaiKhoan?.TenDangNhap
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, KhachHangFormVM vm)
    {
        var kh = await db.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == id);
        if (kh is null) return NotFound();
        TaiKhoan? tkMoi = null;
        if (kh.TaiKhoan is null && !string.IsNullOrWhiteSpace(vm.TenDangNhap))
            tkMoi = await KiemTraTaiKhoanMoiAsync(vm.TenDangNhap, vm.MatKhau, vm.HoTen, vm.Email);
        else { ModelState.Remove(nameof(vm.TenDangNhap)); ModelState.Remove(nameof(vm.MatKhau)); }
        if (!ModelState.IsValid) { vm.MaKhachHang = id; vm.CoTaiKhoan = kh.TaiKhoan is not null; return View("BieuMau", vm); }

        kh.HoTen = vm.HoTen.Trim();
        kh.SoDienThoai = vm.SoDienThoai;
        kh.Email = vm.Email?.Trim();
        kh.DiaChi = vm.DiaChi.Trim();
        kh.TrangThai = vm.TrangThai;
        if (tkMoi is not null) kh.TaiKhoan = tkMoi;
        else if (kh.TaiKhoan is not null) { kh.TaiKhoan.HoTen = kh.HoTen; if (!string.IsNullOrEmpty(kh.Email)) kh.TaiKhoan.Email = kh.Email; }
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật khách hàng {kh.HoTen}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var kh = await db.KhachHangs.FindAsync(id);
        if (kh is null) return NotFound();
        kh.TrangThai = kh.TrangThai == TrangThaiHoatDong.HoatDong ? TrangThaiHoatDong.NgungHoatDong : TrangThaiHoatDong.HoatDong;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{kh.HoTen}: {kh.TrangThai.TenHienThi().ToLower()}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var kh = await db.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == id);
        if (kh is null) return NotFound();
        if (await db.DonGiaoHangs.AnyAsync(d => d.MaKhachHang == id))
        {
            TempData["Error"] = $"Không thể xóa {kh.HoTen} vì đã có đơn giao hàng. Hãy chuyển sang Ngừng hoạt động.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        db.KhachHangs.Remove(kh);
        if (kh.TaiKhoan is not null) db.TaiKhoans.Remove(kh.TaiKhoan);
        await db.SaveChangesAsync();
        anhXuLy.Xoa(kh.TaiKhoan?.AnhDaiDien);
        TempData["Success"] = $"Đã xóa khách hàng {kh.HoTen}";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Tạo tài khoản kèm theo (không bắt buộc): có tên đăng nhập thì bắt buộc mật khẩu, email và không trùng.</summary>
    private async Task<TaiKhoan?> KiemTraTaiKhoanMoiAsync(string? tenDangNhap, string? matKhau, string hoTen, string? email)
    {
        if (string.IsNullOrWhiteSpace(tenDangNhap)) return null;
        tenDangNhap = tenDangNhap.Trim().ToLower();
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap))
            ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại");
        if (string.IsNullOrWhiteSpace(matKhau)) ModelState.AddModelError("MatKhau", "Nhập mật khẩu cho tài khoản");
        if (string.IsNullOrWhiteSpace(email)) ModelState.AddModelError("Email", "Cần email để tạo tài khoản");
        if (!ModelState.IsValid) return null;
        return new TaiKhoan
        {
            TenDangNhap = tenDangNhap, MatKhau = MatKhauHelper.Bam(matKhau!), HoTen = hoTen.Trim(), Email = email!.Trim(),
            VaiTro = VaiTroNguoiDung.KhachHang
        };
    }
}
