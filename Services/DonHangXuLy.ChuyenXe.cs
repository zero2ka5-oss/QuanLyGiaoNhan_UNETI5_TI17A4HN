using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

public partial class DonHangXuLy
{
    private static IEnumerable<int> DsPhuongTien(int? ptId) => ptId.HasValue ? [ptId.Value] : [];

    /// <summary>
    /// Xác định vùng miền của bưu cục dựa trên tỉnh/thành phố: Bắc, Trung, Nam.
    /// </summary>
    public static string LayMien(string? tinhThanh)
    {
        if (string.IsNullOrWhiteSpace(tinhThanh)) return "Bắc";
        string t = tinhThanh.ToLowerInvariant();

        // Miền Trung & Tây Nguyên (19 tỉnh/thành phố)
        if (t.Contains("thanh hóa") || t.Contains("nghệ an") || t.Contains("hà tĩnh") ||
            t.Contains("quảng bình") || t.Contains("quảng trị") || t.Contains("huế") ||
            t.Contains("đà nẵng") || t.Contains("quảng nam") || t.Contains("quảng ngãi") ||
            t.Contains("bình định") || t.Contains("phú yên") || t.Contains("khánh hòa") ||
            t.Contains("ninh thuận") || t.Contains("bình thuận") || t.Contains("kon tum") ||
            t.Contains("gia lai") || t.Contains("đắk lắk") || t.Contains("dak lak") ||
            t.Contains("đắk nông") || t.Contains("dak nong") || t.Contains("lâm đồng"))
            return "Trung";

        // Miền Nam (19 tỉnh/thành phố Đông Nam Bộ & Tây Nam Bộ)
        if (t.Contains("hồ chí minh") || t.Contains("hcm") || t.Contains("sài gòn") ||
            t.Contains("bình dương") || t.Contains("đồng nai") || t.Contains("vũng tàu") ||
            t.Contains("bình phước") || t.Contains("tây ninh") ||
            t.Contains("long an") || t.Contains("tiền giang") || t.Contains("bến tre") ||
            t.Contains("trà vinh") || t.Contains("vĩnh long") || t.Contains("đồng tháp") ||
            t.Contains("an giang") || t.Contains("kiên giang") || t.Contains("cần thơ") ||
            t.Contains("hậu giang") || t.Contains("sóc trăng") || t.Contains("bạc liêu") ||
            t.Contains("cà mau"))
            return "Nam";

        return "Bắc";
    }

    /// <summary>
    /// Kiểm tra xem bưu cục có phải là 1 trong 3 Hub trung tâm khai thác toàn quốc:
    /// - Hub Miền Bắc: Bưu cục Bắc Ninh
    /// - Hub Miền Trung: Bưu cục Đà Nẵng
    /// - Hub Miền Nam: Bưu cục TP. Hồ Chí Minh
    /// </summary>
    public static bool LaHubTrungTam(BuuCuc bc)
    {
        string t = (bc.TinhThanh + " " + bc.TenBuuCuc).ToLowerInvariant();
        return t.Contains("bắc ninh") || t.Contains("đà nẵng") || t.Contains("hồ chí minh");
    }

