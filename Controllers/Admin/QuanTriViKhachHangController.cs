using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriViKhachHangController(QuanLyGiaoNhanDbContext db) : Controller
{
    private const int KichThuocTrang = 25;

    /// <summary>
    /// Quản lý ví khách hàng: danh sách yêu cầu rút tiền COD, lịch sử duyệt chi và biến động ví.
    /// </summary>
    public async Task<IActionResult> Index(string tab = "choduyet", int trang = 1, string? tuKhoa = null)
    {
        var truyVan = db.GiaoDichViKhachHangs.AsNoTracking()
            .Include(g => g.KhachHang)
            .Include(g => g.DonHang)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(g => EF.Functions.Collate(g.TenNganHang ?? "", "Latin1_General_100_CI_AI").Contains(tk)
                                      || (g.SoTaiKhoan != null && g.SoTaiKhoan.Contains(tk))
                                      || (g.ChuTaiKhoan != null && g.ChuTaiKhoan.Contains(tk))
                                      || (g.KhachHang != null && (g.KhachHang.HoTen.Contains(tk) || g.KhachHang.SoDienThoai.Contains(tk))));
        }

        // Thống kê nhanh
        var gocRutTien = db.GiaoDichViKhachHangs.Where(g => g.Loai == LoaiGiaoDichKhach.RutTien);
        var soCho = await gocRutTien.CountAsync(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan);
        var tienCho = await gocRutTien.Where(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan).SumAsync(g => (decimal?)g.SoTien) ?? 0;
        var soDa = await gocRutTien.CountAsync(g => g.TrangThai == TrangThaiGiaoDich.DaXacNhan);
        var tienDa = await gocRutTien.Where(g => g.TrangThai == TrangThaiGiaoDich.DaXacNhan).SumAsync(g => (decimal?)g.SoTien) ?? 0;
        var soTuChoi = await gocRutTien.CountAsync(g => g.TrangThai == TrangThaiGiaoDich.TuChoi);

        // Lọc theo tab
        truyVan = tab switch
        {
            "daduyet" => truyVan.Where(g => g.Loai == LoaiGiaoDichKhach.RutTien && g.TrangThai == TrangThaiGiaoDich.DaXacNhan),
            "tuchoi" => truyVan.Where(g => g.Loai == LoaiGiaoDichKhach.RutTien && g.TrangThai == TrangThaiGiaoDich.TuChoi),
            "tatca" => truyVan,
            _ => truyVan.Where(g => g.Loai == LoaiGiaoDichKhach.RutTien && g.TrangThai == TrangThaiGiaoDich.ChoXacNhan)
        };

        var ketQua = await DanhSachTrang<GiaoDichViKhachHang>.TaoAsync(
            truyVan.OrderByDescending(g => g.NgayTao),
            trang,
            KichThuocTrang
        );

        ViewBag.Tab = tab;
        ViewBag.TuKhoa = tuKhoa;

        var vm = new QuanTriViKhachHangVM
        {
            GiaoDich = ketQua,
            Trang = ketQua.Trang,
            TuKhoa = tuKhoa,
            SoChoXacNhan = soCho,
            TienChoXacNhan = tienCho,
            SoDaXacNhan = soDa,
            TienDaXacNhan = tienDa,
            SoTuChoi = soTuChoi
        };

        return View(vm);
    }

    /// <summary>
    /// Kế toán duyệt lệnh rút tiền: xác nhận đã chuyển khoản ngân hàng thành công cho khách hàng.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(DuyetRutTienVM vm)
    {
        await KhachHangViController.KhoaViKhach.WaitAsync();
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
            {
                await using var giaoTac = await db.Database.BeginTransactionAsync();
                var gd = await db.GiaoDichViKhachHangs
                    .Include(g => g.KhachHang)
                    .FirstOrDefaultAsync(g => g.MaGiaoDich == vm.MaGiaoDich);

                if (gd == null) return NotFound();

                if (gd.TrangThai != TrangThaiGiaoDich.ChoXacNhan)
                {
                    TempData["Error"] = "Lệnh rút tiền này đã được xử lý trước đó.";
                    return RedirectToAction(nameof(Index));
                }

                var bayGio = DateTime.Now;
                var nguoiDuyet = HttpContext.Session.NguoiThucHien();

                gd.TrangThai = TrangThaiGiaoDich.DaXacNhan;
                gd.NgayXuLy = bayGio;
                gd.NguoiXuLy = nguoiDuyet;
                gd.MaThamChieu = vm.MaThamChieu?.Trim();
                if (!string.IsNullOrWhiteSpace(vm.GhiChu))
                {
                    gd.GhiChu = vm.GhiChu.Trim();
                }

                try
                {
                    await db.SaveChangesAsync();

                    // Gửi thông báo tới tài khoản khách hàng
                    var tb = new ThongBaoXuLy(db, () => bayGio);
                    var tkKhach = tb.TkKhachHang(gd.MaKhachHang);
                    if (tkKhach.HasValue)
                    {
                        var maRef = string.IsNullOrEmpty(gd.MaThamChieu) ? "" : $" (Mã GD: {gd.MaThamChieu})";
                        tb.Gui(tkKhach.Value, LoaiThongBao.Vi,
                            $"Rút tiền COD thành công: {DinhDang.Tien(gd.SoTien)}",
                            $"Yêu cầu rút tiền {gd.MaHienThi} ({DinhDang.Tien(gd.SoTien)}) đã được kế toán chuyển khoản thành công vào tài khoản {gd.TenNganHang} ({gd.SoTaiKhoan}){maRef}.",
                            "/KhachHangVi", null);
                        await db.SaveChangesAsync();
                    }

                    await giaoTac.CommitAsync();

                    TempData["Success"] = $"Đã duyệt chi và chuyển khoản thành công {DinhDang.Tien(gd.SoTien)} cho khách hàng {gd.KhachHang?.HoTen}!";
                    return RedirectToAction(nameof(Index), new { tab = "choduyet" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    await giaoTac.RollbackAsync();
                    TempData["Error"] = "Lệnh rút tiền này vừa được thay đổi hoặc xử lý bởi thao tác khác. Vui lòng tải lại trang.";
                    return RedirectToAction(nameof(Index), new { tab = "choduyet" });
                }
            });
        }
        finally
        {
            KhachHangViController.KhoaViKhach.Release();
        }
    }

    /// <summary>
    /// Kế toán từ chối lệnh rút tiền (sai STK, tên chủ TK không khớp...) và hoàn lại tiền vào ví khách hàng.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(TuChoiRutTienVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.LyDo))
        {
            TempData["Error"] = "Vui lòng nhập lý do từ chối lệnh rút tiền.";
            return RedirectToAction(nameof(Index));
        }

        await KhachHangViController.KhoaViKhach.WaitAsync();
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync<IActionResult>(async () =>
            {
                await using var giaoTac = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
                var gd = await db.GiaoDichViKhachHangs
                    .Include(g => g.KhachHang)
                    .FirstOrDefaultAsync(g => g.MaGiaoDich == vm.MaGiaoDich);

                if (gd == null) return NotFound();

                if (gd.TrangThai != TrangThaiGiaoDich.ChoXacNhan)
                {
                    TempData["Error"] = "Lệnh rút tiền này đã được xử lý trước đó.";
                    return RedirectToAction(nameof(Index));
                }

                var khach = gd.KhachHang;
                if (khach == null) return NotFound();

                var bayGio = DateTime.Now;
                var nguoiDuyet = HttpContext.Session.NguoiThucHien();

                // Hoàn lại tiền vào ví khách
                khach.SoDuVi += gd.SoTien;

                gd.TrangThai = TrangThaiGiaoDich.TuChoi;
                gd.NgayXuLy = bayGio;
                gd.NguoiXuLy = nguoiDuyet;
                gd.GhiChu = $"Từ chối: {vm.LyDo.Trim()}";

                try
                {
                    await db.SaveChangesAsync();

                    // Gửi thông báo tới tài khoản khách hàng
                    var tb = new ThongBaoXuLy(db, () => bayGio);
                    var tkKhach = tb.TkKhachHang(gd.MaKhachHang);
                    if (tkKhach.HasValue)
                    {
                        tb.Gui(tkKhach.Value, LoaiThongBao.Vi,
                            $"Lệnh rút tiền bị từ chối: {gd.MaHienThi}",
                            $"Lệnh rút tiền {gd.MaHienThi} ({DinhDang.Tien(gd.SoTien)}) bị từ chối với lý do: \"{vm.LyDo.Trim()}\". Số tiền {DinhDang.Tien(gd.SoTien)} đã được hoàn lại vào Ví của bạn.",
                            "/KhachHangVi", null);
                        await db.SaveChangesAsync();
                    }

                    await giaoTac.CommitAsync();

                    TempData["Success"] = $"Đã từ chối lệnh rút {gd.MaHienThi} và hoàn lại {DinhDang.Tien(gd.SoTien)} vào ví khách hàng {khach.HoTen}.";
                    return RedirectToAction(nameof(Index), new { tab = "choduyet" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    await giaoTac.RollbackAsync();
                    TempData["Error"] = "Lệnh rút tiền này vừa được thay đổi hoặc xử lý bởi thao tác khác. Vui lòng tải lại trang.";
                    return RedirectToAction(nameof(Index), new { tab = "choduyet" });
                }
            });
        }
        finally
        {
            KhachHangViController.KhoaViKhach.Release();
        }
    }
}
