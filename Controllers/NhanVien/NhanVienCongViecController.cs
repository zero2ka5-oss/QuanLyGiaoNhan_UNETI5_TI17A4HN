using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienCongViecController — URL: /NhanVienCongViec/{Index|ChiTiet|Quet|LayHang|BatDau|BatDauTatCa|GiaoThanhCong|
 *                                                     GiaoKhongThanhCong|HenGiaoLai|CapNhatViTri}
 * Quyền: Nhân viên giao hàng (shipper liên tỉnh / khu vực) – chỉ thao tác chặng của CHÍNH MÌNH
 *        (MaNhanVien lấy từ Session; DonHangXuLy kiểm tra lại; phân công của người khác → 404).
 * Liên tỉnh: tới bưu cục gửi QUÉT MÃ nhận hàng → Bắt đầu vận chuyển → bưu cục nhận quét mã nhận hàng (bàn giao).
 * Khu vực:   tới bưu cục nhận QUÉT MÃ nhận hàng → Bắt đầu giao → Thành công / Không thành công → hẹn giao lại / trả về bưu cục.
 * Vị trí: trình duyệt của shipper gửi GPS định kỳ (CapNhatViTri) khi shipper bật "Chia sẻ vị trí".
 */
[YeuCauVaiTro(VaiTroNguoiDung.GiaoHang)]
public class NhanVienCongViecController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy, AnhXuLy anhXuLy) : Controller
{
    private int MaNhanVienCuaToi => HttpContext.Session.MaNhanVien() ?? -1;

    // ===================== CÔNG VIỆC HÔM NAY =====================

