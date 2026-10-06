using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_TI17A4HN.Models;

namespace QuanLyGiaoNhan_UNETI5_TI17A4HN.Controllers;

[SessionAuthorize(Roles = new[] { "Admin", "Nhân viên điều phối", "Nhân viên giao hàng" })]
public class PhanCongGiaoHangController(ApplicationDbContext context) : AppControllerBase
{
    private static readonly string[] ActiveStatuses = ["Đã phân công", "Đã nhận hàng", "Đang giao"];

    public async Task<IActionResult> Index(string? search, string? status, int? maNhanVien, int? maKhuVuc)
    {
        IQueryable<PhanCongGiaoHang> query = context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .AsNoTracking();

        if (!IsCoordinator)
            query = query.Where(x => x.NhanVienGiaoHang!.MaTaiKhoan == CurrentAccountId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchText = search.Trim();
            if (int.TryParse(searchText, out var maDon))
                query = query.Where(x => x.MaDon == maDon || x.DonGiaoHang!.TenNguoiNhan.Contains(searchText) || x.DonGiaoHang.SoDienThoaiNguoiNhan.Contains(searchText) || x.DonGiaoHang.DiaChiNhan.Contains(searchText) || x.NhanVienGiaoHang!.HoTen.Contains(searchText) || x.PhuongTien!.BienSo.Contains(searchText));
            else
                query = query.Where(x => x.DonGiaoHang!.TenNguoiNhan.Contains(searchText) || x.DonGiaoHang.SoDienThoaiNguoiNhan.Contains(searchText) || x.DonGiaoHang.DiaChiNhan.Contains(searchText) || x.NhanVienGiaoHang!.HoTen.Contains(searchText) || x.PhuongTien!.BienSo.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.TrangThai == status);

        if (maNhanVien.HasValue)
            query = query.Where(x => x.MaNhanVien == maNhanVien.Value);

        if (maKhuVuc.HasValue)
            query = query.Where(x => x.DonGiaoHang!.MaKhuVuc == maKhuVuc.Value);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.MaNhanVien = maNhanVien;
        ViewBag.MaKhuVuc = maKhuVuc;
        if (IsCoordinator)
        {
            ViewBag.NhanVienGiaoHangs = await context.NhanVienGiaoHangs
                .AsNoTracking().OrderBy(x => x.HoTen).ToListAsync();
            ViewBag.KhuVucs = await context.KhuVucs
                .AsNoTracking().OrderBy(x => x.TenKhuVuc).ToListAsync();
        }
        return View(await query.OrderByDescending(x => x.NgayPhanCong).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var item = await context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaPhanCong == id);
        if (item is null) return NotFound();
        if (!IsCoordinator && item.NhanVienGiaoHang?.MaTaiKhoan != CurrentAccountId) return Forbid();
        return View(item);
    }

    public async Task<IActionResult> History(int? maDon)
    {
        if (maDon is null) return NotFound();
        var order = await context.DonGiaoHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaDon == maDon);
        if (order is null) return NotFound();

        var assignments = await context.PhanCongGiaoHangs
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .Where(x => x.MaDon == maDon)
            .OrderByDescending(x => x.NgayPhanCong)
            .AsNoTracking()
            .ToListAsync();

        if (!IsCoordinator && !assignments.Any(x => x.NhanVienGiaoHang!.MaTaiKhoan == CurrentAccountId))
            return Forbid();

        return View(new PhanCongLichSuViewModel { DonGiaoHang = order, PhanCongs = assignments });
    }

