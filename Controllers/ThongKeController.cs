using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối" })]
public class ThongKeController(ApplicationDbContext context) : AppControllerBase
{
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TongDonHang = await context.DonGiaoHangs.CountAsync(),
            DonChoPhanCong = await context.DonGiaoHangs.CountAsync(x => x.TrangThai == "Chờ phân công"),
            DonDangGiao = await context.DonGiaoHangs.CountAsync(x => x.TrangThai == "Đang giao"),
            DonHoanTat = await context.DonGiaoHangs.CountAsync(x => x.TrangThai == "Hoàn tất"),
            TongPhiVanChuyen = await context.DonGiaoHangs.SumAsync(x => (decimal?)x.PhiVanChuyen) ?? 0
        };
        return View(model);
    }
}
