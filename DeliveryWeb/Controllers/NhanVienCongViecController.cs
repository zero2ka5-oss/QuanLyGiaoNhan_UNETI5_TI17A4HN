// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Công việc của nhân viên giao hàng: danh sách đơn được giao, xác nhận nhận hàng,
//                     bắt đầu giao, giao thành công, giao không thành công (bắt buộc lý do).

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienCongViecController — URL: /NhanVienCongViec/{Index|ChiTiet|NhanHang|BatDauGiao|GiaoThanhCong|GiaoKhongThanhCong}
 * Quyền: Nhân viên giao hàng – chỉ thao tác phân công của CHÍNH MÌNH
 *        (MaNhanVien lấy từ Session; DonHangXuLy kiểm tra lại; phân công của người khác → 404).
 * Thứ tự bắt buộc: Đã phân công → Nhận hàng → Bắt đầu giao → Thành công / Không thành công.
 */
[YeuCauVaiTro(VaiTroNguoiDung.GiaoHang)]
public class NhanVienCongViecController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    private int MaNhanVienCuaToi => HttpContext.Session.MaNhanVien() ?? -1;

    // ===================== CÔNG VIỆC HÔM NAY =====================

    public async Task<IActionResult> Index()
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == MaNhanVienCuaToi);
        if (nv is null) return RedirectToAction("KhongCoQuyen", "DangNhap");
        var dsHieuLuc = await xuLy.PhanCongHieuLuc().AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LoaiHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhachHang)
            .Include(p => p.PhuongTien)
            .Where(p => p.MaNhanVien == nv.MaNhanVien)
            .OrderBy(p => p.DonGiaoHang!.NgayGiaoDuKien).ThenBy(p => p.NgayPhanCong).ToListAsync();
        var homNay = DateTime.Today;
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
        var thanhCong = db.PhanCongGiaoHangs.Where(p => p.MaNhanVien == nv.MaNhanVien && p.KetQua == KetQuaGiao.GiaoThanhCong);
        return View(new CongViecNhanVienVM
        {
            NhanVien = nv,
            CanNhanHang = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DaPhanCong).ToList(),
            DaNhanHang = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DaNhanHang).ToList(),
            DangGiao = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DangGiao).ToList(),
            GiaoThanhCongHomNay = await db.PhanCongGiaoHangs.CountAsync(p => p.MaNhanVien == nv.MaNhanVien && p.KetQua == KetQuaGiao.GiaoThanhCong && p.NgayKetThuc >= homNay),
            ThatBaiHomNay = await db.PhanCongGiaoHangs.CountAsync(p => p.MaNhanVien == nv.MaNhanVien && p.KetQua == KetQuaGiao.GiaoKhongThanhCong && p.NgayKetThuc >= homNay),
            // Tổng tiền = tổng cước (phí vận chuyển) của các đơn đã giao thành công
            TienHomNay = await thanhCong.Where(p => p.NgayKetThuc >= homNay).SumAsync(p => (decimal?)p.DonGiaoHang!.PhiVanChuyen) ?? 0,
            TienThangNay = await thanhCong.Where(p => p.NgayKetThuc >= dauThang).SumAsync(p => (decimal?)p.DonGiaoHang!.PhiVanChuyen) ?? 0,
            TienCanThu = dsHieuLuc.Sum(p => p.DonGiaoHang!.TongThuNguoiNhan)
        });
    }

    /// <summary>Chi tiết một phân công – nhân viên chỉ xem được phân công của mình (kể cả đã kết thúc).</summary>
    public async Task<IActionResult> ChiTiet(int id)
    {
        var pc = await db.PhanCongGiaoHangs.AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LoaiHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhachHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LichSus)
            .Include(p => p.PhuongTien)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (pc is null || pc.MaNhanVien != MaNhanVienCuaToi) return NotFound();
        return View(pc);
    }

    // ===================== CẬP NHẬT TRẠNG THÁI GIAO =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanHang(int id) =>
        KetQuaThaoTac(await xuLy.NhanHangAsync(id, MaNhanVienCuaToi, HttpContext.Session.NguoiThucHien()), id);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatDauGiao(int id) =>
        KetQuaThaoTac(await xuLy.BatDauGiaoAsync(id, MaNhanVienCuaToi, HttpContext.Session.NguoiThucHien()), id);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoThanhCong(int id, string? ghiChu, IFormFile? anhGiaoHang)
    {
        // Ảnh giao hàng không bắt buộc; có chọn ảnh mà ảnh lỗi thì dừng lại để nhân viên chụp lại
        string? anh = null;
        if (anhGiaoHang is { Length: > 0 })
        {
            (anh, string? loi) = await anhXuLy.LuuAsync(anhGiaoHang, AnhXuLy.ThuMucGiaoHang);
            if (anh is null) { TempData["Error"] = loi; return RedirectToAction(nameof(ChiTiet), new { id }); }
        }
        var kq = await xuLy.GiaoThanhCongAsync(id, MaNhanVienCuaToi, ghiChu, HttpContext.Session.NguoiThucHien());
        if (anh is not null)
        {
            var don = kq.ThanhCong ? await db.PhanCongGiaoHangs.Where(p => p.MaPhanCong == id).Select(p => p.DonGiaoHang).FirstOrDefaultAsync() : null;
            if (don is null) anhXuLy.Xoa(anh);
            else { don.AnhGiaoHang = anh; await db.SaveChangesAsync(); }
        }
        return KetQuaThaoTac(kq, id);
    }

    public async Task<IActionResult> GiaoKhongThanhCong(int id)
    {
        var pc = await db.PhanCongGiaoHangs.AsNoTracking().Include(p => p.DonGiaoHang).FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (pc is null || pc.MaNhanVien != MaNhanVienCuaToi) return NotFound();
        if (pc.TrangThai != TrangThaiPhanCong.DangGiao)
        {
            TempData["Error"] = "Chỉ đơn Đang giao mới được ghi nhận giao không thành công";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        return View(new GiaoThatBaiVM { MaPhanCong = id, Don = pc.DonGiaoHang });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoKhongThanhCong(GiaoThatBaiVM vm)
    {
        if (vm.LyDo == LyDoThatBai.Khac && string.IsNullOrWhiteSpace(vm.ChiTiet))
            ModelState.AddModelError(nameof(vm.ChiTiet), "Mô tả lý do khi chọn 'Lý do khác'");
        if (ModelState.IsValid)
        {
            var kq = await xuLy.GiaoThatBaiAsync(vm.MaPhanCong, MaNhanVienCuaToi, vm.LyDo, vm.ChiTiet, HttpContext.Session.NguoiThucHien());
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao + ". Điều phối sẽ sắp xếp giao lại.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        vm.Don = await db.PhanCongGiaoHangs.Where(p => p.MaPhanCong == vm.MaPhanCong && p.MaNhanVien == MaNhanVienCuaToi)
                         .Select(p => p.DonGiaoHang).FirstOrDefaultAsync();
        if (vm.Don is null) return NotFound();
        return View(vm);
    }

    private IActionResult KetQuaThaoTac(KetQua kq, int maPhanCong)
    {
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id = maPhanCong });
    }
}
