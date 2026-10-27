using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangViController(QuanLyGiaoNhanDbContext db) : Controller
{
    private int MaKhachHangCuaToi => HttpContext.Session.MaKhachHang() ?? 0;

    /// <summary>
    /// Trang tổng quan Ví tiền COD của khách hàng: số dư, lịch sử giao dịch và tài khoản ngân hàng.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var khach = await db.KhachHangs.FindAsync(MaKhachHangCuaToi);
        if (khach == null) return NotFound();

        var lichSu = await db.GiaoDichViKhachHangs.AsNoTracking()
            .Include(g => g.DonHang)
            .Where(g => g.MaKhachHang == MaKhachHangCuaToi)
            .OrderByDescending(g => g.NgayTao)
            .Take(50)
            .ToListAsync();

        var donChoDoiSoat = await db.DonGiaoHangs.AsNoTracking()
            .Where(d => d.MaKhachHang == MaKhachHangCuaToi && d.TrangThai == TrangThaiDon.GiaoThanhCong && d.TienThuHo > 0)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();

        var donHoanTat = await db.DonGiaoHangs.AsNoTracking()
            .Where(d => d.MaKhachHang == MaKhachHangCuaToi && d.TrangThai == TrangThaiDon.HoanTat && d.TienThuHo > 0)
            .CountAsync();

        var tongCODDaNhan = await db.GiaoDichViKhachHangs
            .Where(g => g.MaKhachHang == MaKhachHangCuaToi && g.Loai == LoaiGiaoDichKhach.CongTienCOD && g.TrangThai == TrangThaiGiaoDich.DaXacNhan)
            .SumAsync(g => (decimal?)g.SoTien) ?? 0;

        var tongDaRut = await db.GiaoDichViKhachHangs
            .Where(g => g.MaKhachHang == MaKhachHangCuaToi && g.Loai == LoaiGiaoDichKhach.RutTien && g.TrangThai == TrangThaiGiaoDich.DaXacNhan)
            .SumAsync(g => (decimal?)g.SoTien) ?? 0;

        var tienDangChoRut = await db.GiaoDichViKhachHangs
            .Where(g => g.MaKhachHang == MaKhachHangCuaToi && g.Loai == LoaiGiaoDichKhach.RutTien && g.TrangThai == TrangThaiGiaoDich.ChoXacNhan)
            .SumAsync(g => (decimal?)g.SoTien) ?? 0;

        var vm = new ViKhachHangVM
        {
            KhachHang = khach,
            SoDuVi = khach.SoDuVi,
            TongCODDaNhan = tongCODDaNhan,
            TongDaRut = tongDaRut,
            TienDangChoRut = tienDangChoRut,
            SoDonCODHoanTat = donHoanTat,
            LichSuGiaoDich = lichSu,
            DonCODChoDoiSoat = donChoDoiSoat
        };

        return View(vm);
    }

    /// <summary>
    /// Khách hàng gửi yêu cầu rút tiền COD về tài khoản ngân hàng.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RutTien(RutTienKhachHangVM vm)
    {
        if (vm.SoTien < 50000)
        {
            TempData["Error"] = "Số tiền rút tối thiểu là 50.000 đ.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(vm.TenNganHang) || string.IsNullOrWhiteSpace(vm.SoTaiKhoan) || string.IsNullOrWhiteSpace(vm.ChuTaiKhoan))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ thông tin ngân hàng nhận tiền.";
            return RedirectToAction(nameof(Index));
        }

        return await db.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            var khach = await db.KhachHangs.FirstOrDefaultAsync(k => k.MaKhachHang == MaKhachHangCuaToi);
            if (khach == null) return NotFound();

            if (vm.SoTien > khach.SoDuVi)
            {
                TempData["Error"] = $"Số dư ví của bạn không đủ (Hiện có: {DinhDang.Tien(khach.SoDuVi)}, Yêu cầu rút: {DinhDang.Tien(vm.SoTien)}).";
                return RedirectToAction(nameof(Index));
            }

            var bayGio = DateTime.Now;

            // Trừ tiền trong ví ngay khi tạo lệnh và ghi nhận số dư mới
            khach.SoDuVi -= vm.SoTien;
            khach.TenNganHang = vm.TenNganHang.Trim();
            khach.SoTaiKhoanNganHang = vm.SoTaiKhoan.Trim();
            khach.ChuTaiKhoanNganHang = vm.ChuTaiKhoan.Trim().ToUpperInvariant();

            var gd = new GiaoDichViKhachHang
            {
                MaKhachHang = khach.MaKhachHang,
                Loai = LoaiGiaoDichKhach.RutTien,
                SoTien = vm.SoTien,
                SoDuSauGiaoDich = khach.SoDuVi,
                TrangThai = TrangThaiGiaoDich.ChoXacNhan,
                TenNganHang = khach.TenNganHang,
                SoTaiKhoan = khach.SoTaiKhoanNganHang,
                ChuTaiKhoan = khach.ChuTaiKhoanNganHang,
                GhiChu = string.IsNullOrWhiteSpace(vm.GhiChu) ? "Yêu cầu rút tiền COD về tài khoản ngân hàng" : vm.GhiChu.Trim(),
                NgayTao = bayGio
            };

            db.GiaoDichViKhachHangs.Add(gd);
            await db.SaveChangesAsync();

            // Gửi thông báo đến Quản trị viên (Kế toán)
            var tb = new ThongBaoXuLy(db, () => bayGio);
            tb.GuiNhieu(tb.TkQuanTri(), LoaiThongBao.Vi,
                $"Yêu cầu rút tiền COD mới: {gd.MaHienThi}",
                $"Khách hàng {khach.HoTen} vừa yêu cầu rút {DinhDang.Tien(vm.SoTien)} về {khach.TenNganHang} ({khach.SoTaiKhoanNganHang}). Vui lòng kiểm tra và duyệt chi.",
                "/QuanTriViKhachHang", null);
            await db.SaveChangesAsync();

            await giaoTac.CommitAsync();

            TempData["Success"] = $"Đã tạo lệnh rút tiền {gd.MaHienThi} thành công ({DinhDang.Tien(vm.SoTien)})! Kế toán sẽ chuyển khoản vào tài khoản {khach.TenNganHang} của bạn trong vòng 24 giờ làm việc.";
            return RedirectToAction(nameof(Index));
        });
    }

    /// <summary>
    /// Lưu thông tin ngân hàng nhận tiền COD của khách hàng.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatNganHang(CapNhatNganHangVM vm)
    {
        if (!ModelState.IsValid)
        {
            var loi = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            TempData["Error"] = loi ?? "Thông tin tài khoản ngân hàng không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(vm.TenNganHang) || string.IsNullOrWhiteSpace(vm.SoTaiKhoan) || string.IsNullOrWhiteSpace(vm.ChuTaiKhoan))
        {
            TempData["Error"] = "Vui lòng nhập đầy đủ thông tin ngân hàng nhận tiền.";
            return RedirectToAction(nameof(Index));
        }

        var khach = await db.KhachHangs.FindAsync(MaKhachHangCuaToi);
        if (khach == null) return NotFound();

        khach.TenNganHang = vm.TenNganHang.Trim();
        khach.SoTaiKhoanNganHang = vm.SoTaiKhoan.Trim();
        khach.ChuTaiKhoanNganHang = vm.ChuTaiKhoan.Trim().ToUpperInvariant();

        await db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu thông tin tài khoản ngân hàng nhận tiền COD thành công!";
        return RedirectToAction(nameof(Index));
    }

    public static readonly SemaphoreSlim KhoaViKhach = new(1, 1);

    /// <summary>
    /// Khách hàng chủ động hủy lệnh rút tiền khi chưa được duyệt.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyRutTien(int id)
    {
        await KhoaViKhach.WaitAsync();
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
                var gd = await db.GiaoDichViKhachHangs.FirstOrDefaultAsync(g => g.MaGiaoDich == id && g.MaKhachHang == MaKhachHangCuaToi);
                if (gd == null) return NotFound();

                if (gd.TrangThai != TrangThaiGiaoDich.ChoXacNhan)
                {
                    TempData["Error"] = "Lệnh rút tiền này đã được xử lý hoặc đã hủy trước đó.";
                    return RedirectToAction(nameof(Index));
                }

                var khach = await db.KhachHangs.FindAsync(MaKhachHangCuaToi);
                if (khach == null) return NotFound();

                // Hoàn lại tiền vào ví
                khach.SoDuVi += gd.SoTien;
                gd.TrangThai = TrangThaiGiaoDich.TuChoi;
                gd.NgayXuLy = DateTime.Now;
                gd.NguoiXuLy = $"{khach.HoTen} (Khách hàng)";
                gd.GhiChu = "Khách hàng chủ động hủy yêu cầu rút tiền";

                try
                {
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();

                    TempData["Success"] = $"Đã hủy lệnh rút tiền {gd.MaHienThi}. Số tiền {DinhDang.Tien(gd.SoTien)} đã được hoàn lại vào ví của bạn.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync();
                    TempData["Error"] = "Lệnh rút tiền này vừa được xử lý bởi thao tác khác. Vui lòng tải lại trang.";
                    return RedirectToAction(nameof(Index));
                }
            });
        }
        finally
        {
            KhoaViKhach.Release();
        }
    }
}