    public async Task<IActionResult> Index()
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.BuuCuc).Include(n => n.PhuongTien)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == MaNhanVienCuaToi);
        if (nv is null) return RedirectToAction("KhongCoQuyen", "DangNhap");
        var dsHieuLuc = await xuLy.PhanCongHieuLuc().AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LoaiHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhachHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucGui)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucNhan)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucGui)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucNhan)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucTrungGian)
            .Include(p => p.PhuongTien)
            .Where(p => p.MaNhanVien == nv.MaNhanVien)
            .OrderBy(p => p.DonGiaoHang!.NgayGiaoDuKien).ThenBy(p => p.NgayPhanCong).ToListAsync();
        var homNay = DateTime.Today;
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
        var coThuNhap = db.PhanCongGiaoHangs.Where(p => p.MaNhanVien == nv.MaNhanVien
                                                     && (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc || p.KetQua == KetQuaGiao.DaLayHang || p.KetQua == KetQuaGiao.DaHoanHang));
        return View(new CongViecNhanVienVM
        {
            NhanVien = nv,
            CanNhanHang = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DaPhanCong).ToList(),
            DaNhanHang = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DaNhanHang).ToList(),
            DangGiao = dsHieuLuc.Where(p => p.TrangThai == TrangThaiPhanCong.DangGiao).ToList(),
            GiuSauThatBai = await db.DonGiaoHangs.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.BuuCucNhan).Include(d => d.PhanCongs)
                .Where(d => d.MaNhanVienDangGiu == nv.MaNhanVien && d.TrangThai == TrangThaiDon.GiaoKhongThanhCong)
                .OrderBy(d => d.NgayGiaoDuKien).ToListAsync(),
            GiaoThanhCongHomNay = await coThuNhap.CountAsync(p => p.NgayKetThuc >= homNay),
            ThatBaiHomNay = await db.PhanCongGiaoHangs.CountAsync(p => p.MaNhanVien == nv.MaNhanVien && p.KetQua == KetQuaGiao.GiaoKhongThanhCong && p.NgayKetThuc >= homNay),
            // Thu nhập = hoa hồng (ThuNhapShipper) các chặng hoàn thành: giao thành công / bàn giao bưu cục nhận / lấy hàng về bưu cục / giao hoàn
            TienHomNay = await coThuNhap.Where(p => p.NgayKetThuc >= homNay).SumAsync(p => (decimal?)p.ThuNhapShipper) ?? 0,
            TienThangNay = await coThuNhap.Where(p => p.NgayKetThuc >= dauThang).SumAsync(p => (decimal?)p.ThuNhapShipper) ?? 0,
            TienCanThu = dsHieuLuc.Where(p => p.LoaiChang == LoaiShipper.KhuVuc && p.DonGiaoHang!.TrangThai != TrangThaiDon.DaLayTanNoi).Sum(p => p.DonGiaoHang!.LaDonHoan ? p.DonGiaoHang!.PhiHoanChuaThu : p.DonGiaoHang!.TongThuNguoiNhan)
        });
    }

    /// <summary>Chi tiết một phân công – nhân viên chỉ xem được phân công của mình (kể cả đã kết thúc).</summary>
    public async Task<IActionResult> ChiTiet(int id)
    {
        var pc = await db.PhanCongGiaoHangs.AsNoTracking()
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LoaiHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhachHang)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucGui)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucNhan)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucGui)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucNhan)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucTrungGian)
            .Include(p => p.DonGiaoHang!).ThenInclude(d => d.LichSus).ThenInclude(l => l.BuuCuc)
            .Include(p => p.PhuongTien)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (pc is null || pc.MaNhanVien != MaNhanVienCuaToi) return NotFound();
        return View(pc);
    }

    // ===================== QUÉT MÃ NHẬN HÀNG TẠI BƯU CỤC =====================

    /// <summary>Trang quét QR / barcode: đọc mã → hiện đơn và nút "Nhận hàng" nếu shipper được phân chặng của đơn.</summary>
    public async Task<IActionResult> Quet(string? ma)
    {
        ViewBag.Ma = ma;
        if (string.IsNullOrWhiteSpace(ma)) return View((DonGiaoHang?)null);
        var don = await xuLy.TimTheoMaQuetAsync(ma);
        if (don is null) { ViewBag.Loi = "Không tìm thấy đơn khớp mã vừa quét"; return View((DonGiaoHang?)null); }
        var pc = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai) && p.MaNhanVien == MaNhanVienCuaToi);
        if (pc is null && don.MaNhanVienDangGiu != MaNhanVienCuaToi)
        {
            ViewBag.Loi = $"Đơn {don.MaHienThi} không được phân cho bạn";
            return View((DonGiaoHang?)null);
        }
        ViewBag.PhanCong = pc;
        return View(don);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LayHang(int maDon, string? ma, double? viDo, double? kinhDo)
    {
        var kq = await xuLy.LayHangAsync(maDon, ma, MaNhanVienCuaToi, HttpContext.Session.ThaoTac(viDo, kinhDo));
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return kq.ThanhCong ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Quet), new { ma });
    }

    /// <summary>Shipper xác nhận lấy hàng trực tiếp tại địa chỉ người gửi (không bắt buộc quét mã vạch).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanLayHang(int maDon, double? viDo, double? kinhDo)
    {
        var don = await db.DonGiaoHangs.FindAsync(maDon);
        var maQuet = don?.MaHienThi;
        var kq = await xuLy.LayHangAsync(maDon, maQuet, MaNhanVienCuaToi, HttpContext.Session.ThaoTac(viDo, kinhDo));
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    // ===================== [NHIỆM VỤ: GIAO HÀNG - VẬN CHUYỂN, GIAO HÀNG & KẾT QUẢ] =====================

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>[NHIỆM VỤ: GIAO HÀNG] Bắt đầu xuất phát vận chuyển (liên tỉnh) hoặc đi giao tới khách (khu vực).</summary>
    public async Task<IActionResult> BatDau(int id, double? viDo, double? kinhDo) =>
        KetQuaThaoTac(await xuLy.BatDauAsync(id, MaNhanVienCuaToi, HttpContext.Session.ThaoTac(viDo, kinhDo)), id);

    /// <summary>Shipper liên tỉnh xuất bến: bắt đầu vận chuyển mọi đơn đã nhận.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatDauTatCa(double? viDo, double? kinhDo)
    {
        var kq = await xuLy.BatDauTatCaLienTinhAsync(MaNhanVienCuaToi, HttpContext.Session.ThaoTac(viDo, kinhDo));
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    /// <summary>[NHIỆM VỤ: GIAO HÀNG] Shipper xác nhận giao thành công, chụp ảnh bằng chứng và chốt công nợ COD/ví.</summary>
    public async Task<IActionResult> GiaoThanhCong(int id, string? ghiChu, IFormFile? anhGiaoHang, double? viDo, double? kinhDo)
    {
        // Ảnh giao hàng không bắt buộc; có chọn ảnh mà ảnh lỗi thì dừng lại để nhân viên chụp lại
        string? anh = null;
        if (anhGiaoHang is { Length: > 0 })
        {
            (anh, string? loi) = await anhXuLy.LuuAsync(anhGiaoHang, AnhXuLy.ThuMucGiaoHang);
            if (anh is null) { TempData["Error"] = loi; return RedirectToAction(nameof(ChiTiet), new { id }); }
        }
        var kq = await xuLy.GiaoThanhCongAsync(id, MaNhanVienCuaToi, ghiChu, HttpContext.Session.ThaoTac(viDo, kinhDo));
        if (anh is not null)
        {
            var don = kq.ThanhCong ? await db.PhanCongGiaoHangs.Where(p => p.MaPhanCong == id).Select(p => p.DonGiaoHang).FirstOrDefaultAsync() : null;
            if (don is null) anhXuLy.Xoa(anh);
            else { don.AnhGiaoHang = anh; await db.SaveChangesAsync(); }
        }
        return KetQuaThaoTac(kq, id);
    }

    /// <summary>[NHIỆM VỤ: XỬ LÝ GIAO KHÔNG THÀNH CÔNG] Shipper ghi nhận giao thất bại (chọn lý do), đơn chuyển GiaoKhongThanhCong và shipper tạm giữ kiện hàng.</summary>
    public async Task<IActionResult> GiaoKhongThanhCong(int id)
    {
        var pc = await db.PhanCongGiaoHangs.AsNoTracking().Include(p => p.DonGiaoHang).FirstOrDefaultAsync(p => p.MaPhanCong == id);
        if (pc is null || pc.MaNhanVien != MaNhanVienCuaToi) return NotFound();
        if (pc.TrangThai != TrangThaiPhanCong.DangGiao || pc.LoaiChang != LoaiShipper.KhuVuc)
        {
            TempData["Error"] = "Chỉ đơn Đang giao hàng mới được ghi nhận giao không thành công";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        return View(new GiaoThatBaiVM { MaPhanCong = id, Don = pc.DonGiaoHang });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoKhongThanhCong(GiaoThatBaiVM vm, double? viDo, double? kinhDo)
    {
        if (vm.LyDo == LyDoThatBai.Khac && string.IsNullOrWhiteSpace(vm.ChiTiet))
            ModelState.AddModelError(nameof(vm.ChiTiet), "Mô tả lý do khi chọn 'Lý do khác'");
        if (ModelState.IsValid)
        {
            var kq = await xuLy.GiaoThatBaiAsync(vm.MaPhanCong, MaNhanVienCuaToi, vm.LyDo, vm.ChiTiet, HttpContext.Session.ThaoTac(viDo, kinhDo));
            if (kq.ThanhCong)
            {
                TempData["Success"] = kq.ThongBao;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", kq.ThongBao);
        }
        vm.Don = await db.PhanCongGiaoHangs.Where(p => p.MaPhanCong == vm.MaPhanCong && p.MaNhanVien == MaNhanVienCuaToi)
                         .Select(p => p.DonGiaoHang).FirstOrDefaultAsync();
        if (vm.Don is null) return NotFound();
        return View(vm);
    }

    /// <summary>[NHIỆM VỤ: XỬ LÝ GIAO KHÔNG THÀNH CÔNG] Shipper hẹn ngày giao lại (Rescheduled) sau lần giao lỗi; tạo phân công mới để hôm sau đi giao tiếp.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HenGiaoLai(int maDon, DateTime? ngayHen, string? ghiChu, double? viDo, double? kinhDo)
    {
        var kq = await xuLy.HenGiaoLaiAsync(maDon, MaNhanVienCuaToi, ngayHen, ghiChu, HttpContext.Session.ThaoTac(viDo, kinhDo));
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    // ===================== VỊ TRÍ GPS =====================

    /// <summary>Trình duyệt của shipper gửi vị trí (fetch + header chống CSRF). Vị trí này là vị trí các đơn shipper đang giữ.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatViTri(double viDo, double kinhDo)
    {
        if (viDo is < -90 or > 90 || kinhDo is < -180 or > 180) return BadRequest(new { thongBao = "Toạ độ không hợp lệ" });
        var nv = await db.NhanVienGiaoHangs.FindAsync(MaNhanVienCuaToi);
        if (nv is null) return NotFound();
        nv.ViDo = viDo;
        nv.KinhDo = kinhDo;
        nv.ThoiGianViTri = DateTime.Now;
        await db.SaveChangesAsync();
        return Json(new { daLuu = true, luc = nv.ThoiGianViTri.Value.ToString("HH:mm:ss") });
    }

    private IActionResult KetQuaThaoTac(KetQua kq, int maPhanCong)
    {
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id = maPhanCong });
    }
}
