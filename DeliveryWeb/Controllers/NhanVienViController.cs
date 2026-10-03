// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 4 - Ví của nhân viên giao hàng: số dư tiền ship, tiền thu hộ đang giữ,
//                     nộp tiền thu hộ về công ty, rút tiền ship về ngân hàng, lịch sử giao dịch.

using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * NhanVienViController — URL: /NhanVienVi, /NhanVienVi/NopTien (POST), /NhanVienVi/RutTien (POST)
 * Quyền: Nhân viên giao hàng – chỉ ví của chính mình (MaNhanVien lấy từ Session).
 * Mọi kiểm tra số tiền (đủ số dư, tối thiểu, phải nộp hết tiền thu hộ trước khi rút) nằm trong ViXuLy.
 */
[YeuCauVaiTro(VaiTroNguoiDung.GiaoHang)]
public class NhanVienViController(ViXuLy viXuLy) : Controller
{
    private int MaNhanVienCuaToi => HttpContext.Session.MaNhanVien() ?? -1;

    public async Task<IActionResult> Index()
    {
        var vi = await viXuLy.TinhViAsync(MaNhanVienCuaToi);
        if (vi is null) return RedirectToAction("KhongCoQuyen", "DangNhap");
        return View(vi);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NopTien(NopTienVM vm)
    {
        var kq = ModelState.IsValid
            ? await viXuLy.NopTienAsync(MaNhanVienCuaToi, vm, HttpContext.Session.NguoiThucHien())
            : KetQua<GiaoDichVi>.Loi(LoiDauTien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RutTien(RutTienVM vm)
    {
        var kq = ModelState.IsValid
            ? await viXuLy.RutTienAsync(MaNhanVienCuaToi, vm, HttpContext.Session.NguoiThucHien())
            : KetQua<GiaoDichVi>.Loi(LoiDauTien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private string LoiDauTien() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? "Dữ liệu không hợp lệ";
}
