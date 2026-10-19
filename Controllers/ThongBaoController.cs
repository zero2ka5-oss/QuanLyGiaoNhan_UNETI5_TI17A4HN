using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * ==============================================================================================
 * MODULE THÔNG BÁO TẬP TRUNG (UNIFIED NOTIFICATION SYSTEM)
 * Gồm:
 *   1. ThongBaoController          : Controller điều hướng xem, đọc, lọc và đánh dấu thông báo (/ThongBao)
 *   2. ChuongThongBaoViewComponent : ViewComponent render chuông thông báo trên thanh header
 *   3. ThongBaoXuLy                : Dịch vụ nghiệp vụ tạo và gửi thông báo trong ứng dụng
 *   4. ThongBaoKiemTra             : Bộ lọc & cấu hình thông báo lỗi validation tiếng Việt
 * ==============================================================================================
 */

// ===================== 1. CONTROLLER THÔNG BÁO =====================
/*
 * ThongBaoController — URL: /ThongBao (mọi vai trò đã đăng nhập, chỉ thông báo của chính mình)
 *   Index            : danh sách (lọc chưa đọc, phân trang)
 *   Mo/{id}          : đánh dấu đã đọc rồi mở trang liên quan (đơn hàng, ví, công việc...)
 *   DanhDauDaDoc     : POST – đánh dấu tất cả đã đọc
 *   SoChuaDoc        : JSON – số thông báo chưa đọc (chuông tự cập nhật mỗi phút)
 *   BangChuong       : nạp lại danh sách chuông
 */
[YeuCauVaiTro]
public class ThongBaoController(QuanLyGiaoNhanDbContext db) : Controller
{
    private int MaTaiKhoan => HttpContext.Session.MaTaiKhoan() ?? -1;
    private IQueryable<ThongBao> CuaToi => db.ThongBaos.Where(t => t.MaTaiKhoan == MaTaiKhoan);

    public async Task<IActionResult> Index(bool chuaDoc = false, int trang = 1)
    {
        ViewData["Title"] = "Thông báo";
        ViewBag.ChuaDoc = chuaDoc;
        ViewBag.SoChuaDoc = await CuaToi.CountAsync(t => !t.DaDoc);
        ViewBag.Tong = await CuaToi.CountAsync();
        var truyVan = CuaToi.AsNoTracking();
        if (chuaDoc) truyVan = truyVan.Where(t => !t.DaDoc);
        return View(await DanhSachTrang<ThongBao>.TaoAsync(truyVan.OrderByDescending(t => t.NgayTao).ThenByDescending(t => t.MaThongBao), trang, 20));
    }

    public async Task<IActionResult> Mo(int id)
    {
        var tb = await CuaToi.FirstOrDefaultAsync(t => t.MaThongBao == id);
        if (tb is null) return NotFound();
        if (!tb.DaDoc) { tb.DaDoc = true; await db.SaveChangesAsync(); }
        return !string.IsNullOrEmpty(tb.DuongDan) && Url.IsLocalUrl(tb.DuongDan) ? LocalRedirect(tb.DuongDan) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DanhDauDaDoc(string? quayLai)
    {
        int n = await CuaToi.Where(t => !t.DaDoc).ExecuteUpdateAsync(s => s.SetProperty(t => t.DaDoc, true));
        TempData["Success"] = n > 0 ? $"Đã đánh dấu {n} thông báo là đã đọc" : "Không có thông báo chưa đọc";
        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SoChuaDoc() => Json(new { so = await CuaToi.CountAsync(t => !t.DaDoc) });

    /// <summary>HTML chuông (6 thông báo mới nhất) để trang cập nhật danh sách mà không tải lại.</summary>
    public IActionResult BangChuong() => ViewComponent("ChuongThongBao");
}

// ===================== 2. VIEW COMPONENT CHUÔNG THÔNG BÁO =====================
/// <summary>Chuông thông báo trên thanh đầu trang (mọi layout sau đăng nhập): số chưa đọc + 6 thông báo mới nhất.</summary>
public class ChuongThongBaoViewComponent(QuanLyGiaoNhanDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool nen = false)
    {
        int? ma = HttpContext.Session.MaTaiKhoan();
        if (ma is null) return Content("");
        var ds = await db.ThongBaos.AsNoTracking().Where(t => t.MaTaiKhoan == ma)
                                   .OrderByDescending(t => t.NgayTao).ThenByDescending(t => t.MaThongBao).Take(6).ToListAsync();
        int soChuaDoc = await db.ThongBaos.CountAsync(t => t.MaTaiKhoan == ma && !t.DaDoc);
        ViewBag.Nen = nen;   // true: đặt trên nền cam của app shipper
        return View((ds, soChuaDoc));
    }
}

// ===================== 3. XỬ LÝ & GỬI THÔNG BÁO TRONG HỆ THỐNG =====================
/*
 * ThongBaoXuLy — tạo thông báo trong ứng dụng (bảng ThongBao).
 * Chỉ THÊM vào DbContext, không tự SaveChanges: thông báo được lưu cùng giao tác với thao tác nghiệp vụ gây ra nó
 * (DonHangXuLy, ViXuLy, ViKhachHangXuLy) → thao tác thất bại thì cũng không có thông báo.
 * Các hàm Tk... tìm tài khoản nhận (chỉ tài khoản đang hoạt động).
 * C12 – kết quả tra cứu (tài khoản khách / shipper / điều phối, tên bưu cục) được nhớ trong phạm vi một request (đối tượng sống theo
 * DonHangXuLy – scoped): xếp / xuất bến / hủy chuyến 100 đơn chỉ tra mỗi khóa một lần thay vì ~300 truy vấn.
 */
public class ThongBaoXuLy(QuanLyGiaoNhanDbContext db, Func<DateTime> bayGio)
{
    private readonly Dictionary<string, object?> _nho = [];

