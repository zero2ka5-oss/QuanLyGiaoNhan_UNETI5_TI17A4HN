using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriPhanCongController — URL: /QuanTriPhanCong/{Index|TaoMoi|DoiPhanCong}
 * Quyền: Quản trị, Điều phối (mọi bưu cục), Nhân viên bưu cục (chỉ bưu cục của mình).
 *   Index = bảng điều phối theo bưu cục:
 *     lientinh – đơn đã tiếp nhận ở bưu cục gửi, chờ phân shipper liên tỉnh
 *     khuvuc   – đơn đã đến bưu cục nhận, chờ phân shipper khu vực (kể cả giao lại sau khi nhận lại hàng)
 *     dang     – các chặng đang thực hiện;  lichsu – toàn bộ phân công
 *   Phương tiện không chọn trên form: là xe được gán cố định cho shipper. Kiểm tra nằm trong DonHangXuLy.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.NhanVien)]
public class QuanTriPhanCongController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    /// <summary>Nhân viên bưu cục bị khóa theo bưu cục của mình; quản trị / điều phối chọn bưu cục tùy ý (null = tất cả).</summary>
    private int? BuuCucCuaToi => HttpContext.Session.MaBuuCuc();

    public async Task<IActionResult> Index(string tab = "lientinh", int? maBuuCuc = null, int? maNhanVien = null, int trang = 1)
    {
        maBuuCuc = BuuCucCuaToi ?? maBuuCuc;
        ViewBag.Tab = tab;
        ViewBag.MaBuuCuc = maBuuCuc;
        ViewBag.MaNhanVien = maNhanVien;
        ViewBag.KhoaBuuCuc = BuuCucCuaToi is not null;
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();
        ViewBag.DsNhanVien = await db.NhanVienGiaoHangs.AsNoTracking().Where(n => maBuuCuc == null || n.MaBuuCuc == maBuuCuc)
                                     .OrderBy(n => n.HoTen).ToListAsync();

        var taiBuuCuc = db.DonGiaoHangs.AsNoTracking().Where(d => maBuuCuc == null || d.MaBuuCucHienTai == maBuuCuc);
        var choLienTinh = taiBuuCuc.Where(d => (d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui && d.MaBuuCucGui != d.MaBuuCucNhan) || d.TrangThai == TrangThaiDon.ChoChuyenHoan);
        var choKhuVuc = taiBuuCuc.Where(d => d.TrangThai == TrangThaiDon.DaDenBuuCucNhan || d.TrangThai == TrangThaiDon.DaVeBuuCucGui);
        var choLayHang = db.DonGiaoHangs.AsNoTracking().Where(d => d.TrangThai == TrangThaiDon.ChoLayTanNoi && (maBuuCuc == null || d.MaBuuCucGui == maBuuCuc) && !d.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)));
        var hieuLuc = xuLy.PhanCongHieuLuc().Where(p => maBuuCuc == null || p.NhanVien!.MaBuuCuc == maBuuCuc);
        ViewBag.SoChoLienTinh = await choLienTinh.CountAsync();
        ViewBag.SoChoKhuVuc = await choKhuVuc.CountAsync();
        ViewBag.SoChoLayHang = await choLayHang.CountAsync();
        ViewBag.SoDangThucHien = await hieuLuc.CountAsync();
        ViewBag.SoShipperSanSang = await db.NhanVienGiaoHangs.CountAsync(n => (maBuuCuc == null || n.MaBuuCuc == maBuuCuc)
            && (n.TrangThai == TrangThaiNhanVien.SanSang || n.TrangThai == TrangThaiNhanVien.DangGiaoHang) && n.MaPhuongTien != null);

        if (tab is "lientinh" or "khuvuc" or "layhang")
        {
            var truyVan = (tab == "lientinh" ? choLienTinh : (tab == "khuvuc" ? choKhuVuc : choLayHang))
                .Include(d => d.KhachHang).Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                .Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.PhanCongs);
            ViewBag.DsDonCho = await DanhSachTrang<DonGiaoHang>.TaoAsync(
                truyVan.OrderBy(d => d.NgayGiaoDuKien).ThenBy(d => d.NgayTao), trang, 25);
        }
        else
        {
            var truyVan = (tab == "dang" ? hieuLuc : db.PhanCongGiaoHangs.Where(p => maBuuCuc == null || p.NhanVien!.MaBuuCuc == maBuuCuc))
                .AsNoTracking()
                .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
                .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucGui)
                .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucNhan)
                .Include(p => p.NhanVien).Include(p => p.PhuongTien).AsQueryable();
            if (maNhanVien.HasValue) truyVan = truyVan.Where(p => p.MaNhanVien == maNhanVien);
            ViewBag.DsPhanCong = await DanhSachTrang<PhanCongGiaoHang>.TaoAsync(
                truyVan.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong), trang, 25);
        }
        return View();
    }

    // ===================== PHÂN CÔNG CHẶNG TIẾP THEO =====================

    public async Task<IActionResult> TaoMoi(int maDon)
    {
        var vm = await TaoFormAsync(maDon, false);
        if (vm is null) return NotFound();
        int? buuCucCuaDon = vm.Don!.TrangThai == TrangThaiDon.ChoLayTanNoi ? vm.Don.MaBuuCucGui : (vm.Don.MaBuuCucHienTai ?? vm.Don.MaBuuCucGui);
        if (BuuCucCuaToi is not null && buuCucCuaDon != BuuCucCuaToi)
        {
            TempData["Error"] = $"Đơn {vm.Don.MaHienThi} không thuộc bưu cục phụ trách của bạn";
            return RedirectToAction(nameof(Index));
        }
        if (!DonHangXuLy.CoThePhanCong(vm.Don!))
        {
            TempData["Error"] = DonHangXuLy.ChangCanPhanCong(vm.Don!) == LoaiShipper.KhuVuc && vm.Don.TrangThai != TrangThaiDon.ChoLayTanNoi
                ? $"Đơn {vm.Don!.MaHienThi} đã giao không thành công {DonHangXuLy.SoLanGiaoToiDa} lần – hãy liên hệ người gửi để hủy / chuyển hoàn"
                : $"Đơn {vm.Don!.MaHienThi} đang '{vm.Don.TrangThai.TenHienThi()}' – không có chặng nào cần phân công";
            return QuayVeDon(maDon);
        }
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(PhanCongFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var kq = await xuLy.PhanCongAsync(vm.MaDon, vm.MaNhanVien, vm.GhiChu, HttpContext.Session.ThaoTac());
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao;
                var don = await db.DonGiaoHangs.FindAsync(vm.MaDon);
                string tabRedirect = don?.TrangThai == TrangThaiDon.ChoLayTanNoi ? "layhang" : (vm.LoaiChang == LoaiShipper.LienTinh ? "lientinh" : "khuvuc");
                return RedirectToAction(nameof(Index), new { tab = tabRedirect });
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        var form = await TaoFormAsync(vm.MaDon, false);
        if (form is null) return NotFound();
        form.MaNhanVien = vm.MaNhanVien; form.GhiChu = vm.GhiChu;
        return View("BieuMau", form);
    }

    // ===================== ĐỔI SHIPPER (chưa nhận hàng) =====================

    public async Task<IActionResult> DoiPhanCong(int maDon)
    {
        var vm = await TaoFormAsync(maDon, true);
        if (vm is null) return NotFound();
        int? buuCucCuaDon = vm.Don!.TrangThai == TrangThaiDon.ChoLayTanNoi ? vm.Don.MaBuuCucGui : (vm.Don.MaBuuCucHienTai ?? vm.Don.MaBuuCucGui);
        if (vm.PhanCongHienTai is null || !DonHangXuLy.CoTheDoiPhanCong(vm.Don!.TrangThai)
            || (BuuCucCuaToi is not null && buuCucCuaDon != BuuCucCuaToi))
        {
            TempData["Error"] = "Chỉ đổi shipper khi shipper được phân chưa nhận hàng";
            return QuayVeDon(maDon);
        }
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiPhanCong(PhanCongFormVM vm)
    {
        if (ModelState.IsValid)
        {
            var kq = await xuLy.DoiPhanCongAsync(vm.MaDon, vm.MaNhanVien, vm.GhiChu, HttpContext.Session.ThaoTac());
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao;
                return QuayVeDon(vm.MaDon);
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        var form = await TaoFormAsync(vm.MaDon, true);
        if (form is null) return NotFound();
        form.MaNhanVien = vm.MaNhanVien; form.GhiChu = vm.GhiChu;
        return View("BieuMau", form);
    }

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Nhân viên bưu cục xem đơn ở trang bưu cục; quản trị / điều phối ở trang đơn hàng.</summary>
    private IActionResult QuayVeDon(int maDon) => HttpContext.Session.LaNhanVienBuuCuc()
        ? RedirectToAction("Don", "BuuCuc", new { id = maDon })
        : RedirectToAction("ChiTiet", "QuanTriDonGiaoHang", new { id = maDon });

    /// <summary>Nạp đơn + danh sách shipper đúng loại chặng của bưu cục cần (đổi shipper thì bỏ qua phân công hiện tại).</summary>
    private async Task<PhanCongFormVM?> TaoFormAsync(int maDon, bool laDoi)
    {
        var don = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhachHang).Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                          .Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.BuuCucHienTai)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
                          .AsSplitQuery()
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return null;
        var hienTai = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        var loai = laDoi ? hienTai?.LoaiChang ?? LoaiShipper.KhuVuc : DonHangXuLy.ChangCanPhanCong(don) ?? LoaiShipper.KhuVuc;
        return new PhanCongFormVM
        {
            MaDon = maDon, Don = don, PhanCongHienTai = hienTai, LaDoiPhanCong = laDoi, LoaiChang = loai,
            DsNhanVien = await xuLy.LayLuaChonAsync(don, loai, laDoi ? hienTai?.MaPhanCong : null)
        };
    }
}
