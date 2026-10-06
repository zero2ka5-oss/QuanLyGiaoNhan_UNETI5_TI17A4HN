// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Tra cứu lịch sử giao nhận toàn hệ thống (lọc theo đơn, trạng thái, người thực hiện, ngày;
//                     phân trang làm ở tuần 4).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriLichSuGiaoNhanController — URL: /QuanTriLichSuGiaoNhan?tuKhoa=&trangThaiMoi=&tuNgay=&denNgay=
 * Quyền: Quản trị, Điều phối. Chỉ đọc – lịch sử không bao giờ bị sửa / xóa.
 * Khách hàng xem lịch sử đơn của mình tại DonGiaoHang/ChiTiet; nhân viên tại PhanCongGiaoHang/ChiTiet.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriLichSuGiaoNhanController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiDon? trangThaiMoi, DateTime? tuNgay, DateTime? denNgay)
    {
        var truyVan = db.LichSuGiaoNhans.AsNoTracking().Include(l => l.DonGiaoHang).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            int? maDon = DinhDang.TachMaDon(tuKhoa);
            // So khớp không phân biệt hoa thường và dấu: gõ "huong" vẫn ra "Hướng"
            truyVan = truyVan.Where(l => (maDon != null && l.MaDon == maDon)
                || EF.Functions.Collate(l.NguoiThucHien, "Latin1_General_100_CI_AI").Contains(tuKhoa)
                || EF.Functions.Collate(l.NoiDung, "Latin1_General_100_CI_AI").Contains(tuKhoa));
        }
        if (tuNgay > denNgay) (tuNgay, denNgay) = (denNgay, tuNgay);
        if (trangThaiMoi.HasValue) truyVan = truyVan.Where(l => l.TrangThaiMoi == trangThaiMoi);
        if (tuNgay.HasValue) truyVan = truyVan.Where(l => l.ThoiGian >= tuNgay.Value.Date);
        if (denNgay.HasValue) truyVan = truyVan.Where(l => l.ThoiGian < denNgay.Value.Date.AddDays(1));

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThaiMoi = trangThaiMoi;
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
        // Tạm hiện 100 mốc mới nhất – phân trang làm ở tuần 4
        return View(await truyVan.OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).Take(100).ToListAsync());
    }
}
