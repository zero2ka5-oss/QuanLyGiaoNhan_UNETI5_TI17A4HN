// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Lịch sử công việc của nhân viên giao hàng: các phân công đã kết thúc, lọc theo kết quả,
//                     tổng tiền cước các đơn đã giao thành công.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienLichSuController — URL: /NhanVienLichSu?ketQua=GiaoThanhCong|GiaoKhongThanhCong|DaHuyDon&trang=
 * Quyền: Nhân viên giao hàng – chỉ lịch sử của chính mình (MaNhanVien lấy từ Session).
 */
[YeuCauVaiTro(VaiTroNguoiDung.GiaoHang)]
public class NhanVienLichSuController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(KetQuaGiao? ketQua, int trang = 1)
    {
        int maNhanVien = HttpContext.Session.MaNhanVien() ?? -1;
        var truyVan = db.PhanCongGiaoHangs.AsNoTracking().Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc).Include(p => p.PhuongTien)
                        .Where(p => p.MaNhanVien == maNhanVien && !PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (ketQua.HasValue) truyVan = truyVan.Where(p => p.KetQua == ketQua);
        ViewBag.KetQua = ketQua;
        var cuaToi = db.PhanCongGiaoHangs.Where(p => p.MaNhanVien == maNhanVien);
        ViewBag.TongThanhCong = await cuaToi.CountAsync(p => p.KetQua == KetQuaGiao.GiaoThanhCong);
        ViewBag.TongThatBai = await cuaToi.CountAsync(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);
        ViewBag.TongKhoiLuong = await cuaToi.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong).SumAsync(p => (decimal?)p.DonGiaoHang!.KhoiLuong) ?? 0;
        ViewBag.TongTien = await cuaToi.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong).SumAsync(p => (decimal?)p.DonGiaoHang!.PhiVanChuyen) ?? 0;
        return View(await DanhSachTrang<PhanCongGiaoHang>.TaoAsync(truyVan.OrderByDescending(p => p.NgayKetThuc), trang, 10));
    }
}
