
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public class DonHangXuLy(QuanLyGiaoNhanDbContext db)
{
    public const int SoDonToiDa = 8;
    
    public const int SoLanGiaoToiDa = 3;

    public Func<DateTime> BayGio { get; set; } = () => DateTime.Now;

    public async Task<KetQua<DonGiaoHang>> TaoDonAsync(DonGiaoHangFormVM vm, int maKhachHang, string nguoiThucHien)
    {
        var khachHang = await db.KhachHangs.FindAsync(maKhachHang);
        if (khachHang is null || khachHang.TrangThai != TrangThaiHoatDong.HoatDong)
            return KetQua<DonGiaoHang>.Loi("Khách hàng không tồn tại hoặc đã ngừng hoạt động");

        var (loaiHang, khuVuc, loi) = await LayDanhMucAsync(vm.MaLoaiHang, vm.MaKhuVuc, null);
        if (loi is not null) return KetQua<DonGiaoHang>.Loi(loi);

        var ngayTao = BayGio();
        if (vm.NgayGiaoDuKien.Date < ngayTao.Date)
            return KetQua<DonGiaoHang>.Loi("Ngày giao dự kiến không được trước ngày tạo đơn");
        if (await KiemTraKhoiLuongAsync(vm.KhoiLuong) is string loiKhoiLuong) return KetQua<DonGiaoHang>.Loi(loiKhoiLuong);

        var don = new DonGiaoHang
        {
            MaKhachHang = maKhachHang, MaLoaiHang = loaiHang!.MaLoaiHang, MaKhuVuc = khuVuc!.MaKhuVuc,
            TenNguoiNhan = vm.TenNguoiNhan.Trim(), SoDienThoaiNguoiNhan = vm.SoDienThoaiNguoiNhan.Trim(),
            DiaChiNhan = vm.DiaChiNhan.Trim(), KhoiLuong = vm.KhoiLuong, NgayTao = ngayTao,
            NgayGiaoDuKien = vm.NgayGiaoDuKien.Date, GhiChu = vm.GhiChu?.Trim(), TrangThai = TrangThaiDon.ChoPhanCong,
            TienThuHo = vm.TienThuHo, NguoiTraPhi = vm.NguoiTraPhi
        };
        var phi = ApDungPhi(don, khuVuc, loaiHang);
        don.LichSus.Add(TaoLichSu(null, TrangThaiDon.ChoPhanCong,
            $"Tạo đơn – phí vận chuyển {DinhDang.Tien(phi.Tong)} ({don.NguoiTraPhi.TenHienThi().ToLower()})" +
            (don.TienThuHo > 0 ? $", thu hộ {DinhDang.Tien(don.TienThuHo)}" : ""), nguoiThucHien));

        db.DonGiaoHangs.Add(don);
        await db.SaveChangesAsync();
        return KetQua<DonGiaoHang>.Dat(don, $"Đã tạo đơn {don.MaHienThi}");
    }

    public async Task<KetQua> CapNhatDonAsync(int maDon, DonGiaoHangFormVM vm, string nguoiThucHien, int? maKhachHangCuaToi)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
                                       .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null || (maKhachHangCuaToi.HasValue && don.MaKhachHang != maKhachHangCuaToi))
            return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!CoTheSua(don.TrangThai, maKhachHangCuaToi.HasValue))
            return KetQua.Loi(maKhachHangCuaToi.HasValue
                ? "Chỉ sửa được đơn trước khi nhân viên đến lấy hàng"
                : "Chỉ sửa được đơn trước khi nhân viên nhận hàng");

        var (loaiHang, khuVuc, loi) = await LayDanhMucAsync(vm.MaLoaiHang, vm.MaKhuVuc, don);
        if (loi is not null) return KetQua.Loi(loi);
        if (vm.NgayGiaoDuKien.Date < don.NgayTao.Date)
            return KetQua.Loi("Ngày giao dự kiến không được trước ngày tạo đơn");
        if (await KiemTraKhoiLuongAsync(vm.KhoiLuong) is string loiKhoiLuong) return KetQua.Loi(loiKhoiLuong);

        var phanCong = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (phanCong is not null && vm.KhoiLuong != don.KhoiLuong)
        {
            decimal dangCho = await KhoiLuongDangChoAsync(phanCong.MaPhuongTien, phanCong.MaPhanCong);
            if (dangCho + vm.KhoiLuong > phanCong.PhuongTien!.TaiTrongToiDa)
                return KetQua.Loi($"Khối lượng mới vượt tải trọng phương tiện {phanCong.PhuongTien.BienSo} " +
                                  $"(đang chở {DinhDang.KhoiLuong(dangCho)} / {DinhDang.KhoiLuong(phanCong.PhuongTien.TaiTrongToiDa)})");
        }

        decimal phiCu = don.PhiVanChuyen;
        don.MaLoaiHang = loaiHang!.MaLoaiHang;
        don.MaKhuVuc = khuVuc!.MaKhuVuc;
        don.TenNguoiNhan = vm.TenNguoiNhan.Trim();
        don.SoDienThoaiNguoiNhan = vm.SoDienThoaiNguoiNhan.Trim();
        don.DiaChiNhan = vm.DiaChiNhan.Trim();
        don.KhoiLuong = vm.KhoiLuong;
        don.NgayGiaoDuKien = vm.NgayGiaoDuKien.Date;
        don.GhiChu = vm.GhiChu?.Trim();
        decimal thuHoCu = don.TienThuHo;
        don.TienThuHo = vm.TienThuHo;
        don.NguoiTraPhi = vm.NguoiTraPhi;
        var phi = ApDungPhi(don, khuVuc, loaiHang);   

        string noiDung = phiCu == phi.Tong
            ? "Cập nhật thông tin đơn"
            : $"Cập nhật thông tin đơn – phí {DinhDang.Tien(phiCu)} → {DinhDang.Tien(phi.Tong)}";
        if (thuHoCu != don.TienThuHo) noiDung += $" – thu hộ {DinhDang.Tien(thuHoCu)} → {DinhDang.Tien(don.TienThuHo)}";
        db.LichSuGiaoNhans.Add(TaoLichSu(don.TrangThai, don.TrangThai, noiDung, nguoiThucHien, don.MaDon));
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã cập nhật đơn {don.MaHienThi}");
    }

    public async Task<KetQua> HuyDonAsync(int maDon, string? lyDo, string nguoiThucHien, int? maKhachHangCuaToi)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null || (maKhachHangCuaToi.HasValue && don.MaKhachHang != maKhachHangCuaToi))
            return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!CoTheHuy(don.TrangThai, maKhachHangCuaToi.HasValue))
            return KetQua.Loi(maKhachHangCuaToi.HasValue
                ? "Chỉ hủy được đơn trước khi nhân viên đến lấy hàng"
                : $"Không thể hủy đơn ở trạng thái '{don.TrangThai.TenHienThi()}'");

        var cu = don.TrangThai;
        var phanCong = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (phanCong is not null)
        {
            phanCong.TrangThai = TrangThaiPhanCong.KetThuc;
            phanCong.KetQua = KetQuaGiao.DaHuyDon;
            phanCong.NgayKetThuc = BayGio();
        }
        don.TrangThai = TrangThaiDon.DaHuy;
        db.LichSuGiaoNhans.Add(TaoLichSu(cu, TrangThaiDon.DaHuy,
            "Hủy đơn" + (string.IsNullOrWhiteSpace(lyDo) ? "" : $": {lyDo.Trim()}"), nguoiThucHien, don.MaDon));
        await db.SaveChangesAsync();
        if (phanCong is not null) await CapNhatNguonLucAsync([phanCong.MaNhanVien], [phanCong.MaPhuongTien]);
        return KetQua.Dat($"Đã hủy đơn {don.MaHienThi}");
    }

    public static bool CoTheSua(TrangThaiDon trangThai, bool laKhachHang) =>
        trangThai is TrangThaiDon.ChoPhanCong or TrangThaiDon.DaPhanCong;

    public static bool CoTheHuy(TrangThaiDon trangThai, bool laKhachHang) =>
        laKhachHang ? trangThai is TrangThaiDon.ChoPhanCong or TrangThaiDon.DaPhanCong
                    : trangThai is TrangThaiDon.ChoPhanCong or TrangThaiDon.DaPhanCong or TrangThaiDon.GiaoKhongThanhCong;

    public static bool CoThePhanCong(TrangThaiDon trangThai) =>
        trangThai is TrangThaiDon.ChoPhanCong or TrangThaiDon.GiaoKhongThanhCong;

    public static bool CoTheDoiPhanCong(TrangThaiDon trangThai) =>
        trangThai is TrangThaiDon.DaPhanCong or TrangThaiDon.DaNhanHang;

    public async Task<string?> KiemTraNhanVienAsync(int maNhanVien, int maPhuongTien, int? boQuaMaPhanCong = null)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
        if (nv is null) return "Nhân viên không tồn tại";
        if (nv.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong)
            return $"Nhân viên đang {nv.TrangThai.TenHienThi().ToLower()}";
        if (nv.TaiKhoan?.TrangThai == TrangThaiTaiKhoan.BiKhoa) return "Tài khoản của nhân viên đang bị khóa";

        var dangGiu = await PhanCongHieuLuc().Where(p => p.MaNhanVien == maNhanVien && p.MaPhanCong != boQuaMaPhanCong)
                                             .Select(p => new { p.MaPhuongTien, p.PhuongTien!.BienSo }).ToListAsync();
        if (dangGiu.Count >= SoDonToiDa) return $"Nhân viên đã giữ tối đa {SoDonToiDa} đơn đang hiệu lực";
        var xeKhac = dangGiu.FirstOrDefault(p => p.MaPhuongTien != maPhuongTien);
        if (xeKhac is not null) return $"Nhân viên đang dùng phương tiện khác ({xeKhac.BienSo})";
        return null;
    }

    public async Task<string?> KiemTraPhuongTienAsync(int maPhuongTien, int maNhanVien, decimal khoiLuongDon, int? boQuaMaPhanCong = null)
    {
        var pt = await db.PhuongTiens.FindAsync(maPhuongTien);
        if (pt is null) return "Phương tiện không tồn tại";
        if (pt.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong)
            return $"Phương tiện đang {pt.TrangThai.TenHienThi().ToLower()}";

        var nguoiKhac = await PhanCongHieuLuc()
            .Where(p => p.MaPhuongTien == maPhuongTien && p.MaNhanVien != maNhanVien && p.MaPhanCong != boQuaMaPhanCong)
            .Select(p => p.NhanVien!.HoTen).FirstOrDefaultAsync();
        if (nguoiKhac is not null) return $"Phương tiện đang được {nguoiKhac} sử dụng";

        decimal dangCho = await KhoiLuongDangChoAsync(maPhuongTien, boQuaMaPhanCong);
        if (dangCho + khoiLuongDon > pt.TaiTrongToiDa)
            return $"Quá tải: đang chở {DinhDang.KhoiLuong(dangCho)} + đơn {DinhDang.KhoiLuong(khoiLuongDon)} > tải trọng {DinhDang.KhoiLuong(pt.TaiTrongToiDa)}";
        return null;
    }

    public async Task<(List<LuaChonNhanVien>, List<LuaChonPhuongTien>)> LayLuaChonAsync(DonGiaoHang don, int? boQuaMaPhanCong)
    {
        var hieuLuc = await PhanCongHieuLuc().Where(p => p.MaPhanCong != boQuaMaPhanCong)
            .Select(p => new { p.MaNhanVien, p.MaPhuongTien, p.DonGiaoHang!.KhoiLuong, TenNv = p.NhanVien!.HoTen })
            .ToListAsync();

        var dsNhanVien = await db.NhanVienGiaoHangs.AsNoTracking().Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan)
                                 .OrderBy(n => n.HoTen).ToListAsync();
        var luaChonNv = dsNhanVien.Select(n =>
        {
            int soDon = hieuLuc.Count(h => h.MaNhanVien == n.MaNhanVien);
            string? lyDo = n.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong ? n.TrangThai.TenHienThi()
                         : n.TaiKhoan?.TrangThai == TrangThaiTaiKhoan.BiKhoa ? "Tài khoản bị khóa"
                         : soDon >= SoDonToiDa ? $"Đã giữ {soDon} đơn" : null;
            return new LuaChonNhanVien(n.MaNhanVien, n.HoTen, n.KhuVucPhuTrach?.TenKhuVuc, n.MaKhuVucPhuTrach == don.MaKhuVuc,
                                       n.TrangThai, soDon, lyDo is null, lyDo);
        }).OrderByDescending(x => x.DuocChon).ThenByDescending(x => x.CungKhuVuc).ThenBy(x => x.SoDonDangGiu).ToList();

        var dsPhuongTien = await db.PhuongTiens.AsNoTracking().OrderBy(p => p.TaiTrongToiDa).ToListAsync();
        var luaChonPt = dsPhuongTien.Select(p =>
        {
            var tren = hieuLuc.Where(h => h.MaPhuongTien == p.MaPhuongTien).ToList();
            decimal dangCho = tren.Sum(h => h.KhoiLuong);
            string? nguoiDung = tren.Select(h => h.TenNv).FirstOrDefault();
            string? lyDo = p.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong ? p.TrangThai.TenHienThi()
                         : dangCho + don.KhoiLuong > p.TaiTrongToiDa ? "Không đủ tải trọng" : null;
            return new LuaChonPhuongTien(p.MaPhuongTien, p.BienSo, p.LoaiPhuongTien, p.TaiTrongToiDa, dangCho, nguoiDung,
                                         p.TrangThai, lyDo is null, lyDo);
        }).OrderByDescending(x => x.DuocChon).ThenBy(x => x.TaiTrong).ToList();

        return (luaChonNv, luaChonPt);
    }

    public async Task<KetQua> PhanCongAsync(int maDon, int maNhanVien, int maPhuongTien, string? ghiChu, string nguoiThucHien)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!CoThePhanCong(don.TrangThai))
            return KetQua.Loi($"Không thể phân công đơn ở trạng thái '{don.TrangThai.TenHienThi()}' " +
                              "(chỉ đơn Chờ phân công hoặc giao lại đơn Giao không thành công)");
        if (don.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)))
            return KetQua.Loi("Đơn đã có một phân công đang hiệu lực");

        var loi = await KiemTraNhanVienAsync(maNhanVien, maPhuongTien) ?? await KiemTraPhuongTienAsync(maPhuongTien, maNhanVien, don.KhoiLuong);
        if (loi is not null) return KetQua.Loi(loi);

        var nv = await db.NhanVienGiaoHangs.FindAsync(maNhanVien);
        var pt = await db.PhuongTiens.FindAsync(maPhuongTien);
        bool giaoLai = don.TrangThai == TrangThaiDon.GiaoKhongThanhCong;
        int lanGiao = SoLanThatBai(don) + 1;
        if (lanGiao > SoLanGiaoToiDa)
            return KetQua.Loi($"Đơn đã giao không thành công {SoLanGiaoToiDa} lần – không giao lại nữa, hãy liên hệ khách hàng và hủy / chuyển hoàn đơn");

        db.PhanCongGiaoHangs.Add(new PhanCongGiaoHang
        {
            MaDon = maDon, MaNhanVien = maNhanVien, MaPhuongTien = maPhuongTien, NgayPhanCong = BayGio(),
            TrangThai = TrangThaiPhanCong.DaPhanCong, GhiChu = ghiChu?.Trim(), NguoiPhanCong = nguoiThucHien
        });
        var cu = don.TrangThai;
        don.TrangThai = TrangThaiDon.DaPhanCong;
        db.LichSuGiaoNhans.Add(TaoLichSu(cu, TrangThaiDon.DaPhanCong,
            $"{(giaoLai ? $"Phân công giao lại (lần {lanGiao})" : "Phân công")}: {nv!.HoTen} – {pt!.LoaiPhuongTien} {pt.BienSo}",
            nguoiThucHien, maDon));
        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([maNhanVien], [maPhuongTien]);
        return KetQua.Dat($"Đã phân công đơn {don.MaHienThi} cho {nv.HoTen}");
    }

    public async Task<KetQua> DoiPhanCongAsync(int maDon, int maNhanVien, int maPhuongTien, string? lyDo, string nguoiThucHien)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                                       .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!CoTheDoiPhanCong(don.TrangThai))
            return KetQua.Loi($"Không thể đổi phân công khi đơn '{don.TrangThai.TenHienThi()}'");
        var cu = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (cu is null) return KetQua.Loi("Đơn chưa có phân công đang hiệu lực");
        if (cu.MaNhanVien == maNhanVien && cu.MaPhuongTien == maPhuongTien)
            return KetQua.Loi("Nhân viên và phương tiện mới trùng với phân công hiện tại");
        if (string.IsNullOrWhiteSpace(lyDo)) return KetQua.Loi("Nhập lý do đổi phân công");

        var loi = await KiemTraNhanVienAsync(maNhanVien, maPhuongTien, cu.MaPhanCong)
                  ?? await KiemTraPhuongTienAsync(maPhuongTien, maNhanVien, don.KhoiLuong, cu.MaPhanCong);
        if (loi is not null) return KetQua.Loi(loi);

        var bayGio = BayGio();
        cu.TrangThai = TrangThaiPhanCong.DaThayDoi;
        cu.NgayKetThuc = bayGio;
        cu.GhiChu = string.Join(" | ", new[] { cu.GhiChu, $"Đổi phân công: {lyDo.Trim()}" }.Where(s => !string.IsNullOrEmpty(s)));

        var nv = await db.NhanVienGiaoHangs.FindAsync(maNhanVien);
        var pt = await db.PhuongTiens.FindAsync(maPhuongTien);
        db.PhanCongGiaoHangs.Add(new PhanCongGiaoHang
        {
            MaDon = maDon, MaNhanVien = maNhanVien, MaPhuongTien = maPhuongTien, NgayPhanCong = bayGio,
            TrangThai = TrangThaiPhanCong.DaPhanCong, GhiChu = $"Thay cho phân công #{cu.MaPhanCong}", NguoiPhanCong = nguoiThucHien
        });
        var trangThaiCu = don.TrangThai;
        don.TrangThai = TrangThaiDon.DaPhanCong;
        db.LichSuGiaoNhans.Add(TaoLichSu(trangThaiCu, TrangThaiDon.DaPhanCong,
            $"Đổi phân công: {cu.NhanVien!.HoTen} → {nv!.HoTen} – {pt!.LoaiPhuongTien} {pt.BienSo}. Lý do: {lyDo.Trim()}",
            nguoiThucHien, maDon));
        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([cu.MaNhanVien, maNhanVien], [cu.MaPhuongTien, maPhuongTien]);
        return KetQua.Dat($"Đã đổi phân công đơn {don.MaHienThi} sang {nv.HoTen}");
    }

    public async Task<KetQua> NhanHangAsync(int maPhanCong, int maNhanVien, string nguoiThucHien)
    {
        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        if (pc.NgayNhanHang is not null || pc.TrangThai is TrangThaiPhanCong.DaNhanHang or TrangThaiPhanCong.DangGiao)
            return KetQua.Loi("Đơn đã được xác nhận nhận hàng trước đó");
        if (pc.TrangThai != TrangThaiPhanCong.DaPhanCong || pc.DonGiaoHang!.TrangThai != TrangThaiDon.DaPhanCong)
            return KetQua.Loi("Chỉ đơn Đã phân công mới được xác nhận nhận hàng");

        pc.NgayNhanHang = BayGio();
        pc.TrangThai = TrangThaiPhanCong.DaNhanHang;
        return await DoiTrangThaiDonAsync(pc.DonGiaoHang, TrangThaiDon.DaNhanHang, "Nhân viên đã nhận hàng", nguoiThucHien, pc);
    }

    public async Task<KetQua> BatDauGiaoAsync(int maPhanCong, int maNhanVien, string nguoiThucHien)
    {
        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        if (pc.TrangThai != TrangThaiPhanCong.DaNhanHang || pc.DonGiaoHang!.TrangThai != TrangThaiDon.DaNhanHang)
            return KetQua.Loi("Chỉ đơn Đã nhận hàng mới được bắt đầu giao");

        pc.NgayBatDauGiao = BayGio();
        pc.TrangThai = TrangThaiPhanCong.DangGiao;
        return await DoiTrangThaiDonAsync(pc.DonGiaoHang, TrangThaiDon.DangGiao, "Bắt đầu giao hàng", nguoiThucHien, pc);
    }

    public async Task<KetQua> GiaoThanhCongAsync(int maPhanCong, int maNhanVien, string? ghiChu, string nguoiThucHien)
    {
        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        if (pc.TrangThai != TrangThaiPhanCong.DangGiao || pc.DonGiaoHang!.TrangThai != TrangThaiDon.DangGiao)
            return KetQua.Loi("Chỉ đơn Đang giao mới được xác nhận giao thành công");

        pc.NgayKetThuc = BayGio();
        pc.TrangThai = TrangThaiPhanCong.KetThuc;
        pc.KetQua = KetQuaGiao.GiaoThanhCong;
        if (!string.IsNullOrWhiteSpace(ghiChu)) pc.GhiChu = ghiChu.Trim();
        var donGiao = pc.DonGiaoHang;
        string daThu = donGiao.TongThuNguoiNhan > 0 ? $" – đã thu người nhận {DinhDang.Tien(donGiao.TongThuNguoiNhan)}" : "";
        return await DoiTrangThaiDonAsync(donGiao, TrangThaiDon.GiaoThanhCong,
            "Giao hàng thành công" + daThu + (string.IsNullOrWhiteSpace(ghiChu) ? "" : $" – {ghiChu.Trim()}"), nguoiThucHien, pc);
    }

    public async Task<KetQua> GiaoThatBaiAsync(int maPhanCong, int maNhanVien, LyDoThatBai? lyDo, string? chiTiet, string nguoiThucHien)
    {
        if (lyDo is null) return KetQua.Loi("Bắt buộc chọn lý do giao không thành công");
        if (lyDo == LyDoThatBai.Khac && string.IsNullOrWhiteSpace(chiTiet))
            return KetQua.Loi("Vui lòng mô tả lý do khi chọn 'Lý do khác'");

        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        if (pc.TrangThai != TrangThaiPhanCong.DangGiao || pc.DonGiaoHang!.TrangThai != TrangThaiDon.DangGiao)
            return KetQua.Loi("Chỉ đơn Đang giao mới được ghi nhận giao không thành công");

        pc.NgayKetThuc = BayGio();
        pc.TrangThai = TrangThaiPhanCong.KetThuc;
        pc.KetQua = KetQuaGiao.GiaoKhongThanhCong;
        pc.LyDoThatBai = lyDo;
        pc.GhiChu = chiTiet?.Trim();
        string moTa = lyDo.Value.TenHienThi() + (string.IsNullOrWhiteSpace(chiTiet) ? "" : $" – {chiTiet.Trim()}");
        return await DoiTrangThaiDonAsync(pc.DonGiaoHang, TrangThaiDon.GiaoKhongThanhCong,
            $"Giao không thành công. Lý do: {moTa}", nguoiThucHien, pc);
    }

    public async Task<KetQua> HoanTatAsync(int maDon, string nguoiThucHien, bool tuGiaoDichVi = false)
    {
        var don = await db.DonGiaoHangs.Include(d => d.GiaoDichNop).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai != TrangThaiDon.GiaoThanhCong)
            return KetQua.Loi($"Chỉ đơn Giao thành công mới được hoàn tất (đơn đang '{don.TrangThai.TenHienThi()}')");
        
        if (!tuGiaoDichVi && don.GiaoDichNop is { TrangThai: TrangThaiGiaoDich.ChoXacNhan } gd)
            return KetQua.Loi($"Tiền của đơn nằm trong lệnh nộp {gd.MaHienThi} đang chờ xác nhận – hãy xác nhận ở mục Ví shipper");

        don.NgayHoanTat = BayGio();
        string noiDung = "Xác nhận hoàn tất đơn";
        if (don.TienThuHo > 0 || don.NguoiTraPhi == NguoiTraPhi.NguoiNhan)
        {
            don.NgayDoiSoat = don.NgayHoanTat;
            noiDung += don.TienTraNguoiGui >= 0
                ? $" – đối soát: trả người gửi {DinhDang.Tien(don.TienTraNguoiGui)}"
                : $" – đối soát: người gửi thanh toán phí {DinhDang.Tien(-don.TienTraNguoiGui)}";
        }
        return await DoiTrangThaiDonAsync(don, TrangThaiDon.HoanTat, noiDung, nguoiThucHien, null);
    }

    public static IQueryable<DonGiaoHang> LocDonHang(IQueryable<DonGiaoHang> truyVan, BoLocDonHang boLoc)
    {
        if (!string.IsNullOrWhiteSpace(boLoc.TuKhoa))
        {
            var tuKhoa = boLoc.TuKhoa.Trim();
            int? maDon = DinhDang.TachMaDon(tuKhoa);
            
            string soDienThoai = new(tuKhoa.Where(char.IsDigit).ToArray());
            truyVan = truyVan.Where(d => (maDon != null && d.MaDon == maDon)
                                      || EF.Functions.Collate(d.TenNguoiNhan, "Latin1_General_100_CI_AI").Contains(tuKhoa)
                                      || (soDienThoai.Length >= 3 && d.SoDienThoaiNguoiNhan.Contains(soDienThoai))
                                      || EF.Functions.Collate(d.KhachHang!.HoTen, "Latin1_General_100_CI_AI").Contains(tuKhoa));
        }
        if (boLoc.MaLoaiHang.HasValue) truyVan = truyVan.Where(d => d.MaLoaiHang == boLoc.MaLoaiHang);
        if (boLoc.MaKhuVuc.HasValue) truyVan = truyVan.Where(d => d.MaKhuVuc == boLoc.MaKhuVuc);
        if (boLoc.LoaiNgay == "giao")
        {
            if (boLoc.TuNgay.HasValue) truyVan = truyVan.Where(d => d.NgayGiaoDuKien >= boLoc.TuNgay.Value.Date);
            if (boLoc.DenNgay.HasValue) truyVan = truyVan.Where(d => d.NgayGiaoDuKien <= boLoc.DenNgay.Value.Date);
        }
        else
        {
            if (boLoc.TuNgay.HasValue) truyVan = truyVan.Where(d => d.NgayTao >= boLoc.TuNgay.Value.Date);
            if (boLoc.DenNgay.HasValue) truyVan = truyVan.Where(d => d.NgayTao < boLoc.DenNgay.Value.Date.AddDays(1));
        }
        return truyVan;
    }

    public static IQueryable<DonGiaoHang> SapXepDonHang(IQueryable<DonGiaoHang> truyVan, string? sapXep) => sapXep switch
    {
        "ngay_cu" => truyVan.OrderBy(d => d.NgayTao),
        "kl_tang" => truyVan.OrderBy(d => d.KhoiLuong).ThenByDescending(d => d.NgayTao),
        "kl_giam" => truyVan.OrderByDescending(d => d.KhoiLuong).ThenByDescending(d => d.NgayTao),
        "phi_tang" => truyVan.OrderBy(d => d.PhiVanChuyen).ThenByDescending(d => d.NgayTao),
        "phi_giam" => truyVan.OrderByDescending(d => d.PhiVanChuyen).ThenByDescending(d => d.NgayTao),
        "ten_az" => truyVan.OrderBy(d => d.TenNguoiNhan).ThenByDescending(d => d.NgayTao),
        "ten_za" => truyVan.OrderByDescending(d => d.TenNguoiNhan).ThenByDescending(d => d.NgayTao),
        _ => truyVan.OrderByDescending(d => d.NgayTao)
    };

    public Task<DonGiaoHang?> LayChiTietDonAsync(int maDon) =>
        db.DonGiaoHangs.AsNoTracking()
          .Include(d => d.KhachHang).Include(d => d.LoaiHang).Include(d => d.KhuVuc)
          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien!).ThenInclude(n => n.TaiKhoan)
          .Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
          .Include(d => d.LichSus)
          .AsSplitQuery()
          .FirstOrDefaultAsync(d => d.MaDon == maDon);

    public async Task<TongQuanKhachHangVM> TongQuanKhachHangAsync(KhachHang kh, int soDonGanDay)
    {
        var donCuaKhach = db.DonGiaoHangs.AsNoTracking().Where(d => d.MaKhachHang == kh.MaKhachHang);
        TrangThaiDon[] dangXuLy = [TrangThaiDon.ChoPhanCong, TrangThaiDon.DaPhanCong, TrangThaiDon.DaNhanHang, TrangThaiDon.DangGiao];
        return new TongQuanKhachHangVM
        {
            KhachHang = kh,
            TongDon = await donCuaKhach.CountAsync(),
            DangXuLy = await donCuaKhach.CountAsync(d => dangXuLy.Contains(d.TrangThai)),
            DaGiao = await donCuaKhach.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat),
            ThatBai = await donCuaKhach.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoKhongThanhCong),
            TongPhi = await donCuaKhach.Where(d => d.TrangThai != TrangThaiDon.DaHuy).SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0,
            DonGanDay = await donCuaKhach.Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                                         .OrderByDescending(d => d.NgayTao).Take(soDonGanDay).ToListAsync(),
            DonDangVanChuyen = await donCuaKhach.Include(d => d.KhuVuc)
                                         .Where(d => dangXuLy.Contains(d.TrangThai) || d.TrangThai == TrangThaiDon.GiaoKhongThanhCong)
                                         .OrderByDescending(d => d.NgayTao).Take(5).ToListAsync(),
            SoTheoTrangThai = await donCuaKhach.GroupBy(d => d.TrangThai).ToDictionaryAsync(g => g.Key, g => g.Count()),
            TheoThang = await PhiTheoThangAsync(donCuaKhach, 6)
        };
    }

    private static async Task<List<(string Thang, int SoDon, decimal Phi)>> PhiTheoThangAsync(IQueryable<DonGiaoHang> truyVan, int n)
    {
        var dau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(n - 1));
        var nhom = await truyVan.Where(d => d.NgayTao >= dau && d.TrangThai != TrangThaiDon.DaHuy)
            .GroupBy(d => new { d.NgayTao.Year, d.NgayTao.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, SoDon = g.Count(), Phi = g.Sum(d => d.PhiVanChuyen) })
            .ToListAsync();
        return Enumerable.Range(0, n).Select(i => dau.AddMonths(i)).Select(t =>
        {
            var x = nhom.FirstOrDefault(g => g.Year == t.Year && g.Month == t.Month);
            return ($"T{t.Month}", x?.SoDon ?? 0, x?.Phi ?? 0m);
        }).ToList();
    }

    public async Task<(DonGiaoHang? Don, string? Loi)> TraCuuAsync(string? ma, string? sdt)
    {
        int? maDon = DinhDang.TachMaDon(ma);
        if (maDon is null || string.IsNullOrWhiteSpace(sdt))
            return (null, "Nhập đúng mã đơn (VD: DH000012) và số điện thoại người nhận");
        string soDienThoai = new(sdt.Where(char.IsDigit).ToArray());
        var don = await db.DonGiaoHangs.AsNoTracking()
            .Include(d => d.KhuVuc).Include(d => d.LoaiHang).Include(d => d.LichSus)
            .FirstOrDefaultAsync(d => d.MaDon == maDon && d.SoDienThoaiNguoiNhan == soDienThoai);
        return don is null ? (null, "Không tìm thấy đơn khớp mã đơn và số điện thoại đã nhập") : (don, null);
    }

    public async Task<BangGiaVM> BangGiaAsync(bool laKhachHang) => new(
        await db.KhuVucs.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(k => k.PhiCoBan).ToListAsync(),
        await db.LoaiHangs.AsNoTracking().Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(l => l.HeSoPhuThu).ToListAsync(),
        await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong).MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0,
        laKhachHang);

    public static int SoLanThatBai(DonGiaoHang don) => don.PhanCongs.Count(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);

    private async Task<string?> KiemTraKhoiLuongAsync(decimal khoiLuong)
    {
        decimal lonNhat = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                              .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        return khoiLuong > lonNhat
            ? $"Khối lượng {DinhDang.KhoiLuong(khoiLuong)} vượt tải trọng phương tiện lớn nhất ({DinhDang.KhoiLuong(lonNhat)}) – vui lòng chia thành nhiều đơn"
            : null;
    }

    public IQueryable<PhanCongGiaoHang> PhanCongHieuLuc() =>
        db.PhanCongGiaoHangs.Where(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));

    public async Task<decimal> KhoiLuongDangChoAsync(int maPhuongTien, int? boQuaMaPhanCong) =>
        await PhanCongHieuLuc().Where(p => p.MaPhuongTien == maPhuongTien && p.MaPhanCong != boQuaMaPhanCong)
                               .SumAsync(p => (decimal?)p.DonGiaoHang!.KhoiLuong) ?? 0;

    public async Task CapNhatNguonLucAsync(IEnumerable<int> dsMaNhanVien, IEnumerable<int> dsMaPhuongTien)
    {
        foreach (var ma in dsMaNhanVien.Distinct())
        {
            var nv = await db.NhanVienGiaoHangs.FindAsync(ma);
            if (nv is null || nv.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong) continue;
            bool dangGiao = await db.PhanCongGiaoHangs.AnyAsync(p => p.MaNhanVien == ma && p.TrangThai == TrangThaiPhanCong.DangGiao);
            nv.TrangThai = dangGiao ? TrangThaiNhanVien.DangGiaoHang : TrangThaiNhanVien.SanSang;
        }
        foreach (var ma in dsMaPhuongTien.Distinct())
        {
            var pt = await db.PhuongTiens.FindAsync(ma);
            if (pt is null || pt.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong) continue;
            bool dangDung = await PhanCongHieuLuc().AnyAsync(p => p.MaPhuongTien == ma);
            pt.TrangThai = dangDung ? TrangThaiPhuongTien.DangSuDung : TrangThaiPhuongTien.SanSang;
        }
        await db.SaveChangesAsync();
    }

    private async Task<(PhanCongGiaoHang?, string?)> LayPhanCongCuaNhanVienAsync(int maPhanCong, int maNhanVien)
    {
        var pc = await db.PhanCongGiaoHangs.Include(p => p.DonGiaoHang).FirstOrDefaultAsync(p => p.MaPhanCong == maPhanCong);
        if (pc is null) return (null, "Không tìm thấy phân công");
        if (pc.MaNhanVien != maNhanVien) return (null, "Bạn không được phân công đơn này");
        if (!PhanCongGiaoHang.TrangThaiHieuLuc.Contains(pc.TrangThai)) return (null, "Phân công này đã kết thúc");
        return (pc, null);
    }

    private async Task<KetQua> DoiTrangThaiDonAsync(DonGiaoHang don, TrangThaiDon moi, string noiDung, string nguoi, PhanCongGiaoHang? pc)
    {
        var cu = don.TrangThai;
        don.TrangThai = moi;
        db.LichSuGiaoNhans.Add(TaoLichSu(cu, moi, noiDung, nguoi, don.MaDon));
        await db.SaveChangesAsync();
        if (pc is not null) await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
        return KetQua.Dat($"{don.MaHienThi}: {moi.TenHienThi()}");
    }

    private LichSuGiaoNhan TaoLichSu(TrangThaiDon? cu, TrangThaiDon moi, string noiDung, string nguoi, int maDon = 0) =>
        new() { MaDon = maDon, ThoiGian = BayGio(), TrangThaiCu = cu, TrangThaiMoi = moi, NoiDung = noiDung, NguoiThucHien = nguoi };

    private static KetQuaTinhPhi ApDungPhi(DonGiaoHang don, KhuVuc khuVuc, LoaiHang loaiHang)
    {
        var phi = TinhPhiXuLy.TinhPhi(khuVuc.PhiCoBan, don.KhoiLuong, loaiHang.HeSoPhuThu);
        don.PhiCoBan = phi.PhiCoBan;
        don.PhuPhiKhoiLuong = phi.PhuPhiKhoiLuong;
        don.PhuPhiLoaiHang = phi.PhuPhiLoaiHang;
        don.PhiVanChuyen = phi.Tong;
        return phi;
    }

    private async Task<(LoaiHang?, KhuVuc?, string?)> LayDanhMucAsync(int maLoaiHang, int maKhuVuc, DonGiaoHang? donCu)
    {
        var loaiHang = await db.LoaiHangs.FindAsync(maLoaiHang);
        if (loaiHang is null) return (null, null, "Loại hàng không tồn tại");
        if (loaiHang.TrangThai != TrangThaiHoatDong.HoatDong && donCu?.MaLoaiHang != maLoaiHang)
            return (null, null, $"Loại hàng '{loaiHang.TenLoaiHang}' đã ngừng hoạt động");
        var khuVuc = await db.KhuVucs.FindAsync(maKhuVuc);
        if (khuVuc is null) return (null, null, "Khu vực giao không tồn tại");
        if (khuVuc.TrangThai != TrangThaiHoatDong.HoatDong && donCu?.MaKhuVuc != maKhuVuc)
            return (null, null, $"Khu vực '{khuVuc.TenKhuVuc}' đã ngừng nhận đơn mới");
        return (loaiHang, khuVuc, null);
    }
}
