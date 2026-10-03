// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Xem và cập nhật thông tin cá nhân.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangHoSoController — URL: /KhachHangHoSo
 * Quyền: Khách hàng – chỉ sửa hồ sơ của chính mình (mã khách hàng lấy từ Session, không nhận từ form).
 * Họ tên / email đồng bộ sang tài khoản và cập nhật lại Session để thanh trên cùng hiển thị tên mới.
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangHoSoController(QuanLyGiaoNhanDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var kh = await db.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == HttpContext.Session.MaKhachHang());
        if (kh is null) return NotFound();
        await NapThongTinAsync(kh);
        return View(new KhachHangFormVM
        {
            MaKhachHang = kh.MaKhachHang, HoTen = kh.HoTen, SoDienThoai = kh.SoDienThoai, Email = kh.Email, DiaChi = kh.DiaChi,
            TenDangNhap = kh.TaiKhoan?.TenDangNhap, CoTaiKhoan = true
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(KhachHangFormVM vm)
    {
        ModelState.Remove(nameof(vm.TenDangNhap));
        ModelState.Remove(nameof(vm.MatKhau));
        var kh = await db.KhachHangs.Include(k => k.TaiKhoan).FirstOrDefaultAsync(k => k.MaKhachHang == HttpContext.Session.MaKhachHang());
        if (kh is null) return NotFound();
        if (!ModelState.IsValid) { vm.TenDangNhap = kh.TaiKhoan?.TenDangNhap; vm.CoTaiKhoan = true; await NapThongTinAsync(kh); return View(vm); }

        kh.HoTen = vm.HoTen.Trim();
        kh.SoDienThoai = vm.SoDienThoai;
        kh.Email = vm.Email?.Trim();
        kh.DiaChi = vm.DiaChi.Trim();
        if (kh.TaiKhoan is not null)
        {
            kh.TaiKhoan.HoTen = kh.HoTen;
            if (!string.IsNullOrEmpty(kh.Email)) kh.TaiKhoan.Email = kh.Email;
            HttpContext.Session.GhiPhien(kh.TaiKhoan, kh.MaKhachHang, null);   // cập nhật họ tên hiển thị
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin cá nhân";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Số liệu cho thẻ hồ sơ: ngày tham gia, tổng số đơn, số đơn giao thành công, tổng phí đã dùng.</summary>
    private async Task NapThongTinAsync(KhachHang kh)
    {
        ViewBag.NgayDangKy = kh.NgayDangKy;
        ViewBag.SoDon = await db.DonGiaoHangs.CountAsync(d => d.MaKhachHang == kh.MaKhachHang);
        ViewBag.SoDaGiao = await db.DonGiaoHangs.CountAsync(d => d.MaKhachHang == kh.MaKhachHang
                                                              && (d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat));
        ViewBag.TongPhi = await db.DonGiaoHangs.Where(d => d.MaKhachHang == kh.MaKhachHang && d.TrangThai != TrangThaiDon.DaHuy)
                                               .SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0;
    }
}
