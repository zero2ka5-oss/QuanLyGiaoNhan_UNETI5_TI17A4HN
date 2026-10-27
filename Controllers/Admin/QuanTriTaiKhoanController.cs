using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriTaiKhoanController — URL: /QuanTriTaiKhoan/{Index|TaoMoi|ChinhSua|KhoaMo|DatLaiMatKhau}
 * Quyền: chỉ Quản trị (Điều phối cũng bị chặn).
 * Tạo mới tại đây chỉ cho tài khoản Quản trị / Điều phối; tài khoản khách hàng và nhân viên tạo từ module tương ứng
 * và giữ nguyên vai trò khi sửa. Không cho tự khóa / hạ quyền tài khoản đang đăng nhập.
 * Không khóa tài khoản shipper còn đơn đang hiệu lực (đơn sẽ kẹt) hoặc đang giữ tiền thu hộ chưa nộp (không ai nộp được).
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriTaiKhoanController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, ViXuLy viXuLy) : Controller
{
    private const int KichThuocTrang = 25;

    public async Task<IActionResult> Index(string? tuKhoa, string? vaiTro, TrangThaiTaiKhoan? trangThai, int trang = 1)
    {
        var truyVan = db.TaiKhoans.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            tuKhoa = tuKhoa.Trim();
            truyVan = truyVan.Where(t => t.TenDangNhap.Contains(tuKhoa) || t.HoTen.Contains(tuKhoa) || t.Email.Contains(tuKhoa));
        }
        if (!string.IsNullOrEmpty(vaiTro)) truyVan = truyVan.Where(t => t.VaiTro == vaiTro);
        if (trangThai.HasValue) truyVan = truyVan.Where(t => t.TrangThai == trangThai);

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.VaiTro = vaiTro;
        ViewBag.TrangThai = trangThai;
        ViewBag.SoTheoVaiTro = await db.TaiKhoans.GroupBy(t => t.VaiTro).Select(g => new { g.Key, SoLuong = g.Count() })
                                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        return View(await DanhSachTrang<TaiKhoan>.TaoAsync(truyVan.OrderBy(t => t.VaiTro).ThenBy(t => t.TenDangNhap), trang, KichThuocTrang));
    }

    public async Task<IActionResult> TaoMoi()
    {
        await NapVaiTroAsync();
        return View("BieuMau", new TaiKhoanFormVM());
    }

    /// <summary>Tạo tài khoản Quản trị / Điều phối / Nhân viên bưu cục. Tài khoản shipper và khách hàng tạo từ module tương ứng.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(TaiKhoanFormVM vm)
    {
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu");
        if (vm.VaiTro is not (VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.NhanVien))
            ModelState.AddModelError(nameof(vm.VaiTro), "Chỉ tạo tài khoản Quản trị / Nhân viên tại đây");
        await KiemTraBuuCucAsync(vm);
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) { await NapVaiTroAsync(); return View("BieuMau", vm); }

        db.TaiKhoans.Add(new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap, MatKhau = MatKhauHelper.Bam(vm.MatKhau!), HoTen = vm.HoTen.Trim(),
            Email = vm.Email.Trim(), VaiTro = vm.VaiTro, TrangThai = vm.TrangThai, PhaiDoiMatKhau = true,
            MaBuuCuc = vm.VaiTro == VaiTroNguoiDung.NhanVien ? vm.MaBuuCuc : null
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo tài khoản {vm.TenDangNhap}";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        await NapVaiTroAsync();
        return View("BieuMau", new TaiKhoanFormVM
        {
            MaTaiKhoan = tk.MaTaiKhoan, TenDangNhap = tk.TenDangNhap, HoTen = tk.HoTen, Email = tk.Email,
            VaiTro = tk.VaiTro, TrangThai = tk.TrangThai, MaBuuCuc = tk.MaBuuCuc
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, TaiKhoanFormVM vm)
    {
        var tk = await db.TaiKhoans.Include(t => t.KhachHang).Include(t => t.NhanVienGiaoHang).FirstOrDefaultAsync(t => t.MaTaiKhoan == id);
        if (tk is null) return NotFound();
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLowerInvariant();
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap && t.MaTaiKhoan != id))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        // Tài khoản gắn với khách hàng / nhân viên giao hàng giữ nguyên vai trò
        bool vaiTroCoDinh = tk.VaiTro is VaiTroNguoiDung.KhachHang or VaiTroNguoiDung.GiaoHang;
        if (vaiTroCoDinh) vm.VaiTro = tk.VaiTro;
        else if (vm.VaiTro is not (VaiTroNguoiDung.QuanTri or VaiTroNguoiDung.NhanVien))
            ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ");
        await KiemTraBuuCucAsync(vm);
        if (id == HttpContext.Session.MaTaiKhoan() && (vm.TrangThai == TrangThaiTaiKhoan.BiKhoa || vm.VaiTro != VaiTroNguoiDung.QuanTri))
            ModelState.AddModelError("", "Không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập");
        if (vm.TrangThai == TrangThaiTaiKhoan.BiKhoa && tk.TrangThai != TrangThaiTaiKhoan.BiKhoa && await LyDoKhongKhoaAsync(tk) is string lyDo)
            ModelState.AddModelError(nameof(vm.TrangThai), lyDo);
        if (!ModelState.IsValid) { await NapVaiTroAsync(); vm.MaTaiKhoan = id; return View("BieuMau", vm); }

        tk.TenDangNhap = vm.TenDangNhap;
        tk.HoTen = vm.HoTen.Trim();
        tk.Email = vm.Email.Trim();
        tk.VaiTro = vm.VaiTro;
        tk.MaBuuCuc = vm.VaiTro == VaiTroNguoiDung.NhanVien ? vm.MaBuuCuc : null;
        tk.TrangThai = vm.TrangThai;
        if (!string.IsNullOrWhiteSpace(vm.MatKhau))
        {
            tk.MatKhau = MatKhauHelper.Bam(vm.MatKhau);
            tk.PhaiDoiMatKhau = id != HttpContext.Session.MaTaiKhoan();
        }
        if (tk.KhachHang is not null) { tk.KhachHang.HoTen = tk.HoTen; tk.KhachHang.Email = tk.Email; }
        if (tk.NhanVienGiaoHang is not null) { tk.NhanVienGiaoHang.HoTen = tk.HoTen; tk.NhanVienGiaoHang.Email = tk.Email; }
        await db.SaveChangesAsync();
        if (id == HttpContext.Session.MaTaiKhoan()) HttpContext.Session.GhiPhien(tk, tk.KhachHang?.MaKhachHang, tk.NhanVienGiaoHang?.MaNhanVien);
        TempData["Success"] = $"Đã cập nhật tài khoản {tk.TenDangNhap}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoaMo(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        if (id == HttpContext.Session.MaTaiKhoan()) TempData["Error"] = "Không thể tự khóa tài khoản đang đăng nhập";
        else if (tk.TrangThai == TrangThaiTaiKhoan.HoatDong && await LyDoKhongKhoaAsync(tk) is string lyDo) TempData["Error"] = lyDo;
        else
        {
            tk.TrangThai = tk.TrangThai == TrangThaiTaiKhoan.HoatDong ? TrangThaiTaiKhoan.BiKhoa : TrangThaiTaiKhoan.HoatDong;
            await db.SaveChangesAsync();
            TempData["Success"] = $"{tk.TenDangNhap}: {(tk.TrangThai == TrangThaiTaiKhoan.HoatDong ? "đã mở khóa" : "đã khóa")}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(int id)
    {
        var tk = await db.TaiKhoans.FindAsync(id);
        if (tk is null) return NotFound();
        if (id == HttpContext.Session.MaTaiKhoan())
        {
            TempData["Error"] = "Hãy đổi mật khẩu của chính bạn ở mục Tài khoản của tôi";
            return RedirectToAction(nameof(Index));
        }
        string matKhauTam = MatKhauHelper.TaoMatKhauTam();
        tk.MatKhau = MatKhauHelper.Bam(matKhauTam);
        tk.PhaiDoiMatKhau = true;
        var tb = new ThongBaoXuLy(db, () => DateTime.Now);
        tb.Gui(tk.MaTaiKhoan, LoaiThongBao.HeThong,
            "Mật khẩu của bạn đã được đặt lại",
            "Quản trị viên đã đặt lại mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mật khẩu tạm do công ty cung cấp và đổi mật khẩu mới.",
            "/DangNhap/DoiMatKhau");
        await db.SaveChangesAsync();
        TempData["Success"] = $"Đã đặt lại mật khẩu của {tk.TenDangNhap}. Mật khẩu tạm: {matKhauTam} – chỉ hiện một lần, hãy gửi cho người dùng; họ phải đổi mật khẩu khi đăng nhập.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Lý do chưa được khóa tài khoản shipper; null = khóa được.</summary>
    private async Task<string?> LyDoKhongKhoaAsync(TaiKhoan tk)
    {
        if (tk.VaiTro != VaiTroNguoiDung.GiaoHang) return null;
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().FirstOrDefaultAsync(n => n.MaTaiKhoan == tk.MaTaiKhoan);
        if (nv is null) return null;
        int soDon = await xuLy.PhanCongHieuLuc().CountAsync(p => p.MaNhanVien == nv.MaNhanVien);
        if (soDon > 0) return $"{nv.HoTen} còn {soDon} đơn đang hiệu lực – hãy đổi phân công trước khi khóa tài khoản";
        var vi = await viXuLy.TinhViAsync(nv.MaNhanVien, kemLichSu: false);
        if (vi is { TienDangGiu: > 0 })
            return $"{nv.HoTen} đang giữ {DinhDang.Tien(vi.TienDangGiu)} tiền thu hộ chưa nộp – cần thu tiền về trước khi khóa tài khoản";
        return null;
    }

    private async Task NapVaiTroAsync()
    {
        ViewBag.DsVaiTro = new List<SelectListItem>
        {
            new(HienThi.TenVaiTro(VaiTroNguoiDung.NhanVien), VaiTroNguoiDung.NhanVien),
            new(HienThi.TenVaiTro(VaiTroNguoiDung.QuanTri), VaiTroNguoiDung.QuanTri),
        };
        ViewBag.DsBuuCuc = new SelectList(await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync(), "MaBuuCuc", "TenBuuCuc");
    }

    /// <summary>Nhân viên bưu cục: nếu chọn bưu cục thì bưu cục phải tồn tại (để trống nếu phụ trách toàn hệ thống).</summary>
    private async Task KiemTraBuuCucAsync(TaiKhoanFormVM vm)
    {
        if (vm.VaiTro != VaiTroNguoiDung.NhanVien) return;
        if (vm.MaBuuCuc.HasValue && !await db.BuuCucs.AnyAsync(b => b.MaBuuCuc == vm.MaBuuCuc.Value))
            ModelState.AddModelError(nameof(vm.MaBuuCuc), "Bưu cục đã chọn không tồn tại trong hệ thống");
    }
}
