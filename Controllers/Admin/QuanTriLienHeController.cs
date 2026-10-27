using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.NhanVien)]
public class QuanTriLienHeController(QuanLyGiaoNhanDbContext db) : Controller
{
    private const int KichThuocTrang = 10;
    private int? MaBuuCucCuaToi => HttpContext.Session.MaBuuCuc();
    private bool LaQuanTri => HttpContext.Session.LaQuanTri();
    private int MaTaiKhoanCuaToi => HttpContext.Session.MaTaiKhoan() ?? 0;
    private string HoTenCuaToi => HttpContext.Session.HoTen();

    /// <summary>
    /// Danh sách yêu cầu hỗ trợ / giải đáp thắc mắc của khách hàng.
    /// Admin xem toàn bộ, nhân viên bưu cục ưu tiên các yêu cầu thuộc bưu cục mình quản lý.
    /// </summary>
    public async Task<IActionResult> Index(BoLocLienHe boLoc)
    {
        var truyVan = db.YeuCauLienHes.AsNoTracking()
            .Include(y => y.KhachHang)
            .Include(y => y.BuuCuc)
            .Include(y => y.DonHang)
            .Include(y => y.TinNhans)
            .AsQueryable();

        // Giới hạn phạm vi theo bưu cục nếu là nhân viên bưu cục
        if (!LaQuanTri && MaBuuCucCuaToi.HasValue)
        {
            var maBc = MaBuuCucCuaToi.Value;
            truyVan = truyVan.Where(y => y.MaBuuCuc == maBc
                                      || (y.DonHang != null && (y.DonHang.MaBuuCucGui == maBc || y.DonHang.MaBuuCucNhan == maBc))
                                      || y.MaBuuCuc == null);
        }

        // Lọc theo từ khóa
        if (!string.IsNullOrWhiteSpace(boLoc.TuKhoa))
        {
            var tk = boLoc.TuKhoa.Trim();
            truyVan = truyVan.Where(y => EF.Functions.Collate(y.TieuDe, "Latin1_General_100_CI_AI").Contains(tk)
                                      || EF.Functions.Collate(y.NoiDung, "Latin1_General_100_CI_AI").Contains(tk)
                                      || (y.KhachHang != null && (y.KhachHang.HoTen.Contains(tk) || y.KhachHang.SoDienThoai.Contains(tk)))
                                      || (y.DonHang != null && y.DonHang.TenNguoiNhan.Contains(tk)));
        }

        // Lọc theo bưu cục (Quản trị viên chọn lọc)
        if (boLoc.MaBuuCuc.HasValue)
        {
            truyVan = truyVan.Where(y => y.MaBuuCuc == boLoc.MaBuuCuc.Value);
        }

        // Lọc theo chủ đề
        if (boLoc.ChuDe.HasValue)
        {
            truyVan = truyVan.Where(y => y.ChuDe == boLoc.ChuDe.Value);
        }

        // Thống kê số lượng theo trạng thái trước khi lọc trạng thái
        var gocThongKe = truyVan;
        var tongChoPhanHoi = await gocThongKe.CountAsync(y => y.TrangThai == TrangThaiLienHe.ChoPhanHoi);
        var tongDangXuLy = await gocThongKe.CountAsync(y => y.TrangThai == TrangThaiLienHe.DangXuLy);
        var tongDaPhanHoi = await gocThongKe.CountAsync(y => y.TrangThai == TrangThaiLienHe.DaPhanHoi);
        var tongDaDong = await gocThongKe.CountAsync(y => y.TrangThai == TrangThaiLienHe.DaDong);

        // Lọc theo trạng thái
        if (boLoc.TrangThai.HasValue)
        {
            truyVan = truyVan.Where(y => y.TrangThai == boLoc.TrangThai.Value);
        }

        var ketQua = await DanhSachTrang<YeuCauLienHe>.TaoAsync(
            truyVan.OrderByDescending(y => y.NgayCapNhat ?? y.NgayGui),
            boLoc.Trang,
            KichThuocTrang
        );

        ViewBag.DsBuuCuc = new SelectList(
            await db.BuuCucs.AsNoTracking().Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(b => b.TenBuuCuc).ToListAsync(),
            "MaBuuCuc", "TenBuuCuc", boLoc.MaBuuCuc);

        var vm = new DanhSachLienHeVM
        {
            BoLoc = boLoc,
            LaKhachHang = false,
            TongSoChoPhanHoi = tongChoPhanHoi,
            TongSoDangXuLy = tongDangXuLy,
            TongSoDaPhanHoi = tongDaPhanHoi,
            TongSoDaDong = tongDaDong,
            KetQua = ketQua
        };

        return View(vm);
    }

