// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Điều phối giao hàng: bảng đơn chờ phân công / đang thực hiện / lịch sử,
//                     phân công (kiểm tra nguồn lực), đổi phân công (bảo toàn lịch sử), phân công giao lại.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriPhanCongController — URL: /QuanTriPhanCong/{Index|TaoMoi|DoiPhanCong}
 * Quyền: Quản trị, Điều phối.
 *   Index = bảng điều phối: tab cho (chờ phân công / cần giao lại) | dang (đang thực hiện) | lichsu (toàn bộ phân công).
 *   Kiểm tra xung đột nguồn lực nằm trong DonHangXuLy.PhanCongAsync / DoiPhanCongAsync.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriPhanCongController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> Index(string tab = "cho", int? maNhanVien = null, int? maKhuVuc = null, int trang = 1)
    {
        ViewBag.Tab = tab;
        ViewBag.MaNhanVien = maNhanVien;
        ViewBag.MaKhuVuc = maKhuVuc;
        ViewBag.DsNhanVien = await db.NhanVienGiaoHangs.AsNoTracking().OrderBy(n => n.HoTen).ToListAsync();
        ViewBag.DsKhuVuc = await db.KhuVucs.AsNoTracking().OrderBy(k => k.PhiCoBan).ToListAsync();
        ViewBag.SoChoPhanCong = await db.DonGiaoHangs.CountAsync(d => d.TrangThai == TrangThaiDon.ChoPhanCong || d.TrangThai == TrangThaiDon.GiaoKhongThanhCong);
        ViewBag.SoDangThucHien = await xuLy.PhanCongHieuLuc().CountAsync();
        ViewBag.SoNhanVienSanSang = await db.NhanVienGiaoHangs.CountAsync(n => n.TrangThai == TrangThaiNhanVien.SanSang || n.TrangThai == TrangThaiNhanVien.DangGiaoHang);
        ViewBag.SoXeSanSang = await db.PhuongTiens.CountAsync(p => p.TrangThai == TrangThaiPhuongTien.SanSang);

        if (tab == "cho")
        {
            var truyVan = db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                            .Where(d => d.TrangThai == TrangThaiDon.ChoPhanCong || d.TrangThai == TrangThaiDon.GiaoKhongThanhCong);
            if (maKhuVuc.HasValue) truyVan = truyVan.Where(d => d.MaKhuVuc == maKhuVuc);
            ViewBag.DsDonCho = await DanhSachTrang<DonGiaoHang>.TaoAsync(
                truyVan.OrderByDescending(d => d.TrangThai).ThenBy(d => d.NgayGiaoDuKien).ThenBy(d => d.NgayTao), trang, 15);
        }
        else
        {
            var truyVan = db.PhanCongGiaoHangs.AsNoTracking()
                .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc).Include(p => p.NhanVien).Include(p => p.PhuongTien).AsQueryable();
            if (tab == "dang") truyVan = truyVan.Where(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (maNhanVien.HasValue) truyVan = truyVan.Where(p => p.MaNhanVien == maNhanVien);
            if (maKhuVuc.HasValue) truyVan = truyVan.Where(p => p.DonGiaoHang!.MaKhuVuc == maKhuVuc);
            ViewBag.DsPhanCong = await DanhSachTrang<PhanCongGiaoHang>.TaoAsync(
                truyVan.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong), trang, 15);
        }
        return View();
    }

    // ===================== PHÂN CÔNG / GIAO LẠI =====================

    public async Task<IActionResult> TaoMoi(int maDon)
    {
        var vm = await TaoFormAsync(maDon, false);
        if (vm is null) return NotFound();
        if (!DonHangXuLy.CoThePhanCong(vm.Don!.TrangThai))
        {
            TempData["Error"] = $"Đơn {vm.Don.MaHienThi} đang '{vm.Don.TrangThai.TenHienThi()}' – không thể phân công";
            return RedirectToAction("ChiTiet", "QuanTriDonGiaoHang", new { id = maDon });
        }
        if (DonHangXuLy.SoLanThatBai(vm.Don) >= DonHangXuLy.SoLanGiaoToiDa)
        {
            TempData["Error"] = $"Đơn {vm.Don.MaHienThi} đã giao không thành công {DonHangXuLy.SoLanGiaoToiDa} lần – hãy liên hệ khách hàng và hủy / chuyển hoàn đơn";
            return RedirectToAction("ChiTiet", "QuanTriDonGiaoHang", new { id = maDon });
        }
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(PhanCongFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var kq = await xuLy.PhanCongAsync(vm.MaDon, vm.MaNhanVien, vm.MaPhuongTien, vm.GhiChu, HttpContext.Session.NguoiThucHien());
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        var form = await TaoFormAsync(vm.MaDon, false);
        if (form is null) return NotFound();
        form.MaNhanVien = vm.MaNhanVien; form.MaPhuongTien = vm.MaPhuongTien; form.GhiChu = vm.GhiChu;
        return View("BieuMau", form);
    }

    // ===================== ĐỔI PHÂN CÔNG =====================

    public async Task<IActionResult> DoiPhanCong(int maDon)
    {
        var vm = await TaoFormAsync(maDon, true);
        if (vm is null) return NotFound();
        if (vm.PhanCongHienTai is null || !DonHangXuLy.CoTheDoiPhanCong(vm.Don!.TrangThai))
        {
            TempData["Error"] = "Chỉ đổi phân công khi đơn Đã phân công / Đã nhận hàng";
            return RedirectToAction("ChiTiet", "QuanTriDonGiaoHang", new { id = maDon });
        }
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiPhanCong(PhanCongFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var kq = await xuLy.DoiPhanCongAsync(vm.MaDon, vm.MaNhanVien, vm.MaPhuongTien, vm.GhiChu, HttpContext.Session.NguoiThucHien());
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao;
                return RedirectToAction("ChiTiet", "QuanTriDonGiaoHang", new { id = vm.MaDon });
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        var form = await TaoFormAsync(vm.MaDon, true);
        if (form is null) return NotFound();
        form.MaNhanVien = vm.MaNhanVien; form.MaPhuongTien = vm.MaPhuongTien; form.GhiChu = vm.GhiChu;
        return View("BieuMau", form);
    }

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Nạp đơn + danh sách nhân viên / phương tiện kèm lý do không chọn được (đổi phân công thì bỏ qua phân công hiện tại).</summary>
    private async Task<PhanCongFormVM?> TaoFormAsync(int maDon, bool laDoi)
    {
        var don = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
                          .AsSplitQuery()
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return null;
        var hienTai = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        var (dsNv, dsPt) = await xuLy.LayLuaChonAsync(don, laDoi ? hienTai?.MaPhanCong : null);
        return new PhanCongFormVM
        {
            MaDon = maDon, Don = don, PhanCongHienTai = hienTai, DsNhanVien = dsNv, DsPhuongTien = dsPt, LaDoiPhanCong = laDoi,
            MaNhanVien = hienTai?.MaNhanVien ?? 0, MaPhuongTien = hienTai?.MaPhuongTien ?? 0
        };
    }
}