    public async Task<IActionResult> Create()
    {
        if (!IsCoordinator) return Forbid();
        await LoadSelections();
        return View(new PhanCongGiaoHang());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhanCongGiaoHang model)
    {
        if (!IsCoordinator) return Forbid();

        var order = await context.DonGiaoHangs.FirstOrDefaultAsync(x => x.MaDon == model.MaDon);
        if (order is null)
            ModelState.AddModelError(nameof(model.MaDon), "Đơn giao không tồn tại.");
        else if (order.TrangThai != "Chờ phân công")
            ModelState.AddModelError(nameof(model.MaDon), "Chỉ đơn đang chờ phân công mới được tiếp nhận.");

        await ValidateResources(model.MaDon, model.MaNhanVien, model.MaPhuongTien);
        if (!ModelState.IsValid)
        {
            await LoadSelections(model.MaNhanVien, model.MaPhuongTien);
            return View(model);
        }

        var employee = await context.NhanVienGiaoHangs.FindAsync(model.MaNhanVien);
        var vehicle = await context.PhuongTiens.FindAsync(model.MaPhuongTien);
        var oldStatus = order!.TrangThai;
        var assignment = new PhanCongGiaoHang
        {
            MaDon = model.MaDon,
            MaNhanVien = model.MaNhanVien,
            MaPhuongTien = model.MaPhuongTien,
            NgayPhanCong = DateTime.Now,
            TrangThai = "Đã phân công",
            GhiChu = LimitNote(model.GhiChu)
        };
        order.TrangThai = assignment.TrangThai;
        employee!.TrangThai = "Đang giao";
        vehicle!.TrangThai = "Đang giao";
        context.PhanCongGiaoHangs.Add(assignment);
        AddOrderHistory(order, oldStatus, assignment.TrangThai, "Đơn đã được phân công cho nhân viên và phương tiện.");
        await context.SaveChangesAsync();

        TempData["Success"] = $"Đã phân công đơn #{order.MaDon}.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int? id) => id is null
        ? NotFound()
        : RedirectToAction(nameof(Reassign), new { id });

    public async Task<IActionResult> Reassign(int? id)
    {
        if (!IsCoordinator) return Forbid();
        if (id is null) return NotFound();

        var item = await context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .FirstOrDefaultAsync(x => x.MaPhanCong == id);
        if (item is null) return NotFound();
        if (!CanReassign(item.TrangThai))
        {
            TempData["Error"] = "Phân công đã kết thúc, không thể đổi người hoặc phương tiện.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await LoadSelections(item.MaNhanVien, item.MaPhuongTien);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(int id, PhanCongGiaoHang model)
    {
        if (!IsCoordinator) return Forbid();
        if (id != model.MaPhanCong) return NotFound();

        var current = await context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .FirstOrDefaultAsync(x => x.MaPhanCong == id);
        if (current is null) return NotFound();
        if (!CanReassign(current.TrangThai))
        {
            TempData["Error"] = "Phân công đã kết thúc, không thể đổi người hoặc phương tiện.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await ValidateResources(current.MaDon, model.MaNhanVien, model.MaPhuongTien, id);
        if (!ModelState.IsValid)
        {
            model.DonGiaoHang = current.DonGiaoHang;
            await LoadSelections(model.MaNhanVien, model.MaPhuongTien);
            return View(model);
        }

        var newEmployee = await context.NhanVienGiaoHangs.FindAsync(model.MaNhanVien);
        var newVehicle = await context.PhuongTiens.FindAsync(model.MaPhuongTien);
        var oldOrderStatus = current.DonGiaoHang!.TrangThai;
        var now = DateTime.Now;
        var isRetry = current.TrangThai == "Giao thất bại";
        if (isRetry)
        {
            current.GhiChu = LimitNote(AppendNote(current.GhiChu, $"Đã tạo lần giao lại lúc {now:dd/MM/yyyy HH:mm}."));
        }
        else
        {
            current.TrangThai = "Đã thay đổi";
            current.NgayKetThuc = now;
            current.GhiChu = LimitNote(AppendNote(current.GhiChu, $"Đã thay đổi sang phân công mới lúc {now:dd/MM/yyyy HH:mm}."));
        }
        current.NhanVienGiaoHang!.TrangThai = "Sẵn sàng";
        current.PhuongTien!.TrangThai = "Sẵn sàng";

        var replacement = new PhanCongGiaoHang
        {
            MaDon = current.MaDon,
            MaNhanVien = model.MaNhanVien,
            MaPhuongTien = model.MaPhuongTien,
            NgayPhanCong = now,
            TrangThai = "Đã phân công",
            GhiChu = LimitNote(AppendNote(model.GhiChu, $"Thay thế cho phân công #{current.MaPhanCong}."))
        };
        current.DonGiaoHang.TrangThai = replacement.TrangThai;
        newEmployee!.TrangThai = "Đang giao";
        newVehicle!.TrangThai = "Đang giao";
        context.PhanCongGiaoHangs.Add(replacement);
        AddOrderHistory(current.DonGiaoHang, oldOrderStatus, replacement.TrangThai, $"Đổi nhân viên/phương tiện từ phân công #{current.MaPhanCong}; lịch sử cũ được giữ nguyên.");
        await context.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo phân công mới cho đơn #{current.MaDon}; phân công cũ được giữ lại trong lịch sử.";
        return RedirectToAction(nameof(History), new { maDon = current.MaDon });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateDeliveryStatusViewModel model)
    {
        if (!HttpContext.Session.HasRole("Nhân viên giao hàng")) return Forbid();
        var assignment = await context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .FirstOrDefaultAsync(x => x.MaPhanCong == model.MaPhanCong);
        if (assignment is null) return NotFound();
        if (assignment.NhanVienGiaoHang?.MaTaiKhoan != CurrentAccountId) return Forbid();

        var nextStatuses = assignment.TrangThai switch
        {
            "Đã phân công" => new[] { "Đã nhận hàng" },
            "Đã nhận hàng" => new[] { "Đang giao" },
            "Đang giao" => new[] { "Hoàn tất", "Giao thất bại" },
            _ => Array.Empty<string>()
        };
        if (!nextStatuses.Contains(model.TrangThai, StringComparer.Ordinal))
        {
            TempData["Error"] = "Trạng thái giao hàng không hợp lệ hoặc đã được cập nhật.";
            return RedirectToAction(nameof(Details), new { id = model.MaPhanCong });
        }

        var oldAssignmentStatus = assignment.TrangThai;
        assignment.TrangThai = model.TrangThai;
        assignment.GhiChu = LimitNote(model.GhiChu);
        if (model.TrangThai == "Đã nhận hàng") assignment.NgayNhanHang ??= DateTime.Now;
        if (model.TrangThai == "Đang giao") assignment.NgayBatDauGiao ??= DateTime.Now;
        if (model.TrangThai is "Hoàn tất" or "Giao thất bại")
        {
            assignment.NgayKetThuc = DateTime.Now;
            assignment.NhanVienGiaoHang!.TrangThai = "Sẵn sàng";
            assignment.PhuongTien!.TrangThai = "Sẵn sàng";
        }

        if (assignment.DonGiaoHang is not null)
        {
            var oldOrderStatus = assignment.DonGiaoHang.TrangThai;
            assignment.DonGiaoHang.TrangThai = model.TrangThai;
            AddOrderHistory(assignment.DonGiaoHang, oldOrderStatus, model.TrangThai, model.GhiChu ?? $"Cập nhật phân công từ {oldAssignmentStatus} sang {model.TrangThai}.");
        }
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = model.MaPhanCong });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsCoordinator) return Forbid();
        var assignment = await context.PhanCongGiaoHangs
            .Include(x => x.DonGiaoHang)
            .Include(x => x.NhanVienGiaoHang)
            .Include(x => x.PhuongTien)
            .FirstOrDefaultAsync(x => x.MaPhanCong == id);
        if (assignment is null) return NotFound();
        if (IsTerminal(assignment.TrangThai))
        {
            TempData["Error"] = "Phân công này đã kết thúc.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var oldOrderStatus = assignment.DonGiaoHang?.TrangThai;
        assignment.TrangThai = "Đã hủy";
        assignment.NgayKetThuc = DateTime.Now;
        assignment.GhiChu = LimitNote(AppendNote(assignment.GhiChu, "Phân công đã được hủy bởi điều phối viên."));
        assignment.NhanVienGiaoHang!.TrangThai = "Sẵn sàng";
        assignment.PhuongTien!.TrangThai = "Sẵn sàng";
        if (assignment.DonGiaoHang is not null && oldOrderStatus is not ("Hoàn tất" or "Giao thất bại"))
        {
            assignment.DonGiaoHang.TrangThai = "Chờ phân công";
            AddOrderHistory(assignment.DonGiaoHang, oldOrderStatus!, "Chờ phân công", "Phân công bị hủy; đơn được đưa về hàng chờ phân công.");
        }
        await context.SaveChangesAsync();
        TempData["Success"] = $"Đã hủy phân công #{id}; dữ liệu vẫn được giữ trong lịch sử.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int? id) => id is null
        ? NotFound()
        : RedirectToAction(nameof(Details), new { id });

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        TempData["Error"] = "Không xóa phân công để bảo toàn lịch sử. Hãy dùng chức năng Hủy phân công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateResources(int maDon, int maNhanVien, int maPhuongTien, int? excludedAssignmentId = null)
    {
        var employee = await context.NhanVienGiaoHangs.AsNoTracking().FirstOrDefaultAsync(x => x.MaNhanVien == maNhanVien);
        var currentEmployeeId = await CurrentAssignmentEmployeeId(excludedAssignmentId);
        if (employee is null)
            ModelState.AddModelError(nameof(maNhanVien), "Nhân viên giao hàng không tồn tại.");
        else if (employee.TrangThai != "Sẵn sàng" && employee.MaNhanVien != currentEmployeeId)
            ModelState.AddModelError(nameof(maNhanVien), "Nhân viên không ở trạng thái sẵn sàng.");

        var vehicle = await context.PhuongTiens.AsNoTracking().FirstOrDefaultAsync(x => x.MaPhuongTien == maPhuongTien);
        var currentVehicleId = await CurrentAssignmentVehicleId(excludedAssignmentId);
        if (vehicle is null)
            ModelState.AddModelError(nameof(maPhuongTien), "Phương tiện không tồn tại.");
        else if (vehicle.TrangThai != "Sẵn sàng" && vehicle.MaPhuongTien != currentVehicleId)
            ModelState.AddModelError(nameof(maPhuongTien), "Phương tiện không ở trạng thái sẵn sàng.");

        var activeEmployeeQuery = context.PhanCongGiaoHangs.Where(x => x.MaNhanVien == maNhanVien && ActiveStatuses.Contains(x.TrangThai));
        var activeVehicleQuery = context.PhanCongGiaoHangs.Where(x => x.MaPhuongTien == maPhuongTien && ActiveStatuses.Contains(x.TrangThai));
        var activeOrderQuery = context.PhanCongGiaoHangs.Where(x => x.MaDon == maDon && ActiveStatuses.Contains(x.TrangThai));
        if (excludedAssignmentId.HasValue)
        {
            activeEmployeeQuery = activeEmployeeQuery.Where(x => x.MaPhanCong != excludedAssignmentId.Value);
            activeVehicleQuery = activeVehicleQuery.Where(x => x.MaPhanCong != excludedAssignmentId.Value);
            activeOrderQuery = activeOrderQuery.Where(x => x.MaPhanCong != excludedAssignmentId.Value);
        }
        if (await activeEmployeeQuery.AnyAsync()) ModelState.AddModelError(nameof(maNhanVien), "Nhân viên đang có một phân công chưa kết thúc.");
        if (await activeVehicleQuery.AnyAsync()) ModelState.AddModelError(nameof(maPhuongTien), "Phương tiện đang có một phân công chưa kết thúc.");
        if (await activeOrderQuery.AnyAsync()) ModelState.AddModelError(nameof(maDon), "Đơn đã có một phân công đang hoạt động.");
    }

    private async Task<int?> CurrentAssignmentEmployeeId(int? id) => id is null
        ? null
        : await context.PhanCongGiaoHangs.Where(x => x.MaPhanCong == id).Select(x => (int?)x.MaNhanVien).FirstOrDefaultAsync();

    private async Task<int?> CurrentAssignmentVehicleId(int? id) => id is null
        ? null
        : await context.PhanCongGiaoHangs.Where(x => x.MaPhanCong == id).Select(x => (int?)x.MaPhuongTien).FirstOrDefaultAsync();

    private async Task LoadSelections(int? selectedEmployeeId = null, int? selectedVehicleId = null)
    {
        ViewBag.DonGiaoHangs = await context.DonGiaoHangs
            .Where(x => x.TrangThai == "Chờ phân công")
            .OrderBy(x => x.NgayGiaoDuKien)
            .AsNoTracking()
            .ToListAsync();
        ViewBag.NhanVienGiaoHangs = await context.NhanVienGiaoHangs
            .Where(x => x.TrangThai == "Sẵn sàng" || x.MaNhanVien == selectedEmployeeId)
            .OrderBy(x => x.HoTen)
            .AsNoTracking()
            .ToListAsync();
        ViewBag.PhuongTiens = await context.PhuongTiens
            .Where(x => x.TrangThai == "Sẵn sàng" || x.MaPhuongTien == selectedVehicleId)
            .OrderBy(x => x.BienSo)
            .AsNoTracking()
            .ToListAsync();
    }

    private void AddOrderHistory(DonGiaoHang order, string oldStatus, string newStatus, string note)
    {
        context.LichSuGiaoNhans.Add(new LichSuGiaoNhan
        {
            MaDon = order.MaDon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = newStatus,
            NoiDung = LimitNote(note) ?? "Cập nhật phân công.",
            NguoiThucHien = HttpContext.Session.GetString(SessionHelper.FullName) ?? "Điều phối viên"
        });
    }

    private static bool IsTerminal(string status) => status is "Hoàn tất" or "Giao thất bại" or "Đã hủy" or "Đã thay đổi";
    private static bool CanReassign(string status) => status is "Đã phân công" or "Đã nhận hàng" or "Đang giao" or "Giao thất bại";
    private static string? AppendNote(string? current, string addition) => string.IsNullOrWhiteSpace(current) ? addition : $"{current} {addition}";
    private static string? LimitNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Length <= 500 ? note : note[..500];
}
