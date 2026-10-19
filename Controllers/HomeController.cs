using Microsoft.AspNetCore.Mvc;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;
using System.Diagnostics;
namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