    private T Nho<T>(string khoa, Func<T> tra)
    {
        if (_nho.TryGetValue(khoa, out var v)) return (T)v!;
        var kq = tra();
        _nho[khoa] = kq;
        return kq;
    }

    public void Gui(int? maTaiKhoan, LoaiThongBao loai, string tieuDe, string noiDung, string? duongDan, int? maDon = null)
    {
        if (maTaiKhoan is null) return;
        db.ThongBaos.Add(new ThongBao
        {
            MaTaiKhoan = maTaiKhoan.Value, Loai = loai, TieuDe = DinhDang.Cat(tieuDe, 150) ?? "", NoiDung = DinhDang.Cat(noiDung, 500) ?? "",
            DuongDan = duongDan, MaDon = maDon, NgayTao = bayGio()
        });
    }

    public void GuiNhieu(IEnumerable<int> dsTaiKhoan, LoaiThongBao loai, string tieuDe, string noiDung, string? duongDan, int? maDon = null)
    {
        foreach (int ma in dsTaiKhoan.Distinct()) Gui(ma, loai, tieuDe, noiDung, duongDan, maDon);
    }

    public int? TkKhachHang(int maKhachHang) => Nho($"kh{maKhachHang}", () =>
        db.KhachHangs.Where(k => k.MaKhachHang == maKhachHang && k.TaiKhoan!.TrangThai == TrangThaiTaiKhoan.HoatDong)
                     .Select(k => k.MaTaiKhoan).FirstOrDefault());

    public int? TkShipper(int maNhanVien) => Nho($"nv{maNhanVien}", () =>
        db.NhanVienGiaoHangs.Where(n => n.MaNhanVien == maNhanVien && n.TaiKhoan!.TrangThai == TrangThaiTaiKhoan.HoatDong)
                            .Select(n => n.MaTaiKhoan).FirstOrDefault());

    /// <summary>Điều phối viên của một bưu cục (mỗi bưu cục đúng 1 điều phối đang hoạt động).</summary>
    public List<int> TkDieuPhoi(int? maBuuCuc) => maBuuCuc is null ? [] : Nho($"dp{maBuuCuc}", () =>
        db.TaiKhoans.Where(t => t.VaiTro == VaiTroNguoiDung.DieuPhoi && t.MaBuuCuc == maBuuCuc && t.TrangThai == TrangThaiTaiKhoan.HoatDong)
                    .Select(t => t.MaTaiKhoan).ToList());

    public List<int> TkQuanTri() => Nho("qt", () =>
        db.TaiKhoans.Where(t => t.VaiTro == VaiTroNguoiDung.QuanTri && t.TrangThai == TrangThaiTaiKhoan.HoatDong).Select(t => t.MaTaiKhoan).ToList());

