// Họ và tên: Vũ Thị Vân Anh
// Mã sinh viên: 23103100194
// Nội dung thực hiện: Module 1 - Quản trị Danh sách & Phân quyền Tài khoản

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Data;
using QuanLyGiaoNhan_UNETI15_TI17A1HN.Models;

namespace QuanLyGiaoNhan_UNETI15_TI17A1HN.Controllers
{
    public class QuanTriTaiKhoanController : AppControllerBase[cite: 3]
    {
        private readonly ApplicationDbContext _context;

        public QuanTriTaiKhoanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Index()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            if (vaiTro != "Admin") return RedirectToAction("KhongCoQuyen", "DangNhap");

            var list = await _context.TaiKhoans.ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task DoiTrangThai(int id)
        {
            var item = await _context.TaiKhoans.FindAsync(id);
            if (item != null)
            {
                item.TrangThai = !item.TrangThai;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}