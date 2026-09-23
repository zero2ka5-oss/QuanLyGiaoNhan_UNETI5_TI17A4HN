using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

public class TaiKhoanController(ApplicationDbContext context) : AppControllerBase
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (await context.TaiKhoans.AnyAsync(x => x.TenDangNhap == model.TenDangNhap))
            ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
        if (await context.TaiKhoans.AnyAsync(x => x.Email == model.Email))
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
        if (!ModelState.IsValid) return View(model);

        var account = new TaiKhoan
        {
            TenDangNhap = model.TenDangNhap,
            MatKhau = model.MatKhau,
            HoTen = model.HoTen,
            Email = model.Email,
            VaiTro = "Khách hàng"
        };
        context.TaiKhoans.Add(account);
        await context.SaveChangesAsync();
        context.KhachHangs.Add(new KhachHang
        {
            MaTaiKhoan = account.MaTaiKhoan,
            HoTen = model.HoTen,
            SoDienThoai = model.SoDienThoai,
            Email = model.Email,
            DiaChi = model.DiaChi
        });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TaiKhoan? account;
        try
        {
            account = await context.TaiKhoans.FirstOrDefaultAsync(x => x.TenDangNhap == model.TenDangNhap && x.MatKhau == model.MatKhau && x.TrangThai == "Hoạt động");
        }
        catch (Exception exception) when (exception is SqlException || exception.InnerException is SqlException || exception is InvalidOperationException)
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
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)) return Redirect(model.ReturnUrl);
        return account.VaiTro switch
        {
            "Admin" or "Nhân viên điều phối" => RedirectToAction("Index", "ThongKe"),
            "Nhân viên giao hàng" => RedirectToAction("Index", "PhanCongGiaoHang"),
            _ => RedirectToAction("Index", "Home")
        };
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
        context.Add(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
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
        if (await context.TaiKhoans.AnyAsync(x => x.TenDangNhap == model.TenDangNhap && x.MaTaiKhoan != id)) ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại.");
        if (!ModelState.IsValid) return View(model);
        context.Update(model); await context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
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
        var item = await context.TaiKhoans.FindAsync(id); if (item is not null) { context.Remove(item); await context.SaveChangesAsync(); } return RedirectToAction(nameof(Index));
    }
}