    public string TenBuuCuc(int? maBuuCuc) => maBuuCuc is null ? "bưu cục" : Nho($"bc{maBuuCuc}", () =>
        db.BuuCucs.Where(b => b.MaBuuCuc == maBuuCuc).Select(b => b.TenBuuCuc).FirstOrDefault() ?? "bưu cục");

    // ===================== Đường dẫn theo vai trò =====================
    public static string LinkKhach(int maDon) => $"/KhachHangDonHang/ChiTiet/{maDon}";
    public static string LinkBuuCuc(int maDon) => $"/BuuCuc/Don/{maDon}";
    public static string LinkQuanTri(int maDon) => $"/QuanTriDonGiaoHang/ChiTiet/{maDon}";
    public static string LinkChuyen(int maChuyen) => $"/ChuyenXe/ChiTiet/{maChuyen}";
    public const string LinkCongViecShipper = "/NhanVienCongViec";
    public const string LinkViShipper = "/NhanVienVi";
    public const string LinkViKhach = "/KhachHangVi";
}

// ===================== 4. BỘ LỌC & THÔNG BÁO KIỂM TRA DỮ LIỆU =====================
/// <summary>
/// Thuộc tính kiểu số không nullable (int, decimal...) được MVC tự gắn [Required] với thông báo tiếng Anh.
/// Provider này thay bằng câu tiếng Việt – áp dụng cả kiểm tra phía server lẫn phía trình duyệt.
/// </summary>
public class ThongBaoKiemTraProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var batBuoc in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
            if (batBuoc.ErrorMessage is null && batBuoc.ErrorMessageResourceName is null)
                batBuoc.ErrorMessage = "Vui lòng nhập {0}";
    }
}

/// <summary>
/// Hai người cùng thao tác một bản ghi (hoặc bấm nút hai lần quá nhanh):
///  - DbUpdateConcurrencyException: đơn đã được người khác cập nhật (DonGiaoHang.PhienBan đổi) → lần lưu sau bị từ chối;
///  - Vi phạm chỉ mục duy nhất (SQL 2601 / 2627): VD tạo phân công thứ hai cho cùng một đơn.
/// Thay vì trang lỗi 500, quay lại trang trước với thông báo dễ hiểu; dữ liệu không bị ghi đè hay nhân đôi.
/// </summary>
public class XungDotDuLieuFilter(ITempDataDictionaryFactory tempDataFactory) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        string? thongBao = context.Exception switch
        {
            DbUpdateConcurrencyException =>
                "Dữ liệu vừa được người khác cập nhật trước bạn – trang đã được tải lại, vui lòng kiểm tra rồi thao tác lại.",
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                "Thao tác bị trùng (có thể do bấm nút hai lần hoặc người khác vừa thực hiện) – trang đã được tải lại.",
            _ => null
        };
        if (thongBao is null) return;

        var http = context.HttpContext;
        tempDataFactory.GetTempData(http)["Error"] = thongBao;
        string? quayLai = http.Request.Headers.Referer.ToString();
        bool cungTrang = Uri.TryCreate(quayLai, UriKind.Absolute, out var uri) && uri.Host == http.Request.Host.Host;
        context.Result = cungTrang ? new RedirectResult(uri!.PathAndQuery) : new RedirectToActionResult("TrangLamViec", "DangNhap", null);
        context.ExceptionHandled = true;
    }
}

public static class ThongBaoKiemTra
{
    public static void CauHinh(MvcOptions o)
    {
        o.ModelMetadataDetailsProviders.Add(new ThongBaoKiemTraProvider());
        o.Filters.Add<XungDotDuLieuFilter>();
        var tb = o.ModelBindingMessageProvider;
        tb.SetValueMustNotBeNullAccessor(truong => $"Vui lòng nhập {truong}");
        tb.SetMissingBindRequiredValueAccessor(truong => $"Thiếu giá trị cho {truong}");
        tb.SetAttemptedValueIsInvalidAccessor((giaTri, truong) => $"Giá trị '{giaTri}' không hợp lệ cho {truong}");
        tb.SetNonPropertyAttemptedValueIsInvalidAccessor(giaTri => $"Giá trị '{giaTri}' không hợp lệ");
        tb.SetUnknownValueIsInvalidAccessor(truong => $"Giá trị của {truong} không hợp lệ");
        tb.SetValueIsInvalidAccessor(giaTri => $"Giá trị '{giaTri}' không hợp lệ");
        tb.SetValueMustBeANumberAccessor(truong => $"{truong} phải là số");
        tb.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là số");
    }
}
