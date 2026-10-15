using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangDonHangController — URL: /KhachHangDonHang/{Index|ChiTiet|ViTri|InNhan|TaoMoi|ChinhSua|Huy}
 * Quyền: Khách hàng – chỉ thấy / thao tác đơn của mình: MaKhachHang lấy từ Session, KHÔNG nhận từ URL hay form.
 * Đơn của khách khác → 404 (không để lộ đơn đó có tồn tại hay không).
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangDonHangController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private const int KichThuocTrang = 10;
    private int MaKhachHangCuaToi => HttpContext.Session.MaKhachHang() ?? -1;

    // ===================== DANH SÁCH =====================

    public async Task<IActionResult> Index(BoLocDonHang boLoc)
    {
        var truyVan = DonHangXuLy.LocDonHang(db.DonGiaoHangs.AsNoTracking()
                        .Include(d => d.LoaiHang).Include(d => d.KhuVuc)
                        .Where(d => d.MaKhachHang == MaKhachHangCuaToi), boLoc);

        var soDon = await truyVan.GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        if (boLoc.TrangThai.HasValue) truyVan = truyVan.Where(d => d.TrangThai == boLoc.TrangThai);

        var ketQua = await DanhSachTrang<DonGiaoHang>.TaoAsync(DonHangXuLy.SapXepDonHang(truyVan, boLoc.SapXep), boLoc.Trang, KichThuocTrang);
        boLoc.Trang = ketQua.Trang;
        ViewBag.DsLoaiHang = new SelectList(await db.LoaiHangs.OrderBy(l => l.TenLoaiHang).ToListAsync(), "MaLoaiHang", "TenLoaiHang", boLoc.MaLoaiHang);
        ViewBag.DsKhuVuc = new SelectList(await db.KhuVucs.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", boLoc.MaKhuVuc);
        ViewBag.SoDonNoCuoc = await DonHangXuLy.DonConNoPhi(db.DonGiaoHangs.AsNoTracking().Where(d => d.MaKhachHang == MaKhachHangCuaToi)).CountAsync();
        return View(new DanhSachDonVM { BoLoc = boLoc, KetQua = ketQua, SoDonTheoTrangThai = soDon, LaKhachHang = true });
    }

    // ===================== CHI TIẾT =====================

    public async Task<IActionResult> ChiTiet(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null || don.MaKhachHang != MaKhachHangCuaToi) return NotFound();

        var dsPhanCong = don.PhanCongs.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong).ToList();
        return View(new ChiTietDonVM
        {
            Don = don,
            PhanCongHienTai = dsPhanCong.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
            DsPhanCong = dsPhanCong,
            DsLichSu = don.LichSus.OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).ToList(),
            LaKhachHang = true,
            CoTheSua = DonHangXuLy.CoTheSua(don, true),
            CoTheHuy = DonHangXuLy.CoTheHuy(don, true),
            ViTri = xuLy.XacDinhViTri(don)
        });
    }

    /// <summary>Vị trí hiện tại của đơn (JSON) – bản đồ trên trang chi tiết tự làm mới khi shipper đang giữ hàng.</summary>
    public async Task<IActionResult> ViTri(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null || don.MaKhachHang != MaKhachHangCuaToi) return NotFound();
        return Json(xuLy.XacDinhViTri(don));
    }

    /// <summary>Nhãn dán kiện hàng: mã vận đơn dạng QR + barcode, người gửi / nhận, tuyến bưu cục (khổ in nhiệt 100 × 150 mm).</summary>
    public async Task<IActionResult> InNhan(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null || don.MaKhachHang != MaKhachHangCuaToi) return NotFound();
        return View("~/Views/Shared/NhanDon.cshtml", don);
    }

    // ===================== TẠO / SỬA =====================

    /// <summary>Có thể điền sẵn từ nút "Tạo đơn với thông tin này" của khối ước lượng phí trên trang chủ.</summary>
    public async Task<IActionResult> TaoMoi(int? maKhuVuc, int? maLoaiHang, decimal? khoiLuong, decimal? tienThuHo, NguoiTraPhi? nguoiTraPhi)
    {
        // Khách bị ngừng hoạt động vẫn đăng nhập để theo dõi đơn cũ / công nợ, nhưng không mở form tạo đơn
        if (!await db.KhachHangs.AnyAsync(k => k.MaKhachHang == MaKhachHangCuaToi && k.TrangThai == TrangThaiHoatDong.HoatDong))
        {
            TempData["Error"] = "Tài khoản khách hàng đã ngừng hoạt động – không tạo được đơn mới. Vui lòng liên hệ công ty để mở lại.";
            return RedirectToAction("Index", "KhachHangTrangChu");
        }

        // Chặn tạo đơn mới nếu đang có đơn nợ cước vận chuyển chưa thanh toán (chống bùng cước)
        if (await KiemTraChanNoCuocAsync()) return RedirectToAction(nameof(CongNo));

        await NapDanhMucFormAsync(null);
        var vm = new DonGiaoHangFormVM
        {
            // Mặc định bưu cục gửi lần trước của khách
            MaBuuCucGui = await db.DonGiaoHangs.Where(d => d.MaKhachHang == MaKhachHangCuaToi && d.MaBuuCucGui != null)
                                  .OrderByDescending(d => d.NgayTao).Select(d => d.MaBuuCucGui).FirstOrDefaultAsync()
        };
        if (maKhuVuc > 0) vm.MaKhuVuc = maKhuVuc.Value;
        if (maLoaiHang > 0) vm.MaLoaiHang = maLoaiHang.Value;
        if (khoiLuong is > 0 and <= 100_000) vm.KhoiLuong = khoiLuong.Value;
        if (tienThuHo is > 0 and <= 1_000_000_000) vm.TienThuHo = tienThuHo.Value;
        if (nguoiTraPhi.HasValue) vm.NguoiTraPhi = nguoiTraPhi.Value;
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(DonGiaoHangFormVM vm)
    {
        if (await KiemTraChanNoCuocAsync()) return RedirectToAction(nameof(CongNo));

        if (vm.NgayGiaoDuKien.Date < DateTime.Today)
            ModelState.AddModelError(nameof(vm.NgayGiaoDuKien), "Ngày giao dự kiến không được trước ngày tạo đơn");
        if (!ModelState.IsValid) { await NapDanhMucFormAsync(null); return View("BieuMau", vm); }

        var kq = await xuLy.TaoDonAsync(vm, MaKhachHangCuaToi, HttpContext.Session.NguoiThucHien());
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError("", kq.ThongBao);
            await NapDanhMucFormAsync(null);
            return View("BieuMau", vm);
        }
        TempData["Success"] = $"{kq.ThongBao}. Phí vận chuyển {DinhDang.Tien(kq.DuLieu!.PhiVanChuyen)} – in nhãn dán lên kiện hàng trước khi gửi.";
        return RedirectToAction(nameof(ChiTiet), new { id = kq.DuLieu.MaDon });
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var don = await db.DonGiaoHangs.AsNoTracking().Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == id && d.MaKhachHang == MaKhachHangCuaToi);
        if (don is null) return NotFound();
        if (!DonHangXuLy.CoTheSua(don, true))
        {
            TempData["Error"] = $"Đơn {don.MaHienThi} đang '{don.TrangThai.TenHienThi()}' nên không thể sửa";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        await NapDanhMucFormAsync(don);
        ViewBag.Don = don;
        return View("BieuMau", DonGiaoHangFormVM.TuDon(don));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, DonGiaoHangFormVM vm)
    {
        var kq = ModelState.IsValid ? await xuLy.CapNhatDonAsync(id, vm, HttpContext.Session.NguoiThucHien(), MaKhachHangCuaToi) : null;
        if (kq is { ThanhCong: true })
        {
            TempData["Success"] = kq.ThongBao;
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (kq is not null) ModelState.AddModelError("", kq.ThongBao);
        var don = await db.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDon == id && d.MaKhachHang == MaKhachHangCuaToi);
        if (don is null) return NotFound();
        await NapDanhMucFormAsync(don);
        ViewBag.Don = don;
        vm.MaDon = id;
        return View("BieuMau", vm);
    }

    // ===================== HỦY =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo)
    {
        var kq = await xuLy.HuyDonAsync(id, lyDo, HttpContext.Session.ThaoTac(), MaKhachHangCuaToi);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    // ===================== CÔNG NỢ & THANH TOÁN CƯỚC =====================

    [HttpGet]
    public async Task<IActionResult> CongNo()
    {
        var dsNo = await DonHangXuLy.DonConNoPhi(db.DonGiaoHangs.AsNoTracking()
                                    .Include(d => d.KhuVuc)
                                    .Include(d => d.LoaiHang)
                                    .Include(d => d.BuuCucGui)
                                    .Include(d => d.BuuCucNhan)
                                    .Where(d => d.MaKhachHang == MaKhachHangCuaToi))
                                    .OrderByDescending(d => d.NgayHoanTat ?? d.NgayHoanHang ?? d.NgayTao)
                                    .ToListAsync();

        var kh = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKhachHang == MaKhachHangCuaToi);
        ViewBag.KhachHang = kh;
        ViewBag.TongNo = dsNo.Sum(d => d.PhiNguoiGuiPhaiTra);
        return View(dsNo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToanCuoc(int id)
    {
        var don = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaDon == id && d.MaKhachHang == MaKhachHangCuaToi);
        if (don is null) return NotFound();
        if (!don.ConNoPhi)
        {
            TempData["Error"] = $"Đơn {don.MaHienThi} không có khoản cước nợ cần thanh toán.";
            return RedirectToAction(nameof(CongNo));
        }

        don.NgayYeuCauThanhToan = DateTime.Now;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã gửi yêu cầu xác nhận thanh toán cước cho đơn {don.MaHienThi}. Trạng thái chuyển sang Chờ duyệt (Pending) – Vui lòng đợi Admin kiểm tra tài khoản và phê duyệt!";
        return RedirectToAction(nameof(CongNo));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToanTatCaNo()
    {
        var dsNo = await DonHangXuLy.DonConNoPhi(db.DonGiaoHangs.Where(d => d.MaKhachHang == MaKhachHangCuaToi && d.NgayYeuCauThanhToan == null)).ToListAsync();
        if (dsNo.Count == 0)
        {
            TempData["Success"] = "Tất cả các đơn nợ cước của bạn đã được gửi yêu cầu thanh toán và đang ở trạng thái Chờ duyệt (Pending).";
            return RedirectToAction(nameof(CongNo));
        }

        decimal tongTien = dsNo.Sum(d => d.PhiNguoiGuiPhaiTra);
        var bayGio = DateTime.Now;
        foreach (var don in dsNo)
        {
            don.NgayYeuCauThanhToan = bayGio;
        }
        await db.SaveChangesAsync();

        TempData["Success"] = $"Đã gửi yêu cầu thanh toán cho {dsNo.Count} đơn hàng (tổng {DinhDang.Tien(tongTien)}). Trạng thái đã chuyển sang Chờ duyệt (Pending) – Vui lòng đợi Admin kiểm tra và duyệt!";
        return RedirectToAction(nameof(CongNo));
    }

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Kiểm tra nếu khách đang có đơn nợ cước thì thiết lập thông báo và trả về true để chuyển hướng sang trang Công nợ.</summary>
    private async Task<bool> KiemTraChanNoCuocAsync()
    {
        var dsNoCuoc = await DonHangXuLy.DonConNoPhi(db.DonGiaoHangs.Where(d => d.MaKhachHang == MaKhachHangCuaToi)).ToListAsync();
        if (dsNoCuoc.Count == 0) return false;

        decimal tongNo = dsNoCuoc.Sum(d => d.PhiNguoiGuiPhaiTra);
        int soChoDuyet = dsNoCuoc.Count(d => d.DangChoDuyetCuoc);
        TempData["Error"] = soChoDuyet == dsNoCuoc.Count
            ? $"Bạn đang có {dsNoCuoc.Count} đơn nợ cước ({DinhDang.Tien(tongNo)}) đang chờ Admin xét duyệt. Ngay khi Admin duyệt, tài khoản sẽ được mở khóa tạo đơn mới."
            : $"Bạn đang có {dsNoCuoc.Count} đơn nợ cước vận chuyển chưa thanh toán (tổng nợ: {DinhDang.Tien(tongNo)}). Vui lòng thanh toán công nợ cước để tiếp tục tạo đơn mới.";
        return true;
    }

    /// <summary>Bưu cục khách có thể mang hàng tới gửi (đang hoạt động; khi sửa đơn giữ thêm bưu cục đã chọn).</summary>
    public static Task<List<BuuCuc>> DanhSachBuuCucGuiAsync(QuanLyGiaoNhanDbContext db, DonGiaoHang? donCu) =>
        db.BuuCucs.AsNoTracking()
          .Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && b.MaBuuCuc == donCu.MaBuuCucGui))
          .OrderBy(b => b.TinhThanh).ThenBy(b => b.TenBuuCuc).ToListAsync();

    /// <summary>Chỉ liệt kê loại hàng / khu vực đang hoạt động; khi sửa đơn cũ thì giữ thêm mục đang chọn.</summary>
    private async Task NapDanhMucFormAsync(DonGiaoHang? donCu)
    {
        ViewBag.DsLoaiHang = await db.LoaiHangs.AsNoTracking()
            .Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && l.MaLoaiHang == donCu.MaLoaiHang))
            .OrderBy(l => l.HeSoPhuThu).ToListAsync();
        ViewBag.DsKhuVuc = await db.KhuVucs.AsNoTracking().Include(k => k.BuuCuc)
            .Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && k.MaKhuVuc == donCu.MaKhuVuc))
            .OrderBy(k => k.PhiCoBan).ToListAsync();
        ViewBag.DsBuuCuc = await DanhSachBuuCucGuiAsync(db, donCu);
        // Thẻ "Người gửi" trên form: thông tin người gửi (chính khách hàng đang đăng nhập)
        ViewBag.KhachHang = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKhachHang == MaKhachHangCuaToi);
    }
}
