// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cổng khách hàng - Đơn hàng của tôi: tìm kiếm, lọc, sắp xếp, phân trang; tạo đơn (xem trước phí),
//                     sửa/hủy khi còn chờ phân công; theo dõi tiến trình và lịch sử giao nhận.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * KhachHangDonHangController — URL: /KhachHangDonHang/{Index|ChiTiet|TaoMoi|ChinhSua|Huy}
 * Quyền: Khách hàng – chỉ thấy / thao tác đơn của mình: MaKhachHang lấy từ Session, KHÔNG nhận từ URL hay form.
 * Đơn của khách khác → 404 (không để lộ đơn đó có tồn tại hay không).
 */
[YeuCauVaiTro(VaiTroNguoiDung.KhachHang)]
public class KhachHangDonHangController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private const int KichThuocTrang = 10;
    private int MaKhachHangCuaToi => HttpContext.Session.MaKhachHang() ?? -1;

    // ===================== DANH SÁCH =====================

    public async Task<IActionResult> Index(BoLocDonHang boLoc)
    {
        var truyVan = DonHangXuLy.LocDonHang(db.DonGiaoHangs.AsNoTracking()
                        .Include(d => d.LoaiHang).Include(d => d.KhuVuc)
                        .Where(d => d.MaKhachHang == MaKhachHangCuaToi), boLoc);

        var soDon = await truyVan.GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        if (boLoc.TrangThai.HasValue) truyVan = truyVan.Where(d => d.TrangThai == boLoc.TrangThai);

        var ketQua = await DanhSachTrang<DonGiaoHang>.TaoAsync(DonHangXuLy.SapXepDonHang(truyVan, boLoc.SapXep), boLoc.Trang, KichThuocTrang);
        boLoc.Trang = ketQua.Trang;
        ViewBag.DsLoaiHang = new SelectList(await db.LoaiHangs.OrderBy(l => l.TenLoaiHang).ToListAsync(), "MaLoaiHang", "TenLoaiHang", boLoc.MaLoaiHang);
        ViewBag.DsKhuVuc = new SelectList(await db.KhuVucs.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", boLoc.MaKhuVuc);
        return View(new DanhSachDonVM { BoLoc = boLoc, KetQua = ketQua, SoDonTheoTrangThai = soDon, LaKhachHang = true });
    }

    // ===================== CHI TIẾT =====================

    public async Task<IActionResult> ChiTiet(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null || don.MaKhachHang != MaKhachHangCuaToi) return NotFound();

        var dsPhanCong = don.PhanCongs.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong).ToList();
        return View(new ChiTietDonVM
        {
            Don = don,
            PhanCongHienTai = dsPhanCong.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
            DsPhanCong = dsPhanCong,
            DsLichSu = don.LichSus.OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).ToList(),
            LaKhachHang = true,
            CoTheSua = DonHangXuLy.CoTheSua(don.TrangThai, true),
            CoTheHuy = DonHangXuLy.CoTheHuy(don.TrangThai, true)
        });
    }

    // ===================== TẠO / SỬA =====================

    /// <summary>Có thể điền sẵn từ nút "Tạo đơn với thông tin này" của khối ước lượng phí trên trang chủ.</summary>
    public async Task<IActionResult> TaoMoi(int? maKhuVuc, int? maLoaiHang, decimal? khoiLuong, decimal? tienThuHo, NguoiTraPhi? nguoiTraPhi)
    {
        await NapDanhMucFormAsync(null);
        var vm = new DonGiaoHangFormVM();
        if (maKhuVuc > 0) vm.MaKhuVuc = maKhuVuc.Value;
        if (maLoaiHang > 0) vm.MaLoaiHang = maLoaiHang.Value;
        if (khoiLuong is > 0 and <= 100_000) vm.KhoiLuong = khoiLuong.Value;
        if (tienThuHo is > 0 and <= 1_000_000_000) vm.TienThuHo = tienThuHo.Value;
        if (nguoiTraPhi.HasValue) vm.NguoiTraPhi = nguoiTraPhi.Value;
        return View("BieuMau", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(DonGiaoHangFormVM vm)
    {
        if (vm.NgayGiaoDuKien.Date < DateTime.Today)
            ModelState.AddModelError(nameof(vm.NgayGiaoDuKien), "Ngày giao dự kiến không được trước ngày tạo đơn");
        if (!ModelState.IsValid) { await NapDanhMucFormAsync(null); return View("BieuMau", vm); }

        var kq = await xuLy.TaoDonAsync(vm, MaKhachHangCuaToi, HttpContext.Session.NguoiThucHien());
        if (!kq.ThanhCong)
        {
            ModelState.AddModelError("", kq.ThongBao);
            await NapDanhMucFormAsync(null);
            return View("BieuMau", vm);
        }
        TempData["Success"] = $"{kq.ThongBao} – phí vận chuyển {DinhDang.Tien(kq.DuLieu!.PhiVanChuyen)}. Đơn đang chờ phân công.";
        return RedirectToAction(nameof(ChiTiet), new { id = kq.DuLieu.MaDon });
    }

    public async Task<IActionResult> ChinhSua(int id)
    {
        var don = await db.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDon == id && d.MaKhachHang == MaKhachHangCuaToi);
        if (don is null) return NotFound();
        if (!DonHangXuLy.CoTheSua(don.TrangThai, true))
        {
            TempData["Error"] = $"Đơn {don.MaHienThi} đang '{don.TrangThai.TenHienThi()}' nên không thể sửa";
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        await NapDanhMucFormAsync(don);
        ViewBag.Don = don;
        return View("BieuMau", DonGiaoHangFormVM.TuDon(don));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, DonGiaoHangFormVM vm)
    {
        var kq = ModelState.IsValid ? await xuLy.CapNhatDonAsync(id, vm, HttpContext.Session.NguoiThucHien(), MaKhachHangCuaToi) : null;
        if (kq is { ThanhCong: true })
        {
            TempData["Success"] = kq.ThongBao;
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (kq is not null) ModelState.AddModelError("", kq.ThongBao);
        var don = await db.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDon == id && d.MaKhachHang == MaKhachHangCuaToi);
        if (don is null) return NotFound();
        await NapDanhMucFormAsync(don);
        ViewBag.Don = don;
        vm.MaDon = id;
        return View("BieuMau", vm);
    }

    // ===================== HỦY =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo)
    {
        var kq = await xuLy.HuyDonAsync(id, lyDo, HttpContext.Session.NguoiThucHien(), MaKhachHangCuaToi);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Chỉ liệt kê loại hàng / khu vực đang hoạt động; khi sửa đơn cũ thì giữ thêm mục đang chọn.</summary>
    private async Task NapDanhMucFormAsync(DonGiaoHang? donCu)
    {
        ViewBag.DsLoaiHang = await db.LoaiHangs.AsNoTracking()
            .Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && l.MaLoaiHang == donCu.MaLoaiHang))
            .OrderBy(l => l.HeSoPhuThu).ToListAsync();
        ViewBag.DsKhuVuc = await db.KhuVucs.AsNoTracking()
            .Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && k.MaKhuVuc == donCu.MaKhuVuc))
            .OrderBy(k => k.PhiCoBan).ToListAsync();
        // Thẻ "Lấy hàng tại" trên form: thông tin người gửi (chính khách hàng đang đăng nhập)
        ViewBag.KhachHang = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKhachHang == MaKhachHangCuaToi);
    }
}
