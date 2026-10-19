// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Tài khoản, đăng nhập/đăng xuất, phân quyền, quản lý loại hàng và khu vực giao.
using Microsoft.AspNetCore.Mvc;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;
public abstract class AppControllerBase : Controller
{
    protected int? CurrentAccountId => HttpContext.Session.GetInt32(SessionHelper.AccountId);
    protected bool IsCoordinator => HttpContext.Session.HasRole("Admin", "Nhân viên điều phối");
}
