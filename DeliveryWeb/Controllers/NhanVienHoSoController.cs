// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Hồ sơ nhân viên giao hàng: thông tin cá nhân, xe đang dùng, tổng tiền cước đã giao,
//                     thống kê theo tháng; nhân viên tự cập nhật số điện thoại / email.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienHoSoController — URL: /NhanVienHoSo
 * Quyền: Nhân viên giao hàng – chỉ hồ sơ của chính mình (MaNhanVien lấy từ Session).
 * "Tổng tiền" = tổng phí vận chuyển (cước) của các đơn mà nhân viên đã giao THÀNH CÔNG (LINQ Sum trên PhanCongGiaoHang).
 * Họ tên, khu vực phụ trách, trạng thái do Quản trị / Điều phối quản lý; nhân viên chỉ sửa SĐT và email.
 */
[YeuCauVaiTro(VaiTroNguoiDung.GiaoHang)]
public class NhanVienHoSoController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private int MaNhanVienCuaToi => HttpContext.Session.MaNhanVien() ?? -1;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = await TaoHoSoAsync();
        if (vm is null) return RedirectToAction("KhongCoQuyen", "DangNhap");
        vm.LienHe = new CapNhatLienHeVM { SoDienThoai = vm.NhanVien.SoDienThoai, Email = vm.NhanVien.Email };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index([Bind(Prefix = "LienHe")] CapNhatLienHeVM lienHe)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == MaNhanVienCuaToi);
        if (nv is null) return NotFound();
        if (!ModelState.IsValid)
        {
            var vm = (await TaoHoSoAsync())!;
            vm.LienHe = lienHe;
            return View(vm);
        }
        nv.SoDienThoai = lienHe.SoDienThoai.Trim();
        nv.Email = lienHe.Email.Trim();
        if (nv.TaiKhoan is not null) nv.TaiKhoan.Email = nv.Email;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật thông tin liên hệ";
        return RedirectToAction(nameof(Index));
    }

    private async Task<HoSoNhanVienVM?> TaoHoSoAsync()
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == MaNhanVienCuaToi);
        if (nv is null) return null;

        var cuaToi = db.PhanCongGiaoHangs.AsNoTracking().Where(p => p.MaNhanVien == nv.MaNhanVien);
        var thanhCong = cuaToi.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong);
        int soThanhCong = await thanhCong.CountAsync();
        int soThatBai = await cuaToi.CountAsync(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);

        // Thống kê 6 tháng gần nhất: chiếu (ngày kết thúc, cước) rồi gom nhóm theo tháng
        var dauThang = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var tuThang = dauThang.AddMonths(-5);
        var ganDay = await thanhCong.Where(p => p.NgayKetThuc >= tuThang)
                                    .Select(p => new { p.NgayKetThuc, p.DonGiaoHang!.PhiVanChuyen }).ToListAsync();
        var theoThang = Enumerable.Range(0, 6).Select(i => tuThang.AddMonths(i)).Select(thang =>
        {
            var trongThang = ganDay.Where(x => x.NgayKetThuc!.Value.Year == thang.Year && x.NgayKetThuc.Value.Month == thang.Month).ToList();
            return new DongThongKe(thang.ToString("MM/yyyy"), trongThang.Count, trongThang.Sum(x => x.PhiVanChuyen));
        }).ToList();

        return new HoSoNhanVienVM
        {
            NhanVien = nv,
            TenDangNhap = nv.TaiKhoan?.TenDangNhap,
            XeDangDung = await xuLy.PhanCongHieuLuc().Where(p => p.MaNhanVien == nv.MaNhanVien).Select(p => p.PhuongTien).FirstOrDefaultAsync(),
            SoDonDangGiu = await xuLy.PhanCongHieuLuc().CountAsync(p => p.MaNhanVien == nv.MaNhanVien),
            TongThanhCong = soThanhCong,
            TongThatBai = soThatBai,
            TiLeThanhCong = soThanhCong + soThatBai == 0 ? 0 : Math.Round(100.0 * soThanhCong / (soThanhCong + soThatBai), 1),
            TongTien = await thanhCong.SumAsync(p => (decimal?)p.DonGiaoHang!.PhiVanChuyen) ?? 0,
            TongKhoiLuong = await thanhCong.SumAsync(p => (decimal?)p.DonGiaoHang!.KhoiLuong) ?? 0,
            TienThangNay = theoThang[^1].GiaTriPhu ?? 0,
            // Tiền đã thu của người nhận nhưng đơn chưa hoàn tất (chưa nộp / đối soát)
            TienDangGiu = await thanhCong.Where(p => p.DonGiaoHang!.TrangThai == TrangThaiDon.GiaoThanhCong && p.DonGiaoHang.MaGiaoDichNop == null)
                .SumAsync(p => (decimal?)(p.DonGiaoHang!.TienThuHo + (p.DonGiaoHang.NguoiTraPhi == NguoiTraPhi.NguoiNhan ? p.DonGiaoHang.PhiVanChuyen : 0))) ?? 0,
            SoDonDangGiuTien = await thanhCong.CountAsync(p => p.DonGiaoHang!.TrangThai == TrangThaiDon.GiaoThanhCong && p.DonGiaoHang.MaGiaoDichNop == null),
            DonThangNay = (int)theoThang[^1].GiaTri,
            TheoThang = theoThang
        };
    }
}
