// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Hoàn tất đơn (đối soát thu hộ) trên trang Quản lý đơn giao hàng – chỉ Quản trị.

using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của QuanTriDonGiaoHangController (partial): HoanTat.</summary>
public partial class QuanTriDonGiaoHangController
{
    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanTat(int id, string? quayLai)
    {
        var kq = await xuLy.HoanTatAsync(id, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        if (!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai)) return LocalRedirect(quayLai);
        return RedirectToAction(nameof(ChiTiet), new { id });
    }
}
