// Họ và tên: [Vu Thi Van Anh]
// Mã sinh viên: [23103100194]
// Nội dung thực hiện: Module 1 - Quản lý Loại hàng

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class LoaiHangController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public LoaiHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("AccessDenied", "TaiKhoan");

            return View(await _context.LoaiHangs.ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("AccessDenied", "TaiKhoan");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Create(LoaiHang loaiHang)
        {
            if (await _context.LoaiHangs.AnyAsync(l => l.TenLoaiHang == loaiHang.TenLoaiHang))
            {
                ModelState.AddModelError("TenLoaiHang", "Tên loại hàng đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(loaiHang);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiHang);
        }

        [HttpGet]
        public async Task Edit(int? id)
        {
            if (id == null) return NotFound();

            var loaiHang = await _context.LoaiHangs.FindAsync(id);
            if (loaiHang == null) return NotFound();

            return View(loaiHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Edit(int id, LoaiHang loaiHang)
        {
            if (id != loaiHang.MaLoaiHang) return NotFound();

            if (await _context.LoaiHangs.AnyAsync(l => l.TenLoaiHang == loaiHang.TenLoaiHang && l.MaLoaiHang != id))
            {
                ModelState.AddModelError("TenLoaiHang", "Tên loại hàng bị trùng.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(loaiHang);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loaiHang);
        }
    }
}