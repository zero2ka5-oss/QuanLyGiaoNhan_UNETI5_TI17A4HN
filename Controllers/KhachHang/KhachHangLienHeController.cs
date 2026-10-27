using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangLienHeController(QuanLyGiaoNhanDbContext db) : Controller
{
    private int MaKhachHangCuaToi => HttpContext.Session.MaKhachHang() ?? 0;
    private int MaTaiKhoanCuaToi => HttpContext.Session.MaTaiKhoan() ?? 0;

    /// <summary>Danh sách yêu cầu liên hệ & hỗ trợ của khách hàng.</summary>
    public async Task<IActionResult> Index(BoLocLienHe boLoc)
    {
        var truyVan = db.YeuCauLienHes.AsNoTracking()
            .Include(y => y.DonHang)
            .Include(y => y.BuuCuc)
            .Include(y => y.TinNhans)
            .Where(y => y.MaKhachHang == MaKhachHangCuaToi);

        if (!string.IsNullOrWhiteSpace(boLoc.TuKhoa))
        {
            var tk = boLoc.TuKhoa.Trim();
            truyVan = truyVan.Where(y => EF.Functions.Collate(y.TieuDe, "Latin1_General_100_CI_AI").Contains(tk)
                                      || EF.Functions.Collate(y.NoiDung, "Latin1_General_100_CI_AI").Contains(tk)
                                      || (y.DonHang != null && y.DonHang.TenNguoiNhan.Contains(tk)));
        }

        if (boLoc.TrangThai.HasValue)
        {
            truyVan = truyVan.Where(y => y.TrangThai == boLoc.TrangThai.Value);
        }

        if (boLoc.ChuDe.HasValue)
        {
            truyVan = truyVan.Where(y => y.ChuDe == boLoc.ChuDe.Value);
        }

        // Đếm theo trạng thái (chưa lọc trạng thái)
        var goc = db.YeuCauLienHes.Where(y => y.MaKhachHang == MaKhachHangCuaToi);
        var vm = new DanhSachLienHeVM
        {
            BoLoc = boLoc,
            LaKhachHang = true,
            TongSoChoPhanHoi = await goc.CountAsync(y => y.TrangThai == TrangThaiLienHe.ChoPhanHoi),
            TongSoDangXuLy = await goc.CountAsync(y => y.TrangThai == TrangThaiLienHe.DangXuLy),
            TongSoDaPhanHoi = await goc.CountAsync(y => y.TrangThai == TrangThaiLienHe.DaPhanHoi),
            TongSoDaDong = await goc.CountAsync(y => y.TrangThai == TrangThaiLienHe.DaDong),
            KetQua = await DanhSachTrang<YeuCauLienHe>.TaoAsync(
                truyVan.OrderByDescending(y => y.NgayCapNhat ?? y.NgayGui), boLoc.Trang, 10)
        };

        return View(vm);
    }

    /// <summary>Form gửi yêu cầu liên hệ / câu hỏi mới.</summary>
    public async Task<IActionResult> TaoMoi(int? maDon)
    {
        await NapDuLieuChonAsync(maDon);
        var vm = new TaoLienHeVM { MaDonHang = maDon };
        if (maDon.HasValue)
        {
            var don = await db.DonGiaoHangs.FindAsync(maDon.Value);
            if (don != null && don.MaKhachHang == MaKhachHangCuaToi)
            {
                vm.TieuDe = $"Thắc mắc đơn hàng {don.MaHienThi}";
                vm.MaBuuCuc = don.MaBuuCucGui;
            }
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(TaoLienHeVM vm)
    {
        if (!ModelState.IsValid)
        {
            await NapDuLieuChonAsync(vm.MaDonHang);
            return View(vm);
        }

        var bayGio = DateTime.Now;
        var lienHe = new YeuCauLienHe
        {
            MaKhachHang = MaKhachHangCuaToi,
            MaDonHang = vm.MaDonHang,
            MaBuuCuc = vm.MaBuuCuc,
            TieuDe = vm.TieuDe.Trim(),
            ChuDe = vm.ChuDe,
            NoiDung = vm.NoiDung.Trim(),
            NgayGui = bayGio,
            TrangThai = TrangThaiLienHe.ChoPhanHoi,
            NgayCapNhat = bayGio
        };

        db.YeuCauLienHes.Add(lienHe);
        await db.SaveChangesAsync();

        // Ghi tin nhắn đầu tiên
        var tinNhan = new TinNhanLienHe
        {
            MaLienHe = lienHe.MaLienHe,
            MaTaiKhoan = MaTaiKhoanCuaToi,
            NguoiGui = $"{HttpContext.Session.HoTen()} (Khách hàng)",
            LaNhanVien = false,
            NoiDung = vm.NoiDung.Trim(),
            NgayGui = bayGio
        };
        db.TinNhanLienHes.Add(tinNhan);
        await db.SaveChangesAsync();

        // Bắn thông báo cho Admin & Nhân viên bưu cục tiếp nhận
        var tb = new ThongBaoXuLy(db, () => DateTime.Now);
        var dsTkNhan = tb.TkQuanTri();
        if (vm.MaBuuCuc.HasValue)
        {
            var tkBc = await db.TaiKhoans.Where(t => t.MaBuuCuc == vm.MaBuuCuc.Value).Select(t => t.MaTaiKhoan).ToListAsync();
            dsTkNhan = dsTkNhan.Concat(tkBc).Distinct().ToList();
        }

        tb.GuiNhieu(dsTkNhan, LoaiThongBao.HeThong,
            $"Yêu cầu hỗ trợ mới: {lienHe.TieuDe}",
            $"Khách hàng {HttpContext.Session.HoTen()} vừa gửi yêu cầu hỗ trợ ({lienHe.ChuDe.TenHienThi()}): {lienHe.TieuDe}.",
            "/QuanTriLienHe/ChiTiet/" + lienHe.MaLienHe, lienHe.MaDonHang);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã gửi yêu cầu hỗ trợ {lienHe.MaHienThi}. Nhân viên CSKH sẽ phản hồi bạn trong thời gian sớm nhất!";
        return RedirectToAction(nameof(ChiTiet), new { id = lienHe.MaLienHe });
    }

    /// <summary>Chi tiết cuộc trao đổi liên hệ.</summary>
    public async Task<IActionResult> ChiTiet(int id)
    {
        var lienHe = await db.YeuCauLienHes
            .Include(y => y.KhachHang)
            .Include(y => y.BuuCuc)
            .Include(y => y.DonHang).ThenInclude(d => d!.KhuVuc)
            .Include(y => y.TinNhans.OrderBy(t => t.NgayGui))
            .FirstOrDefaultAsync(y => y.MaLienHe == id && y.MaKhachHang == MaKhachHangCuaToi);

        if (lienHe == null) return NotFound();
        return View(lienHe);
    }

    /// <summary>Khách hàng gửi thêm tin nhắn phản hồi trong cuộc trao đổi.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiTinNhan(int id, string noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung))
        {
            TempData["Error"] = "Vui lòng nhập nội dung tin nhắn.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var lienHe = await db.YeuCauLienHes.FirstOrDefaultAsync(y => y.MaLienHe == id && y.MaKhachHang == MaKhachHangCuaToi);
        if (lienHe == null) return NotFound();

        if (lienHe.TrangThai == TrangThaiLienHe.DaDong)
        {
            TempData["Error"] = "Yêu cầu này đã đóng, không thể gửi thêm tin nhắn. Bạn vui lòng tạo yêu cầu mới.";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }

        var bayGio = DateTime.Now;
        var tin = new TinNhanLienHe
        {
            MaLienHe = id,
            MaTaiKhoan = MaTaiKhoanCuaToi,
            NguoiGui = $"{HttpContext.Session.HoTen()} (Khách hàng)",
            LaNhanVien = false,
            NoiDung = noiDung.Trim(),
            NgayGui = bayGio
        };
        db.TinNhanLienHes.Add(tin);

        // Khách gửi tin -> cập nhật trạng thái về Chờ phản hồi để NV xử lý tiếp
        lienHe.TrangThai = TrangThaiLienHe.ChoPhanHoi;
        lienHe.NgayCapNhat = bayGio;
        await db.SaveChangesAsync();

        // Bắn thông báo cho Admin & Nhân viên
        var tb = new ThongBaoXuLy(db, () => DateTime.Now);
        var dsTk = tb.TkQuanTri();
        if (lienHe.MaBuuCuc.HasValue)
        {
            var tkBc = await db.TaiKhoans.Where(t => t.MaBuuCuc == lienHe.MaBuuCuc.Value).Select(t => t.MaTaiKhoan).ToListAsync();
            dsTk = dsTk.Concat(tkBc).Distinct().ToList();
        }
        tb.GuiNhieu(dsTk, LoaiThongBao.HeThong,
            $"Khách phản hồi yêu cầu {lienHe.MaHienThi}",
            $"Khách hàng {HttpContext.Session.HoTen()} vừa gửi thêm phản hồi: \"{DinhDang.Cat(noiDung, 80)}\".",
            "/QuanTriLienHe/ChiTiet/" + lienHe.MaLienHe, lienHe.MaDonHang);
        await db.SaveChangesAsync();

        TempData["Success"] = "Đã gửi tin nhắn phản hồi thành công!";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    /// <summary>Khách hàng chủ động đóng yêu cầu khi đã được giải đáp thỏa đáng.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DongYeuCau(int id)
    {
        var lienHe = await db.YeuCauLienHes.FirstOrDefaultAsync(y => y.MaLienHe == id && y.MaKhachHang == MaKhachHangCuaToi);
        if (lienHe == null) return NotFound();

        lienHe.TrangThai = TrangThaiLienHe.DaDong;
        lienHe.NgayCapNhat = DateTime.Now;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã đóng yêu cầu {lienHe.MaHienThi}. Cảm ơn bạn đã tin tưởng dịch vụ GiaoNhanh!";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    private async Task NapDuLieuChonAsync(int? maDonHienTai)
    {
        ViewBag.DsDonHang = await db.DonGiaoHangs.AsNoTracking()
            .Where(d => d.MaKhachHang == MaKhachHangCuaToi)
            .OrderByDescending(d => d.NgayTao)
            .Take(25)
            .Select(d => new { d.MaDon, d.MaHienThi, d.TenNguoiNhan, d.NgayTao })
            .ToListAsync();

        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking()
            .Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong)
            .OrderBy(b => b.TinhThanh).ThenBy(b => b.TenBuuCuc)
            .ToListAsync();
    }
}
