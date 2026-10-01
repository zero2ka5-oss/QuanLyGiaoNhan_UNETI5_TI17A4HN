// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung trang lịch sử giao nhận toàn hệ thống: 50 mốc mới nhất (bộ lọc hoàn thiện tuần 3).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriLichSuGiaoNhanController — URL: /QuanTriLichSuGiaoNhan
 * Quyền: Quản trị, Điều phối. Chỉ đọc – lịch sử không bao giờ bị sửa / xóa.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriLichSuGiaoNhanController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.LichSuGiaoNhans.AsNoTracking().Include(l => l.DonGiaoHang)
                     .OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).Take(50).ToListAsync());
}
