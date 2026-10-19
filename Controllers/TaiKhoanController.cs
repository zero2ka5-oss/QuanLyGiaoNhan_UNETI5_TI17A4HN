// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Tài khoản, đăng nhập/đăng xuất, phân quyền, quản lý loại hàng và khu vực giao.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;
public class TaiKhoanController(ApplicationDbContext context) : AppControllerBase
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TaiKhoan? account;
        try
        {
            account = await context.TaiKhoans.FirstOrDefaultAsync(x => x.TenDangNhap == model.TenDangNhap && x.MatKhau == model.MatKhau && x.TrangThai == "Hoạt động");
        } catch (Exception exception) when (exception is SqlException || exception.InnerException is SqlException || exception is InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối cơ sở dữ liệu. Hãy kiểm tra SQL Server rồi thử lại.");
            return View(model);
        }
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }
        HttpContext.Session.SetInt32(SessionHelper.AccountId, account.MaTaiKhoan);
        HttpContext.Session.SetString(SessionHelper.FullName, account.HoTen);
        HttpContext.Session.SetString(SessionHelper.Role, account.VaiTro);
        return !string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl) : RedirectToAction("Index", "Home");
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
    [SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Index() => View(await context.TaiKhoans.AsNoTracking().ToListAsync());
    [SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
        return item is null ? NotFound() : View(item);
    }
    [SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public IActionResult Create() => View(new TaiKhoan());
    [HttpPost, ValidateAntiForgeryToken, SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Create(TaiKhoan model)
    {
        if (await context.TaiKhoans.AnyAsync(x => x.TenDangNhap == model.TenDangNhap)) ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        context.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    [SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.TaiKhoans.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken, SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Edit(int id, TaiKhoan model)
    {
        if (id != model.MaTaiKhoan) return NotFound();
        var existing = await context.TaiKhoans.FindAsync(id);
        if (existing is null) return NotFound();
        if (string.IsNullOrWhiteSpace(model.MatKhau))
        {
            ModelState.Remove(nameof(model.MatKhau));
            model.MatKhau = existing.MatKhau;
        }
        if (await context.TaiKhoans.AnyAsync(x => x.TenDangNhap == model.TenDangNhap && x.MaTaiKhoan != id))
        ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        existing.TenDangNhap = model.TenDangNhap;
        existing.MatKhau = model.MatKhau;
        existing.HoTen = model.HoTen;
        existing.Email = model.Email;
        existing.VaiTro = model.VaiTro;
        existing.TrangThai = model.TrangThai;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật tài khoản.";
        return RedirectToAction(nameof(Index));
    }
    [SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(x => x.MaTaiKhoan == id);
        return item is null ? NotFound() : View(item);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await context.TaiKhoans.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));
        if (id == CurrentAccountId)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }
        if (await context.KhachHangs.AnyAsync(x => x.MaTaiKhoan == id) ||
        await context.NhanVienGiaoHangs.AnyAsync(x => x.MaTaiKhoan == id))
        {
            TempData["ErrorMessage"] = "Tài khoản đang liên kết với hồ sơ khách hàng/nhân viên nên không thể xóa.";
            return RedirectToAction(nameof(Index));
        }
        context.TaiKhoans.Remove(item);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
