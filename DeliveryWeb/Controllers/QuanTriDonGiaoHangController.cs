// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 2 + 5 - Quản lý đơn giao hàng: tìm kiếm, lọc, sắp xếp, phân trang; tạo đơn hộ khách,
//                     sửa/hủy đơn; xem chi tiết + lịch sử; xác nhận hoàn tất.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriDonGiaoHangController — URL: /QuanTriDonGiaoHang/{Index|ChiTiet|TaoMoi|ChinhSua|Huy|HoanTat}
 * Quyền: Quản trị, Điều phối – toàn bộ đơn, tìm thêm theo tên khách hàng, xác nhận hoàn tất.
 * Đối soát & hoàn tất đơn: chỉ Quản trị (kế toán).
 *
 * Danh sách (mục 6.6):  Truy vấn → Tìm kiếm → Lọc → Sắp xếp → Phân trang → Hiển thị
 *   Toàn bộ Where / OrderBy ghép trên IQueryable (DonHangXuLy.LocDonHang / SapXepDonHang),
 *   chỉ đến DanhSachTrang.TaoAsync mới Count + Skip/Take chạy trên SQL.
 *   Các điều kiện nằm trên query string nên khi chuyển trang vẫn được giữ nguyên.
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri, VaiTroNguoiDung.DieuPhoi)]
public class QuanTriDonGiaoHangController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private const int KichThuocTrang = 10;

    // ===================== DANH SÁCH: TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG =====================

    public async Task<IActionResult> Index(BoLocDonHang boLoc)
    {
        // 1. Truy vấn gốc  2 + 3. Tìm kiếm, lọc
        var truyVan = DonHangXuLy.LocDonHang(db.DonGiaoHangs.AsNoTracking()
                        .Include(d => d.KhachHang).Include(d => d.LoaiHang).Include(d => d.KhuVuc), boLoc);

        // Số đếm theo trạng thái cho các tab (tính trên bộ lọc hiện tại, trước khi lọc trạng thái)
        var soDon = await truyVan.GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() })
                                 .ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        if (boLoc.TrangThai.HasValue) truyVan = truyVan.Where(d => d.TrangThai == boLoc.TrangThai);

        // 4. Sắp xếp  5. Phân trang (Count + Skip/Take trên SQL Server)
        var ketQua = await DanhSachTrang<DonGiaoHang>.TaoAsync(DonHangXuLy.SapXepDonHang(truyVan, boLoc.SapXep), boLoc.Trang, KichThuocTrang);
        boLoc.Trang = ketQua.Trang;
        ViewBag.DsLoaiHang = new SelectList(await db.LoaiHangs.OrderBy(l => l.TenLoaiHang).ToListAsync(), "MaLoaiHang", "TenLoaiHang", boLoc.MaLoaiHang);
        ViewBag.DsKhuVuc = new SelectList(await db.KhuVucs.OrderBy(k => k.PhiCoBan).ToListAsync(), "MaKhuVuc", "TenKhuVuc", boLoc.MaKhuVuc);
        return View(new DanhSachDonVM { BoLoc = boLoc, KetQua = ketQua, SoDonTheoTrangThai = soDon, LaKhachHang = false });
    }

    // ===================== CHI TIẾT =====================

    public async Task<IActionResult> ChiTiet(int id)
    {
        var don = await xuLy.LayChiTietDonAsync(id);
        if (don is null) return NotFound();

        var dsPhanCong = don.PhanCongs.OrderByDescending(p => p.NgayPhanCong).ThenByDescending(p => p.MaPhanCong).ToList();
        return View(new ChiTietDonVM
        {
            Don = don,
            PhanCongHienTai = dsPhanCong.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
            DsPhanCong = dsPhanCong,
            DsLichSu = don.LichSus.OrderByDescending(l => l.ThoiGian).ThenByDescending(l => l.MaLichSu).ToList(),
            LaKhachHang = false,
            CoTheSua = DonHangXuLy.CoTheSua(don.TrangThai, false),
            CoTheHuy = DonHangXuLy.CoTheHuy(don.TrangThai, false),
            CoTheHoanTat = don.TrangThai == TrangThaiDon.GiaoThanhCong && HttpContext.Session.LaQuanTri(),
            CoThePhanCong = DonHangXuLy.CoThePhanCong(don.TrangThai) && DonHangXuLy.SoLanThatBai(don) < DonHangXuLy.SoLanGiaoToiDa,
            CoTheDoiPhanCong = DonHangXuLy.CoTheDoiPhanCong(don.TrangThai)
        });
    }

    // ===================== TẠO ĐƠN HỘ KHÁCH / SỬA =====================

    public async Task<IActionResult> TaoMoi(int? maKhachHang)
    {
        await NapDanhMucFormAsync(null);
        return View("BieuMau", new DonGiaoHangFormVM { MaKhachHang = maKhachHang });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(DonGiaoHangFormVM vm)
    {
        if (vm.MaKhachHang is null or <= 0) ModelState.AddModelError(nameof(vm.MaKhachHang), "Chọn khách hàng");
        if (vm.NgayGiaoDuKien.Date < DateTime.Today)
            ModelState.AddModelError(nameof(vm.NgayGiaoDuKien), "Ngày giao dự kiến không được trước ngày tạo đơn");
        if (!ModelState.IsValid) { await NapDanhMucFormAsync(null); return View("BieuMau", vm); }

        var kq = await xuLy.TaoDonAsync(vm, vm.MaKhachHang!.Value, HttpContext.Session.NguoiThucHien());
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
        var don = await db.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDon == id);
        if (don is null) return NotFound();
        if (!DonHangXuLy.CoTheSua(don.TrangThai, false))
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
        var kq = ModelState.IsValid ? await xuLy.CapNhatDonAsync(id, vm, HttpContext.Session.NguoiThucHien(), null) : null;
        if (kq is { ThanhCong: true })
        {
            TempData["Success"] = kq.ThongBao;
            return RedirectToAction(nameof(ChiTiet), new { id });
        }
        if (kq is not null) ModelState.AddModelError("", kq.ThongBao);
        var don = await db.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDon == id);
        if (don is null) return NotFound();
        await NapDanhMucFormAsync(don);
        ViewBag.Don = don;
        vm.MaDon = id;
        return View("BieuMau", vm);
    }

    // ===================== HỦY / HOÀN TẤT =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo)
    {
        var kq = await xuLy.HuyDonAsync(id, lyDo, HttpContext.Session.NguoiThucHien(), null);
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [YeuCauVaiTro(VaiTroNguoiDung.QuanTri), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanTat(int id, string? quayLai)
    {
        var kq = await xuLy.HoanTatAsync(id, HttpContext.Session.NguoiThucHien());
        TempData[kq.ThanhCong ? "Success" : "Error"] = kq.ThongBao;
        if (!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai)) return LocalRedirect(quayLai);
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    // ===================== HÀM DÙNG CHUNG =====================

    /// <summary>Chỉ liệt kê loại hàng / khu vực / khách hàng đang hoạt động; khi sửa đơn cũ thì giữ thêm mục đang chọn.</summary>
    private async Task NapDanhMucFormAsync(DonGiaoHang? donCu)
    {
        ViewBag.DsLoaiHang = await db.LoaiHangs.AsNoTracking()
            .Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && l.MaLoaiHang == donCu.MaLoaiHang))
            .OrderBy(l => l.HeSoPhuThu).ToListAsync();
        ViewBag.DsKhuVuc = await db.KhuVucs.AsNoTracking()
            .Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && k.MaKhuVuc == donCu.MaKhuVuc))
            .OrderBy(k => k.PhiCoBan).ToListAsync();
        ViewBag.DsKhachHang = new SelectList(await db.KhachHangs.AsNoTracking()
            .Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong || (donCu != null && k.MaKhachHang == donCu.MaKhachHang))
            .OrderBy(k => k.HoTen).Select(k => new { k.MaKhachHang, Ten = k.HoTen + " – " + k.SoDienThoai }).ToListAsync(),
            "MaKhachHang", "Ten");
    }
}
