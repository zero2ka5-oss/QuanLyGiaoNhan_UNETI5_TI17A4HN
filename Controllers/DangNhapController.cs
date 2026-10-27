using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * DangNhapController — URL: /DangNhap/{Index|DangKy|DangXuat|DoiMatKhau|KhongCoQuyen|TrangLamViec}
 * Quyền: mọi người (DoiMatKhau, TrangLamViec cần đã đăng nhập).
 * Đăng nhập: LINQ tìm tài khoản theo tên đăng nhập → kiểm tra mật khẩu băm → tài khoản bị khóa thì từ chối →
 *            lưu Mã tài khoản, Họ tên, Vai trò (+ mã khách hàng / nhân viên) vào Session → chuyển trang theo vai trò.
 * Đăng xuất: Session.Clear() – Session cũ không dùng tiếp được.
 * Chống dò mật khẩu: sai SoLanSaiToiDa lần liên tiếp (theo tên đăng nhập + IP) → tạm khóa đăng nhập ThoiGianKhoa.
 */
public class DangNhapController(QuanLyGiaoNhanDbContext db, IMemoryCache boNho) : Controller
{
    private const int SoLanSaiToiDa = 5;
    private static readonly TimeSpan ThoiGianKhoa = TimeSpan.FromMinutes(15);

    // ===================== ĐĂNG NHẬP =====================

    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (HttpContext.Session.DaDangNhap())
        {
            return ChuyenTheoVaiTro();
        }
        ViewBag.ReturnUrl = returnUrl;
        return View(new DangNhapVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(DangNhapVM vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var tenDangNhap = vm.TenDangNhap.Trim().ToLowerInvariant();


        string khoaDem = $"DangNhapSai:{tenDangNhap}:{HttpContext.Connection.RemoteIpAddress}";
        if (boNho.TryGetValue(khoaDem, out int soLanSai) && soLanSai >= SoLanSaiToiDa)
        {
            ModelState.AddModelError("", $"Bạn đã nhập sai {SoLanSaiToiDa} lần. Vui lòng thử lại sau {ThoiGianKhoa.TotalMinutes:0} phút.");
            return View(vm);
        }
        var tk = await db.TaiKhoans.Include(t => t.KhachHang).Include(t => t.NhanVienGiaoHang)
                                   .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);
        if (tk is null || !MatKhauHelper.KiemTra(tk.MatKhau, vm.MatKhau))
        {
            boNho.Set(khoaDem, soLanSai + 1, ThoiGianKhoa);   // khóa tính từ lần sai gần nhất
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
            return View(vm);
        }
        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(vm);
        }

