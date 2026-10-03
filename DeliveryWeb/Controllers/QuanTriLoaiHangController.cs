// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Quản trị Loại Hàng

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class QuanTriLoaiHangController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public QuanTriLoaiHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("KhongCoQuyen", "DangNhap");

            return View(await _context.LoaiHangs.ToListAsync());
        }

        [HttpGet]
        public IActionResult BieuMau(int? id)
        {
            if (id == null) return View(new LoaiHang());

            var item = _context.LoaiHangs.Find(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task BieuMau(LoaiHang model)
        {
            if (ModelState.IsValid)
            {
                if (model.MaLoaiHang == 0)
                    _context.Add(model);
                else
                    _context.Update(model);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}