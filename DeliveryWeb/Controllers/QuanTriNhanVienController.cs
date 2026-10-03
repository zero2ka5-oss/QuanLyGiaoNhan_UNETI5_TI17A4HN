// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 3 - Quản lý nhân viên giao hàng: CRUD, tạo tài khoản, trạng thái sẵn sàng,
//                     xem công việc hiện tại và lịch sử phân công.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriNhanVienController — URL: /QuanTriNhanVien/{Index|ChiTiet|TaoMoi|ChinhSua|Xoa}
 * Quyền: Quản trị, Điều phối.
 * Trạng thái (mục 7.4): người dùng chỉ chọn Sẵn sàng / Tạm nghỉ / Ngừng hoạt động.
 *   "Đang giao hàng" do hệ thống tự cập nhật theo phân công; không cho chuyển Tạm nghỉ / Ngừng khi còn đơn hiệu lực.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriNhanVienController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    public async Task<IActionResult> Index(string? tuKhoa, TrangThaiNhanVien? trangThai, int? maKhuVuc, int trang = 1)
    {
        var truyVan = db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan).AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(n => n.HoTen.Contains(tuKhoa) || n.SoDienThoai.Contains(tuKhoa) || n.Email.Contains(tuKhoa));
        }
        if (trangThai.HasValue) truyVan = truyVan.Where(n => n.TrangThai == trangThai);
        if (maKhuVuc.HasValue) truyVan = truyVan.Where(n => n.MaKhuVucPhuTrach == maKhuVuc);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.TrangThai = trangThai;
        ViewBag.DsKhuVuc = new SelectList(await db.KhuVucs.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", maKhuVuc);
        ViewBag.SoTheoTrangThai = await db.NhanVienGiaoHangs.GroupBy(n => n.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                          .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        // Số đơn đang giữ và số đơn đã giao thành công của từng nhân viên (LINQ GroupBy)
        ViewBag.DangGiu = await xuLy.PhanCongHieuLuc().GroupBy(p => p.MaNhanVien).Select(g => new { g.Key, SoLuong = g.Count() })
                                    .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        ViewBag.DaGiao = await db.PhanCongGiaoHangs.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong)
                                 .GroupBy(p => p.MaNhanVien).Select(g => new { g.Key, SoLuong = g.Count() })
                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<NhanVienGiaoHang>.TaoAsync(truyVan.OrderBy(n => n.TrangThai).ThenBy(n => n.HoTen), trang, 10));
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        var dsPhanCong = await db.PhanCongGiaoHangs.AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc).Include(p => p.PhuongTien)
            .Where(p => p.MaNhanVien == id).OrderByDescending(p => p.NgayPhanCong).ToListAsync();
        ViewBag.DsPhanCong = dsPhanCong;
        ViewBag.ThanhCong = dsPhanCong.Count(p => p.KetQua == KetQuaGiao.GiaoThanhCong);
        ViewBag.ThatBai = dsPhanCong.Count(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);
        ViewBag.TongKhoiLuong = dsPhanCong.Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong).Sum(p => p.DonGiaoHang!.KhoiLuong);
        return View(nv);
    }

    public async Task<IActionResult> TaoMoi()
    {
        await NapKhuVucAsync(null);
        return View("BieuMau", new NhanVienFormVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(NhanVienFormVM vm)
    {
        if (vm.TrangThai == TrangThaiNhanVien.DangGiaoHang)
            ModelState.AddModelError(nameof(vm.TrangThai), "Trạng thái 'Đang giao hàng' do hệ thống tự cập nhật");
        TaiKhoan? tk = null;
        if (!string.IsNullOrWhiteSpace(vm.TenDangNhap))
        {
            var ten = vm.TenDangNhap.Trim().ToLower();
            if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == ten)) ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
            if (string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu cho tài khoản");
            else tk = new TaiKhoan { TenDangNhap = ten, MatKhau = MatKhauHelper.Bam(vm.MatKhau), HoTen = vm.HoTen.Trim(), Email = vm.Email.Trim(), VaiTro = VaiTroNguoiDung.GiaoHang };
        }
        if (!ModelState.IsValid) { await NapKhuVucAsync(vm.MaKhuVucPhuTrach); return View("BieuMau", vm); }

        db.NhanVienGiaoHangs.Add(new NhanVienGiaoHang
        {
            TaiKhoan = tk, HoTen = vm.HoTen.Trim(), SoDienThoai = vm.SoDienThoai, Email = vm.Email.Trim(),
            MaKhuVucPhuTrach = vm.MaKhuVucPhuTrach, TrangThai = vm.TrangThai
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm nhân viên {vm.HoTen}" + (tk is null ? "" : $" (tài khoản {tk.TenDangNhap})");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        await NapKhuVucAsync(nv.MaKhuVucPhuTrach);
        ViewBag.TrangThaiHienTai = nv.TrangThai;
        return View("BieuMau", new NhanVienFormVM
        {
            MaNhanVien = nv.MaNhanVien, HoTen = nv.HoTen, SoDienThoai = nv.SoDienThoai, Email = nv.Email,
            MaKhuVucPhuTrach = nv.MaKhuVucPhuTrach, TrangThai = nv.TrangThai == TrangThaiNhanVien.DangGiaoHang ? TrangThaiNhanVien.SanSang : nv.TrangThai,
            TenDangNhap = nv.TaiKhoan?.TenDangNhap, CoTaiKhoan = nv.TaiKhoan is not null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, NhanVienFormVM vm)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        ModelState.Remove(nameof(vm.TenDangNhap));
        ModelState.Remove(nameof(vm.MatKhau));
        if (vm.TrangThai == TrangThaiNhanVien.DangGiaoHang)
            ModelState.AddModelError(nameof(vm.TrangThai), "Trạng thái 'Đang giao hàng' do hệ thống tự cập nhật");
        int soDonHieuLuc = await xuLy.PhanCongHieuLuc().CountAsync(p => p.MaNhanVien == id);
        if (vm.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong && soDonHieuLuc > 0)
            ModelState.AddModelError(nameof(vm.TrangThai), $"Nhân viên còn {soDonHieuLuc} đơn đang hiệu lực – hãy đổi phân công trước");
        if (!ModelState.IsValid)
        {
            await NapKhuVucAsync(vm.MaKhuVucPhuTrach);
            vm.MaNhanVien = id; vm.CoTaiKhoan = nv.TaiKhoan is not null; vm.TenDangNhap = nv.TaiKhoan?.TenDangNhap;
            ViewBag.TrangThaiHienTai = nv.TrangThai;
            return View("BieuMau", vm);
        }

        nv.HoTen = vm.HoTen.Trim();
        nv.SoDienThoai = vm.SoDienThoai;
        nv.Email = vm.Email.Trim();
        nv.MaKhuVucPhuTrach = vm.MaKhuVucPhuTrach;
        nv.TrangThai = vm.TrangThai;
        if (nv.TaiKhoan is not null) { nv.TaiKhoan.HoTen = nv.HoTen; nv.TaiKhoan.Email = nv.Email; }
        await db.SaveChangesAsync();
        await xuLy.CapNhatNguonLucAsync([id], []);   // Sẵn sàng nhưng đang có đơn đang giao → tự về Đang giao hàng
        TempData["Success"] = $"Đã cập nhật nhân viên {nv.HoTen}";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == id);
        if (nv is null) return NotFound();
        if (await db.PhanCongGiaoHangs.AnyAsync(p => p.MaNhanVien == id))
        {
            TempData["Error"] = $"Không thể xóa {nv.HoTen} vì đã có lịch sử phân công. Hãy chuyển sang Ngừng hoạt động.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        db.NhanVienGiaoHangs.Remove(nv);
        if (nv.TaiKhoan is not null) db.TaiKhoans.Remove(nv.TaiKhoan);
        await db.SaveChangesAsync();
        anhXuLy.Xoa(nv.TaiKhoan?.AnhDaiDien);
        TempData["Success"] = $"Đã xóa nhân viên {nv.HoTen}";
        return RedirectToAction(nameof(Index));
    }

    private async Task NapKhuVucAsync(int? dangChon) =>
        ViewBag.DsKhuVuc = new SelectList(await db.KhuVucs.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", dangChon);
}
