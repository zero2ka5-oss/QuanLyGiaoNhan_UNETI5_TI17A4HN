// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Áp phí vào đơn, hoàn tất & đối soát thu hộ, ghi lịch sử, bảng giá, tra cứu đơn (đã chạy);
//                     thống kê phí theo tháng (khung, hoàn thiện ở tuần 6).

using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * DonHangXuLy (partial – Module 5)
 * Lớp DonHangXuLy được chia nhiều tệp theo module để mỗi thành viên commit đúng phần việc của mình;
 * khi biên dịch C# gộp lại thành một lớp duy nhất (cùng DbContext db, cùng hàm BayGio, DoiTrangThaiDonAsync...).
 * Các hàm khung trả kết quả tạm để các module khác của nhóm gọi được ngay từ đầu.
 */
public partial class DonHangXuLy
{
    // =====================================================================
    // TÍNH PHÍ
    // =====================================================================

    private static KetQuaTinhPhi ApDungPhi(DonGiaoHang don, KhuVuc khuVuc, LoaiHang loaiHang)
    {
        var phi = TinhPhiXuLy.TinhPhi(khuVuc.PhiCoBan, don.KhoiLuong, loaiHang.HeSoPhuThu);
        don.PhiCoBan = phi.PhiCoBan;
        don.PhuPhiKhoiLuong = phi.PhuPhiKhoiLuong;
        don.PhuPhiLoaiHang = phi.PhuPhiLoaiHang;
        don.PhiVanChuyen = phi.Tong;
        return phi;
    }

    // =====================================================================
    // HOÀN TẤT & ĐỐI SOÁT THU HỘ  (khung – hoàn thiện tuần 4)
    // =====================================================================

    /// <summary>
    /// Hoàn tất = đối soát: shipper đã nộp tiền thu của người nhận; hệ thống trả tiền thu hộ cho người gửi
    /// (nếu người gửi trả phí thì phí ship được trừ vào tiền thu hộ).
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

    // =====================================================================
    // LỊCH SỬ GIAO NHẬN
    // =====================================================================

    /// <summary>Một dòng lịch sử giao nhận: mọi thay đổi trạng thái đơn đều được ghi lại (ai, lúc nào, nội dung).</summary>
    private LichSuGiaoNhan TaoLichSu(TrangThaiDon? cu, TrangThaiDon moi, string noiDung, string nguoi, int maDon = 0) =>
        new() { MaDon = maDon, ThoiGian = BayGio(), TrangThaiCu = cu, TrangThaiMoi = moi, NoiDung = noiDung, NguoiThucHien = nguoi };

    // =====================================================================
    // THỐNG KÊ, BẢNG GIÁ, TRA CỨU  (khung – hoàn thiện tuần 2, 3, 6)
    // =====================================================================

    /// <summary>Khung: n tháng gần nhất, chưa có số liệu.</summary>
    private static Task<List<(string Thang, int SoDon, decimal Phi)>> PhiTheoThangAsync(IQueryable<DonGiaoHang> truyVan, int n)
    {
        var dau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-(n - 1));
        return Task.FromResult(Enumerable.Range(0, n).Select(i => ($"T{dau.AddMonths(i).Month}", 0, 0m)).ToList());
    }

    /// <summary>
    /// Tra cứu đơn theo mã đơn + số điện thoại người nhận (trang công khai và trang Tra cứu của khách hàng).
    /// Số điện thoại chỉ giữ chữ số nên nhập "0912 345 678" hay "0912.345.678" đều được.
    /// </summary>
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

    /// <summary>Dữ liệu khối ước lượng phí + bảng giá: khu vực, loại hàng đang hoạt động và khối lượng tối đa một đơn.</summary>
    public async Task<BangGiaVM> BangGiaAsync(bool laKhachHang) => new(
        await db.KhuVucs.AsNoTracking().Where(k => k.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(k => k.PhiCoBan).ToListAsync(),
        await db.LoaiHangs.AsNoTracking().Where(l => l.TrangThai == TrangThaiHoatDong.HoatDong).OrderBy(l => l.HeSoPhuThu).ToListAsync(),
        await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong).MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0,
        laKhachHang);
}
