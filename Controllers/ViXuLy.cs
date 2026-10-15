using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * Dòng tiền trên ví shipper (giống các app giao hàng):
 *   Giao thành công → shipper cầm tiền thu của người nhận (COD + phí ship nếu người nhận trả) = "đang giữ".
 *   Nộp tiền: gom mọi đơn đang giữ vào 1 giao dịch → chủ quản xác nhận → các đơn được đối soát & hoàn tất.
 *   Thu nhập = thù lao hoàn thành các chặng (theo đơn, cự ly km, khối lượng & đúng hẹn); rút khi đã nộp hết tiền đang giữ (và lệnh nộp đã được xác nhận),
 *   tối thiểu RutToiThieu, không vượt số dư; chủ quản xác nhận đã chuyển khoản hoặc từ chối.
 *   Số dư tính từ dữ liệu (không lưu cứng) nên không bao giờ lệch.
 */
public class ViXuLy(QuanLyGiaoNhanDbContext db, DonHangXuLy donHangXuLy)
{
    /*
     * Khóa loại trừ: nộp / rút / xác nhận / từ chối chạy lần lượt từng thao tác một.
     * Tránh bấm hai lần (hai yêu cầu song song) cùng vượt qua bước kiểm tra số dư / trạng thái rồi ghi trùng.
     */
    private static readonly SemaphoreSlim KhoaVi = new(1, 1);

    private static async Task<T> DocQuyenAsync<T>(Func<Task<T>> thaoTac)
    {
        await KhoaVi.WaitAsync();
        try { return await thaoTac(); }
        finally { KhoaVi.Release(); }
    }

    public Task<KetQua<GiaoDichVi>> NopTienAsync(int maNhanVien, NopTienVM vm, string nguoiThucHien) => DocQuyenAsync(() => NopTienKhongKhoaAsync(maNhanVien, vm, nguoiThucHien));
    public Task<KetQua<GiaoDichVi>> RutTienAsync(int maNhanVien, RutTienVM vm, string nguoiThucHien) => DocQuyenAsync(() => RutTienKhongKhoaAsync(maNhanVien, vm, nguoiThucHien));
    public Task<KetQua> XacNhanAsync(int maGiaoDich, string nguoiThucHien) => DocQuyenAsync(() => XacNhanKhongKhoaAsync(maGiaoDich, nguoiThucHien));
    public Task<KetQua> TuChoiAsync(int maGiaoDich, string? lyDo, string nguoiThucHien) => DocQuyenAsync(() => TuChoiKhongKhoaAsync(maGiaoDich, lyDo, nguoiThucHien));
    public Task<KetQua> TatToanAsync(int maNhanVien, string nguoiThucHien) => DocQuyenAsync(() => TatToanKhongKhoaAsync(maNhanVien, nguoiThucHien));

    public const decimal RutToiThieu = 50_000m;
    private ThongBaoXuLy TB => new(db, BayGio);
    public static readonly string[] DsPhuongThucNop = ["Chuyển khoản ngân hàng", "Ví MoMo", "Nộp tiền mặt tại bưu cục"];
    public static readonly string[] DsNganHang = ["Vietcombank", "VietinBank", "BIDV", "Agribank", "Techcombank", "MB Bank", "ACB", "VPBank", "TPBank", "Sacombank"];

    private DateTime BayGio() => donHangXuLy.BayGio();