        boNho.Remove(khoaDem);
        if (tk.VaiTro is "DieuPhoi" or "BuuCuc")
        {
            tk.VaiTro = VaiTroNguoiDung.NhanVien;
            await db.SaveChangesAsync();
        }
        HttpContext.Session.Clear();
        HttpContext.Session.GhiPhien(tk, tk.KhachHang?.MaKhachHang, tk.NhanVienGiaoHang?.MaNhanVien);
        if (tk.PhaiDoiMatKhau)
        {
            TempData["Error"] = "Mật khẩu của bạn do quản trị viên đặt – vui lòng đổi mật khẩu mới để tiếp tục.";
            return RedirectToAction(nameof(DoiMatKhau));
        }
        ThietLapPopupChaoMung(tk);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && LaDuongDanHopLeChoVaiTro(returnUrl, tk.VaiTro))
        {
            return LocalRedirect(returnUrl);
        }
        return ChuyenTheoVaiTro();
    }

    // ===================== ĐĂNG KÝ / ĐĂNG XUẤT =====================

    [HttpGet]
    public IActionResult DangKy()
    {
        if (HttpContext.Session.DaDangNhap()) return ChuyenTheoVaiTro();
        return View(new DangKyVM());
    }

    /// <summary>Khách hàng tự đăng ký: tạo TaiKhoan (vai trò Khách hàng) + KhachHang trong một lần SaveChanges.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyVM vm)
    {
        vm.TenDangNhap = vm.TenDangNhap.Trim().ToLowerInvariant();
        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == vm.TenDangNhap))
            ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (await db.KhachHangs.AnyAsync(k => k.SoDienThoai == vm.SoDienThoai))
            ModelState.AddModelError(nameof(vm.SoDienThoai), "Số điện thoại này đã được đăng ký – hãy đăng nhập hoặc dùng số khác");
        if (!ModelState.IsValid) return View(vm);

        var tk = new TaiKhoan
        {
            TenDangNhap = vm.TenDangNhap, MatKhau = MatKhauHelper.Bam(vm.MatKhau), HoTen = vm.HoTen.Trim(),
            Email = vm.Email.Trim(), VaiTro = VaiTroNguoiDung.KhachHang
        };
        var kh = new KhachHang
        {
            TaiKhoan = tk, HoTen = tk.HoTen, SoDienThoai = vm.SoDienThoai, Email = tk.Email, DiaChi = vm.DiaChi.Trim(),
            NgayDangKy = DateTime.Now
        };
        db.KhachHangs.Add(kh);
        await db.SaveChangesAsync();

        var tb = new ThongBaoXuLy(db, () => DateTime.Now);
        tb.Gui(tk.MaTaiKhoan, LoaiThongBao.HeThong,
            "Chào mừng bạn đến với hệ thống giao nhận!",
            "Tài khoản của bạn đã được khởi tạo thành công. Bạn có thể bắt đầu tạo đơn và theo dõi hành trình ngay bây giờ.",
            "/KhachHangDonHang/TaoMoi");
        tb.GuiNhieu(tb.TkQuanTri(), LoaiThongBao.HeThong,
            $"Khách hàng mới: {kh.HoTen}",
            $"Khách hàng {kh.HoTen} ({kh.SoDienThoai}) vừa đăng ký tài khoản thành công.",
            "/QuanTriKhachHang");
        await db.SaveChangesAsync();

        HttpContext.Session.GhiPhien(tk, kh.MaKhachHang, null);
        ThietLapPopupChaoMung(tk);
        TempData["Success"] = "Đăng ký thành công! Bạn có thể tạo đơn giao hàng ngay.";
        return RedirectToAction("Index", "KhachHangTrangChu");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DangXuat()
    {
        var hoTen = HttpContext.Session.HoTen();
        var anh = HttpContext.Session.AnhDaiDien();
        HttpContext.Session.Clear();
        TempData["PopupDangXuat"] = "Đăng xuất thành công";
        TempData["PopupDangXuatHoTen"] = hoTen;
        TempData["PopupDangXuatMoTa"] = "Đã đăng xuất an toàn khỏi tài khoản.";
        TempData["PopupDangXuatAnh"] = anh;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult KhongCoQuyen(string? returnUrl) { ViewBag.ReturnUrl = returnUrl; return View(); }

    // ===================== ĐỔI MẬT KHẨU (mọi vai trò) =====================

    [YeuCauVaiTro, HttpGet]
    public IActionResult DoiMatKhau() => View(new DoiMatKhauVM());

    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        if (!MatKhauHelper.KiemTra(tk.MatKhau, vm.MatKhauHienTai))
        {
            ModelState.AddModelError(nameof(vm.MatKhauHienTai), "Mật khẩu hiện tại không đúng");
            return View(vm);
        }
        if (MatKhauHelper.KiemTra(tk.MatKhau, vm.MatKhauMoi))
        {
            ModelState.AddModelError(nameof(vm.MatKhauMoi), "Mật khẩu mới phải khác mật khẩu hiện tại");
            return View(vm);
        }
        tk.MatKhau = MatKhauHelper.Bam(vm.MatKhauMoi);
        tk.PhaiDoiMatKhau = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đổi mật khẩu thành công";
        return RedirectToAction(nameof(TrangLamViec));
    }

    // ===================== ẢNH ĐẠI DIỆN (mọi vai trò) =====================

    /// <summary>Tải ảnh đại diện mới cho chính tài khoản đang đăng nhập; ảnh cũ bị xóa khỏi ổ đĩa.</summary>
    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiAnhDaiDien(IFormFile? anh, [FromServices] AnhXuLy anhXuLy, string? returnUrl = null)
    {
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        var (duongDan, loi) = await anhXuLy.LuuAsync(anh, AnhXuLy.ThuMucAnhDaiDien);
        if (duongDan is null) TempData["Error"] = loi;
        else
        {
            anhXuLy.Xoa(tk.AnhDaiDien);
            tk.AnhDaiDien = duongDan;
            await db.SaveChangesAsync();
            HttpContext.Session.GhiAnhDaiDien(duongDan);
            TempData["Success"] = "Đã cập nhật ảnh đại diện";
        }
        return QuayLai(returnUrl);
    }

    [YeuCauVaiTro, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaAnhDaiDien([FromServices] AnhXuLy anhXuLy, string? returnUrl = null)
    {
        var tk = await db.TaiKhoans.FindAsync(HttpContext.Session.MaTaiKhoan());
        if (tk is null) return NotFound();
        anhXuLy.Xoa(tk.AnhDaiDien);
        tk.AnhDaiDien = null;
        await db.SaveChangesAsync();
        HttpContext.Session.GhiAnhDaiDien(null);
        TempData["Success"] = "Đã xóa ảnh đại diện";
        return QuayLai(returnUrl);
    }

    private IActionResult QuayLai(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(DoiMatKhau));

    /// <summary>Nút "Vào trang làm việc" trên trang chủ.</summary>
    [YeuCauVaiTro]
    public IActionResult TrangLamViec() => ChuyenTheoVaiTro();

    // ===================== HÀM DÙNG CHUNG =====================

    private static bool LaDuongDanHopLeChoVaiTro(string? url, string? vaiTro)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var path = url.Split('?')[0].Trim().ToLowerInvariant();
        if (path.StartsWith("/dangnhap") || path == "/" || path == "/trangchu") return false;

        return vaiTro switch
        {
            VaiTroNguoiDung.QuanTri => path.StartsWith("/quantri"),
            VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc" =>
                path.StartsWith("/nhanvienvanhanh") || path.StartsWith("/buucuc") || path.StartsWith("/quantriphancong") || path.StartsWith("/quantridongiaohang") || path.StartsWith("/quantrinhanvien"),
            VaiTroNguoiDung.GiaoHang => path.StartsWith("/nhanviencongviec") || path.StartsWith("/nhanvienhoso"),
            VaiTroNguoiDung.KhachHang => path.StartsWith("/khachhang"),
            _ => false
        };
    }

    /// <summary>Mỗi vai trò có trang chủ riêng: QuanTri* / KhachHang* / NhanVien*.</summary>
    private IActionResult ChuyenTheoVaiTro() => HttpContext.Session.VaiTro() switch
    {
        VaiTroNguoiDung.QuanTri => RedirectToAction("Index", "QuanTriBangDieuKhien"),
        VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc" => RedirectToAction("Index", "NhanVienVanHanh"),
        VaiTroNguoiDung.GiaoHang => RedirectToAction("Index", "NhanVienCongViec"),
        VaiTroNguoiDung.KhachHang => RedirectToAction("Index", "KhachHangTrangChu"),
        _ => RedirectToAction("Index", "TrangChu")
    };

    /// <summary>Thiết lập thông báo popup góc màn hình khi đăng nhập thành công.</summary>
    private void ThietLapPopupChaoMung(TaiKhoan tk)
    {
        string tieuDe;
        string moTa;
        if (tk.VaiTro == VaiTroNguoiDung.QuanTri)
        {
            tieuDe = "Chào mừng, quản trị viên!";
            moTa = "Chào mừng bạn quay lại hệ thống quản trị và điều hành GiaoNhanh.";
        }
        else
        {
            tieuDe = $"Xin chào, {tk.HoTen}";
            moTa = tk.VaiTro switch
            {
                VaiTroNguoiDung.GiaoHang => "Chúc bạn ca giao hàng an toàn, vạn dặm bình an!",
                VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc" => "Chúc bạn một ngày làm việc hiệu quả tại bưu cục!",
                _ => "Chào mừng bạn quay lại ứng dụng giao nhận GiaoNhanh!"
            };
        }

        TempData["PopupChaoMung"] = tieuDe;
        TempData["PopupVaiTro"] = tk.VaiTro;
        TempData["PopupMoTa"] = moTa;
        TempData["PopupHoTen"] = tk.HoTen;
        TempData["PopupAnh"] = tk.AnhDaiDien;
    }
}
