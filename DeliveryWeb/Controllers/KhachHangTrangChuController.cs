// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Trang tổng quan: số đơn theo tình trạng, tổng phí, đơn gần đây.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangTrangChuController — URL: /KhachHangTrangChu (trang chủ sau khi khách hàng đăng nhập)
 * Quyền: Khách hàng. Mã khách hàng lấy từ Session, không nhận từ URL.
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangTrangChuController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> Index()
    {
        var kh = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKhachHang == HttpContext.Session.MaKhachHang());
        if (kh is null) return RedirectToAction("KhongCoQuyen", "DangNhap");
        return View(await xuLy.TongQuanKhachHangAsync(kh, 6));
    }
}
