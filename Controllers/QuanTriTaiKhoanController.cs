// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Quản lý tài khoản (tạo, sửa, khóa/mở, đặt lại mật khẩu) – chỉ Quản trị.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriTaiKhoanController — URL: /QuanTriTaiKhoan/{Index|TaoMoi|ChinhSua|KhoaMo|DatLaiMatKhau}
 * Quyền: chỉ Quản trị (Điều phối cũng bị chặn).
 * Tạo mới tại đây chỉ cho tài khoản Quản trị / Điều phối; tài khoản khách hàng và nhân viên tạo từ module tương ứng
 * và giữ nguyên vai trò khi sửa. Không cho tự khóa / hạ quyền tài khoản đang đăng nhập.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriTaiKhoanController(QuanLyGiaoNhanDbContext db) : Controller
{
    private const int KichThuocTrang = 15;

    public async Task<IActionResult> Index(string? tuKhoa, string? vaiTro, TrangThaiTaiKhoan? trangThai, int trang = 1)
    {
        var truyVan = db.TaiKhoans.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(t => t.TenDangNhap.Contains(tuKhoa) || t.HoTen.Contains(tuKhoa) || t.Email.Contains(tuKhoa));
        }
        if (!string.IsNullOrEmpty(vaiTro)) truyVan = truyVan.Where(t => t.VaiTro == vaiTro);
        if (trangThai.HasValue) truyVan = truyVan.Where(t => t.TrangThai == trangThai);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.VaiTro = vaiTro;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoTheoVaiTro = await db.TaiKhoans.GroupBy(t => t.VaiTro).Select(g => new { g.Key, SoLuong = g.Count() })
                                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<TaiKhoan>.TaoAsync(truyVan.OrderBy(t => t.VaiTro).ThenBy(t => t.TenDangNhap), trang, KichThuocTrang));
    }

    public IActionResult TaoMoi()
    {
        NapVaiTro();
        return View("BieuMau", new TaiKhoanFormVM());
    }

    /// <summary>Tạo tài khoản Quản trị / Điều phối. Tài khoản nhân viên giao hàng và khách hàng tạo từ module tương ứng.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(TaiKhoanFormVM vm)
    {
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu");
        if (vm.VaiTro is not (VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.DieuPhoi))
            ModelState.AddModelError(nameof(vm.VaiTro), "Chỉ tạo tài khoản Quản trị / Điều phối tại đây");
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) { NapVaiTro(); return View("BieuMau", vm); }

        db.TaiKhoans.Add(new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap, MatKhau = MatKhauHelper.Bam(vm.MatKhau!), HoTen = vm.HoTen.Trim(),
            Email = vm.Email.Trim(), VaiTro = vm.VaiTro, TrangThai = vm.TrangThai
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo tài khoản {vm.TenDangNhap}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        NapVaiTro();
        return View("BieuMau", new TaiKhoanFormVM
        {
            MaTaiKhoan = tk.MaTaiKhoan, TenDangNhap = tk.TenDangNhap, HoTen = tk.HoTen, Email = tk.Email,
            VaiTro = tk.VaiTro, TrangThai = tk.TrangThai
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, TaiKhoanFormVM vm)
    {
        var tk = await db.TaiKhoans.Include(t => t.KhachHang).Include(t => t.NhanVienGiaoHang).FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
        if (tk is null) return NotFound();
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLower();
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap && t.MaTaiKhoan != id))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        // Tài khoản gắn với khách hàng / nhân viên giao hàng giữ nguyên vai trò
        bool vaiTroCoDinh = tk.VaiTro is VaiTroNguoiDung.KhachHang or VaiTroNguoiDung.GiaoHang;
        if (vaiTroCoDinh) vm.VaiTro = tk.VaiTro;
        else if (vm.VaiTro is not (VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.DieuPhoi))
            ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ");
        if (id == HttpContext.Session.MaTaiKhoan() && (vm.TrangThai == TrangThaiTaiKhoan.BiKhoa || vm.VaiTro != VaiTroNguoiDung.QuanTri))
            ModelState.AddModelError("", "Không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập");
        if (!ModelState.IsValid) { NapVaiTro(); vm.MaTaiKhoan = id; return View("BieuMau", vm); }

        tk.TenDangNhap = vm.TenDangNhap;
        tk.HoTen = vm.HoTen.Trim();
        tk.Email = vm.Email.Trim();
        tk.VaiTro = vm.VaiTro;
        tk.TrangThai = vm.TrangThai;
        if (!string.IsNullOrWhiteSpace(vm.MatKhau)) tk.MatKhau = MatKhauHelper.Bam(vm.MatKhau);
        // Đồng bộ họ tên / email sang hồ sơ khách hàng hoặc nhân viên gắn với tài khoản
        if (tk.KhachHang is not null) { tk.KhachHang.HoTen = tk.HoTen; tk.KhachHang.Email = tk.Email; }
        if (tk.NhanVienGiaoHang is not null) { tk.NhanVienGiaoHang.HoTen = tk.HoTen; tk.NhanVienGiaoHang.Email = tk.Email; }
        await db.SaveChangesAsync();
        if (id == HttpContext.Session.MaTaiKhoan()) HttpContext.Session.GhiPhien(tk, tk.KhachHang?.MaKhachHang, tk.NhanVienGiaoHang?.MaNhanVien);
        TempData["Success"] = $"Đã cập nhật tài khoản {tk.TenDangNhap}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaMo(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        if (id == HttpContext.Session.MaTaiKhoan()) TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập";
        else
        {
            tk.TrangThai = tk.TrangThai == TrangThaiTaiKhoan.HoatDong ? TrangThaiTaiKhoan.BiKhoa : TrangThaiTaiKhoan.HoatDong;
            await db.SaveChangesAsync();
            TempData["Success"] = $"{tk.TenDangNhap}: {(tk.TrangThai == TrangThaiTaiKhoan.HoatDong ? "đã mở khóa" : "đã khóa")}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        tk.MatKhau = MatKhauHelper.Bam("123456");
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã đặt lại mật khẩu của {tk.TenDangNhap} về 123456";
        return RedirectToAction(nameof(Index));
    }

    private void NapVaiTro() => ViewBag.DsVaiTro = new List<SelectListItem>
    {
        new(HienThi.TenVaiTro(VaiTroNguoiDung.DieuPhoi), VaiTroNguoiDung.DieuPhoi),
        new(HienThi.TenVaiTro(VaiTroNguoiDung.QuanTri), VaiTroNguoiDung.QuanTri),
    };
}
