// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Tiện ích: bảng giá + ước lượng phí (đã chạy); tra cứu đơn (khung – hoàn thiện tuần 3).

using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangTienIchController — URL: /KhachHangTienIch/{BangGia|TraCuu}
 * Quyền: Khách hàng. Hiển thị trong layout khách hàng (không chuyển ra trang công khai).
 * - BangGia: khối ước lượng phí dùng chung (_UocLuongPhi) – nút "Tạo đơn với thông tin này" điền sẵn form tạo đơn.
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangTienIchController(DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> BangGia() => View(await xuLy.BangGiaAsync(laKhachHang: true));

    public IActionResult TraCuu() => View();
}
