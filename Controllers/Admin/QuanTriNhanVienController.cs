using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriNhanVienController — URL: /QuanTriNhanVien/{Index|ChiTiet|TaoMoi|ChinhSua|Xoa}
 * Quyền: Quản trị, Điều phối.
 * Trạng thái: người dùng chỉ chọn Sẵn sàng / Tạm nghỉ / Ngừng hoạt động.
 *   "Đang giao hàng" do hệ thống tự cập nhật theo phân công; không cho chuyển Tạm nghỉ / Ngừng khi còn đơn hiệu lực.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.NhanVien)]
public class QuanTriNhanVienController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    /// <summary>Nhân viên bưu cục bị khóa theo bưu cục của mình; quản trị xem tất cả hoặc lọc theo bưu cục tùy ý.</summary>
    private int? BuuCucCuaToi => HttpContext.Session.MaBuuCuc();

    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiNhanVien? trangThai, int? maKhuVuc, int? maBuuCuc, int trang = 1)
    {
        bool laNvBuuCuc = BuuCucCuaToi is not null;
        if (laNvBuuCuc)
        {
            maBuuCuc = BuuCucCuaToi;
        }

        var truyVan = db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan).Include(n => n.BuuCuc).Include(n => n.PhuongTien).AsQueryable();

        if (maBuuCuc.HasValue)
        {
            truyVan = truyVan.Where(n => n.MaBuuCuc == maBuuCuc);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(n => n.HoTen.Contains(tuKhoa) || n.SoDienThoai.Contains(tuKhoa) || n.Email.Contains(tuKhoa));
        }
        if (trangThai.HasValue) truyVan = truyVan.Where(n => n.TrangThai == trangThai);
        if (maKhuVuc.HasValue) truyVan = truyVan.Where(n => n.MaKhuVucPhuTrach == maKhuVuc);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.MaBuuCuc = maBuuCuc;
        ViewBag.MaKhuVuc = maKhuVuc;
        ViewBag.KhoaBuuCuc = laNvBuuCuc;
        ViewBag.TenBuuCuc = laNvBuuCuc && maBuuCuc.HasValue ? (await db.BuuCucs.FindAsync(maBuuCuc.Value))?.TenBuuCuc : null;
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();

        var dsKhuVucTruyVan = db.KhuVucs.AsNoTracking().AsQueryable();
        if (maBuuCuc.HasValue)
        {
            dsKhuVucTruyVan = dsKhuVucTruyVan.Where(k => k.MaBuuCuc == maBuuCuc);
        }
        ViewBag.DsKhuVuc = new SelectList(await dsKhuVucTruyVan.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", maKhuVuc);

        var baseNv = db.NhanVienGiaoHangs.AsNoTracking().Where(n => maBuuCuc == null || n.MaBuuCuc == maBuuCuc);
        ViewBag.SoTheoTrangThai = await baseNv.GroupBy(n => n.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                          .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.DangGiu = await xuLy.PhanCongHieuLuc().Where(p => maBuuCuc == null || p.NhanVien!.MaBuuCuc == maBuuCuc)
                                    .GroupBy(p => p.MaNhanVien).Select(g => new { g.Key, SoLuong = g.Count() })
                                    .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.DaGiao = await db.PhanCongGiaoHangs.Where(p => (maBuuCuc == null || p.NhanVien!.MaBuuCuc == maBuuCuc) && (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc))
                                 .GroupBy(p => p.MaNhanVien).Select(g => new { g.Key, SoLuong = g.Count() })
                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<NhanVienGiaoHang>.TaoAsync(truyVan.OrderBy(n => n.TrangThai).ThenBy(n => n.HoTen), trang, 25));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan)
                         .Include(n => n.BuuCuc).Include(n => n.PhuongTien)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        if (BuuCucCuaToi is not null && nv.MaBuuCuc != BuuCucCuaToi)
        {
            TempData["Error"] = "Bạn chỉ có thể xem shipper thuộc bưu cục của mình.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.LichSuXe = await db.LichSuPhuongTiens.AsNoTracking().Include(l => l.PhuongTienCu).Include(l => l.PhuongTienMoi)
                                   .Where(l => l.MaNhanVien == id).OrderByDescending(l => l.ThoiGian).ToListAsync();
        var dsPhanCong = await db.PhanCongGiaoHangs.AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc).Include(p => p.PhuongTien)
            .Where(p => p.MaNhanVien == id).OrderByDescending(p => p.NgayPhanCong).ToListAsync();
        ViewBag.DsPhanCong = dsPhanCong;
        ViewBag.ThanhCong = dsPhanCong.Count(p => (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc));
        ViewBag.ThatBai = dsPhanCong.Count(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);
        ViewBag.TongKhoiLuong = dsPhanCong.Where(p => (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc)).Sum(p => p.DonGiaoHang!.KhoiLuong);
        return View(nv);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public async Task<IActionResult> TaoMoi()
    {
        await NapDanhMucAsync(null);
        return View("BieuMau", new NhanVienFormVM());
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(NhanVienFormVM vm)
    {
        if (vm.TrangThai == TrangThaiNhanVien.DangGiaoHang)
            ModelState.AddModelError(nameof(vm.TrangThai), "Trạng thái 'Đang giao hàng' do hệ thống tự cập nhật");
        await KiemTraPhanBoAsync(vm, null);
        TaiKhoan? tk = null;
        if (!string.IsNullOrWhiteSpace(vm.TenDangNhap))
        {
            var ten = vm.TenDangNhap.Trim().ToLower();
            if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == ten)) ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
            if (string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu cho tài khoản");
            else tk = new TaiKhoan { TenDangNhap = ten, MatKhau = MatKhauHelper.Bam(vm.MatKhau), HoTen = vm.HoTen.Trim(), Email = vm.Email.Trim(), VaiTro = VaiTroNguoiDung.GiaoHang, PhaiDoiMatKhau = true };
        }
        if (await db.NhanVienGiaoHangs.AnyAsync(n => n.SoDienThoai == vm.SoDienThoai))
            ModelState.AddModelError(nameof(vm.SoDienThoai), "Số điện thoại này đã được một shipper khác sử dụng");
        if (!ModelState.IsValid) { await NapDanhMucAsync(null); return View("BieuMau", vm); }

        var nv = new NhanVienGiaoHang
        {
            TaiKhoan = tk, HoTen = vm.HoTen.Trim(), SoDienThoai = vm.SoDienThoai, Email = vm.Email.Trim(), TrangThai = vm.TrangThai,
            LoaiShipper = vm.LoaiShipper, MaBuuCuc = vm.MaBuuCuc, MaPhuongTien = vm.MaPhuongTien,
            MaKhuVucPhuTrach = vm.LoaiShipper == LoaiShipper.KhuVuc ? vm.MaKhuVucPhuTrach : null
        };
        db.NhanVienGiaoHangs.Add(nv);
        await db.SaveChangesAsync();
        if (nv.MaPhuongTien is not null) GhiLichSuXe(nv.MaNhanVien, null, nv.MaPhuongTien, "Gán phương tiện khi thêm shipper");
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm shipper {vm.HoTen}" + (tk is null ? "" : $" (tài khoản {tk.TenDangNhap})");
        return RedirectToAction(nameof(Index));
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        await NapDanhMucAsync(nv);
        ViewBag.TrangThaiHienTai = nv.TrangThai;
        ViewBag.DangGiuHang = await DangGiuHangAsync(id);
        return View("BieuMau", new NhanVienFormVM
        {
            MaNhanVien = nv.MaNhanVien, HoTen = nv.HoTen, SoDienThoai = nv.SoDienThoai, Email = nv.Email,
            LoaiShipper = nv.LoaiShipper, MaBuuCuc = nv.MaBuuCuc, MaKhuVucPhuTrach = nv.MaKhuVucPhuTrach, MaPhuongTien = nv.MaPhuongTien,
            TrangThai = nv.TrangThai == TrangThaiNhanVien.DangGiaoHang ? TrangThaiNhanVien.SanSang : nv.TrangThai,
            TenDangNhap = nv.TaiKhoan?.TenDangNhap, CoTaiKhoan = nv.TaiKhoan is not null
        });
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, NhanVienFormVM vm)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        TaiKhoan? tkMoi = null;
        if (nv.TaiKhoan is null && !string.IsNullOrWhiteSpace(vm.TenDangNhap))
        {
            var ten = vm.TenDangNhap.Trim().ToLower();
            if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == ten)) ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
            if (string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu cho tài khoản");
            else tkMoi = new TaiKhoan { TenDangNhap = ten, MatKhau = MatKhauHelper.Bam(vm.MatKhau), HoTen = vm.HoTen.Trim(), Email = vm.Email.Trim(), VaiTro = VaiTroNguoiDung.GiaoHang, PhaiDoiMatKhau = true };
        }
        else
        {
            ModelState.Remove(nameof(vm.TenDangNhap));
            ModelState.Remove(nameof(vm.MatKhau));
        }
        if (vm.TrangThai == TrangThaiNhanVien.DangGiaoHang)
            ModelState.AddModelError(nameof(vm.TrangThai), "Trạng thái 'Đang giao hàng' do hệ thống tự cập nhật");
        int dangGiu = await DangGiuHangAsync(id);
        if (vm.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong && dangGiu > 0)
            ModelState.AddModelError(nameof(vm.TrangThai), $"Shipper còn {dangGiu} đơn được phân / đang giữ – hãy đổi shipper hoặc nhận lại hàng trước");
        if (dangGiu > 0 && (vm.LoaiShipper != nv.LoaiShipper || vm.MaBuuCuc != nv.MaBuuCuc || vm.MaPhuongTien != nv.MaPhuongTien))
            ModelState.AddModelError("", $"Shipper đang giữ / được phân {dangGiu} đơn – không đổi loại shipper, bưu cục hay phương tiện lúc này");
        if (vm.TrangThai == TrangThaiNhanVien.NgungHoatDong) vm.MaPhuongTien = null;
        await KiemTraPhanBoAsync(vm, nv);
        if (vm.MaPhuongTien != nv.MaPhuongTien && nv.MaPhuongTien is not null && vm.TrangThai != TrangThaiNhanVien.NgungHoatDong
            && string.IsNullOrWhiteSpace(vm.LyDoDoiXe))
            ModelState.AddModelError(nameof(vm.LyDoDoiXe), "Nhập lý do đổi phương tiện");
        if (await db.NhanVienGiaoHangs.AnyAsync(n => n.SoDienThoai == vm.SoDienThoai && n.MaNhanVien != id))
            ModelState.AddModelError(nameof(vm.SoDienThoai), "Số điện thoại này đã được một shipper khác sử dụng");
        if (!ModelState.IsValid)
        {
            await NapDanhMucAsync(nv);
            vm.MaNhanVien = id; vm.CoTaiKhoan = nv.TaiKhoan is not null; vm.TenDangNhap = nv.TaiKhoan?.TenDangNhap;
            ViewBag.TrangThaiHienTai = nv.TrangThai;
            ViewBag.DangGiuHang = dangGiu;
            return View("BieuMau", vm);
        }

        if (vm.MaPhuongTien != nv.MaPhuongTien)
            GhiLichSuXe(id, nv.MaPhuongTien, vm.MaPhuongTien,
                vm.TrangThai == TrangThaiNhanVien.NgungHoatDong ? "Shipper ngừng hoạt động – trả phương tiện" : vm.LyDoDoiXe?.Trim() ?? "Gán phương tiện");
        nv.HoTen = vm.HoTen.Trim();
        nv.SoDienThoai = vm.SoDienThoai;
        nv.Email = vm.Email.Trim();
        nv.LoaiShipper = vm.LoaiShipper;
        nv.MaBuuCuc = vm.MaBuuCuc;
        nv.MaKhuVucPhuTrach = vm.LoaiShipper == LoaiShipper.KhuVuc ? vm.MaKhuVucPhuTrach : null;
        nv.MaPhuongTien = vm.MaPhuongTien;
        nv.TrangThai = vm.TrangThai;
        if (tkMoi is not null) nv.TaiKhoan = tkMoi;
        else if (nv.TaiKhoan is not null) { nv.TaiKhoan.HoTen = nv.HoTen; nv.TaiKhoan.Email = nv.Email; }
        await db.SaveChangesAsync();
        await xuLy.CapNhatNguonLucAsync([id], []);
        TempData["Success"] = $"Đã cập nhật shipper {nv.HoTen}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    /// <summary>Số đơn shipper đang được phân (chặng hiệu lực) hoặc đang giữ hàng (kể cả sau lần giao lỗi).</summary>
    private async Task<int> DangGiuHangAsync(int maNhanVien) =>
        await xuLy.PhanCongHieuLuc().Where(p => p.MaNhanVien == maNhanVien).Select(p => p.MaDon)
                  .Union(db.DonGiaoHangs.Where(d => d.MaNhanVienDangGiu == maNhanVien).Select(d => d.MaDon)).CountAsync();

    /// <summary>
    /// Bưu cục bắt buộc; shipper khu vực phải phụ trách một khu vực do chính bưu cục đó phát; phương tiện (bắt buộc khi còn hoạt động)
    /// không ngừng hoạt động và không đang gán cho shipper khác (1 shipper ↔ 1 phương tiện).
    /// </summary>
    private async Task KiemTraPhanBoAsync(NhanVienFormVM vm, NhanVienGiaoHang? nv)
    {
        if (vm.MaBuuCuc is null) ModelState.AddModelError(nameof(vm.MaBuuCuc), "Chọn bưu cục của shipper");
        if (vm.LoaiShipper == LoaiShipper.KhuVuc)
        {
            if (vm.MaKhuVucPhuTrach is null) ModelState.AddModelError(nameof(vm.MaKhuVucPhuTrach), "Shipper khu vực phải phụ trách một khu vực giao");
            else if (vm.MaBuuCuc is not null && !await db.KhuVucs.AnyAsync(k => k.MaKhuVuc == vm.MaKhuVucPhuTrach && k.MaBuuCuc == vm.MaBuuCuc))
                ModelState.AddModelError(nameof(vm.MaKhuVucPhuTrach), "Khu vực này không do bưu cục đã chọn phát hàng");
        }
        if (vm.MaPhuongTien is null)
        {
            if (vm.TrangThai != TrangThaiNhanVien.NgungHoatDong)
                ModelState.AddModelError(nameof(vm.MaPhuongTien), "Mỗi shipper phải được gán một phương tiện");
            return;
        }
        var pt = await db.PhuongTiens.Include(p => p.NhanVien).FirstOrDefaultAsync(p => p.MaPhuongTien == vm.MaPhuongTien);
        if (pt is null) ModelState.AddModelError(nameof(vm.MaPhuongTien), "Phương tiện không tồn tại");
        else if (pt.NhanVien is not null && pt.NhanVien.MaNhanVien != nv?.MaNhanVien)
            ModelState.AddModelError(nameof(vm.MaPhuongTien), $"{pt.MaHienThi} {pt.BienSo} đang được gán cho {pt.NhanVien.HoTen}");
        else if (pt.TrangThai == TrangThaiPhuongTien.NgungHoatDong && pt.MaPhuongTien != nv?.MaPhuongTien)
            ModelState.AddModelError(nameof(vm.MaPhuongTien), $"{pt.BienSo} đã ngừng hoạt động");
    }

    private void GhiLichSuXe(int maNhanVien, int? cu, int? moi, string lyDo) =>
        db.LichSuPhuongTiens.Add(new LichSuPhuongTien
        {
            MaNhanVien = maNhanVien, MaPhuongTienCu = cu, MaPhuongTienMoi = moi, ThoiGian = DateTime.Now,
            NguoiThucHien = HttpContext.Session.NguoiThucHien(), LyDo = lyDo
        });

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        if (await db.PhanCongGiaoHangs.AnyAsync(p => p.MaNhanVien == id)
            || await db.ChuyenXes.AnyAsync(c => c.MaNhanVien == id)
            || await db.DonGiaoHangs.AnyAsync(d => d.MaNhanVienDangGiu == id)
            || await db.GiaoDichVis.AnyAsync(g => g.MaNhanVien == id)
            || await db.LichSuPhuongTiens.AnyAsync(l => l.MaNhanVien == id))
        {
            TempData["Error"] = $"Không thể xóa {nv.HoTen} vì đã có dữ liệu phân công / chuyến xe / ví liên kết. Hãy chuyển sang Ngừng hoạt động.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        try
        {
            db.NhanVienGiaoHangs.Remove(nv);
            if (nv.TaiKhoan is not null) db.TaiKhoans.Remove(nv.TaiKhoan);
            await db.SaveChangesAsync();
            anhXuLy.Xoa(nv.TaiKhoan?.AnhDaiDien);
            TempData["Success"] = $"Đã xóa nhân viên {nv.HoTen}";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"Không thể xóa {nv.HoTen} do có ràng buộc dữ liệu liên kết. Hãy chuyển sang Ngừng hoạt động.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
    }

    /// <summary>Danh mục cho form: bưu cục, khu vực (kèm bưu cục phát), phương tiện còn trống + phương tiện đang gán cho shipper này.</summary>
    private async Task NapDanhMucAsync(NhanVienGiaoHang? nv)
    {
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();
        ViewBag.DsKhuVucDayDu = await db.KhuVucs.AsNoTracking().Include(k => k.BuuCuc).OrderBy(k => k.PhiCoBan).ToListAsync();
        int? maXe = nv?.MaPhuongTien;
        ViewBag.XeHienTai = maXe;
        ViewBag.DsPhuongTien = await db.PhuongTiens.AsNoTracking()
            .Where(p => p.MaPhuongTien == maXe || (p.NhanVien == null && p.TrangThai != TrangThaiPhuongTien.NgungHoatDong))
            .OrderBy(p => p.TaiTrongToiDa).ThenBy(p => p.BienSo).ToListAsync();
    }
}
