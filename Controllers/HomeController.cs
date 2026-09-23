using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
using System.Diagnostics;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers
{
    public class HomeController(ApplicationDbContext context) : AppControllerBase
    {
        public async Task<IActionResult> Index()
        {
            if (!HttpContext.Session.IsLoggedIn()) return View();
            if (IsCoordinator) return RedirectToAction("Index", "ThongKe");

            if (HttpContext.Session.HasRole("Khách hàng"))
            {
                var customerId = await context.KhachHangs.Where(x => x.MaTaiKhoan == CurrentAccountId).Select(x => x.MaKhachHang).FirstOrDefaultAsync();
                if (customerId == 0) return Forbid();
                var orders = context.DonGiaoHangs.Where(x => x.MaKhachHang == customerId);
                return View("CustomerDashboard", new CustomerDashboardViewModel
                {
                    HoTen = HttpContext.Session.GetString(SessionHelper.FullName) ?? string.Empty,
                    TongDonHang = await orders.CountAsync(),
                    DonChoPhanCong = await orders.CountAsync(x => x.TrangThai == "Chờ phân công"),
                    DonDangGiao = await orders.CountAsync(x => x.TrangThai == "Đang giao"),
                    DonHoanTat = await orders.CountAsync(x => x.TrangThai == "Hoàn tất"),
                    DonGanDay = await orders.OrderByDescending(x => x.NgayTao).Take(5).AsNoTracking().ToListAsync()
                });
            }

            if (HttpContext.Session.HasRole("Nhân viên giao hàng"))
            {
                var assignments = context.PhanCongGiaoHangs
                    .Include(x => x.DonGiaoHang)
                    .Where(x => x.NhanVienGiaoHang!.MaTaiKhoan == CurrentAccountId);
                return View("ShipperDashboard", new ShipperDashboardViewModel
                {
                    HoTen = HttpContext.Session.GetString(SessionHelper.FullName) ?? string.Empty,
                    TongPhanCong = await assignments.CountAsync(),
                    DonDangGiao = await assignments.CountAsync(x => x.TrangThai == "Đang giao"),
                    DonHoanTat = await assignments.CountAsync(x => x.TrangThai == "Hoàn tất"),
                    PhanCongGanDay = await assignments.OrderByDescending(x => x.NgayPhanCong).Take(5).AsNoTracking().ToListAsync()
                });
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
