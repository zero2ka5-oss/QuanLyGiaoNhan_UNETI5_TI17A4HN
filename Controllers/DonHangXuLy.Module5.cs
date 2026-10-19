// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Tính phí khi lưu đơn, Hoàn tất & đối soát, ghi Lịch sử giao nhận, Sắp xếp danh sách đơn, công nợ phí, tra cứu đơn, bảng giá.

using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của DonHangXuLy (partial).</summary>
public partial class DonHangXuLy
{
    /// <summary>Bước Sắp xếp: ngày tạo, khối lượng, phí, tên người nhận (mặc định: mới nhất trước).</summary>
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

    /// <summary>
    /// Ghi một Tracking Event và đổi trạng thái đơn. Đơn mới (chưa có MaDon) gắn qua navigation để lưu cùng một lần SaveChanges.
    /// </summary>
    private void GhiSuKien(DonGiaoHang don, TrangThaiDon moi, string noiDung, ThaoTac tt,
                           int? maBuuCuc = null, PhanCongGiaoHang? pc = null, bool quetMa = false)
    {
        var suKien = new LichSuGiaoNhan
        {
            MaDon = don.MaDon, ThoiGian = BayGio(), TrangThaiCu = don.MaDon == 0 ? null : don.TrangThai, TrangThaiMoi = moi,
            NoiDung = DinhDang.Cat(noiDung, DoDaiGhiChu) ?? "", NguoiThucHien = tt.NguoiThucHien,
            MaBuuCuc = maBuuCuc, MaNhanVien = pc?.MaNhanVien, MaPhuongTien = pc?.MaPhuongTien,
            ViDo = tt.ViDo, KinhDo = tt.KinhDo, QuetMa = quetMa
        };
        if (don.MaDon == 0) don.LichSus.Add(suKien); else db.LichSuGiaoNhans.Add(suKien);
        BaoTheoSuKien(don, don.TrangThai, moi, noiDung, tt, pc);   // thông báo cho khách / shipper / bưu cục (cùng giao tác)
        don.TrangThai = moi;
    }

    private static KetQuaTinhPhi ApDungPhi(DonGiaoHang don, KhuVuc khuVuc, LoaiHang loaiHang, BuuCuc? buuCucGui = null, BuuCuc? buuCucNhan = null)
    {
        decimal phiCoBan = khuVuc.PhiCoBan;
        int soNgayGiao = 2;
        if (buuCucGui?.ViDo != null && buuCucGui?.KinhDo != null && buuCucNhan?.ViDo != null && buuCucNhan?.KinhDo != null)
        {
            double km = TinhPhiXuLy.TinhKhoangCachKm(buuCucGui.ViDo.Value, buuCucGui.KinhDo.Value, buuCucNhan.ViDo.Value, buuCucNhan.KinhDo.Value);
            var info = TinhPhiXuLy.LayPhiCoBanTheoKhoangCach(km, khuVuc.PhiCoBan);
            phiCoBan = info.PhiCoBan;
            soNgayGiao = info.SoNgay;
        }
        var phi = TinhPhiXuLy.TinhPhi(phiCoBan, don.KhoiLuong, don.KichThuocM3, loaiHang.HeSoPhuThu, soNgayGiao);
        don.PhiCoBan = phi.PhiCoBan;
        don.PhuPhiKhoiLuong = phi.PhuPhiKhoiLuong;
        don.PhuPhiKichThuoc = phi.PhuPhiKichThuoc;
        don.PhuPhiLoaiHang = phi.PhuPhiLoaiHang;
        don.PhiVanChuyen = phi.Tong;
        return phi;
    }

    /// <summary>
    /// Hoàn tất = đối soát: shipper đã nộp tiền thu của người nhận; hệ thống trả tiền thu hộ cho người gửi
    /// (nếu người gửi trả phí thì phí ship được trừ vào tiền thu hộ; trừ không đủ thì phần thiếu thành công nợ của người gửi).
    /// </summary>
    /// <param name="tuGiaoDichVi">true khi gọi từ việc xác nhận lệnh nộp tiền trên ví shipper.</param>
    public async Task<KetQua> HoanTatAsync(int maDon, string nguoiThucHien, bool tuGiaoDichVi = false)
    {
        var don = await db.DonGiaoHangs.Include(d => d.GiaoDichNop).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai != TrangThaiDon.GiaoThanhCong)
            return KetQua.Loi($"Chỉ đơn Giao thành công mới được hoàn tất (đơn đang '{don.TrangThai.TenHienThi()}')");
        // Tiền của đơn đã nằm trong lệnh nộp của shipper → xác nhận ở trang Ví shipper để không đối soát 2 lần
        if (!tuGiaoDichVi && don.GiaoDichNop is { TrangThai: TrangThaiGiaoDich.ChoXacNhan } gd)
            return KetQua.Loi($"Tiền của đơn nằm trong lệnh nộp {gd.MaHienThi} đang chờ xác nhận – hãy xác nhận ở mục Ví shipper");
        // Shipper đang cầm tiền của người nhận → chỉ hoàn tất qua lệnh nộp, nếu không tiền sẽ mất khỏi ví shipper mà chưa ai thu
        if (!tuGiaoDichVi && don.TongThuNguoiNhan > 0)
            return KetQua.Loi($"Shipper đã thu {DinhDang.Tien(don.TongThuNguoiNhan)} của người nhận – đơn được đối soát & hoàn tất khi xác nhận lệnh nộp tiền ở mục Ví shipper");

