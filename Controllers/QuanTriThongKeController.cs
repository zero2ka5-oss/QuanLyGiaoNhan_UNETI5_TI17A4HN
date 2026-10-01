// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung trang thống kê (thống kê LINQ đầy đủ ở tuần 6).

using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>QuanTriThongKeController — URL: /QuanTriThongKe. Quyền: chỉ Quản trị. KHUNG tuần 1.</summary>
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriThongKeController : Controller
{
    public IActionResult Index() => View();
}
