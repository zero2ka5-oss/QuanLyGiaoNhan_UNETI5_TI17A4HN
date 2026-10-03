// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Bộ lọc phân quyền theo Session gắn trên Controller/Action.

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
 *   3. Sai vai trò (VD khách hàng gõ URL quản trị) → /DangNhap/KhongCoQuyen
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
        var trangThai = db.TaiKhoans.AsNoTracking().Where(t => t.MaTaiKhoan == maTk)
                                    .Select(t => (TrangThaiTaiKhoan?)t.TrangThai).FirstOrDefault();
        if (trangThai != TrangThaiTaiKhoan.HoatDong)
        {
            phien.Clear();
            if (context.Controller is Controller c) c.TempData["Error"] = "Tài khoản đã bị khóa hoặc không còn tồn tại.";
            context.Result = new RedirectToActionResult("Index", "DangNhap", null);
            return;
        }

        if (dsVaiTro.Length > 0 && !dsVaiTro.Contains(phien.VaiTro()))
        {
            context.Result = new RedirectToActionResult("KhongCoQuyen", "DangNhap", new { returnUrl = duongDan.ToString() });
            return;
        }

        base.OnActionExecuting(context);
    }
}