    /// <summary>
    /// Xem chi tiết yêu cầu hỗ trợ, lịch sử trao đổi và gửi phản hồi.
    /// </summary>
    public async Task<IActionResult> ChiTiet(int id)
    {
        var lienHe = await db.YeuCauLienHes
            .Include(y => y.KhachHang)
            .Include(y => y.BuuCuc)
            .Include(y => y.DonHang).ThenInclude(d => d!.BuuCucGui)
            .Include(y => y.DonHang).ThenInclude(d => d!.BuuCucNhan)
            .Include(y => y.DonHang).ThenInclude(d => d!.KhuVuc)
            .Include(y => y.TinNhans.OrderBy(t => t.NgayGui))
            .FirstOrDefaultAsync(y => y.MaLienHe == id);

        if (lienHe == null) return NotFound();

        if (!LaQuanTri && MaBuuCucCuaToi.HasValue)
        {
            var maBc = MaBuuCucCuaToi.Value;
            bool thuocBuuCuc = lienHe.MaBuuCuc == null || lienHe.MaBuuCuc == maBc
                || (lienHe.DonHang != null && (lienHe.DonHang.MaBuuCucGui == maBc || lienHe.DonHang.MaBuuCucNhan == maBc));
            if (!thuocBuuCuc) return Forbid();
        }

        // Nếu đang ở trạng thái Chờ phản hồi thì tự động chuyển sang Đang xử lý khi CSKH mở xem
        if (lienHe.TrangThai == TrangThaiLienHe.ChoPhanHoi)
        {
            lienHe.TrangThai = TrangThaiLienHe.DangXuLy;
            lienHe.NgayCapNhat = DateTime.Now;
            if (!lienHe.MaBuuCuc.HasValue && MaBuuCucCuaToi.HasValue)
            {
                lienHe.MaBuuCuc = MaBuuCucCuaToi.Value;
            }
            await db.SaveChangesAsync();
        }

        return View(lienHe);
    }

    /// <summary>
    /// Nhân viên / Quản trị viên gửi phản hồi giải đáp cho khách hàng.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanHoi(GuiPhanHoiVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.NoiDung))
        {
            TempData["Error"] = "Vui lòng nhập nội dung phản hồi giải đáp thắc mắc.";
            return RedirectToAction(nameof(ChiTiet), new { id = vm.MaLienHe });
        }

        var lienHe = await db.YeuCauLienHes
            .Include(y => y.KhachHang)
            .Include(y => y.DonHang)
            .FirstOrDefaultAsync(y => y.MaLienHe == vm.MaLienHe);

        if (lienHe == null) return NotFound();

        if (!LaQuanTri && MaBuuCucCuaToi.HasValue)
        {
            var maBc = MaBuuCucCuaToi.Value;
            bool thuocBuuCuc = lienHe.MaBuuCuc == null || lienHe.MaBuuCuc == maBc
                || (lienHe.DonHang != null && (lienHe.DonHang.MaBuuCucGui == maBc || lienHe.DonHang.MaBuuCucNhan == maBc));
            if (!thuocBuuCuc) return Forbid();
        }

        var bayGio = DateTime.Now;
        var vaiTroHienThi = LaQuanTri ? "CSKH Tổng đài" : "Điều phối bưu cục";

        var tinMoi = new TinNhanLienHe
        {
            MaLienHe = lienHe.MaLienHe,
            MaTaiKhoan = MaTaiKhoanCuaToi,
            NguoiGui = $"{HoTenCuaToi} ({vaiTroHienThi})",
            LaNhanVien = true,
            NoiDung = vm.NoiDung.Trim(),
            NgayGui = bayGio
        };

        db.TinNhanLienHes.Add(tinMoi);

        // Cập nhật trạng thái
        lienHe.TrangThai = vm.TrangThaiMoi ?? TrangThaiLienHe.DaPhanHoi;
        lienHe.NgayCapNhat = bayGio;
        if (!lienHe.MaBuuCuc.HasValue && MaBuuCucCuaToi.HasValue)
        {
            lienHe.MaBuuCuc = MaBuuCucCuaToi.Value;
        }

        await db.SaveChangesAsync();

        // Gửi thông báo đến tài khoản khách hàng
        var tb = new ThongBaoXuLy(db, () => DateTime.Now);
        var tkKhach = tb.TkKhachHang(lienHe.MaKhachHang);
        if (tkKhach.HasValue)
        {
            tb.Gui(tkKhach.Value, LoaiThongBao.HoTro,
                $"Phản hồi yêu cầu {lienHe.MaHienThi}",
                $"CSKH đã phản hồi thắc mắc: \"{DinhDang.Cat(vm.NoiDung, 100)}\". Bấm để xem chi tiết.",
                $"/KhachHangLienHe/ChiTiet/{lienHe.MaLienHe}",
                lienHe.MaDonHang);
            await db.SaveChangesAsync();
        }

        TempData["Success"] = "Đã gửi câu trả lời và thông báo tới khách hàng thành công!";
        return RedirectToAction(nameof(ChiTiet), new { id = lienHe.MaLienHe });
    }

    /// <summary>
    /// Thay đổi nhanh trạng thái yêu cầu liên hệ.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id, TrangThaiLienHe trangThai)
    {
        var lienHe = await db.YeuCauLienHes.FirstOrDefaultAsync(y => y.MaLienHe == id);
        if (lienHe == null) return NotFound();

        lienHe.TrangThai = trangThai;
        lienHe.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật trạng thái yêu cầu {lienHe.MaHienThi} thành \"{trangThai.TenHienThi()}\".";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    /// <summary>
    /// Xóa yêu cầu hỗ trợ (chỉ Quản trị viên).
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        if (!LaQuanTri)
        {
            TempData["Error"] = "Chỉ Quản trị viên mới có quyền xóa yêu cầu hỗ trợ.";
            return RedirectToAction(nameof(Index));
        }

        var lienHe = await db.YeuCauLienHes.Include(y => y.TinNhans).FirstOrDefaultAsync(y => y.MaLienHe == id);
        if (lienHe == null) return NotFound();

        db.TinNhanLienHes.RemoveRange(lienHe.TinNhans);
        db.YeuCauLienHes.Remove(lienHe);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa yêu cầu hỗ trợ {lienHe.MaHienThi}.";
        return RedirectToAction(nameof(Index));
    }
}