    /// <summary>Đơn shipper đã giao thành công có thu tiền, chưa đối soát, chưa nằm trong lần nộp tiền nào.</summary>
    private IQueryable<DonGiaoHang> DonDangGiuTien(int maNhanVien) =>
        db.DonGiaoHangs.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong && d.MaGiaoDichNop == null
                                && (d.TienThuHo > 0 || d.NguoiTraPhi == NguoiTraPhi.NguoiNhan)
                                && d.PhanCongs.Any(p => p.MaNhanVien == maNhanVien && p.KetQua == KetQuaGiao.GiaoThanhCong));

    public async Task<ViShipperVM?> TinhViAsync(int maNhanVien, bool kemLichSu = true)
    {
        var nv = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
        if (nv is null) return null;

        var donGiu = await DonDangGiuTien(maNhanVien).AsNoTracking().OrderBy(d => d.MaDon).ToListAsync();
        // Thu nhập: hoa hồng các chặng hoàn thành (giao thành công + bàn giao liên tỉnh)
        var thanhCong = db.PhanCongGiaoHangs.Where(p => p.MaNhanVien == maNhanVien && (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc));
        var giaoDich = db.GiaoDichVis.AsNoTracking().Where(g => g.MaNhanVien == maNhanVien);

        return new ViShipperVM
        {
            NhanVien = nv,
            DonDangGiu = donGiu,
            TienDangGiu = donGiu.Sum(d => d.TongThuNguoiNhan),
            SoDonDangGiu = donGiu.Count,
            TienChoXacNhanNop = await giaoDich.Where(g => g.Loai == LoaiGiaoDich.NopTienThuHo && g.TrangThai == TrangThaiGiaoDich.ChoXacNhan)
                                              .SumAsync(g => (decimal?)g.SoTien) ?? 0,
            ThuNhap = await thanhCong.SumAsync(p => (decimal?)p.ThuNhapShipper) ?? 0,
            SoDonThanhCong = await thanhCong.CountAsync(),
            DaRut = await giaoDich.Where(g => g.Loai == LoaiGiaoDich.RutTien && g.TrangThai == TrangThaiGiaoDich.DaXacNhan)
                                  .SumAsync(g => (decimal?)g.SoTien) ?? 0,
            DangChoRut = await giaoDich.Where(g => g.Loai == LoaiGiaoDich.RutTien && g.TrangThai == TrangThaiGiaoDich.ChoXacNhan)
                                       .SumAsync(g => (decimal?)g.SoTien) ?? 0,
            LichSu = kemLichSu ? await giaoDich.OrderByDescending(g => g.NgayTao).ThenByDescending(g => g.MaGiaoDich).ToListAsync() : []
        };
    }

    /// <summary>
    /// Ví của mọi shipper cho trang quản trị: 4 truy vấn gom nhóm (GROUP BY) thay vì gọi TinhViAsync cho từng người
    /// (~7 truy vấn × số shipper). Kết quả giống hệt TinhViAsync(kemLichSu: false).
    /// </summary>
    public async Task<List<ViShipperVM>> TinhTatCaViAsync()
    {
        var dsNhanVien = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).OrderBy(n => n.HoTen).ToListAsync();
        // Đơn đang giữ tiền: giao thành công, chưa nằm trong lệnh nộp – kèm mã shipper đã giao
        var donGiu = await db.PhanCongGiaoHangs.AsNoTracking()
            .Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong && p.DonGiaoHang!.TrangThai == TrangThaiDon.GiaoThanhCong && p.DonGiaoHang.MaGiaoDichNop == null
                     && (p.DonGiaoHang.TienThuHo > 0 || p.DonGiaoHang.NguoiTraPhi == NguoiTraPhi.NguoiNhan))
            .Select(p => new { p.MaNhanVien, Don = p.DonGiaoHang! })
            .ToListAsync();
        var thuNhap = await db.PhanCongGiaoHangs.Where(p => (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc))
            .GroupBy(p => p.MaNhanVien)
            .Select(g => new { MaNhanVien = g.Key, Tien = g.Sum(p => p.ThuNhapShipper), SoDon = g.Count() })
            .ToDictionaryAsync(x => x.MaNhanVien);
        var giaoDich = await db.GiaoDichVis.Where(g => g.TrangThai != TrangThaiGiaoDich.TuChoi)
            .GroupBy(g => new { g.MaNhanVien, g.Loai, g.TrangThai })
            .Select(g => new { g.Key.MaNhanVien, g.Key.Loai, g.Key.TrangThai, Tien = g.Sum(x => x.SoTien) })
            .ToListAsync();
        decimal TongGiaoDich(int ma, LoaiGiaoDich loai, TrangThaiGiaoDich trangThai) =>
            giaoDich.Where(x => x.MaNhanVien == ma && x.Loai == loai && x.TrangThai == trangThai).Sum(x => x.Tien);

        return dsNhanVien.Select(nv =>
        {
            var giu = donGiu.Where(x => x.MaNhanVien == nv.MaNhanVien).Select(x => x.Don).OrderBy(d => d.MaDon).ToList();
            var tn = thuNhap.GetValueOrDefault(nv.MaNhanVien);
            return new ViShipperVM
            {
                NhanVien = nv,
                DonDangGiu = giu,
                TienDangGiu = giu.Sum(d => d.TongThuNguoiNhan),
                SoDonDangGiu = giu.Count,
                TienChoXacNhanNop = TongGiaoDich(nv.MaNhanVien, LoaiGiaoDich.NopTienThuHo, TrangThaiGiaoDich.ChoXacNhan),
                ThuNhap = tn?.Tien ?? 0,
                SoDonThanhCong = tn?.SoDon ?? 0,
                DaRut = TongGiaoDich(nv.MaNhanVien, LoaiGiaoDich.RutTien, TrangThaiGiaoDich.DaXacNhan),
                DangChoRut = TongGiaoDich(nv.MaNhanVien, LoaiGiaoDich.RutTien, TrangThaiGiaoDich.ChoXacNhan)
            };
        }).ToList();
    }

    // ===================== SHIPPER =====================

    private async Task<KetQua<GiaoDichVi>> NopTienKhongKhoaAsync(int maNhanVien, NopTienVM vm, string nguoiThucHien)
    {
        if (!DsPhuongThucNop.Contains(vm.PhuongThuc)) return KetQua<GiaoDichVi>.Loi("Phương thức nộp tiền không hợp lệ");
        var dsDon = await DonDangGiuTien(maNhanVien).ToListAsync();
        if (vm.MaDonChon is { Count: > 0 })
            dsDon = dsDon.Where(d => vm.MaDonChon.Contains(d.MaDon)).ToList();
        if (dsDon.Count == 0) return KetQua<GiaoDichVi>.Loi("Bạn không giữ tiền của đơn nào cần nộp");

        var gd = new GiaoDichVi
        {
            MaNhanVien = maNhanVien, Loai = LoaiGiaoDich.NopTienThuHo, SoTien = dsDon.Sum(d => d.TongThuNguoiNhan),
            PhuongThuc = vm.PhuongThuc, ThongTinThanhToan = string.IsNullOrWhiteSpace(vm.MaThamChieu) ? null : vm.MaThamChieu.Trim(),
            NgayTao = BayGio(), GhiChu = $"Nộp tiền {dsDon.Count} đơn"
        };
        db.GiaoDichVis.Add(gd);
        foreach (var d in dsDon) d.GiaoDichNop = gd;
        await db.SaveChangesAsync();

        var nvNop = await db.NhanVienGiaoHangs.FindAsync(maNhanVien);
        TB.GuiNhieu(TB.TkQuanTri(), LoaiThongBao.Vi,
            $"Lệnh nộp {gd.MaHienThi} chờ xác nhận",
            $"{nvNop?.HoTen ?? "Shipper"} nộp {DinhDang.Tien(gd.SoTien)} (nộp tiền {dsDon.Count} đơn) qua {gd.PhuongThuc}.",
            "/QuanTriViShipper");
        await db.SaveChangesAsync();

        return KetQua<GiaoDichVi>.Dat(gd, $"Đã tạo lệnh nộp {DinhDang.Tien(gd.SoTien)} ({dsDon.Count} đơn) – chờ công ty xác nhận");
    }

    private async Task<KetQua<GiaoDichVi>> RutTienKhongKhoaAsync(int maNhanVien, RutTienVM vm, string nguoiThucHien)
    {
        var vi = await TinhViAsync(maNhanVien, kemLichSu: false);
        if (vi is null) return KetQua<GiaoDichVi>.Loi("Không tìm thấy nhân viên");
        if (vi.TienDangGiu > 0)
            return KetQua<GiaoDichVi>.Loi($"Bạn đang giữ {DinhDang.Tien(vi.TienDangGiu)} tiền thu hộ – hãy nộp về công ty trước khi rút tiền");
        // Lệnh nộp chưa được xác nhận thì tiền chưa chắc đã về công ty (có thể bị từ chối) → chưa cho rút
        if (vi.TienChoXacNhanNop > 0)
            return KetQua<GiaoDichVi>.Loi($"Lệnh nộp {DinhDang.Tien(vi.TienChoXacNhanNop)} đang chờ công ty xác nhận – chỉ rút được sau khi lệnh nộp được xác nhận");
        if (!DsNganHang.Contains(vm.NganHang)) return KetQua<GiaoDichVi>.Loi("Ngân hàng không hợp lệ");
        if (vm.SoTien < RutToiThieu) return KetQua<GiaoDichVi>.Loi($"Số tiền rút tối thiểu {DinhDang.Tien(RutToiThieu)}");
        if (vm.SoTien % 1000 != 0) return KetQua<GiaoDichVi>.Loi("Số tiền rút phải là bội số của 1.000 đ");
        if (vm.SoTien > vi.SoDu) return KetQua<GiaoDichVi>.Loi($"Số dư khả dụng chỉ còn {DinhDang.Tien(vi.SoDu)}");

        var gd = new GiaoDichVi
        {
            MaNhanVien = maNhanVien, Loai = LoaiGiaoDich.RutTien, SoTien = vm.SoTien, PhuongThuc = "Chuyển khoản ngân hàng",
            ThongTinThanhToan = $"{vm.NganHang} – {vm.SoTaiKhoan.Trim()} – {vm.ChuTaiKhoan.Trim().ToUpper()}", NgayTao = BayGio()
        };
        db.GiaoDichVis.Add(gd);
        await db.SaveChangesAsync();

        var nvRut = await db.NhanVienGiaoHangs.FindAsync(maNhanVien);
        TB.GuiNhieu(TB.TkQuanTri(), LoaiThongBao.Vi,
            $"Yêu cầu rút ví shipper SP{maNhanVien:D6}",
            $"{nvRut?.HoTen ?? "Shipper"} yêu cầu rút {DinhDang.Tien(gd.SoTien)} về {gd.ThongTinThanhToan}.",
            "/QuanTriViShipper");
        await db.SaveChangesAsync();

        return KetQua<GiaoDichVi>.Dat(gd, $"Đã gửi yêu cầu rút {DinhDang.Tien(gd.SoTien)} – chờ công ty chuyển khoản");
    }

    // ===================== CHỦ QUẢN =====================

    /// <summary>
    /// Xác nhận: nộp tiền → đối soát & hoàn tất các đơn trong giao dịch; rút tiền → đã chuyển khoản.
    /// Chạy trong một giao tác: đơn nào hoàn tất lỗi thì trả lại toàn bộ (không có lệnh nộp "xác nhận một nửa").
    /// Dùng execution strategy vì DbContext bật EnableRetryOnFailure.
    /// </summary>
    private Task<KetQua> XacNhanKhongKhoaAsync(int maGiaoDich, string nguoiThucHien) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var giaoTac = await db.Database.BeginTransactionAsync();
            var kq = await XacNhanTrongGiaoTacAsync(maGiaoDich, nguoiThucHien);
            if (kq.ThanhCong) await giaoTac.CommitAsync();
            else db.ChangeTracker.Clear();   // không commit → giao tác tự hủy khi dispose
            return kq;
        });

    private async Task<KetQua> XacNhanTrongGiaoTacAsync(int maGiaoDich, string nguoiThucHien)
    {
        var gd = await db.GiaoDichVis.Include(g => g.DonNop).FirstOrDefaultAsync(g => g.MaGiaoDich == maGiaoDich);
        if (gd is null) return KetQua.Loi("Không tìm thấy giao dịch");
        if (gd.TrangThai != TrangThaiGiaoDich.ChoXacNhan) return KetQua.Loi($"Giao dịch {gd.MaHienThi} đã được xử lý");

        gd.TrangThai = TrangThaiGiaoDich.DaXacNhan;
        gd.NgayXuLy = BayGio();
        gd.NguoiXuLy = nguoiThucHien;
        await db.SaveChangesAsync();

        if (gd.Loai == LoaiGiaoDich.NopTienThuHo)
        {
            foreach (var maDon in gd.DonNop.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong).Select(d => d.MaDon).ToList())
            {
                var kqDon = await donHangXuLy.HoanTatAsync(maDon, $"{nguoiThucHien} – {gd.MaHienThi}", tuGiaoDichVi: true);
                if (!kqDon.ThanhCong) return KetQua.Loi($"Không hoàn tất được đơn trong {gd.MaHienThi}: {kqDon.ThongBao}");
            }
            TB.Gui(TB.TkShipper(gd.MaNhanVien), LoaiThongBao.Vi,
                $"Lệnh nộp {gd.MaHienThi} đã được duyệt",
                $"Công ty đã xác nhận thu {DinhDang.Tien(gd.SoTien)}. {gd.DonNop.Count} đơn hàng đã được đối soát & hoàn tất.",
                "/NhanVienVi");
            await db.SaveChangesAsync();
            return KetQua.Dat($"Đã xác nhận nhận {DinhDang.Tien(gd.SoTien)} ({gd.MaHienThi}) – {gd.DonNop.Count} đơn đã đối soát & hoàn tất");
        }
        TB.Gui(TB.TkShipper(gd.MaNhanVien), LoaiThongBao.Vi,
            $"Lệnh rút {gd.MaHienThi} đã chuyển khoản",
            $"Công ty đã xác nhận chuyển khoản {DinhDang.Tien(gd.SoTien)} về {gd.ThongTinThanhToan}.",
            "/NhanVienVi");
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã xác nhận chuyển khoản {DinhDang.Tien(gd.SoTien)} cho yêu cầu rút {gd.MaHienThi}");
    }

    /// <summary>Từ chối: nộp tiền → các đơn trở lại "đang giữ tiền"; rút tiền → số tiền trở lại số dư.</summary>
    private async Task<KetQua> TuChoiKhongKhoaAsync(int maGiaoDich, string? lyDo, string nguoiThucHien)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) return KetQua.Loi("Nhập lý do từ chối");
        var gd = await db.GiaoDichVis.Include(g => g.DonNop).FirstOrDefaultAsync(g => g.MaGiaoDich == maGiaoDich);
        if (gd is null) return KetQua.Loi("Không tìm thấy giao dịch");
        if (gd.TrangThai != TrangThaiGiaoDich.ChoXacNhan) return KetQua.Loi($"Giao dịch {gd.MaHienThi} đã được xử lý");

        gd.TrangThai = TrangThaiGiaoDich.TuChoi;
        gd.NgayXuLy = BayGio();
        gd.NguoiXuLy = nguoiThucHien;
        gd.GhiChu = DinhDang.Cat(string.Join(" | ", new[] { gd.GhiChu, $"Từ chối: {lyDo.Trim()}" }.Where(s => !string.IsNullOrEmpty(s))), 255);
        foreach (var d in gd.DonNop) d.MaGiaoDichNop = null;
        TB.Gui(TB.TkShipper(gd.MaNhanVien), LoaiThongBao.Vi,
            $"Lệnh {gd.MaHienThi} bị từ chối",
            $"Lệnh {gd.Loai.TenHienThi().ToLower()} {DinhDang.Tien(gd.SoTien)} bị từ chối. Lý do: {lyDo.Trim()}.",
            "/NhanVienVi");
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã từ chối giao dịch {gd.MaHienThi}");
    }

    /// <summary>Tất toán toàn bộ số dư khả dụng khi shipper nghỉ việc hoặc ngừng hoạt động.</summary>
    private async Task<KetQua> TatToanKhongKhoaAsync(int maNhanVien, string nguoiThucHien)
    {
        var vi = await TinhViAsync(maNhanVien, kemLichSu: false);
        if (vi is null) return KetQua.Loi("Không tìm thấy nhân viên");
        if (vi.TienDangGiu > 0)
            return KetQua.Loi($"Shipper đang giữ {DinhDang.Tien(vi.TienDangGiu)} tiền thu hộ – cần nộp tiền trước khi tất toán");
        if (vi.TienChoXacNhanNop > 0)
            return KetQua.Loi($"Lệnh nộp {DinhDang.Tien(vi.TienChoXacNhanNop)} đang chờ xác nhận – hãy xử lý trước khi tất toán");
        if (vi.SoDu <= 0)
            return KetQua.Loi("Số dư ví shipper là 0 đ, không có tiền cần tất toán");

        var gd = new GiaoDichVi
        {
            MaNhanVien = maNhanVien,
            Loai = LoaiGiaoDich.RutTien,
            SoTien = vi.SoDu,
            PhuongThuc = "Tất toán số dư ví",
            ThongTinThanhToan = "Tất toán khi nghỉ việc / chấm dứt hợp đồng",
            NgayTao = BayGio(),
            NgayXuLy = BayGio(),
            NguoiXuLy = nguoiThucHien,
            TrangThai = TrangThaiGiaoDich.DaXacNhan,
            GhiChu = $"Quản trị {nguoiThucHien} tất toán ví shipper ({DinhDang.Tien(vi.SoDu)})"
        };
        db.GiaoDichVis.Add(gd);
        TB.Gui(TB.TkShipper(maNhanVien), LoaiThongBao.Vi,
            "Tất toán số dư ví shipper",
            $"Quản trị viên đã tất toán toàn bộ số dư {DinhDang.Tien(gd.SoTien)} trên ví shipper của bạn.",
            "/NhanVienVi");
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã tất toán toàn bộ {DinhDang.Tien(gd.SoTien)} cho shipper {vi.NhanVien.HoTen}");
    }
}
