// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Trang chủ công khai: xem trước phí vận chuyển (AJAX) và tra cứu đơn.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của TrangChuController (partial).</summary>
public partial class TrangChuController
{
    /// <summary>
    /// Xem trước phí (AJAX) – dùng ở form tạo / sửa đơn của Khách hàng, Quản trị và khối Ước lượng phí.
    /// GET /TrangChu/TinhPhi?maLoaiHang=1&maKhuVuc=2&khoiLuong=3.5&maBuuCucGui=3 → JSON các thành phần phí.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TinhPhi(int maLoaiHang, int maKhuVuc, decimal khoiLuong, decimal kichThuocM3 = 0.005m, int? maBuuCucGui = null, bool giuLoai = false, bool giuKhuVuc = false)
    {
        var loai = await db.LoaiHangs.FindAsync(maLoaiHang);
        var khuVuc = await db.KhuVucs.FindAsync(maKhuVuc);
        if (loai is null || khuVuc is null || khoiLuong <= 0 || khoiLuong > 100_000 || kichThuocM3 <= 0)
            return BadRequest(new { thongBao = "Chọn loại hàng, khu vực và nhập khối lượng > 0" });
        if (loai.TrangThai != TrangThaiHoatDong.HoatDong && !giuLoai)
            return BadRequest(new { thongBao = $"Loại hàng '{loai.TenLoaiHang}' đã ngừng nhận" });
        if (khuVuc.TrangThai != TrangThaiHoatDong.HoatDong && !giuKhuVuc)
            return BadRequest(new { thongBao = $"Khu vực '{khuVuc.TenKhuVuc}' đã ngừng nhận đơn mới" });
        decimal toiDa = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                            .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        if (toiDa > 0 && khoiLuong > toiDa)
            return BadRequest(new { thongBao = $"Khối lượng tối đa một đơn là {DinhDang.KhoiLuong(toiDa)}" });

        // Tính khoảng cách giữa Bưu cục gửi và Bưu cục nhận (phụ trách khu vực)
        double? khoangCachKm = null;
        string? phanLoaiCuLy = null;
        int soNgayGiao = 2;
        decimal phiCoBan = khuVuc.PhiCoBan;

        if (maBuuCucGui.HasValue && maBuuCucGui.Value > 0)
        {
            var buuCucGui = await db.BuuCucs.FindAsync(maBuuCucGui.Value);
            var buuCucNhan = khuVuc.MaBuuCuc.HasValue ? await db.BuuCucs.FindAsync(khuVuc.MaBuuCuc.Value) : null;
            if (buuCucGui?.ViDo != null && buuCucGui?.KinhDo != null && buuCucNhan?.ViDo != null && buuCucNhan?.KinhDo != null)
            {
                khoangCachKm = TinhPhiXuLy.TinhKhoangCachKm(buuCucGui.ViDo.Value, buuCucGui.KinhDo.Value, buuCucNhan.ViDo.Value, buuCucNhan.KinhDo.Value);
                var kqCuLy = TinhPhiXuLy.LayPhiCoBanTheoKhoangCach(khoangCachKm, khuVuc.PhiCoBan);
                phiCoBan = kqCuLy.PhiCoBan;
                phanLoaiCuLy = kqCuLy.TenCuLy;
                soNgayGiao = kqCuLy.SoNgay;
            }
        }

        var phi = TinhPhiXuLy.TinhPhi(phiCoBan, khoiLuong, kichThuocM3, loai.HeSoPhuThu, soNgayGiao);
        var ngayGiaoDuKien = DateTime.Today.AddDays(soNgayGiao);
        return Json(new
        {
            phi.PhiCoBan, phi.KhoiLuongVuot, phi.PhuPhiKhoiLuong,
            phi.PhuPhiKichThuoc, moTaKichThuoc = phi.MoTaKichThuoc,
            phi.HeSoPhuThu, phi.PhuPhiLoaiHang, phi.Tong,
            khoangCachKm, phanLoaiCuLy, soNgayGiao,
            ngayGiaoDuKien = ngayGiaoDuKien.ToString("yyyy-MM-dd"),
            ngayGiaoDuKienHienThi = ngayGiaoDuKien.ToString("dd/MM/yyyy"),
            nguongKg = TinhPhiXuLy.NguongKhoiLuongKg, donGiaVuot = TinhPhiXuLy.DonGiaVuotMoiKg
        });
    }

    public async Task<IActionResult> TraCuu(string? ma, string? sdt)
    {
        ViewBag.Ma = ma;
        ViewBag.Sdt = sdt;
        if (string.IsNullOrWhiteSpace(ma) && string.IsNullOrWhiteSpace(sdt)) return View(null);

        var (don, loi) = await xuLy.TraCuuAsync(ma, sdt);
        ViewBag.Loi = loi;
        return View(don);
    }
}
