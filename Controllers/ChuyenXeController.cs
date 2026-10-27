using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * ChuyenXeController — URL: /ChuyenXe
 * Điều hành vận chuyển liên tỉnh theo mô hình Hub & Spoke:
 *   - Bưu cục vệ tinh: gom toàn bộ đơn liên tỉnh về Hub vùng (Miền Bắc: Hub Bắc Ninh, Miền Trung: Hub Đà Nẵng, Miền Nam: Hub TP.HCM).
 *   - Bưu cục Hub: trung tâm phân loại, chia luồng kết nối liên Hub và phân tỏa về các bưu cục phát.
 * Quyền: Nhân viên vận hành bưu cục, Quản trị viên.
 */
[YeuCauVaiTro(VaiTroNguoiDung.NhanVien, VaiTroNguoiDung.QuanTri)]
public class ChuyenXeController(QuanLyGiaoNhanDbContext db, DonHangXuLy xuLy) : Controller
{
    private int? BuuCucCuaToi => HttpContext.Session.MaBuuCuc();

    public async Task<IActionResult> Index(int? maBuuCuc = null, string tab = "dangxep")
    {
        maBuuCuc = BuuCucCuaToi ?? maBuuCuc;
        var bc = maBuuCuc is null
            ? await db.BuuCucs.AsNoTracking().OrderBy(b => b.MaBuuCuc).FirstOrDefaultAsync()
            : await db.BuuCucs.AsNoTracking().FirstOrDefaultAsync(b => b.MaBuuCuc == maBuuCuc);

        if (bc is null) return View("ChuaCoBuuCuc");

        int idBc = bc.MaBuuCuc;
        var dsBuuCuc = await db.BuuCucs.AsNoTracking().OrderBy(b => b.TenBuuCuc).ToListAsync();

        bool laHub = DonHangXuLy.LaHubTrungTam(bc);
        string tenMien = DonHangXuLy.LayMien(bc.TinhThanh);
        var hubVung = DonHangXuLy.TimHubVung(bc, dsBuuCuc);

        // 1. Đơn chờ xếp chuyến tại Bưu cục hiện tại (chưa gán vào chuyến xe nào)
        var donChoQuery = db.DonGiaoHangs
            .AsNoTracking()
            .Include(d => d.BuuCucGui)
            .Include(d => d.BuuCucNhan)
            .Include(d => d.LoaiHang)
            .Include(d => d.KhachHang)
            .Where(d => d.MaChuyenXe == null &&
                        ((d.MaBuuCucHienTai == idBc && d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui && d.MaBuuCucGui != d.MaBuuCucNhan) ||
                         (d.MaBuuCucHienTai == idBc && d.TrangThai == TrangThaiDon.ChoLayLienTinh) ||
                         (d.MaBuuCucHienTai == idBc && d.TrangThai == TrangThaiDon.ChoChuyenHoan)));

        var dsDonCho = await donChoQuery.ToListAsync();

        List<NhomDonTheoDichDenVM> donTheoDichDen;

        if (!laHub)
        {
            // BƯU CỤC VỆ TINH: Gom toàn bộ đơn liên tỉnh vào 1 luồng duy nhất về Hub Vùng (Bắc Ninh nếu miền Bắc)
            int idHubDich = hubVung?.MaBuuCuc ?? idBc;
            donTheoDichDen = dsDonCho.Count == 0 ? [] :
            [
                new NhomDonTheoDichDenVM
                {
                    MaBuuCucNhan = idHubDich,
                    TenBuuCucNhan = $"Hub {hubVung?.TenBuuCuc ?? "Khai thác"} (Trung tâm phân loại Miền {tenMien})",
                    SoLuongDon = dsDonCho.Count,
                    TongKhoiLuong = dsDonCho.Sum(x => x.KhoiLuong),
                    DsDonHang = dsDonCho,
                    LaTuyenVeHub = true,
                    MoTaTuyen = $"Tuyến con thoi gom toàn bộ kiện liên tỉnh về Hub Trung tâm {hubVung?.TenBuuCuc}"
                }
            ];
        }
        else
        {
            // BƯU CỤC HUB TRUNG TÂM (Bắc Ninh, Đà Nẵng, TP.HCM):
            // Phân loại đơn:
            // - Đơn đi miền khác: gom theo Hub đích của miền đó.
            // - Đơn đi cùng miền: gom trực tiếp theo từng Bưu cục phát đích.
            var hubBac = dsBuuCuc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("bắc ninh"));
            var hubTrung = dsBuuCuc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("đà nẵng"));
            var hubNam = dsBuuCuc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("hồ chí minh"));

            donTheoDichDen = dsDonCho
                .GroupBy(d =>
                {
                    int? maDichCuoi = d.TrangThai == TrangThaiDon.ChoChuyenHoan ? d.MaBuuCucGui : d.MaBuuCucNhan;
                    var bcDich = dsBuuCuc.FirstOrDefault(b => b.MaBuuCuc == maDichCuoi);
                    string mienDich = DonHangXuLy.LayMien(bcDich?.TinhThanh);

                    // Nếu đích đến ở miền khác -> gom sang Hub của miền đó
                    if (mienDich != tenMien)
                    {
                        var hubDich = mienDich switch
                        {
                            "Trung" => hubTrung,
                            "Nam" => hubNam,
                            _ => hubBac
                        };
                        return hubDich?.MaBuuCuc ?? maDichCuoi ?? 0;
                    }

                    // Cùng miền -> gom trực tiếp về Bưu cục tỉnh đích
                    return maDichCuoi ?? 0;
                })
                .Where(g => g.Key > 0 && g.Key != idBc)
                .Select(g =>
                {
                    var bcDich = dsBuuCuc.FirstOrDefault(b => b.MaBuuCuc == g.Key);
                    bool laTuyenLienHub = bcDich != null && DonHangXuLy.LaHubTrungTam(bcDich);
                    string tenTuyen = laTuyenLienHub
                        ? $"Hub {bcDich!.TenBuuCuc} (Tuyến đường dài kết nối liên miền)"
                        : (bcDich?.TenBuuCuc ?? $"Bưu cục #{g.Key}");

                    return new NhomDonTheoDichDenVM
                    {
                        MaBuuCucNhan = g.Key,
                        TenBuuCucNhan = tenTuyen,
                        SoLuongDon = g.Count(),
                        TongKhoiLuong = g.Sum(x => x.KhoiLuong),
                        DsDonHang = g.ToList(),
                        LaTuyenVeHub = laTuyenLienHub,
                        MoTaTuyen = laTuyenLienHub ? "Tuyến trục xe container / xe tải lớn liên miền" : $"Tuyến phân tỏa nội miền về {bcDich?.TenBuuCuc}"
                    };
                })
                .OrderByDescending(x => x.LaTuyenVeHub)
                .ThenByDescending(x => x.SoLuongDon)
                .ToList();
        }

        // 2. Danh sách xe liên tỉnh đang đậu tại bưu cục
        var xeDangDau = await db.NhanVienGiaoHangs
            .AsNoTracking()
            .Include(n => n.PhuongTien)
            .Include(n => n.TaiKhoan)
            .Where(n => n.MaBuuCuc == idBc && n.LoaiShipper == LoaiShipper.LienTinh)
            .OrderBy(n => n.HoTen)
            .ToListAsync();

        var dsTaiXeDangChayIndex = await db.ChuyenXes
            .AsNoTracking()
            .Where(c => c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy)
            .Select(c => c.MaNhanVien)
            .Distinct()
            .ToListAsync();

        foreach (var x in xeDangDau)
        {
            if (dsTaiXeDangChayIndex.Contains(x.MaNhanVien) && x.TrangThai == TrangThaiNhanVien.SanSang)
            {
                x.TrangThai = TrangThaiNhanVien.DangGiaoHang;
            }
        }

        // Danh sách xe vệ tinh đang đậu tại Hub (đã đến nơi và sẵn sàng chạy chuyến lượt về)
        List<NhanVienGiaoHang> xeVeTinhTaiHub = [];
        if (laHub)
        {
            var dsTaiXeVeTinhId = await db.ChuyenXes
                .AsNoTracking()
                .Where(c => c.MaBuuCucNhan == idBc && c.TrangThai == TrangThaiChuyenXe.DaDenNoi && c.NhanVien != null && c.NhanVien.MaBuuCuc != idBc)
                .OrderByDescending(c => c.NgayDenNoi)
                .Select(c => c.MaNhanVien)
                .Distinct()
                .ToListAsync();

            if (dsTaiXeVeTinhId.Count > 0)
            {
                xeVeTinhTaiHub = await db.NhanVienGiaoHangs
                    .AsNoTracking()
                    .Include(n => n.PhuongTien)
                    .Include(n => n.BuuCuc)
                    .Where(n => dsTaiXeVeTinhId.Contains(n.MaNhanVien) && !dsTaiXeDangChayIndex.Contains(n.MaNhanVien))
                    .ToListAsync();
            }
        }
        ViewBag.XeVeTinhTaiHub = xeVeTinhTaiHub;

        // 3. Danh sách chuyến xe theo các Tab
        var qChuyen = db.ChuyenXes
            .AsNoTracking()
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs);

        int soDangXep = await qChuyen.CountAsync(c => c.MaBuuCucGui == idBc && c.TrangThai == TrangThaiChuyenXe.DangXepHang);
        int soSapDen = await qChuyen.CountAsync(c =>
            (c.MaBuuCucNhan == idBc && (c.TrangThai == TrangThaiChuyenXe.DangVanChuyen || c.TrangThai == TrangThaiChuyenXe.DangXepHang)) ||
            (c.MaBuuCucTrungGian == idBc && (c.TrangThai == TrangThaiChuyenXe.DangGheTram || (c.TrangThai == TrangThaiChuyenXe.DangVanChuyen && !c.DaGheTram))));
        int soLichSu = await qChuyen.CountAsync(c =>
            (c.MaBuuCucGui == idBc || c.MaBuuCucNhan == idBc || (c.MaBuuCucTrungGian == idBc && c.DaGheTram)) &&
            (c.TrangThai == TrangThaiChuyenXe.DaDenNoi || c.TrangThai == TrangThaiChuyenXe.DaHuy));

        List<ChuyenXe> dsChuyenXe = tab switch
        {
            "sapden" => await qChuyen.Where(c =>
                (c.MaBuuCucNhan == idBc && (c.TrangThai == TrangThaiChuyenXe.DangVanChuyen || c.TrangThai == TrangThaiChuyenXe.DangXepHang)) ||
                (c.MaBuuCucTrungGian == idBc && (c.TrangThai == TrangThaiChuyenXe.DangGheTram || (c.TrangThai == TrangThaiChuyenXe.DangVanChuyen && !c.DaGheTram))))
                .OrderByDescending(c => c.NgayXuatBen ?? c.NgayTao).ToListAsync(),
            "lichsu" => await qChuyen.Where(c =>
                (c.MaBuuCucGui == idBc || c.MaBuuCucNhan == idBc || (c.MaBuuCucTrungGian == idBc && c.DaGheTram)) &&
                (c.TrangThai == TrangThaiChuyenXe.DaDenNoi || c.TrangThai == TrangThaiChuyenXe.DaHuy))
                .OrderByDescending(c => c.NgayDenNoi ?? c.NgayTao).Take(50).ToListAsync(),
            _ => await qChuyen.Where(c => c.MaBuuCucGui == idBc && c.TrangThai == TrangThaiChuyenXe.DangXepHang)
                              .OrderByDescending(c => c.NgayTao).ToListAsync()
        };

        var vm = new ChuyenXeIndexVM
        {
            BuuCucHienTai = bc,
            DsBuuCuc = dsBuuCuc,
            DonChoTheoDichDen = donTheoDichDen,
            XeLienTinhDangDau = xeDangDau,
            DsChuyenXe = dsChuyenXe,
            Tab = tab,
            SoDangXep = soDangXep,
            SoSapDen = soSapDen,
            SoLichSu = soLichSu,
            LaHubTrungTam = laHub,
            TenHubVung = hubVung?.TenBuuCuc ?? "Bắc Ninh",
            HubVung = hubVung,
            TenMien = tenMien
        };

        ViewBag.KhoaBuuCuc = BuuCucCuaToi is not null;
        return View(vm);
    }

    // ===================== LẬP CHUYẾN & XẾP ĐƠN NHANH =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XepNhanh(int maBuuCucGui, int maBuuCucNhan, List<int> maDonHangs)
    {
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && maBuuCucGui != BuuCucCuaToi.Value)
            return Forbid();

        if (maDonHangs == null || maDonHangs.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một kiện hàng để xếp chuyến!";
            return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCucGui });
        }

        // Ưu tiên 1: Nếu đã có chuyến xe cùng tuyến đích đang ở trạng thái 'Đang xếp hàng' (chưa xuất bến),
        // và còn đủ tải trọng chứa các kiện này -> Xếp thêm trực tiếp vào chuyến xe đó!
        var dsChuyenDangXep = await db.ChuyenXes
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
            .Where(c => c.MaBuuCucGui == maBuuCucGui 
                && c.MaBuuCucNhan == maBuuCucNhan 
                && c.TrangThai == TrangThaiChuyenXe.DangXepHang)
            .OrderByDescending(c => c.NgayTao)
            .ToListAsync();

        decimal kgThem = await db.DonGiaoHangs
            .Where(d => maDonHangs.Contains(d.MaDon))
            .SumAsync(d => d.KhoiLuong);

        var chuyenPhuHop = dsChuyenDangXep.FirstOrDefault(c =>
        {
            decimal kgHienTai = c.DonHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy).Sum(d => d.KhoiLuong);
            decimal taiTrong = c.PhuongTien?.TaiTrongToiDa ?? 0;
            return taiTrong <= 0 || (kgHienTai + kgThem) <= taiTrong;
        });

        if (chuyenPhuHop != null)
        {
            var kqThem = await xuLy.ThemDonVaoChuyenXeAsync(chuyenPhuHop.MaChuyen, maDonHangs, HttpContext.Session.ThaoTac());
            if (kqThem.ThanhCong)
                TempData["Success"] = $"Đã xếp thêm {maDonHangs.Count} kiện hàng ({kgThem:N1} kg) vào chuyến xe {chuyenPhuHop.MaHienThi} (đang chờ xuất bến)!";
            else
                TempData["Error"] = kqThem.ThongBao;

            return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCucGui, tab = "dangxep" });
        }

        // Ưu tiên 2: Chưa có chuyến xe đang xếp hàng hoặc các chuyến hiện tại đều đã đầy tải -> Tìm tài xế sẵn sàng để lập chuyến xe mới
        var dsTaiXeDangChay = await db.ChuyenXes
            .AsNoTracking()
            .Where(c => c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy)
            .Select(c => c.MaNhanVien)
            .Distinct()
            .ToListAsync();

        var bcGui = await db.BuuCucs.FindAsync(maBuuCucGui);
        var bcDich = await db.BuuCucs.FindAsync(maBuuCucNhan);
        bool laHubGui = bcGui != null && DonHangXuLy.LaHubTrungTam(bcGui);
        bool laHub = bcDich != null && DonHangXuLy.LaHubTrungTam(bcDich);

        var shipper = await db.NhanVienGiaoHangs
            .Include(n => n.PhuongTien)
            .FirstOrDefaultAsync(n => n.MaBuuCuc == maBuuCucGui 
                && n.LoaiShipper == LoaiShipper.LienTinh 
                && n.TrangThai == TrangThaiNhanVien.SanSang 
                && !dsTaiXeDangChay.Contains(n.MaNhanVien));

        // Nếu xuất phát từ Hub mà xe của Hub đang bận, cho phép điều động xe của bưu cục đích đang sẵn sàng tại Hub (chuyến con thoi lượt về)
        if (shipper is null && laHubGui)
        {
            shipper = await db.NhanVienGiaoHangs
                .Include(n => n.PhuongTien)
                .FirstOrDefaultAsync(n => n.MaBuuCuc == maBuuCucNhan
                    && n.LoaiShipper == LoaiShipper.LienTinh
                    && n.TrangThai == TrangThaiNhanVien.SanSang
                    && !dsTaiXeDangChay.Contains(n.MaNhanVien));
        }

        if (shipper is null)
        {
            string thongBaoLoi = dsChuyenDangXep.Count > 0
                ? $"Chuyến xe tuyến này ({string.Join(", ", dsChuyenDangXep.Select(c => c.MaHienThi))}) đã đầy tải trọng ({kgThem:N1} kg bổ sung sẽ vượt tải), và bưu cục hiện không còn tài xế xe tải liên tỉnh nào khác sẵn sàng để lập thêm chuyến mới!"
                : "Bưu cục hiện tại chưa có tài xế xe tải liên tỉnh sẵn sàng (tài xế đang phụ trách chuyến xe khác hoặc tạm nghỉ)! Vui lòng tiếp nhận/hoàn thành chuyến xe trước hoặc điều động thêm phương tiện.";
            TempData["Error"] = thongBaoLoi;
            return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCucGui });
        }

        // Tự động gán trạm dừng Đà Nẵng nếu tuyến Bắc - Nam đường dài liên miền
        int? maBuuCucTrungGian = null;
        if (bcGui != null && bcDich != null)
        {
            string mienGui = DonHangXuLy.LayMien(bcGui.TinhThanh);
            string mienNhan = DonHangXuLy.LayMien(bcDich.TinhThanh);
            if ((mienGui == "Bắc" && mienNhan == "Nam") || (mienGui == "Nam" && mienNhan == "Bắc"))
            {
                var dsBc = await db.BuuCucs.AsNoTracking().ToListAsync();
                var hubDaNang = dsBc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("đà nẵng"));
                if (hubDaNang != null && hubDaNang.MaBuuCuc != maBuuCucGui && hubDaNang.MaBuuCuc != maBuuCucNhan)
                {
                    maBuuCucTrungGian = hubDaNang.MaBuuCuc;
                }
            }
        }

        string ghiChu = laHub
            ? (maBuuCucTrungGian.HasValue
                ? $"Chuyến trục liên miền gom về Hub {bcDich?.TenBuuCuc} (ghé trạm Hub Đà Nẵng) lúc {DateTime.Now:dd/MM/yyyy HH:mm}"
                : $"Chuyến gom hàng về Hub {bcDich?.TenBuuCuc} lúc {DateTime.Now:dd/MM/yyyy HH:mm}")
            : $"Chuyến trung chuyển tuyến {bcDich?.TenBuuCuc} lúc {DateTime.Now:dd/MM/yyyy HH:mm}";

        var kq = await xuLy.TaoChuyenXeAsync(
            maBuuCucGui,
            maBuuCucNhan,
            shipper.MaNhanVien,
            shipper.MaPhuongTien,
            maDonHangs,
            ghiChu,
            HttpContext.Session.ThaoTac(),
            maBuuCucTrungGian);

        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCucGui, tab = "dangxep" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMoi(TaoChuyenXeVM vm)
    {
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && vm.MaBuuCucGui != BuuCucCuaToi.Value)
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu lập chuyến không hợp lệ, vui lòng kiểm tra lại!";
            return RedirectToAction(nameof(Index), new { maBuuCuc = vm.MaBuuCucGui });
        }

        var kq = await xuLy.TaoChuyenXeAsync(
            vm.MaBuuCucGui,
            vm.MaBuuCucNhan,
            vm.MaNhanVien,
            vm.MaPhuongTien,
            vm.DsMaDon,
            vm.GhiChu,
            HttpContext.Session.ThaoTac(),
            vm.MaBuuCucTrungGian);

        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = vm.MaBuuCucGui, tab = "dangxep" });
    }

    // ===================== TÁC VỤ CHUYẾN XE =====================

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XuatBen(int id, int? maBuuCuc)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == id);
        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && chuyen.MaBuuCucGui != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.XuatBenChuyenXeAsync(id, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "dangxep" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TiepNhan(int id, int? maBuuCuc)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == id);
        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && chuyen.MaBuuCucNhan != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.TiepNhanChuyenXeAsync(id, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "sapden" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LapChuyenLuotVe(int id, int? maBuuCuc)
    {
        var kq = await xuLy.TaoChuyenXeLuotVeAsync(id, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
        {
            TempData["Success"] = kq.ThongBao;
            return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "dangxep" });
        }

        TempData["Error"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "sapden" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GheTram(int id, List<int>? maDonBocThem, int? maBuuCuc)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == id);
        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && chuyen.MaBuuCucTrungGian != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.GheTramChuyenXeAsync(id, maDonBocThem, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "sapden" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RoiTram(int id, int? maBuuCuc)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == id);
        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && chuyen.MaBuuCucTrungGian != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.RoiTramChuyenXeAsync(id, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "sapden" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo, int? maBuuCuc)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == id);
        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && chuyen.MaBuuCucGui != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.HuyChuyenXeAsync(id, lyDo, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(Index), new { maBuuCuc = maBuuCuc, tab = "dangxep" });
    }

    // ===================== CHI TIẾT & BỔ SUNG ĐƠN =====================

    public async Task<IActionResult> ChiTiet(int id)
    {
        var chuyen = await db.ChuyenXes
            .AsNoTracking()
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.KhachHang)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.LoaiHang)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.KhuVuc)
            .FirstOrDefaultAsync(c => c.MaChuyen == id);

        if (chuyen is null) return NotFound();
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri()
            && chuyen.MaBuuCucGui != BuuCucCuaToi.Value
            && chuyen.MaBuuCucNhan != BuuCucCuaToi.Value
            && chuyen.MaBuuCucTrungGian != BuuCucCuaToi.Value)
            return Forbid();

        bool denHub = chuyen.BuuCucNhan != null && DonHangXuLy.LaHubTrungTam(chuyen.BuuCucNhan);

        // Tìm thêm các đơn đang chờ có thể gán vào chuyến này:
        // - Nếu chuyến đang ở bưu cục gửi (DangXepHang): lấy đơn tại bưu cục gửi
        // - Nếu chuyến đang dừng tại trạm trung gian (DangGheTram): lấy đơn tại trạm trung gian gửi đến bưu cục đích
        int maBuuCucLayDon = chuyen.TrangThai == TrangThaiChuyenXe.DangGheTram && chuyen.MaBuuCucTrungGian.HasValue
            ? chuyen.MaBuuCucTrungGian.Value
            : chuyen.MaBuuCucGui;

        var donCoTheThem = await db.DonGiaoHangs
            .AsNoTracking()
            .Include(d => d.KhachHang)
            .Include(d => d.LoaiHang)
            .Where(d => d.MaChuyenXe == null &&
                        d.MaBuuCucHienTai == maBuuCucLayDon &&
                        ((denHub && d.MaBuuCucGui != d.MaBuuCucNhan && (d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || d.TrangThai == TrangThaiDon.ChoLayLienTinh)) ||
                         (!denHub && d.MaBuuCucNhan == chuyen.MaBuuCucNhan && (d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || d.TrangThai == TrangThaiDon.ChoLayLienTinh))))
            .ToListAsync();

        ViewBag.DonCoTheThem = donCoTheThem;
        return View(chuyen);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemDon(int maChuyen, List<int> maDonHangs)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);
        if (chuyen is null) return NotFound();
        int maBuuCucHanhDong = chuyen.TrangThai == TrangThaiChuyenXe.DangGheTram && chuyen.MaBuuCucTrungGian.HasValue
            ? chuyen.MaBuuCucTrungGian.Value
            : chuyen.MaBuuCucGui;
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && maBuuCucHanhDong != BuuCucCuaToi.Value)
            return Forbid();

        if (maDonHangs == null || maDonHangs.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn kiện hàng cần thêm vào chuyến!";
            return RedirectToAction(nameof(ChiTiet), new { id = maChuyen });
        }

        var kq = await xuLy.ThemDonVaoChuyenXeAsync(maChuyen, maDonHangs, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(ChiTiet), new { id = maChuyen });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaDon(int maChuyen, int maDon)
    {
        var chuyen = await db.ChuyenXes.AsNoTracking().FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);
        if (chuyen is null) return NotFound();
        int maBuuCucHanhDong = chuyen.TrangThai == TrangThaiChuyenXe.DangGheTram && chuyen.MaBuuCucTrungGian.HasValue
            ? chuyen.MaBuuCucTrungGian.Value
            : chuyen.MaBuuCucGui;
        if (BuuCucCuaToi.HasValue && !HttpContext.Session.LaQuanTri() && maBuuCucHanhDong != BuuCucCuaToi.Value)
            return Forbid();

        var kq = await xuLy.XoaDonKhoiChuyenXeAsync(maChuyen, maDon, HttpContext.Session.ThaoTac());
        if (kq.ThanhCong)
            TempData["Success"] = kq.ThongBao;
        else
            TempData["Error"] = kq.ThongBao;

        return RedirectToAction(nameof(ChiTiet), new { id = maChuyen });
    }
}
