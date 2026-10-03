// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Tiện ích ngay trong trang khách hàng: bảng giá + ước lượng phí, tra cứu đơn.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangTienIchController — URL: /KhachHangTienIch/{BangGia|TraCuu}
 * Quyền: Khách hàng. Hiển thị trong layout khách hàng (không chuyển ra trang công khai).
 * - BangGia: khối ước lượng phí dùng chung (_UocLuongPhi) – nút "Tạo đơn với thông tin này" điền sẵn form tạo đơn.
 * - TraCuu: tra cứu mọi đơn theo mã + số điện thoại người nhận (kể cả đơn người khác gửi cho mình),
 *           kèm lối tắt các đơn đang vận chuyển của chính khách hàng.
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangTienIchController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private int MaKhachHangCuaToi => HttpContext.Session.MaKhachHang() ?? -1;

    public async Task<IActionResult> BangGia() => View(await xuLy.BangGiaAsync(laKhachHang: true));

    public async Task<IActionResult> TraCuu(string? ma, string? sdt)
    {
        ViewBag.Ma = ma;
        ViewBag.Sdt = sdt;
        TrangThaiDon[] dangVanChuyen = [TrangThaiDon.ChoPhanCong, TrangThaiDon.DaPhanCong, TrangThaiDon.DaNhanHang, TrangThaiDon.DangGiao, TrangThaiDon.GiaoKhongThanhCong];
        ViewBag.DonCuaToi = await db.DonGiaoHangs.AsNoTracking()
            .Where(d => d.MaKhachHang == MaKhachHangCuaToi && dangVanChuyen.Contains(d.TrangThai))
            .OrderByDescending(d => d.NgayTao).Take(6).ToListAsync();
        if (string.IsNullOrWhiteSpace(ma) && string.IsNullOrWhiteSpace(sdt)) return View(null);
        var (don, loi) = await xuLy.TraCuuAsync(ma, sdt);
        ViewBag.Loi = loi;
        return View(don);
    }
}
