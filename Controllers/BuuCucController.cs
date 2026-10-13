using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * BuuCucController — URL: /BuuCuc/{Index|Quet|TiepNhan|NhanHangDen|NhanLai|Don|Huy|HoanHang|BaoSuCo}
 * Quyền: Nhân viên bưu cục (bưu cục của mình), Quản trị / Điều phối (chọn bưu cục để thao tác thay – ?maBuuCuc=).
 *   Index: tồn kho của bưu cục – chờ khách mang tới, đang ở bưu cục, đang đến (liên tỉnh), shipper khu vực đang giữ, chờ hoàn hàng.
 *   Quet:  quét QR / barcode (camera hoặc gõ mã) → hiện đơn và thao tác hợp lệ tại bưu cục:
 *          tiếp nhận hàng khách gửi (RECEIVED_AT_ORIGIN_HUB), nhận hàng shipper liên tỉnh bàn giao (ARRIVED_AT_DESTINATION_HUB),
 *          nhận lại hàng từ shipper khu vực, phân shipper cho chặng tiếp theo.
 *   Mỗi thao tác gửi kèm mã quét + toạ độ trình duyệt (nếu cho phép) để ghi Tracking Event.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.NhanVien)]
public class BuuCucController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    /// <summary>Bưu cục đang thao tác: nhân viên bưu cục cố định; quản trị / điều phối chọn (mặc định bưu cục đầu tiên).</summary>
    private async Task<BuuCuc?> LayBuuCucAsync(int? maBuuCuc)
    {
        int? ma = HttpContext.Session.MaBuuCuc() ?? maBuuCuc;
        return ma is null
            ? await db.BuuCucs.AsNoTracking().OrderBy(b => b.MaBuuCuc).FirstOrDefaultAsync()
            : await db.BuuCucs.AsNoTracking().FirstOrDefaultAsync(b => b.MaBuuCuc == ma);
    }

    private ThaoTac TaoThaoTac(int maBuuCuc, double? viDo, double? kinhDo) =>
        HttpContext.Session.ThaoTac(viDo, kinhDo) with { MaBuuCuc = maBuuCuc };

    private async Task NapChonBuuCucAsync(BuuCuc bc)
    {
        ViewBag.BuuCuc = bc;
        ViewBag.KhoaBuuCuc = HttpContext.Session.MaBuuCuc() is not null;
        ViewBag.DsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();
    }

    // ===================== TỒN KHO BƯU CỤC =====================

    public async Task<IActionResult> Index(int? maBuuCuc, string tab = "taibc")
    {
        var bc = await LayBuuCucAsync(maBuuCuc);
        if (bc is null) return View("ChuaCoBuuCuc");
        await NapChonBuuCucAsync(bc);
        int ma = bc.MaBuuCuc;

        var don = db.DonGiaoHangs.AsNoTracking();
        TrangThaiDon[] shipperKhuVucGiu = [TrangThaiDon.DaLayGiaoHang, TrangThaiDon.DangGiao, TrangThaiDon.GiaoKhongThanhCong, TrangThaiDon.HenGiaoLai, TrangThaiDon.DaLayTanNoi, TrangThaiDon.DangGiaoHoan];
        var nhom = new Dictionary<string, IQueryable<DonGiaoHang>>
        {
            ["cho"] = don.Where(d => d.TrangThai == TrangThaiDon.DaTao && d.MaBuuCucGui == ma),
            ["layhang"] = don.Where(d => (d.TrangThai == TrangThaiDon.ChoLayTanNoi || d.TrangThai == TrangThaiDon.DaLayTanNoi) && d.MaBuuCucGui == ma),
            ["taibc"] = don.Where(d => d.MaBuuCucHienTai == ma && d.TrangThai != TrangThaiDon.DaHuy && d.TrangThai != TrangThaiDon.DaHoanHang),
            ["dangden"] = don.Where(d => (d.MaBuuCucNhan == ma && (d.TrangThai == TrangThaiDon.DaLayLienTinh || d.TrangThai == TrangThaiDon.DangTrungChuyen))
                                      || (d.MaBuuCucGui == ma && d.TrangThai == TrangThaiDon.DangChuyenHoan)),
            ["shipper"] = don.Where(d => ((d.MaBuuCucNhan == ma && d.TrangThai != TrangThaiDon.DaLayTanNoi && d.TrangThai != TrangThaiDon.DangGiaoHoan)
                                       || (d.MaBuuCucGui == ma && (d.TrangThai == TrangThaiDon.DaLayTanNoi || d.TrangThai == TrangThaiDon.DangGiaoHoan)))
                                       && d.MaNhanVienDangGiu != null && shipperKhuVucGiu.Contains(d.TrangThai)),
            ["hoanhang"] = DonHangXuLy.DonChoHoanHang(don).Where(d => d.MaBuuCucHienTai == ma)
        };
        if (!nhom.ContainsKey(tab)) tab = "taibc";
        ViewBag.Tab = tab;
        ViewBag.SoTheoTab = new Dictionary<string, int>();
        foreach (var (k, q) in nhom) ((Dictionary<string, int>)ViewBag.SoTheoTab)[k] = await q.CountAsync();
        ViewBag.SoShipper = await db.NhanVienGiaoHangs.CountAsync(n => n.MaBuuCuc == ma && n.TrangThai != TrangThaiNhanVien.NgungHoatDong);

        var ds = await nhom[tab].Include(d => d.KhachHang).Include(d => d.KhuVuc).Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan)
                                .Include(d => d.NhanVienDangGiu).Include(d => d.PhanCongs)
                                .OrderBy(d => d.NgayGiaoDuKien).ThenBy(d => d.MaDon).Take(200).ToListAsync();
        return View(ds);
    }

    // ===================== QUÉT MÃ =====================

    public async Task<IActionResult> Quet(string? ma, int? maBuuCuc)
    {
        var bc = await LayBuuCucAsync(maBuuCuc);
        if (bc is null) return View("ChuaCoBuuCuc");
        await NapChonBuuCucAsync(bc);
        ViewBag.Ma = ma;
        if (string.IsNullOrWhiteSpace(ma)) return View((DonGiaoHang?)null);
        var don = await xuLy.TimTheoMaQuetAsync(ma);
        if (don is null) ViewBag.Loi = $"Không tìm thấy đơn khớp mã \"{ma}\"";
        return View(don);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TiepNhan(int maDon, string? ma, int maBuuCuc, double? viDo, double? kinhDo, bool daThuCuoc = false) =>
        await SauQuetAsync(maBuuCuc, async bc => await xuLy.TiepNhanAsync(maDon, ma, TaoThaoTac(bc, viDo, kinhDo), daThuCuoc));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanHangDen(int maDon, string? ma, int maBuuCuc, double? viDo, double? kinhDo) =>
        await SauQuetAsync(maBuuCuc, async bc => await xuLy.NhanHangDenAsync(maDon, ma, TaoThaoTac(bc, viDo, kinhDo)));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanLai(int maDon, string? ma, int maBuuCuc, double? viDo, double? kinhDo) =>
        await SauQuetAsync(maBuuCuc, async bc => await xuLy.NhanLaiTuShipperAsync(maDon, ma, TaoThaoTac(bc, viDo, kinhDo)));

    /// <summary>Sau mỗi lần quét xong trở lại trang quét (sẵn sàng quét kiện tiếp theo).</summary>
    private async Task<IActionResult> SauQuetAsync(int maBuuCuc, Func<int, Task<KetQua>> thaoTac)
    {
        var bc = await LayBuuCucAsync(maBuuCuc);
        if (bc is null) return NotFound();
        var kq = await thaoTac(bc.MaBuuCuc);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Quet), new { maBuuCuc = bc.MaBuuCuc });
    }

    // ===================== CHI TIẾT ĐƠN TẠI BƯU CỤC =====================

    public async Task<IActionResult> Don(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null || !LienQuan(don)) return NotFound();
        var dsPhanCong = don.PhanCongs.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong).ToList();
        return View(new ChiTietDonVM
        {
            Don = don,
            PhanCongHienTai = dsPhanCong.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
            DsPhanCong = dsPhanCong,
            DsLichSu = don.LichSus.OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).ToList(),
            CoTheHuy = DonHangXuLy.CoTheHuy(don, false) && CuaToi(don.MaBuuCucHienTai),
            CoThePhanCong = DonHangXuLy.CoThePhanCong(don) && CuaToi(don.MaBuuCucHienTai),
            CoTheDoiPhanCong = DonHangXuLy.CoTheDoiPhanCong(don.TrangThai) && CuaToi(don.MaBuuCucHienTai),
            ViTri = xuLy.XacDinhViTri(don)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo)
    {
        var kq = await xuLy.HuyDonAsync(id, lyDo, HttpContext.Session.ThaoTac(), null);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Don), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YeuCauChuyenHoan(int id, string? lyDo)
    {
        var kq = await xuLy.YeuCauChuyenHoanAsync(id, lyDo, HttpContext.Session.ThaoTac());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Don), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanHangTaiQuay(int id, bool daThuCuoc = false)
    {
        var kq = await xuLy.HoanHangTaiQuayAsync(id, daThuCuoc, HttpContext.Session.ThaoTac());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Don), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanHang(int id)
    {
        var kq = await xuLy.XacNhanHoanHangAsync(id, HttpContext.Session.ThaoTac());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Don), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BaoSuCo(int id, TrangThaiDon loai, string? moTa)
    {
        var kq = await xuLy.BaoSuCoAsync(id, loai, moTa, HttpContext.Session.ThaoTac());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Don), new { id });
    }

    public async Task<IActionResult> InNhan(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        return don is null || !LienQuan(don) ? NotFound() : View("~/Views/Shared/NhanDon.cshtml", don);
    }

    /// <summary>Nhân viên bưu cục chỉ xem đơn đi / đến / đang nằm ở bưu cục mình, hoặc shipper của bưu cục mình đang giữ.</summary>
    private bool LienQuan(DonGiaoHang don)
    {
        int? ma = HttpContext.Session.MaBuuCuc();
        return ma is null || don.MaBuuCucGui == ma || don.MaBuuCucNhan == ma || don.MaBuuCucHienTai == ma || don.NhanVienDangGiu?.MaBuuCuc == ma;
    }

    private bool CuaToi(int? maBuuCuc) => HttpContext.Session.MaBuuCuc() is not { } ma || ma == maBuuCuc;
}
