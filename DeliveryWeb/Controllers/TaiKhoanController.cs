// Họ và tên: [Vu Thi Van Anh]
// Mã sinh viên: [23103100194]
// Nội dung thực hiện: Module 1 - Xử lý Đăng nhập, Đăng xuất và Quản lý Tài khoản

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.ViewModels;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Helpers;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class TaiKhoanController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public TaiKhoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var account = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == model.TenDangNhap && t.MatKhau == model.MatKhau);

            if (account == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!account.TrangThai)
            {
                ModelState.AddModelError("", "Tài khoản hiện đang bị khóa.");
                return View(model);
            }
            SessionHelper.SetObjectAsJson(HttpContext.Session, "UserLogin", account);
            HttpContext.Session.SetString("VaiTro", account.VaiTro);
            HttpContext.Session.SetString("HoTen", account.HoTen);

            if (account.VaiTro == "Admin")
                return RedirectToAction("Index", "TaiKhoan");
            else if (account.VaiTro == "NhanVien")
                return RedirectToAction("Index", "PhanCongGiaoHang");
            else
                return RedirectToAction("Index", "DonGiaoHang");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("AccessDenied");

            var list = await _context.TaiKhoans.ToListAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("AccessDenied");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Create(TaiKhoan taiKhoan)
        {
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == taiKhoan.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(taiKhoan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taiKhoan);
        }
    }
}