using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * QuanTriThongKeController — URL: /QuanTriThongKe?tuNgay=&denNgay=
 * Quyền: chỉ Quản trị (số liệu doanh thu, thu hộ, thu nhập shipper).
 */
[YeuCauVaiTro(VaiTroNguoiDung.QuanTri)]
public class QuanTriThongKeController(QuanLyGiaoNhanDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
    {
        var tu = (tuNgay ?? DateTime.Today.AddMonths(-5).AddDays(1 - DateTime.Today.Day)).Date;
        var den = (denNgay ?? DateTime.Today).Date;
        if (tu > den) (tu, den) = (den, tu);
        var dsDon = db.DonGiaoHangs.AsNoTracking().Where(d => d.NgayTao >= tu && d.NgayTao < den.AddDays(1));
        var donHopLe = dsDon.Where(d => d.TrangThai != TrangThaiDon.DaHuy);
        var phanCongKetThuc = db.PhanCongGiaoHangs.AsNoTracking()
            .Where(p => p.KetQua != null && p.KetQua != KetQuaGiao.DaHuyDon && p.NgayKetThuc >= tu && p.NgayKetThuc < den.AddDays(1));

        // 1. Số đơn theo trạng thái
        var theoTrangThai = (await dsDon.GroupBy(d => d.TrangThai).Select(g => new { g.Key, SoLuong = g.Count() }).ToListAsync())
            .OrderBy(x => x.Key).Select(x => new DongThongKe(x.Key.TenHienThi(), x.SoLuong)).ToList();

        // 2 + 10. Số đơn và tổng khối lượng theo khu vực
        var theoKhuVuc = (await dsDon.GroupBy(d => d.KhuVuc!.TenKhuVuc)
                .Select(g => new { Ten = g.Key, SoDon = g.Count(), KhoiLuong = g.Sum(d => d.KhoiLuong) })
                .OrderByDescending(x => x.SoDon).ToListAsync())
            .Select(x => new DongThongKe(x.Ten, x.SoDon, x.KhoiLuong)).ToList();

        // 3. Số đơn theo loại hàng
        var theoLoaiHang = (await dsDon.GroupBy(d => d.LoaiHang!.TenLoaiHang)
                .Select(g => new { Ten = g.Key, SoDon = g.Count(), TongPhi = g.Sum(d => d.PhiVanChuyen) })
                .OrderByDescending(x => x.SoDon).ToListAsync())
            .Select(x => new DongThongKe(x.Ten, x.SoDon, x.TongPhi)).ToList();

        // 4 + 5. Số đơn mỗi nhân viên đã giao
        var theoNhanVien = (await phanCongKetThuc.GroupBy(p => p.NhanVien!.HoTen)
                .Select(g => new
                {
                    Ten = g.Key,
                    ThanhCong = g.Count(p => (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc)),
                    ThatBai = g.Count(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong)
                })
                .OrderByDescending(x => x.ThanhCong).ThenBy(x => x.ThatBai).ToListAsync())
            .Select(x => new DongThongKe(x.Ten, x.ThanhCong, x.ThatBai)).ToList();

        // 6. Số lần giao không thành công theo lý do
        var theoLyDo = (await phanCongKetThuc.Where(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong && p.LyDoThatBai != null)
                .GroupBy(p => p.LyDoThatBai!.Value).Select(g => new { g.Key, SoLuong = g.Count() }).ToListAsync())
            .OrderByDescending(x => x.SoLuong).Select(x => new DongThongKe(x.Key.TenHienThi(), x.SoLuong)).ToList();

        // 7. Tỷ lệ giao thành công trên các đơn đã có kết quả
        int thanhCong = await dsDon.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat);
        int coKetQua = thanhCong + await dsDon.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoKhongThanhCong);

        // 8 + 9. Số đơn và tổng phí vận chuyển theo tháng
        var theoThang = (await donHopLe.GroupBy(d => new { d.NgayTao.Year, d.NgayTao.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, SoLuong = g.Count(), TongPhi = g.Sum(d => d.PhiVanChuyen) })
                .ToListAsync())
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new DongThongKe($"{x.Month:D2}/{x.Year}", x.SoLuong, x.TongPhi)).ToList();

        // 11. Thu hộ (COD)
        var coCod = donHopLe.Where(d => d.TienThuHo > 0);
        int soDonHopLe = await donHopLe.CountAsync();
        int codSoDon = await coCod.CountAsync();
        decimal codTong = await coCod.SumAsync(d => (decimal?)d.TienThuHo) ?? 0;
        decimal codDaThu = await coCod.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat)
                                      .SumAsync(d => (decimal?)d.TienThuHo) ?? 0;
        decimal codChoDoiSoat = await coCod.Where(d => d.TrangThai == TrangThaiDon.GiaoThanhCong).SumAsync(d => (decimal?)d.TienThuHo) ?? 0;
        var daDoiSoat = coCod.Where(d => d.NgayDoiSoat != null);
        decimal codDaDoiSoat = await daDoiSoat.SumAsync(d => (decimal?)d.TienThuHo) ?? 0;
        decimal thucTraShop = await daDoiSoat.SumAsync(d => (decimal?)(d.NguoiTraPhi == NguoiTraPhi.NguoiGui && d.NgayThuPhiNguoiGui == null
            ? (d.TienThuHo > d.PhiVanChuyen ? d.TienThuHo - d.PhiVanChuyen : 0) : d.TienThuHo)) ?? 0;

        var codTheoThang = (await coCod.GroupBy(d => new { d.NgayTao.Year, d.NgayTao.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Cod = g.Sum(d => d.TienThuHo), DaDoiSoat = g.Where(d => d.NgayDoiSoat != null).Sum(d => d.TienThuHo) })
                .ToListAsync())
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new DongThongKe($"{x.Month:D2}/{x.Year}", x.Cod, x.DaDoiSoat)).ToList();

        var topKhach = (await coCod.GroupBy(d => d.KhachHang!.HoTen)
                .Select(g => new { Ten = g.Key, Cod = g.Sum(d => d.TienThuHo), SoDon = g.Count() })
                .OrderByDescending(x => x.Cod).Take(5).ToListAsync())
            .Select(x => new DongThongKe(x.Ten, x.Cod, x.SoDon)).ToList();

        var giuTien = (await db.PhanCongGiaoHangs.AsNoTracking()
                .Where(p => p.KetQua == KetQuaGiao.GiaoThanhCong && p.DonGiaoHang!.TrangThai == TrangThaiDon.GiaoThanhCong
                         && p.DonGiaoHang.MaGiaoDichNop == null)
                .GroupBy(p => p.NhanVien!.HoTen)
                .Select(g => new
                {
                    Ten = g.Key, SoDon = g.Count(),
                    Cod = g.Sum(p => p.DonGiaoHang!.TienThuHo),
                    PhiShip = g.Sum(p => p.DonGiaoHang!.NguoiTraPhi == NguoiTraPhi.NguoiNhan ? p.DonGiaoHang.PhiVanChuyen : 0)
                })
                .ToListAsync())
            .Select(x => new DongGiuTien(x.Ten, x.SoDon, x.Cod, x.PhiShip))
            .OrderByDescending(x => x.TongPhaiNop).ToList();

        // 12. Thu nhập (hoa hồng) shipper
        var tienShipTho = await phanCongKetThuc.Where(p => (p.KetQua == KetQuaGiao.GiaoThanhCong || p.KetQua == KetQuaGiao.DaBanGiaoBuuCuc))
                .GroupBy(p => p.NhanVien!.HoTen)
                .Select(g => new { Ten = g.Key, Tien = g.Sum(p => p.ThuNhapShipper), SoDon = g.Count(),
                                   Cuoc = g.Sum(p => p.KetQua == KetQuaGiao.GiaoThanhCong ? p.DonGiaoHang!.PhiVanChuyen : 0) })
                .OrderByDescending(x => x.Tien).ToListAsync();
        var tienShip = tienShipTho.Select(x => new DongThongKe(x.Ten, x.Tien, x.SoDon)).ToList();

        // 13. Công nợ phí người gửi
        var donNoPhi = DonHangXuLy.DonConNoPhi(db.DonGiaoHangs.AsNoTracking());

        return View(new ThongKeVM
        {
            TienShipTheoNhanVien = tienShip,
            CuocGiaoThanhCong = tienShipTho.Sum(x => x.Cuoc),
            ChiTraShipper = tienShipTho.Sum(x => x.Tien),
            CongNoPhi = await donNoPhi.SumAsync(d => (decimal?)(d.PhiVanChuyen - d.TienThuHo)) ?? 0,
            SoDonNoPhi = await donNoPhi.CountAsync(),
            CodTong = codTong, CodSoDon = codSoDon,
            CodTiLeDon = soDonHopLe == 0 ? 0 : Math.Round(100.0 * codSoDon / soDonHopLe, 1),
            CodDaThu = codDaThu, CodChoDoiSoat = codChoDoiSoat, CodDaDoiSoat = codDaDoiSoat, CodThucTraShop = thucTraShop,
            CodChuaThu = codTong - codDaThu,
            CodTheoThang = codTheoThang, TopKhachThuHo = topKhach, ShipperGiuTien = giuTien,
            TienChoXacNhanNop = await db.GiaoDichVis.Where(g => g.Loai == LoaiGiaoDich.NopTienThuHo && g.TrangThai == TrangThaiGiaoDich.ChoXacNhan)
                                                   .SumAsync(g => (decimal?)g.SoTien) ?? 0,
            TuNgay = tu, DenNgay = den,
            TheoTrangThai = theoTrangThai, TheoKhuVuc = theoKhuVuc, TheoLoaiHang = theoLoaiHang,
            DonDaGiaoTheoNhanVien = theoNhanVien,
            NhanVienXuatSac = theoNhanVien.FirstOrDefault(x => x.GiaTri > 0),
            ThatBaiTheoLyDo = theoLyDo,
            SoDonCoKetQua = coKetQua,
            TiLeThanhCong = coKetQua == 0 ? 0 : Math.Round(100.0 * thanhCong / coKetQua, 1),
            TheoThang = theoThang,
            KhoiLuongTrungBinh = await donHopLe.AverageAsync(d => (decimal?)d.KhoiLuong) ?? 0,
            PhiTrungBinh = await donHopLe.AverageAsync(d => (decimal?)d.PhiVanChuyen) ?? 0
        });
    }
}