    /// <summary>
    /// Tìm Bưu cục Hub trung tâm vùng tương ứng cho một bưu cục vệ tinh:
    /// Miền Bắc -> Bưu cục Bắc Ninh; Miền Trung -> Bưu cục Đà Nẵng; Miền Nam -> Bưu cục TP. Hồ Chí Minh.
    /// </summary>
    public static BuuCuc? TimHubVung(BuuCuc bc, IEnumerable<BuuCuc> dsBc)
    {
        string mien = LayMien(bc.TinhThanh);
        return mien switch
        {
            "Trung" => dsBc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("đà nẵng")),
            "Nam" => dsBc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("hồ chí minh")),
            _ => dsBc.FirstOrDefault(b => (b.TinhThanh + " " + b.TenBuuCuc).ToLowerInvariant().Contains("bắc ninh"))
        };
    }

    /// <summary>
    /// Lập chuyến xe liên tỉnh mới và gom các đơn hàng đang chờ vào chuyến xe.
    /// </summary>
    public async Task<KetQua> TaoChuyenXeAsync(
        int maBuuCucGui,
        int maBuuCucNhan,
        int maNhanVien,
        int? maPhuongTien,
        List<int> maDonHangs,
        string? ghiChu,
        ThaoTac tt,
        int? maBuuCucTrungGian = null)
    {
        var bcGui = await db.BuuCucs.FindAsync(maBuuCucGui);
        var bcNhan = await db.BuuCucs.FindAsync(maBuuCucNhan);
        if (bcGui is null || bcNhan is null)
            return KetQua.Loi("Bưu cục gửi hoặc bưu cục nhận không tồn tại");

        if (maBuuCucGui == maBuuCucNhan)
            return KetQua.Loi("Chuyến xe liên tỉnh yêu cầu bưu cục gửi và bưu cục nhận phải khác nhau");

        var dsAllBc = await db.BuuCucs.AsNoTracking().ToListAsync();
        bool laHubGui = LaHubTrungTam(bcGui);
        var hubVungGui = TimHubVung(bcGui, dsAllBc);

        if (!laHubGui)
        {
            // BƯU CỤC KHU VỰC: Xe liên tỉnh chỉ được phép nhận và gom hàng về Hub trung tâm phụ trách vùng
            if (hubVungGui != null && maBuuCucNhan != hubVungGui.MaBuuCuc)
            {
                return KetQua.Loi($"Bưu cục khu vực ({bcGui.TenBuuCuc}) chỉ có nhiệm vụ gom kiện hàng gửi về Hub trung tâm vùng phụ trách (Hub {hubVungGui.TenBuuCuc})!");
            }
            if (maBuuCucTrungGian.HasValue)
            {
                return KetQua.Loi("Xe liên tỉnh của bưu cục khu vực chỉ chạy tuyến con thoi gom hàng thẳng về Hub, không áp dụng ghé trạm trung gian!");
            }
        }
        else
        {
            // HUB TRUNG TÂM: Xe liên tỉnh chuyên trách kết nối giữa các Hub và phân tỏa nội miền
            string mienGui = LayMien(bcGui.TinhThanh);
            string mienNhan = LayMien(bcNhan.TinhThanh);
            if (mienGui != mienNhan && !LaHubTrungTam(bcNhan))
            {
                return KetQua.Loi($"Tuyến đường dài liên miền từ Hub {bcGui.TenBuuCuc} bắt buộc phải kết nối tới Hub trung tâm của miền đích ({mienNhan}), không được gửi trực tiếp tới bưu cục khu vực {bcNhan.TenBuuCuc}!");
            }
        }

        if (maBuuCucTrungGian.HasValue && (maBuuCucTrungGian.Value == maBuuCucGui || maBuuCucTrungGian.Value == maBuuCucNhan))
            return KetQua.Loi("Trạm dừng trung gian không được trùng với bưu cục xuất phát hoặc bưu cục đích");

        var bcTrungGian = maBuuCucTrungGian.HasValue ? await db.BuuCucs.FindAsync(maBuuCucTrungGian.Value) : null;

        var nv = await db.NhanVienGiaoHangs.Include(n => n.PhuongTien).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
        if (nv is null)
            return KetQua.Loi("Không tìm thấy nhân viên / tài xế được chọn");

        bool laChuyenVeCuaXeVeTinh = laHubGui && nv.MaBuuCuc == maBuuCucNhan;
        if (nv.MaBuuCuc != maBuuCucGui && !laChuyenVeCuaXeVeTinh)
            return KetQua.Loi($"Tài xế {nv.HoTen} không thuộc biên chế bưu cục xuất phát ({bcGui.TenBuuCuc})!");

        // Kiểm tra xung đột: tài xế không được nằm trong chuyến xe khác đang hoạt động
        bool taiXeDangBan = await db.ChuyenXes.AnyAsync(c => c.MaNhanVien == maNhanVien
            && c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy);
        if (taiXeDangBan)
            return KetQua.Loi($"Tài xế {nv.HoTen} hiện đang phụ trách chuyến xe khác đang hoạt động");

        int? phuongTienId = maPhuongTien ?? nv.MaPhuongTien;
        var pt = phuongTienId.HasValue ? await db.PhuongTiens.FindAsync(phuongTienId.Value) : null;
        int maPtChuan = phuongTienId ?? nv.MaPhuongTien ?? 0;

        // Kiểm tra xung đột: phương tiện không được nằm trong chuyến xe khác đang hoạt động
        if (phuongTienId.HasValue)
        {
            bool xeDangBan = await db.ChuyenXes.AnyAsync(c => c.MaPhuongTien == phuongTienId.Value
                && c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy);
            if (xeDangBan)
                return KetQua.Loi($"Phương tiện {(pt?.BienSo ?? phuongTienId.Value.ToString())} hiện đang được sử dụng trong chuyến xe khác đang hoạt động");
        }

        List<DonGiaoHang> dsDon = [];
        if (maDonHangs != null && maDonHangs.Count > 0)
        {
            dsDon = await db.DonGiaoHangs
                .Include(d => d.PhanCongs)
                .Include(d => d.BuuCucGui)
                .Include(d => d.BuuCucNhan)
                .Where(d => maDonHangs.Contains(d.MaDon))
                .ToListAsync();

            decimal tongKg = dsDon.Sum(d => d.KhoiLuong);
            decimal taiTrong = pt?.TaiTrongToiDa ?? 0;
            if (taiTrong > 0 && tongKg > taiTrong)
                return KetQua.Loi($"Tổng khối lượng các kiện hàng ({tongKg:N1} kg) vượt quá tải trọng tối đa của phương tiện ({taiTrong:N1} kg)");
        }

        var chuyen = new ChuyenXe
        {
            MaBuuCucGui = maBuuCucGui,
            MaBuuCucNhan = maBuuCucNhan,
            MaBuuCucTrungGian = maBuuCucTrungGian,
            MaNhanVien = maNhanVien,
            MaPhuongTien = phuongTienId,
            TrangThai = TrangThaiChuyenXe.DangXepHang,
            NgayTao = BayGio(),
            GhiChu = ghiChu?.Trim()
        };
        db.ChuyenXes.Add(chuyen);
        await db.SaveChangesAsync();

        if (dsDon.Count > 0)
        {
            foreach (var don in dsDon)
            {
                don.MaChuyenXe = chuyen.MaChuyen;

                // Tạo hoặc cập nhật phân công liên tỉnh
                var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
                if (pc is null)
                {
                    pc = new PhanCongGiaoHang
                    {
                        MaDon = don.MaDon,
                        MaNhanVien = maNhanVien,
                        MaPhuongTien = maPtChuan,
                        LoaiChang = LoaiShipper.LienTinh,
                        TrangThai = TrangThaiPhanCong.DaNhanHang,
                        NgayPhanCong = BayGio(),
                        NgayNhanHang = BayGio(),
                        GhiChu = $"Gom vào chuyến xe CX{chuyen.MaChuyen:D6}"
                    };
                    db.PhanCongGiaoHangs.Add(pc);
                }
                else
                {
                    pc.MaNhanVien = maNhanVien;
                    pc.MaPhuongTien = maPtChuan;
                    pc.TrangThai = TrangThaiPhanCong.DaNhanHang;
                    pc.NgayNhanHang ??= BayGio();
                }

                if (don.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || don.TrangThai == TrangThaiDon.ChoLayLienTinh)
                {
                    don.TrangThai = TrangThaiDon.DaLayLienTinh;
                }
                DatNguoiGiu(don, null, maNhanVien);

                string loTrinhStr = bcTrungGian != null
                    ? $"{bcGui.TenBuuCuc} ➔ (Ghé {bcTrungGian.TenBuuCuc}) ➔ {bcNhan.TenBuuCuc}"
                    : $"{bcGui.TenBuuCuc} ➔ {bcNhan.TenBuuCuc}";

                string moTaChuyen = $"Đã xếp kiện vào Chuyến xe CX{chuyen.MaChuyen:D6} ({loTrinhStr}) – Tài xế: {nv.HoTen} ({pt?.BienSo ?? "Xe tải"})";

                GhiSuKien(don, don.TrangThai, moTaChuyen, tt, maBuuCuc: maBuuCucGui, pc: pc, quetMa: true);
            }

            await db.SaveChangesAsync();
        }

        await CapNhatNguonLucAsync([maNhanVien], DsPhuongTien(phuongTienId));
        string loTrinhThanhCong = bcTrungGian != null
            ? $"{bcGui.TenBuuCuc} ➔ (Ghé {bcTrungGian.TenBuuCuc}) ➔ {bcNhan.TenBuuCuc}"
            : $"{bcGui.TenBuuCuc} ➔ {bcNhan.TenBuuCuc}";
        return KetQua.Dat($"Đã lập thành công Chuyến xe CX{chuyen.MaChuyen:D6} ({loTrinhThanhCong})");
    }

    /// <summary>
    /// Thêm danh sách đơn hàng vào chuyến xe đang chờ xếp hàng.
    /// </summary>
    public async Task<KetQua> ThemDonVaoChuyenXeAsync(int maChuyen, List<int> maDonHangs, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangXepHang && chuyen.TrangThai != TrangThaiChuyenXe.DangGheTram)
            return KetQua.Loi($"Chuyến xe đang ở trạng thái '{chuyen.TrangThai.TenHienThi()}', không thể thêm kiện hàng");

        var dsDon = await db.DonGiaoHangs
            .Include(d => d.PhanCongs)
            .Include(d => d.BuuCucGui)
            .Include(d => d.BuuCucNhan)
            .Where(d => maDonHangs.Contains(d.MaDon))
            .ToListAsync();

        decimal kgHienTai = chuyen.DonHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy).Sum(d => d.KhoiLuong);
        decimal kgThem = dsDon.Where(d => !d.MaChuyenXe.HasValue || d.MaChuyenXe.Value == maChuyen).Sum(d => d.KhoiLuong);
        decimal taiTrong = chuyen.PhuongTien?.TaiTrongToiDa ?? 0;
        if (taiTrong > 0 && (kgHienTai + kgThem) > taiTrong)
            return KetQua.Loi($"Tổng khối lượng ({kgHienTai + kgThem:N1} kg) vượt quá tải trọng tối đa của phương tiện ({taiTrong:N1} kg)");

        bool dangGheTram = chuyen.TrangThai == TrangThaiChuyenXe.DangGheTram;
        int maPtChuan = chuyen.MaPhuongTien ?? chuyen.NhanVien?.MaPhuongTien ?? 0;
        int dem = 0;
        foreach (var don in dsDon)
        {
            if (don.MaChuyenXe.HasValue && don.MaChuyenXe.Value != maChuyen) continue;

            don.MaChuyenXe = chuyen.MaChuyen;
            var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pc is null)
            {
                pc = new PhanCongGiaoHang
                {
                    MaDon = don.MaDon,
                    MaNhanVien = chuyen.MaNhanVien,
                    MaPhuongTien = maPtChuan,
                    LoaiChang = LoaiShipper.LienTinh,
                    TrangThai = dangGheTram ? TrangThaiPhanCong.DangGiao : TrangThaiPhanCong.DaNhanHang,
                    NgayPhanCong = BayGio(),
                    NgayNhanHang = BayGio(),
                    NgayBatDauGiao = dangGheTram ? BayGio() : null,
                    GhiChu = dangGheTram ? $"Bốc thêm tại trạm dừng vào chuyến xe CX{chuyen.MaChuyen:D6}" : $"Bổ sung vào chuyến xe CX{chuyen.MaChuyen:D6}"
                };
                db.PhanCongGiaoHangs.Add(pc);
            }
            else
            {
                pc.MaNhanVien = chuyen.MaNhanVien;
                pc.MaPhuongTien = maPtChuan;
                pc.TrangThai = dangGheTram ? TrangThaiPhanCong.DangGiao : TrangThaiPhanCong.DaNhanHang;
                pc.NgayNhanHang ??= BayGio();
                if (dangGheTram) pc.NgayBatDauGiao ??= BayGio();
            }

            if (dangGheTram)
            {
                don.TrangThai = TrangThaiDon.DangTrungChuyen;
            }
            else if (don.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui || don.TrangThai == TrangThaiDon.ChoLayLienTinh)
            {
                don.TrangThai = TrangThaiDon.DaLayLienTinh;
            }
            DatNguoiGiu(don, null, chuyen.MaNhanVien);

            string moTaSuKien = dangGheTram
                ? $"Đã bốc thêm kiện tại trạm trung gian vào Chuyến xe {chuyen.MaHienThi} – Tài xế: {chuyen.NhanVien?.HoTen} ({chuyen.PhuongTien?.BienSo ?? "Xe tải"})"
                : $"Đã xếp thêm kiện vào Chuyến xe {chuyen.MaHienThi} – Tài xế: {chuyen.NhanVien?.HoTen} ({chuyen.PhuongTien?.BienSo ?? "Xe tải"})";

            GhiSuKien(don, don.TrangThai, moTaSuKien, tt, maBuuCuc: dangGheTram ? chuyen.MaBuuCucTrungGian : chuyen.MaBuuCucGui, pc: pc, quetMa: true);
            dem++;
        }

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        return KetQua.Dat($"Đã thêm thành công {dem} đơn hàng vào chuyến xe {chuyen.MaHienThi}");
    }

    /// <summary>
    /// Gỡ đơn hàng ra khỏi chuyến xe khi chuyến còn đang xếp hàng.
    /// </summary>
    public async Task<KetQua> XoaDonKhoiChuyenXeAsync(int maChuyen, int maDon, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes.FindAsync(maChuyen);
        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangXepHang)
            return KetQua.Loi("Chỉ có thể dỡ hàng khi chuyến xe đang xếp hàng");

        var don = await db.DonGiaoHangs
            .Include(d => d.PhanCongs)
            .Include(d => d.BuuCucGui)
            .FirstOrDefaultAsync(d => d.MaDon == maDon && d.MaChuyenXe == maChuyen);

        if (don is null) return KetQua.Loi("Đơn hàng không thuộc chuyến xe này");

        don.MaChuyenXe = null;
        var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc != null)
        {
            pc.TrangThai = TrangThaiPhanCong.DaThayDoi;
            pc.NgayKetThuc = BayGio();
            pc.KetQua = KetQuaGiao.DaHuyDon;
            pc.GhiChu = $"Dỡ khỏi chuyến CX{chuyen.MaChuyen:D6}";
        }

        if (don.TrangThai == TrangThaiDon.DaLayLienTinh)
        {
            don.TrangThai = TrangThaiDon.DaNhanTaiBuuCucGui;
        }
        DatNguoiGiu(don, chuyen.MaBuuCucGui, null);

        GhiSuKien(don, don.TrangThai,
            $"Đã dỡ kiện hàng khỏi Chuyến xe CX{chuyen.MaChuyen:D6}, lưu giữ tại {don.BuuCucGui?.TenBuuCuc}",
            tt, maBuuCuc: chuyen.MaBuuCucGui, quetMa: true);

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        return KetQua.Dat($"Đã dỡ kiện hàng {don.MaHienThi} khỏi chuyến xe");
    }

    /// <summary>
    /// Xuất bến chuyến xe liên tỉnh, bắt đầu hành trình vận chuyển trung chuyển.
    /// </summary>
    public async Task<KetQua> XuatBenChuyenXeAsync(int maChuyen, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.PhanCongs)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangXepHang)
            return KetQua.Loi($"Chuyến xe đang ở trạng thái '{chuyen.TrangThai.TenHienThi()}', không thể xuất bến");

        if (chuyen.DonHangs.Count == 0)
            return KetQua.Loi("Chuyến xe chưa có kiện hàng nào, vui lòng xếp hàng trước khi xuất bến");

        decimal tongKgChuyen = chuyen.DonHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy && d.TrangThai != TrangThaiDon.ThatLac && d.TrangThai != TrangThaiDon.HuHong).Sum(d => d.KhoiLuong);
        decimal taiTrongToiDa = chuyen.PhuongTien?.TaiTrongToiDa ?? 0;
        if (taiTrongToiDa > 0 && tongKgChuyen > taiTrongToiDa)
            return KetQua.Loi($"Chuyến xe vượt quá tải trọng tối đa ({tongKgChuyen:N1} kg / {taiTrongToiDa:N1} kg), không thể xuất bến");

        chuyen.TrangThai = TrangThaiChuyenXe.DangVanChuyen;
        chuyen.NgayXuatBen = BayGio();

        if (chuyen.PhuongTien != null)
        {
            chuyen.PhuongTien.TrangThai = TrangThaiPhuongTien.DangSuDung;
        }

        bool denHub = chuyen.BuuCucNhan != null && LaHubTrungTam(chuyen.BuuCucNhan);

        foreach (var don in chuyen.DonHangs.Where(d => d.TrangThai != TrangThaiDon.DaHuy && d.TrangThai != TrangThaiDon.ThatLac && d.TrangThai != TrangThaiDon.HuHong))
        {
            var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pc != null)
            {
                pc.TrangThai = TrangThaiPhanCong.DangGiao;
                pc.NgayBatDauGiao = BayGio();
            }

            bool laHangHoan = don.TrangThai is TrangThaiDon.DangChuyenHoan or TrangThaiDon.ChoChuyenHoan;
            TrangThaiDon trangThaiMoi = laHangHoan ? TrangThaiDon.DangChuyenHoan : TrangThaiDon.DangTrungChuyen;
            don.TrangThai = trangThaiMoi;

            string noiDung = chuyen.MaBuuCucTrungGian.HasValue && chuyen.BuuCucTrungGian != null
                ? $"Chuyến xe CX{chuyen.MaChuyen:D6} xuất bến {chuyen.BuuCucGui?.TenBuuCuc} (Lộ trình: ghé trạm trung gian {chuyen.BuuCucTrungGian.TenBuuCuc} ➔ đích {chuyen.BuuCucNhan?.TenBuuCuc}) – Xe: {chuyen.PhuongTien?.BienSo}"
                : (denHub
                    ? $"Chuyến xe CX{chuyen.MaChuyen:D6} xuất bến {chuyen.BuuCucGui?.TenBuuCuc} gom hàng về Hub {chuyen.BuuCucNhan?.TenBuuCuc} – Xe: {chuyen.PhuongTien?.BienSo}"
                    : $"Chuyến xe CX{chuyen.MaChuyen:D6} xuất bến {chuyen.BuuCucGui?.TenBuuCuc} đi {chuyen.BuuCucNhan?.TenBuuCuc} – Xe: {chuyen.PhuongTien?.BienSo}");

            GhiSuKien(don, trangThaiMoi, noiDung, tt, pc: pc);
        }

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        string loTrinhMsg = chuyen.MaBuuCucTrungGian.HasValue && chuyen.BuuCucTrungGian != null
            ? $"{chuyen.BuuCucGui?.TenBuuCuc} ➔ (Ghé {chuyen.BuuCucTrungGian.TenBuuCuc}) ➔ {chuyen.BuuCucNhan?.TenBuuCuc}"
            : $"{chuyen.BuuCucGui?.TenBuuCuc} ➔ {chuyen.BuuCucNhan?.TenBuuCuc}";
        return KetQua.Dat($"Chuyến xe CX{chuyen.MaChuyen:D6} ({loTrinhMsg}) đã xuất bến thành công!");
    }

    /// <summary>
    /// Chuyến xe ghé trạm trung gian:
    /// - Dỡ các kiện hàng có đích đến là trạm trung gian (hoặc vùng phụ trách của trạm trung gian) để nhập kho trung gian phân luồng/phát hàng.
    /// - Các kiện hàng đi trạm cuối vẫn giữ nguyên trên xe.
    /// - Có thể bốc thêm các kiện hàng đang chờ tại trạm trung gian gửi đến bưu cục đích cuối.
    /// - Chuyển trạng thái chuyến xe sang DangGheTram (đang dừng tại trạm trung gian).
    /// </summary>
    public async Task<KetQua> GheTramChuyenXeAsync(int maChuyen, List<int>? maDonBocThem, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.PhanCongs)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.BuuCucNhan)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (!chuyen.MaBuuCucTrungGian.HasValue)
            return KetQua.Loi("Chuyến xe này không đăng ký trạm dừng trung gian giữa đường");

        if (chuyen.TrangThai != TrangThaiChuyenXe.DangVanChuyen)
            return KetQua.Loi($"Chuyến xe đang ở trạng thái '{chuyen.TrangThai.TenHienThi()}', chỉ có thể ghé trạm khi xe đang vận chuyển");

        if (chuyen.DaGheTram)
            return KetQua.Loi("Chuyến xe đã từng ghé trạm trung gian trước đó rồi");

        int maTram = chuyen.MaBuuCucTrungGian.Value;
        var bcTram = chuyen.BuuCucTrungGian ?? await db.BuuCucs.FindAsync(maTram);
        string tenTram = bcTram?.TenBuuCuc ?? $"Trạm #{maTram}";
        string mienTram = LayMien(bcTram?.TinhThanh);

        chuyen.TrangThai = TrangThaiChuyenXe.DangGheTram;
        chuyen.NgayGheTram = BayGio();

        int soDonDo = 0;
        int soDonGiu = 0;

        // 1. Dỡ các kiện hàng đích đến là trạm này hoặc vùng này (không phải đích cuối của chuyến)
        var dsDonHienCo = chuyen.DonHangs.ToList();
        foreach (var don in dsDonHienCo)
        {
            int? maDichCuoi = don.TrangThai == TrangThaiDon.DangChuyenHoan ? don.MaBuuCucGui : don.MaBuuCucNhan;
            bool doTaiTram = maDichCuoi == maTram || (LayMien(don.BuuCucNhan?.TinhThanh) == mienTram && maDichCuoi != chuyen.MaBuuCucNhan);

            if (doTaiTram)
            {
                var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
                if (pc != null)
                {
                    pc.TrangThai = TrangThaiPhanCong.KetThuc;
                    pc.KetQua = KetQuaGiao.DaBanGiaoBuuCuc;
                    pc.NgayKetThuc = BayGio();
                    pc.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.LienTinh, KetQuaGiao.DaBanGiaoBuuCuc, pc.NgayKetThuc);
                }

                bool laDichChinhThuc = maDichCuoi == maTram;
                TrangThaiDon trangThaiMoi = laDichChinhThuc
                    ? (don.TrangThai == TrangThaiDon.DangChuyenHoan ? TrangThaiDon.DaVeBuuCucGui : TrangThaiDon.DaDenBuuCucNhan)
                    : (don.TrangThai == TrangThaiDon.DangChuyenHoan ? TrangThaiDon.ChoChuyenHoan : TrangThaiDon.DaNhanTaiBuuCucGui); // Lưu kho trạm để phân tỏa tiếp

                don.TrangThai = trangThaiMoi;
                don.MaChuyenXe = null;
                DatNguoiGiu(don, maTram, null);

                string moTaDo = laDichChinhThuc
                    ? (don.TrangThai == TrangThaiDon.DaVeBuuCucGui
                        ? $"{tenTram} đã dỡ kiện từ Chuyến CX{chuyen.MaChuyen:D6} (trạm dừng trung gian) – Kiện hoàn đã về bưu cục gửi, sẵn sàng trả người gửi"
                        : $"{tenTram} đã dỡ kiện từ Chuyến CX{chuyen.MaChuyen:D6} (trạm dừng trung gian) – Đã đến bưu cục phát, sẵn sàng giao người nhận")
                    : (don.TrangThai == TrangThaiDon.ChoChuyenHoan
                        ? $"{tenTram} đã dỡ kiện hoàn từ Chuyến CX{chuyen.MaChuyen:D6} (trạm dừng trung gian) – Nhập kho phân loại để chuyển hoàn tiếp"
                        : $"{tenTram} đã dỡ kiện từ Chuyến CX{chuyen.MaChuyen:D6} (trạm dừng trung gian) – Nhập kho phân loại để trung chuyển tiếp");

                GhiSuKien(don, trangThaiMoi, moTaDo, tt, maBuuCuc: maTram, pc: pc, quetMa: true);
                soDonDo++;
            }
            else
            {
                // Giữ nguyên trên xe cho chặng tiếp theo
                GhiSuKien(don, don.TrangThai,
                    $"Chuyến xe CX{chuyen.MaChuyen:D6} đang dừng ghé trạm trung gian {tenTram} (dỡ/bốc hàng) – Kiện hàng lưu chuyển tiếp tục trên xe",
                    tt, maBuuCuc: maTram);
                soDonGiu++;
            }
        }

        // 2. Bốc thêm hàng tại trạm dừng nếu có
        int soDonBoc = 0;
        if (maDonBocThem != null && maDonBocThem.Count > 0)
        {
            var dsDonBoc = await db.DonGiaoHangs
                .Include(d => d.PhanCongs)
                .Include(d => d.BuuCucGui)
                .Include(d => d.BuuCucNhan)
                .Where(d => maDonBocThem.Contains(d.MaDon) && d.MaChuyenXe == null)
                .ToListAsync();

            decimal kgConLaiTrenXe = chuyen.DonHangs.Where(d => d.MaChuyenXe == chuyen.MaChuyen && d.TrangThai != TrangThaiDon.DaHuy).Sum(d => d.KhoiLuong);
            decimal kgBocThem = dsDonBoc.Sum(d => d.KhoiLuong);
            decimal taiTrong = chuyen.PhuongTien?.TaiTrongToiDa ?? 0;
            if (taiTrong > 0 && (kgConLaiTrenXe + kgBocThem) > taiTrong)
                return KetQua.Loi($"Không thể bốc thêm: Tổng khối lượng trên xe ({kgConLaiTrenXe + kgBocThem:N1} kg) vượt quá tải trọng tối đa ({taiTrong:N1} kg)");

            int maPtChuan = chuyen.MaPhuongTien ?? chuyen.NhanVien?.MaPhuongTien ?? 0;

            foreach (var donMoi in dsDonBoc)
            {
                donMoi.MaChuyenXe = chuyen.MaChuyen;
                var pc = donMoi.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
                if (pc is null)
                {
                    pc = new PhanCongGiaoHang
                    {
                        MaDon = donMoi.MaDon,
                        MaNhanVien = chuyen.MaNhanVien,
                        MaPhuongTien = maPtChuan,
                        LoaiChang = LoaiShipper.LienTinh,
                        TrangThai = TrangThaiPhanCong.DangGiao,
                        NgayPhanCong = BayGio(),
                        NgayNhanHang = BayGio(),
                        NgayBatDauGiao = BayGio(),
                        GhiChu = $"Bốc thêm tại trạm dừng {tenTram} vào CX{chuyen.MaChuyen:D6}"
                    };
                    db.PhanCongGiaoHangs.Add(pc);
                }
                else
                {
                    pc.MaNhanVien = chuyen.MaNhanVien;
                    pc.MaPhuongTien = maPtChuan;
                    pc.TrangThai = TrangThaiPhanCong.DangGiao;
                    pc.NgayNhanHang ??= BayGio();
                    pc.NgayBatDauGiao ??= BayGio();
                }

                donMoi.TrangThai = TrangThaiDon.DangTrungChuyen;
                DatNguoiGiu(donMoi, null, chuyen.MaNhanVien);

                GhiSuKien(donMoi, TrangThaiDon.DangTrungChuyen,
                    $"Bốc thêm kiện tại trạm trung gian {tenTram} lên Chuyến xe CX{chuyen.MaChuyen:D6} tiếp tục hành trình đến {chuyen.BuuCucNhan?.TenBuuCuc}",
                    tt, maBuuCuc: maTram, pc: pc, quetMa: true);

                soDonBoc++;
            }
        }

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        return KetQua.Dat($"Chuyến xe CX{chuyen.MaChuyen:D6} đã ghé trạm {tenTram}: dỡ {soDonDo} kiện, bốc thêm {soDonBoc} kiện (trên xe hiện có {soDonGiu + soDonBoc} kiện)!");
    }

    /// <summary>
    /// Chuyến xe rời trạm trung gian, tiếp tục hành trình đến bưu cục đích cuối.
    /// </summary>
    public async Task<KetQua> RoiTramChuyenXeAsync(int maChuyen, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.PhanCongs)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangGheTram)
            return KetQua.Loi($"Chuyến xe đang ở trạng thái '{chuyen.TrangThai.TenHienThi()}', chỉ có thể rời trạm khi xe đang dừng ghé trạm");

        string tenTram = chuyen.BuuCucTrungGian?.TenBuuCuc ?? "Trạm trung gian";

        chuyen.TrangThai = TrangThaiChuyenXe.DangVanChuyen;
        chuyen.DaGheTram = true;
        chuyen.NgayRoiTram = BayGio();

        foreach (var don in chuyen.DonHangs)
        {
            var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            GhiSuKien(don, don.TrangThai,
                $"Chuyến xe CX{chuyen.MaChuyen:D6} đã rời trạm dừng {tenTram}, tiếp tục vận chuyển đến {chuyen.BuuCucNhan?.TenBuuCuc}",
                tt, maBuuCuc: chuyen.MaBuuCucTrungGian, pc: pc);
        }

        await db.SaveChangesAsync();
        return KetQua.Dat($"Chuyến xe CX{chuyen.MaChuyen:D6} đã rời trạm dừng {tenTram}, tiếp tục chặng 2 tới {chuyen.BuuCucNhan?.TenBuuCuc}!");
    }

    /// <summary>
    /// Bưu cục nhận tiếp nhận chuyến xe đến, dỡ hàng:
    /// - Nếu là Hub phân loại trung gian: dỡ hàng nhập kho Hub để phân luồng tiếp theo.
    /// - Nếu là Bưu cục đích cuối: chuyển DaDenBuuCucNhan để shipper khu vực phát hàng.
    /// </summary>
    public async Task<KetQua> TiepNhanChuyenXeAsync(int maChuyen, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.PhanCongs)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.BuuCucNhan)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangVanChuyen)
            return KetQua.Loi($"Chuyến xe đang ở trạng thái '{chuyen.TrangThai.TenHienThi()}'. Chỉ có thể tiếp nhận chuyến xe đang trong quá trình vận chuyển (đã xuất bến).");

        chuyen.TrangThai = TrangThaiChuyenXe.DaDenNoi;
        chuyen.NgayDenNoi = BayGio();

        bool buuCucTiepNhanLaHub = chuyen.BuuCucNhan != null && LaHubTrungTam(chuyen.BuuCucNhan);

        foreach (var don in chuyen.DonHangs)
        {
            var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pc != null)
            {
                pc.TrangThai = TrangThaiPhanCong.KetThuc;
                pc.KetQua = KetQuaGiao.DaBanGiaoBuuCuc;
                pc.NgayKetThuc = BayGio();
                pc.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.LienTinh, KetQuaGiao.DaBanGiaoBuuCuc, pc.NgayKetThuc);
            }

            int? maDichCuoi = don.TrangThai == TrangThaiDon.DangChuyenHoan ? don.MaBuuCucGui : don.MaBuuCucNhan;
            bool laDichCuoi = maDichCuoi == chuyen.MaBuuCucNhan;

            if (laDichCuoi)
            {
                // Đã đến bưu cục đích phát hàng
                TrangThaiDon trangThaiMoi = don.TrangThai == TrangThaiDon.DangChuyenHoan ? TrangThaiDon.DaVeBuuCucGui : TrangThaiDon.DaDenBuuCucNhan;
                don.TrangThai = trangThaiMoi;
                don.MaChuyenXe = null;
                DatNguoiGiu(don, chuyen.MaBuuCucNhan, null);

                string thongBaoDich = don.TrangThai == TrangThaiDon.DaVeBuuCucGui
                    ? $"{chuyen.BuuCucNhan?.TenBuuCuc} đã tiếp nhận Chuyến xe CX{chuyen.MaChuyen:D6} – Kiện hoàn đã về bưu cục gửi, sẵn sàng trả người gửi"
                    : $"{chuyen.BuuCucNhan?.TenBuuCuc} đã tiếp nhận Chuyến xe CX{chuyen.MaChuyen:D6} – Kiện hàng đã đến bưu cục đích, sẵn sàng giao người nhận";

                GhiSuKien(don, trangThaiMoi, thongBaoDich, tt, maBuuCuc: chuyen.MaBuuCucNhan, pc: pc, quetMa: true);
            }
            else
            {
                // Đến Hub trung tâm phân loại trung gian (ví dụ Hub Bắc Ninh, Hub Đà Nẵng, Hub TP.HCM)
                if (don.TrangThai == TrangThaiDon.DangChuyenHoan)
                {
                    don.TrangThai = TrangThaiDon.ChoChuyenHoan; // Chờ xếp xe chuyển hoàn chặng tiếp theo về bưu cục gửi
                    don.MaChuyenXe = null;
                    DatNguoiGiu(don, chuyen.MaBuuCucNhan, null);

                    GhiSuKien(don, TrangThaiDon.ChoChuyenHoan,
                        $"{chuyen.BuuCucNhan?.TenBuuCuc} (Hub Phân loại) đã dỡ kiện hoàn từ Chuyến CX{chuyen.MaChuyen:D6} – Đang phân luồng chuyển hoàn tiếp về {don.BuuCucGui?.TenBuuCuc}",
                        tt, maBuuCuc: chuyen.MaBuuCucNhan, pc: pc, quetMa: true);
                }
                else
                {
                    don.TrangThai = TrangThaiDon.DaNhanTaiBuuCucGui; // Lưu kho tại Hub, sẵn sàng xếp chuyến phân tỏa tiếp
                    don.MaChuyenXe = null;
                    DatNguoiGiu(don, chuyen.MaBuuCucNhan, null);

                    GhiSuKien(don, TrangThaiDon.DaNhanTaiBuuCucGui,
                        $"{chuyen.BuuCucNhan?.TenBuuCuc} (Hub Phân loại) đã dỡ kiện từ Chuyến CX{chuyen.MaChuyen:D6} – Đang phân luồng trung chuyển tiếp đến {don.BuuCucNhan?.TenBuuCuc}",
                        tt, maBuuCuc: chuyen.MaBuuCucNhan, pc: pc, quetMa: true);
                }
            }
        }

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        return KetQua.Dat($"Bưu cục {chuyen.BuuCucNhan?.TenBuuCuc} đã tiếp nhận thành công Chuyến xe CX{chuyen.MaChuyen:D6}!");
    }

    /// <summary>
    /// Hủy chuyến xe khi chưa xuất bến.
    /// </summary>
    public async Task<KetQua> HuyChuyenXeAsync(int maChuyen, string? lyDo, ThaoTac tt)
    {
        var chuyen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.BuuCucTrungGian)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .Include(c => c.DonHangs)
                .ThenInclude(d => d.PhanCongs)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyen);

        if (chuyen is null) return KetQua.Loi("Không tìm thấy chuyến xe");
        if (chuyen.TrangThai != TrangThaiChuyenXe.DangXepHang)
            return KetQua.Loi("Chỉ có thể hủy chuyến xe khi đang trong giai đoạn xếp hàng");

        chuyen.TrangThai = TrangThaiChuyenXe.DaHuy;
        chuyen.GhiChu = string.IsNullOrWhiteSpace(lyDo) ? chuyen.GhiChu : $"{chuyen.GhiChu} [Đã hủy: {lyDo}]";

        foreach (var don in chuyen.DonHangs)
        {
            don.MaChuyenXe = null;
            var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pc != null)
            {
                pc.TrangThai = TrangThaiPhanCong.DaThayDoi;
                pc.KetQua = KetQuaGiao.DaHuyDon;
                pc.NgayKetThuc = BayGio();
                pc.GhiChu = $"Hủy chuyến CX{chuyen.MaChuyen:D6}: {lyDo}";
            }

            if (don.TrangThai == TrangThaiDon.DaLayLienTinh)
            {
                don.TrangThai = TrangThaiDon.DaNhanTaiBuuCucGui;
            }
            DatNguoiGiu(don, chuyen.MaBuuCucGui, null);

            GhiSuKien(don, don.TrangThai,
                $"Chuyến xe CX{chuyen.MaChuyen:D6} bị hủy ({lyDo ?? "Kế hoạch thay đổi"}), kiện hàng lưu kho tại {chuyen.BuuCucGui?.TenBuuCuc}",
                tt, maBuuCuc: chuyen.MaBuuCucGui);
        }

        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([chuyen.MaNhanVien], DsPhuongTien(chuyen.MaPhuongTien));
        return KetQua.Dat($"Đã hủy thành công Chuyến xe CX{chuyen.MaChuyen:D6}");
    }

    /// <summary>
    /// Tạo chuyến xe lượt về (Return Trip / Shuttle Loop) cho xe tải bưu cục vệ tinh sau khi đã đến và trả hàng tại Hub.
    /// Chuyến xe sẽ xuất phát từ Hub quay về Bưu cục vệ tinh gốc, tự động xếp toàn bộ các đơn đang lưu kho tại Hub
    /// có đích phát là Bưu cục vệ tinh đó.
    /// </summary>
    public async Task<KetQua> TaoChuyenXeLuotVeAsync(int maChuyenDen, ThaoTac tt)
    {
        var chuyenDen = await db.ChuyenXes
            .Include(c => c.BuuCucGui)
            .Include(c => c.BuuCucNhan)
            .Include(c => c.NhanVien)
            .Include(c => c.PhuongTien)
            .FirstOrDefaultAsync(c => c.MaChuyen == maChuyenDen);

        if (chuyenDen is null)
            return KetQua.Loi("Không tìm thấy chuyến xe đến");

        if (chuyenDen.TrangThai != TrangThaiChuyenXe.DaDenNoi)
            return KetQua.Loi($"Chuyến xe {chuyenDen.MaHienThi} chưa hoàn tất tiếp nhận (đang ở trạng thái '{chuyenDen.TrangThai.TenHienThi()}'). Chỉ có thể lập chuyến lượt về sau khi xe đã đến nơi và dỡ hàng.");

        var hubNoiXeDangDau = chuyenDen.BuuCucNhan;
        var buuCucVeTinhGoc = chuyenDen.BuuCucGui;
        if (hubNoiXeDangDau is null || buuCucVeTinhGoc is null)
            return KetQua.Loi("Thông tin bưu cục của chuyến xe không hợp lệ");

        if (!LaHubTrungTam(hubNoiXeDangDau))
            return KetQua.Loi($"Bưu cục {hubNoiXeDangDau.TenBuuCuc} không phải là Hub trung tâm để lập chuyến phân tỏa lượt về");

        // Kiểm tra tài xế hoặc phương tiện có đang phụ trách chuyến xe khác chưa hoàn tất không
        bool taiXeDangBan = await db.ChuyenXes.AnyAsync(c => c.MaNhanVien == chuyenDen.MaNhanVien
            && c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy);
        if (taiXeDangBan)
            return KetQua.Loi($"Tài xế {chuyenDen.NhanVien?.HoTen} hiện đang phụ trách một chuyến xe khác đang hoạt động");

        // Tìm tất cả các đơn hàng đang lưu kho tại Hub này có đích phát là Bưu cục vệ tinh gốc
        var dsDonCho = await db.DonGiaoHangs
            .Where(d => d.MaChuyenXe == null
                && d.MaBuuCucHienTai == hubNoiXeDangDau.MaBuuCuc
                && (d.MaBuuCucNhan == buuCucVeTinhGoc.MaBuuCuc || (d.TrangThai == TrangThaiDon.ChoChuyenHoan && d.MaBuuCucGui == buuCucVeTinhGoc.MaBuuCuc))
                && d.TrangThai != TrangThaiDon.DaHuy && d.TrangThai != TrangThaiDon.DaHoanHang)
            .Select(d => d.MaDon)
            .ToListAsync();

        string ghiChu = $"Chuyến xe con thoi lượt về từ Hub {hubNoiXeDangDau.TenBuuCuc} ➔ {buuCucVeTinhGoc.TenBuuCuc} (Khứ hồi sau chuyến {chuyenDen.MaHienThi}) lúc {BayGio():dd/MM/yyyy HH:mm}";

        var kq = await TaoChuyenXeAsync(
            maBuuCucGui: hubNoiXeDangDau.MaBuuCuc,
            maBuuCucNhan: buuCucVeTinhGoc.MaBuuCuc,
            maNhanVien: chuyenDen.MaNhanVien,
            maPhuongTien: chuyenDen.MaPhuongTien,
            maDonHangs: dsDonCho,
            ghiChu: ghiChu,
            tt: tt);

        if (kq.ThanhCong)
        {
            return KetQua.Dat($"Đã lập thành công Chuyến xe lượt về từ Hub {hubNoiXeDangDau.TenBuuCuc} ➔ {buuCucVeTinhGoc.TenBuuCuc} (Tài xế: {chuyenDen.NhanVien?.HoTen}) – Đã tự động gom {dsDonCho.Count} kiện hàng vào chuyến xe!");
        }

        return kq;
    }
}
