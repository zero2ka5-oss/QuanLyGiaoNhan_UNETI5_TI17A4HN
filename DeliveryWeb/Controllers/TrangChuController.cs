// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Trang chủ công khai – giới thiệu dịch vụ, ước tính phí, bảng giá, tin tức, câu hỏi thường gặp, tra cứu đơn.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * TrangChuController — trang công khai (không cần đăng nhập)
 * URL: / , /TrangChu/TraCuu?ma=DH000012&sdt=09..., /TrangChu/TinhPhi (AJAX), /TrangChu/LienHe, /TrangChu/TinTuc?chuyenMuc=,
 *      /TrangChu/ChiTietTinTuc/5, /TrangChu/Loi
 * Tra cứu công khai yêu cầu mã đơn + SĐT người nhận khớp nhau để không lộ thông tin đơn của người khác.
 */
public class TrangChuController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.BangGia = await xuLy.BangGiaAsync(HttpContext.Session.LaKhachHang());
        ViewBag.SoKhachHang = await db.KhachHangs.CountAsync();
        ViewBag.SoDonDaGiao = await db.DonGiaoHangs.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat);
        ViewBag.SoNhanVien = await db.NhanVienGiaoHangs.CountAsync(n => n.TrangThai != TrangThaiNhanVien.NgungHoatDong);
        ViewBag.SoPhuongTien = await db.PhuongTiens.CountAsync(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong);
        ViewBag.DsBanner = await db.BannerTrangChus.AsNoTracking().Where(b => b.HienThi)
                                   .OrderBy(b => b.ThuTu).ThenBy(b => b.MaBanner).ToListAsync();
        ViewBag.TinMoi = await db.TinTucs.AsNoTracking().DangHienThi()
                                 .OrderByDescending(t => t.NgayDang).ThenByDescending(t => t.MaTinTuc).Take(3).ToListAsync();
        return View();
    }

    /// <summary>Trang liên hệ: tổng đài, email, địa chỉ, giờ làm việc, các kênh hỗ trợ nhanh.</summary>
    public IActionResult LienHe() => View();

    /// <summary>Danh sách tin: lọc chuyên mục, tìm theo tiêu đề / tóm tắt (không phân biệt dấu).
    /// Không lọc: bài mới nhất tách ra làm bài nổi bật ở đầu trang 1.</summary>
    public async Task<IActionResult> TinTuc(ChuyenMucTinTuc? chuyenMuc, string? tuKhoa, int trang = 1)
    {
        tuKhoa = tuKhoa?.Trim();
        var congKhai = db.TinTucs.AsNoTracking().DangHienThi();
        var truyVan = congKhai.Where(t => chuyenMuc == null || t.ChuyenMuc == chuyenMuc);
        if (!string.IsNullOrEmpty(tuKhoa))
            truyVan = truyVan.Where(t => EF.Functions.Collate(t.TieuDe, "Latin1_General_100_CI_AI").Contains(tuKhoa)
                                      || EF.Functions.Collate(t.TomTat, "Latin1_General_100_CI_AI").Contains(tuKhoa));
        truyVan = truyVan.OrderByDescending(t => t.NgayDang).ThenByDescending(t => t.MaTinTuc);

        // Không lọc: bài mới nhất là bài nổi bật (chỉ hiện ở trang 1) và luôn bỏ khỏi lưới để các trang không lệch nhau
        TinTuc? noiBat = null;
        if (chuyenMuc is null && string.IsNullOrEmpty(tuKhoa))
        {
            noiBat = await truyVan.FirstOrDefaultAsync();
            if (noiBat is not null) truyVan = truyVan.Where(t => t.MaTinTuc != noiBat.MaTinTuc);
        }
        ViewBag.NoiBat = trang <= 1 ? noiBat : null;
        ViewBag.ChuyenMuc = chuyenMuc;
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.SoTheoChuyenMuc = await congKhai.GroupBy(t => t.ChuyenMuc).ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await DanhSachTrang<TinTuc>.TaoAsync(truyVan, trang, 9));
    }

    /// <summary>Bài chưa hiển thị (đã ẩn / hẹn ngày đăng) chỉ Quản trị xem trước được.</summary>
    public async Task<IActionResult> ChiTietTinTuc(int id)
    {
        var tin = await db.TinTucs.AsNoTracking().FirstOrDefaultAsync(t => t.MaTinTuc == id);
        bool congKhai = tin is not null && tin.HienThi && tin.NgayDang <= DateTime.Today;
        if (tin is null || (!congKhai && !HttpContext.Session.LaQuanTri())) return NotFound();
        ViewBag.XemTruoc = !congKhai;
        ViewBag.PhutDoc = Math.Max(1, (int)Math.Ceiling((tin.TomTat.Length + tin.NoiDung.Length) / 5.5 / 200));
        ViewBag.TinKhac = await db.TinTucs.AsNoTracking().DangHienThi().Where(t => t.MaTinTuc != id)
                                  .OrderByDescending(t => t.ChuyenMuc == tin.ChuyenMuc).ThenByDescending(t => t.NgayDang)
                                  .Take(3).ToListAsync();
        return View(tin);
    }

    public async Task<IActionResult> TraCuu(string? ma, string? sdt)
    {
        ViewBag.Ma = ma;
        ViewBag.Sdt = sdt;
        if (string.IsNullOrWhiteSpace(ma) && string.IsNullOrWhiteSpace(sdt)) return View(null);

        var (don, loi) = await xuLy.TraCuuAsync(ma, sdt);
        ViewBag.Loi = loi;
        return View(don);
    }

    /// <summary>
    /// Xem trước phí (AJAX) – dùng ở form tạo / sửa đơn của Khách hàng và Quản trị.
    /// GET /TrangChu/TinhPhi?maLoaiHang=1&amp;maKhuVuc=2&amp;khoiLuong=3.5 → JSON các thành phần phí.
    /// Chỉ để hiển thị; khi lưu đơn, DonHangXuLy luôn tính lại phí trên server.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TinhPhi(int maLoaiHang, int maKhuVuc, decimal khoiLuong)
    {
        var loai = await db.LoaiHangs.FindAsync(maLoaiHang);
        var khuVuc = await db.KhuVucs.FindAsync(maKhuVuc);
        if (loai is null || khuVuc is null || khoiLuong <= 0 || khoiLuong > 100_000)
            return BadRequest(new { thongBao = "Chọn loại hàng, khu vực và nhập khối lượng > 0" });
        decimal toiDa = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                            .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        if (toiDa > 0 && khoiLuong > toiDa)
            return BadRequest(new { thongBao = $"Khối lượng tối đa một đơn là {DinhDang.KhoiLuong(toiDa)}" });
        var phi = TinhPhiXuLy.TinhPhi(khuVuc.PhiCoBan, khoiLuong, loai.HeSoPhuThu);
        return Json(new
        {
            phi.PhiCoBan, phi.KhoiLuongVuot, phi.PhuPhiKhoiLuong, phi.HeSoPhuThu, phi.PhuPhiLoaiHang, phi.Tong,
            nguongKg = TinhPhiXuLy.NguongKhoiLuongKg, donGiaVuot = TinhPhiXuLy.DonGiaVuotMoiKg
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Loi(int? ma)
    {
        ViewBag.Ma = ma ?? 500;
        ViewBag.MaYeuCau = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}
