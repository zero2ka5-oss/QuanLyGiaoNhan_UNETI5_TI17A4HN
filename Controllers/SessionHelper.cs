using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

public static class SessionHelper
{
    public const string AccountId = "MaTaiKhoan";
    public const string FullName = "HoTen";
    public const string Role = "VaiTro";

    public static bool IsLoggedIn(this ISession session) => session.GetInt32(AccountId).HasValue;

    public static bool HasRole(this ISession session, params string[] roles) =>
        session.GetString(Role) is string role && roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : ActionFilterAttribute
{
    public string[] Roles { get; set; } = [];

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (!session.IsLoggedIn())
        {
            context.Result = new RedirectToActionResult("Login", "TaiKhoan", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        if (Roles.Length > 0 && !session.HasRole(Roles))
            context.Result = new ForbidResult();
    }
}
