using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * [YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)] — cổng bảo vệ phía server
 * ---------------------------------------------------------------------------------------------
 * Chạy TRƯỚC action:
 *   1. Chưa đăng nhập (Session trống)      → chuyển về /DangNhap?returnUrl=...
 *   2. Tài khoản đã bị khóa trong lúc đang dùng → xóa Session, về trang đăng nhập
 *   3. Vai trò trong CSDL khác Session (bị đổi quyền khi đang đăng nhập) → xóa Session, đăng nhập lại
 *   4. Mật khẩu do Quản trị đặt (PhaiDoiMatKhau) → chỉ vào được /DangNhap/DoiMatKhau
 *   5. Sai vai trò (VD khách hàng gõ URL quản trị) → /DangNhap/KhongCoQuyen
 * Không truyền vai trò nào = chỉ cần đăng nhập. Việc ẩn menu/nút trên View chỉ là phụ.
 */
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class YeuCauVaiTroAttribute(params string[] dsVaiTro) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var phien = http.Session;
        var duongDan = http.Request.Path + http.Request.QueryString;

        if (!phien.DaDangNhap())
        {
            context.Result = new RedirectToActionResult("Index", "DangNhap", new { returnUrl = duongDan.ToString() });
            return;
        }

        var db = http.RequestServices.GetRequiredService<QuanLyGiaoNhanDbContext>();
        int maTk = phien.MaTaiKhoan()!.Value;
        var tk = db.TaiKhoans.AsNoTracking().Where(t => t.MaTaiKhoan == maTk)
                             .Select(t => new { t.TrangThai, t.VaiTro, t.PhaiDoiMatKhau, t.MaBuuCuc }).FirstOrDefault();
        if (tk is null || tk.TrangThai != TrangThaiTaiKhoan.HoatDong)
        {
            phien.Clear();
            if (context.Controller is Controller c) c.TempData["Error"] = "Tài khoản đã bị khóa hoặc không còn tồn tại.";
            context.Result = new RedirectToActionResult("Index", "DangNhap", null);
            return;
        }
        // Chuẩn hóa vai trò cũ / mới để tránh đá nhầm session
        var vaiTroDb = tk.VaiTro is "DieuPhoi" or "BuuCuc" ? VaiTroNguoiDung.NhanVien : tk.VaiTro;
        var vaiTroPhien = phien.VaiTro() is "DieuPhoi" or "BuuCuc" ? VaiTroNguoiDung.NhanVien : phien.VaiTro();

        // Vai trò bị đổi trong lúc đang đăng nhập → Session cũ không còn hiệu lực
        if (vaiTroDb != vaiTroPhien)
        {
            phien.Clear();
            if (context.Controller is Controller c) c.TempData["Error"] = "Quyền của tài khoản đã thay đổi. Vui lòng đăng nhập lại.";
            context.Result = new RedirectToActionResult("Index", "DangNhap", new { returnUrl = duongDan.ToString() });
            return;
        }

        // Tự động đồng bộ bưu cục mới nhất vào Session nếu có thay đổi
        if (vaiTroDb == VaiTroNguoiDung.NhanVien && tk.MaBuuCuc != phien.MaBuuCuc())
        {
            if (tk.MaBuuCuc.HasValue) phien.SetInt32("TK.MaBuuCuc", tk.MaBuuCuc.Value);
            else phien.Remove("TK.MaBuuCuc");
        }

        // Mật khẩu do Quản trị đặt / đặt lại: chỉ được vào trang Đổi mật khẩu cho tới khi tự đổi
        if (tk.PhaiDoiMatKhau && !(context.RouteData.Values["controller"] as string == "DangNhap"
                                   && context.RouteData.Values["action"] as string == "DoiMatKhau"))
        {
            if (context.Controller is Controller c) c.TempData["Error"] = "Mật khẩu của bạn do quản trị viên đặt – vui lòng đổi mật khẩu mới để tiếp tục.";
            context.Result = new RedirectToActionResult("DoiMatKhau", "DangNhap", null);
            return;
        }

        string? vaiTro = vaiTroPhien;
        if (dsVaiTro.Length > 0)
        {
            var dsHopLe = dsVaiTro.Select(v => v is "DieuPhoi" or "BuuCuc" ? VaiTroNguoiDung.NhanVien : v);
            if (!dsHopLe.Contains(vaiTro))
            {
                context.Result = new RedirectToActionResult("KhongCoQuyen", "DangNhap", new { returnUrl = duongDan.ToString() });
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
