// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 + 5 - Quản lý ví shipper: số dư / tiền đang giữ của từng shipper, lịch sử giao dịch,
//                     xác nhận nộp tiền thu hộ (đối soát đơn) và xác nhận / từ chối yêu cầu rút tiền.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriViShipperController — URL: /QuanTriViShipper/{Index|ChiTiet|XacNhan|TuChoi}
 * Quyền: Quản trị, Điều phối (bộ phận chủ quản / kế toán đối soát).
 * Xác nhận / từ chối giao dịch (nộp tiền, rút tiền): chỉ Quản trị – kế toán đối soát; Điều phối chỉ xem.
 * Xác nhận lệnh nộp tiền → các đơn trong lệnh được đối soát & hoàn tất; từ chối → đơn trở lại "shipper đang giữ tiền".
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriViShipperController(QuanLyGiaoNhanDbContext db, ViXuLy viXuLy) : Controller
{
    public async Task<IActionResult> Index(LoaiGiaoDich? loai, TrangThaiGiaoDich? trangThai, int? maNhanVien, int trang = 1)
    {
        var truyVan = db.GiaoDichVis.AsNoTracking().Include(g => g.NhanVien).AsQueryable();
        if (loai.HasValue) truyVan = truyVan.Where(g => g.Loai == loai);
        if (trangThai.HasValue) truyVan = truyVan.Where(g => g.TrangThai == trangThai);
        if (maNhanVien.HasValue) truyVan = truyVan.Where(g => g.MaNhanVien == maNhanVien);

        var dsVi = await viXuLy.TinhTatCaViAsync();

        var cho = db.GiaoDichVis.Where(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan);
        ViewBag.DsNhanVien = dsVi.Select(v => v.NhanVien).ToList();
        return View(new QuanTriViVM
        {
            DsVi = dsVi,
            // giao dịch chờ xác nhận lên đầu, sau đó mới nhất trước
            GiaoDich = await DanhSachTrang<GiaoDichVi>.TaoAsync(
                truyVan.OrderBy(g => g.TrangThai == TrangThaiGiaoDich.ChoXacNhan ? 0 : 1).ThenByDescending(g => g.NgayTao).ThenByDescending(g => g.MaGiaoDich), trang, 12),
            Loai = loai, TrangThai = trangThai, MaNhanVien = maNhanVien,
            SoChoXacNhan = await cho.CountAsync(),
            TienNopChoXacNhan = await cho.Where(g => g.Loai == LoaiGiaoDich.NopTienThuHo).SumAsync(g => (decimal?)g.SoTien) ?? 0,
            TienRutChoXacNhan = await cho.Where(g => g.Loai == LoaiGiaoDich.RutTien).SumAsync(g => (decimal?)g.SoTien) ?? 0
        });
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var gd = await db.GiaoDichVis.AsNoTracking().Include(g => g.NhanVien)
                         .Include(g => g.DonNop).ThenInclude(d => d.KhachHang)
                         .FirstOrDefaultAsync(g => g.MaGiaoDich == id);
        return gd is null ? NotFound() : View(gd);
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id, string? quayLai)
    {
        var kq = await viXuLy.XacNhanAsync(id, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(ChiTiet), new { id });
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? lyDo)
    {
        var kq = await viXuLy.TuChoiAsync(id, lyDo, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
