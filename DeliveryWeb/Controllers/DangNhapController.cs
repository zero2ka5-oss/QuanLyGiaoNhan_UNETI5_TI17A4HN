// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Đăng nhập/đăng xuất bằng Session, đăng ký khách hàng, đổi mật khẩu, chuyển trang theo vai trò.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * DangNhapController — URL: /DangNhap/{Index|DangKy|DangXuat|DoiMatKhau|KhongCoQuyen|TrangLamViec}
 * Quyền: mọi người (DoiMatKhau, TrangLamViec cần đã đăng nhập).
 * Đăng nhập: LINQ tìm tài khoản theo tên đăng nhập → kiểm tra mật khẩu băm → tài khoản bị khóa thì từ chối →
 *            lưu Mã tài khoản, Họ tên, Vai trò (+ mã khách hàng / nhân viên) vào Session → chuyển trang theo vai trò.
 * Đăng xuất: Session.Clear() – Session cũ không dùng tiếp được.
 */
public class DangNhapController(QuanLyGiaoNhanDbContext db) : Controller
{
    // ===================== ĐĂNG NHẬP =====================

    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (HttpContext.Session.DaDangNhap()) return ChuyenTheoVaiTro();
        ViewBag.ReturnUrl = returnUrl;
        return View(new DangNhapVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DangNhapVM vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var tenDangNhap = vm.TenDangNhap.Trim().ToLower();
        var tk = await db.TaiKhoans.Include(t => t.KhachHang).Include(t => t.NhanVienGiaoHang)
                                   .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);
        if (tk is null || !MatKhauHelper.KiemTra(tk.MatKhau, vm.MatKhau))
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
            return View(vm);
        }
        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(vm);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.GhiPhien(tk, tk.KhachHang?.MaKhachHang, tk.NhanVienGiaoHang?.MaNhanVien);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return ChuyenTheoVaiTro();
    }

    // ===================== ĐĂNG KÝ / ĐĂNG XUẤT =====================

    [HttpGet]
    public IActionResult DangKy()
    {
        if (HttpContext.Session.DaDangNhap()) return ChuyenTheoVaiTro();
        return View(new DangKyVM());
    }

    /// <summary>Khách hàng tự đăng ký: tạo TaiKhoan (vai trò Khách hàng) + KhachHang trong một lần SaveChanges.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyVM vm)
    {
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLower();
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) return View(vm);

        var tk = new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap, MatKhau = MatKhauHelper.Bam(vm.MatKhau), HoTen = vm.HoTen.Trim(),
            Email = vm.Email.Trim(), VaiTro = VaiTroNguoiDung.KhachHang
        };
        var kh = new KhachHang
        {
            TaiKhoan = tk, HoTen = tk.HoTen, SoDienThoai = vm.SoDienThoai, Email = tk.Email, DiaChi = vm.DiaChi.Trim(),
            NgayDangKy = DateTime.Now
        };
        db.KhachHangs.Add(kh);
        await db.SaveChangesAsync();

        HttpContext.Session.GhiPhien(tk, kh.MaKhachHang, null);
        TempData["Success"] = "Đăng ký thành công! Bạn có thể tạo đơn giao hàng ngay.";
        return RedirectToAction("Index", "KhachHangTrangChu");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DangXuat()
    {
        HttpContext.Session.Clear();
        TempData["Success"] = "Bạn đã đăng xuất.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult KhongCoQuyen(string? returnUrl) { ViewBag.ReturnUrl = returnUrl; return View(); }

    // ===================== ĐỔI MẬT KHẨU (mọi vai trò) =====================

    [YeuCauVaiTro, HttpGet]
    public IActionResult DoiMatKhau() => View(new DoiMatKhauVM());

    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        if (!MatKhauHelper.KiemTra(tk.MatKhau, vm.MatKhauHienTai))
        {
            ModelState.AddModelError(nameof(vm.MatKhauHienTai), "Mật khẩu hiện tại không đúng");
            return View(vm);
        }
        tk.MatKhau = MatKhauHelper.Bam(vm.MatKhauMoi);
        await db.SaveChangesAsync();
        TempData["Success"] = "Đổi mật khẩu thành công";
        return RedirectToAction(nameof(DoiMatKhau));
    }

    // ===================== ẢNH ĐẠI DIỆN (mọi vai trò) =====================

    /// <summary>Tải ảnh đại diện mới cho chính tài khoản đang đăng nhập; ảnh cũ bị xóa khỏi ổ đĩa.</summary>
    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiAnhDaiDien(IFormFile? anh, [FromServices] AnhXuLy anhXuLy, string? returnUrl = null)
    {
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        var (duongDan, loi) = await anhXuLy.LuuAsync(anh, AnhXuLy.ThuMucAnhDaiDien);
        if (duongDan is null) TempData["Error"] = loi;
        else
        {
            anhXuLy.Xoa(tk.AnhDaiDien);
            tk.AnhDaiDien = duongDan;
            await db.SaveChangesAsync();
            HttpContext.Session.GhiAnhDaiDien(duongDan);
            TempData["Success"] = "Đã cập nhật ảnh đại diện";
        }
        return QuayLai(returnUrl);
    }

    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnhDaiDien([FromServices] AnhXuLy anhXuLy, string? returnUrl = null)
    {
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        anhXuLy.Xoa(tk.AnhDaiDien);
        tk.AnhDaiDien = null;
        await db.SaveChangesAsync();
        HttpContext.Session.GhiAnhDaiDien(null);
        TempData["Success"] = "Đã xóa ảnh đại diện";
        return QuayLai(returnUrl);
    }

    private IActionResult QuayLai(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(DoiMatKhau));

    /// <summary>Nút "Vào trang làm việc" trên trang chủ.</summary>
    [YeuCauVaiTro]
    public IActionResult TrangLamViec() => ChuyenTheoVaiTro();

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Mỗi vai trò có trang chủ riêng: QuanTri* / KhachHang* / NhanVien*.</summary>
    private IActionResult ChuyenTheoVaiTro() => HttpContext.Session.VaiTro() switch
    {
        VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.DieuPhoi => RedirectToAction("Index", "QuanTriBangDieuKhien"),
        VaiTroNguoiDung.GiaoHang => RedirectToAction("Index", "NhanVienCongViec"),
        VaiTroNguoiDung.KhachHang => RedirectToAction("Index", "KhachHangTrangChu"),
        _ => RedirectToAction("Index", "TrangChu")
    };
}
