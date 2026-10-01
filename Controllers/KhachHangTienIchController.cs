// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Khung tiện ích: bảng giá + ước lượng phí, tra cứu đơn (hoàn thiện tuần 2, 3).

using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangTienIchController — URL: /KhachHangTienIch/{BangGia|TraCuu}
 * Quyền: Khách hàng. KHUNG tuần 1: hai trang hiển thị thông báo đang phát triển.
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangTienIchController : Controller
{
    public IActionResult BangGia() => View();

    public IActionResult TraCuu() => View();
}
