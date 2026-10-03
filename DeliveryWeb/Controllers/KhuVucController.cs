// Họ và tên: [Vu Thi Van Anh]
// Mã sinh viên: [23103100194]
// Nội dung thực hiện: Module 1 - Quản lý Khu vực giao hàng

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class KhuVucController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public KhuVucController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("AccessDenied", "TaiKhoan");

            return View(await _context.KhuVucs.ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Create(KhuVuc khuVuc)
        {
            if (await _context.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc))
            {
                ModelState.AddModelError("TenKhuVuc", "Tên khu vực đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(khuVuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khuVuc);
        }

        [HttpGet]
        public async Task Edit(int? id)
        {
            if (id == null) return NotFound();

            var khuVuc = await _context.KhuVucs.FindAsync(id);
            if (khuVuc == null) return NotFound();

            return View(khuVuc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Edit(int id, KhuVuc khuVuc)
        {
            if (id != khuVuc.MaKhuVuc) return NotFound();

            if (await _context.KhuVucs.AnyAsync(k => k.TenKhuVuc == khuVuc.TenKhuVuc && k.MaKhuVuc != id))
            {
                ModelState.AddModelError("TenKhuVuc", "Tên khu vực bị trùng.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(khuVuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(khuVuc);
        }
    }
}