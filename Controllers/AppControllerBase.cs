using Microsoft.AspNetCore.Mvc;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

public abstract class AppControllerBase : Controller
{
    protected int? CurrentAccountId => HttpContext.Session.GetInt32(SessionHelper.AccountId);
    protected bool IsCoordinator => HttpContext.Session.HasRole("Admin", "Nhân viên điều phối");
}
