// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung bảng điều khiển Quản trị / Điều phối: số đơn theo trạng thái (đầy đủ ở tuần 5).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriBangDieuKhienController — URL: /QuanTriBangDieuKhien (trang đầu tiên sau khi Quản trị / Điều phối đăng nhập)
 * KHUNG tuần 1: đếm đơn theo trạng thái để các thành viên kiểm thử luồng; KPI, biểu đồ, việc cần xử lý làm ở tuần 5.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriBangDieuKhienController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.SoTheoTrangThai = await db.DonGiaoHangs.GroupBy(d => d.TrangThai)
                                          .ToDictionaryAsync(g => g.Key, g => g.Count());
        return View();
    }
}