        don.NgayHoanTat = BayGio();
        // Mọi đơn hoàn tất đều được đối soát: trả tiền thu hộ (sau khi trừ phí) và / hoặc ghi công nợ phí người gửi
        don.NgayDoiSoat = don.NgayHoanTat;
        var dsDoiSoat = new List<string>();
        if (don.TienTraNguoiGui > 0)
        {
            var khach = await db.KhachHangs.FindAsync(don.MaKhachHang);
            if (khach != null)
            {
                khach.SoDuVi += don.TienTraNguoiGui;
                var gdVi = new GiaoDichViKhachHang
                {
                    MaKhachHang = don.MaKhachHang,
                    Loai = LoaiGiaoDichKhach.CongTienCOD,
                    SoTien = don.TienTraNguoiGui,
                    SoDuSauGiaoDich = khach.SoDuVi,
                    MaDonHang = don.MaDon,
                    TrangThai = TrangThaiGiaoDich.DaXacNhan,
                    NgayTao = don.NgayHoanTat.Value,
                    NgayXuLy = don.NgayHoanTat.Value,
                    NguoiXuLy = nguoiThucHien,
                    GhiChu = $"Cộng tiền thu hộ COD đơn {don.MaHienThi} (sau khi đối soát trừ cước nếu có)"
                };
                db.GiaoDichViKhachHangs.Add(gdVi);
            }
            dsDoiSoat.Add($"cộng ví người gửi {DinhDang.Tien(don.TienTraNguoiGui)}");
        }
        if (don.PhiNguoiGuiPhaiTra > 0) dsDoiSoat.Add($"ghi công nợ phí người gửi {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)}");
        string noiDung = "Xác nhận hoàn tất đơn" + (dsDoiSoat.Count > 0 ? " – đối soát: " + string.Join(", ", dsDoiSoat) : "");
        GhiSuKien(don, TrangThaiDon.HoanTat, noiDung, new ThaoTac(nguoiThucHien));
        await db.SaveChangesAsync();
        return KetQua.Dat($"{don.MaHienThi}: {TrangThaiDon.HoanTat.TenHienThi()}");
    }

    /// <summary>Đơn của khách còn nợ phí người gửi (đã hoàn tất hoặc đã hoàn hàng, chưa thu) – dùng cho tổng quan khách hàng và thống kê.</summary>
    public static IQueryable<DonGiaoHang> DonConNoPhi(IQueryable<DonGiaoHang> truyVan) =>
        truyVan.Where(d => (d.NgayThuPhiNguoiGui == null && d.TrangThai == TrangThaiDon.HoanTat && d.NguoiTraPhi == NguoiTraPhi.NguoiGui && d.PhiVanChuyen > d.TienThuHo)
                        || (d.NgayThuPhiNguoiGui == null && d.TrangThai == TrangThaiDon.DaHuy && d.NgayHoanHang != null && d.PhiVanChuyen > 0)
                        || ((d.TrangThai == TrangThaiDon.DaHoanHang || d.TrangThai == TrangThaiDon.ChoChuyenHoan || d.TrangThai == TrangThaiDon.DangChuyenHoan || d.TrangThai == TrangThaiDon.DaVeBuuCucGui || d.TrangThai == TrangThaiDon.DangGiaoHoan)
                            && (d.NgayThuPhiNguoiGui == null || d.NgayThuPhiHoan == null)));

    /// <summary>
    /// Tra cứu đơn theo mã đơn + số điện thoại người nhận (trang công khai và trang Tra cứu của khách hàng).
    /// Số điện thoại chỉ giữ chữ số nên nhập "0912 345 678" hay "0912.345.678" đều được.
    /// </summary>
    public async Task<(DonGiaoHang? Don, string? Loi)> TraCuuAsync(string? ma, string? sdt)
    {
        int? maDon = DinhDang.TachMaDon(ma);
        if (maDon is null || string.IsNullOrWhiteSpace(sdt))
            return (null, "Nhập đúng mã vận đơn (VD: DH202610060041) và số điện thoại người nhận");
        string soDienThoai = new(sdt.Where(char.IsDigit).ToArray());
        var don = await db.DonGiaoHangs.AsNoTracking()
            .Include(d => d.KhuVuc).Include(d => d.LoaiHang).Include(d => d.LichSus).ThenInclude(l => l.BuuCuc)
            .Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.BuuCucHienTai)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.MaDon == maDon && d.SoDienThoaiNguoiNhan == soDienThoai);
        return don is null ? (null, "Không tìm thấy đơn khớp mã đơn và số điện thoại đã nhập") : (don, null);
    }

    /// <summary>Dữ liệu khối ước lượng phí + bảng giá: khu vực, loại hàng đang hoạt động, bưu cục, khung cự ly và khối lượng tối đa một đơn.</summary>
    public async Task<BangGiaVM> BangGiaAsync(bool laKhachHang) => new(
        await db.KhuVucs.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).Include(k => k.BuuCuc).OrderBy(k => k.TenKhuVuc).ToListAsync(),
        await db.LoaiHangs.AsNoTracking().Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(l => l.HeSoPhuThu).ToListAsync(),
        await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong).MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0,
        laKhachHang,
        await db.BuuCucs.AsNoTracking().Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(b => b.TinhThanh).ThenBy(b => b.TenBuuCuc).ToListAsync(),
        TinhPhiXuLy.BangGiaCuLy);
}
