// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Controller Xử lý Đăng nhập, Đăng ký và Đăng xuất

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.ViewModels;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Helpers;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class DangNhapController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public DangNhapController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Index(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var account = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.TenDangNhap == model.TenDangNhap && t.MatKhau == model.MatKhau);

            if (account == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            if (!account.TrangThai)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa.");
                return View(model);
            }

            SessionHelper.SetObjectAsJson(HttpContext.Session, "UserLogin", account);[cite: 3]
            HttpContext.Session.SetString("VaiTro", account.VaiTro);
            HttpContext.Session.SetString("HoTen", account.HoTen);

            if (account.VaiTro == "Admin")
                return RedirectToAction("Index", "QuanTriBangDieuKhien");
            if (account.VaiTro == "NhanVien")
                return RedirectToAction("Index", "NhanVienCongViec");

            return RedirectToAction("Index", "KhachHangTrangChu");
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task DangKy(TaiKhoan model)
        {
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                model.VaiTro = "KhachHang";
                model.TrangThai = true;
                model.NgayTao = DateTime.Now;

                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult KhongCoQuyen()
        {
            return View();
        }
    }
}