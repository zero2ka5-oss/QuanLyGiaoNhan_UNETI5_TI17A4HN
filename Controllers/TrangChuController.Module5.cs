// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Trang chủ công khai: xem trước phí vận chuyển (AJAX); tra cứu đơn (khung – hoàn thiện tuần 3).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của TrangChuController (partial): TinhPhi, TraCuu.</summary>
public partial class TrangChuController
{
    /// <summary>
    /// Xem trước phí (AJAX) – dùng ở form tạo / sửa đơn của Khách hàng và Quản trị.
    /// GET /TrangChu/TinhPhi?maLoaiHang=1&amp;maKhuVuc=2&amp;khoiLuong=3.5 → JSON các thành phần phí.
    /// Chỉ để hiển thị; khi lưu đơn, DonHangXuLy luôn tính lại phí trên server.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TinhPhi(int maLoaiHang, int maKhuVuc, decimal khoiLuong)
    {
        var loai = await db.LoaiHangs.FindAsync(maLoaiHang);
        var khuVuc = await db.KhuVucs.FindAsync(maKhuVuc);
        if (loai is null || khuVuc is null || khoiLuong <= 0 || khoiLuong > 100_000)
            return BadRequest(new { thongBao = "Chọn loại hàng, khu vực và nhập khối lượng > 0" });
        decimal toiDa = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                            .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        if (toiDa > 0 && khoiLuong > toiDa)
            return BadRequest(new { thongBao = $"Khối lượng tối đa một đơn là {DinhDang.KhoiLuong(toiDa)}" });
        var phi = TinhPhiXuLy.TinhPhi(khuVuc.PhiCoBan, khoiLuong, loai.HeSoPhuThu);
        return Json(new
        {
            phi.PhiCoBan, phi.KhoiLuongVuot, phi.PhuPhiKhoiLuong, phi.HeSoPhuThu, phi.PhuPhiLoaiHang, phi.Tong,
            nguongKg = TinhPhiXuLy.NguongKhoiLuongKg, donGiaVuot = TinhPhiXuLy.DonGiaVuotMoiKg
        });
    }

    /// <summary>KHUNG: tra cứu đơn hoàn thiện ở tuần 3.</summary>
    public IActionResult TraCuu(string? ma, string? sdt)
    {
        ViewBag.Ma = ma;
        ViewBag.Sdt = sdt;
        return View();
    }
}
