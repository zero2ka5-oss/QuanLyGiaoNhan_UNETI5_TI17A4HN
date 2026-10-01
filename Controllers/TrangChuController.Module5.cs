// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung trang chủ công khai: xem trước phí (AJAX) và tra cứu đơn (hoàn thiện tuần 2, 3).

using Microsoft.AspNetCore.Mvc;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của TrangChuController (partial) – KHUNG tuần 1.</summary>
public partial class TrangChuController
{
    [HttpGet]
    public IActionResult TinhPhi(int maLoaiHang, int maKhuVuc, decimal khoiLuong) =>
        BadRequest(new { thongBao = "Chức năng xem trước phí đang được phát triển" });

    public IActionResult TraCuu(string? ma, string? sdt)
    {
        ViewBag.Ma = ma;
        ViewBag.Sdt = sdt;
        return View();
    }
}
