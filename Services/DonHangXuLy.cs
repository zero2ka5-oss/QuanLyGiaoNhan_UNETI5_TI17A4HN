using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

/*
 * LUỒNG VẬN CHUYỂN: Khách hàng → Bưu cục gửi → Shipper liên tỉnh → Bưu cục nhận → Shipper khu vực → Người nhận
 * ------------------------------------------------------------------------------------------------------------
 *   DaTao ──NV bưu cục gửi quét──▶ DaNhanTaiBuuCucGui ──phân shipper liên tỉnh──▶ ChoLayLienTinh
 *     ──shipper quét nhận──▶ DaLayLienTinh ──bắt đầu chạy──▶ DangTrungChuyen ──NV bưu cục nhận quét──▶ DaDenBuuCucNhan
 *     ──phân shipper khu vực──▶ ChoLayGiaoHang ──shipper quét nhận──▶ DaLayGiaoHang ──bắt đầu giao──▶ DangGiao
 *     ──▶ GiaoThanhCong ──đối soát──▶ HoanTat
 *   Bưu cục gửi = bưu cục nhận (đơn nội vùng): tiếp nhận xong chuyển thẳng DaDenBuuCucNhan, không có chặng liên tỉnh.
 *   Ngoại lệ: DangGiao ──▶ GiaoKhongThanhCong (shipper vẫn giữ hàng) ──hẹn lại──▶ HenGiaoLai ──▶ DangGiao
 *                                                                    └─NV bưu cục quét nhận lại──▶ DaDenBuuCucNhan
 *             Hàng đang ở bưu cục / đang trong tay shipper ──▶ ThatLac / HuHong;  ở bưu cục ──hủy──▶ DaHuy (chờ chuyển hoàn)
 *
 * Mỗi thao tác ghi một Tracking Event (LichSuGiaoNhan: bưu cục, shipper, phương tiện, toạ độ, có quét mã hay không)
 * và cập nhật "ai đang giữ hàng" (MaBuuCucHienTai / MaNhanVienDangGiu) – vị trí đơn suy ra từ đối tượng đang giữ.
 *
 * Shipper & phương tiện: 1 shipper ↔ 1 phương tiện cố định (NhanVienGiaoHang.MaPhuongTien). Mỗi chặng (PhanCongGiaoHang)
 * ghi lại phương tiện của shipper tại lúc phân công. Liên tỉnh xuất phát từ bưu cục gửi; khu vực thuộc bưu cục nhận
 * và phụ trách đúng khu vực giao của đơn. Kiểm tra tải trọng theo tổng khối lượng các đơn shipper đang giữ.
 *
 * Tệp này: tạo / sửa / hủy đơn, quy tắc được phép, truy vấn dùng chung. DonHangXuLy.VanChuyen.cs: quét – bàn giao – phân công.
 */
public partial class DonHangXuLy(QuanLyGiaoNhanDbContext db)
{
    /// <summary>Số đơn tối đa một shipper khu vực giữ cùng lúc (liên tỉnh chỉ giới hạn theo tải trọng xe).</summary>
    public const int SoDonToiDa = 8;
    /// <summary>Số lần giao tối đa cho một đơn (giao lần đầu + giao lại); quá số lần này thì trả về bưu cục, hủy / chuyển hoàn.</summary>
    public const int SoLanGiaoToiDa = 3;

    /// <summary>Độ dài tối đa ghi chú phân công / nội dung lịch sử (cột nvarchar(500)).</summary>
    public const int DoDaiGhiChu = 500;

    /// <summary>
    /// Lưu thay đổi an toàn vào CSDL với cơ chế xử lý xung đột phiên (Optimistic Concurrency Control).
    /// Bắt DbUpdateConcurrencyException khi đơn hàng bị sửa đổi đồng thời bởi thao tác khác.
    /// </summary>
    public async Task<(bool ThanhCong, string? Loi)> LuuThayDoiAnToanAsync(string? maDonHienThi = null)
    {
        try
        {
            await db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            string msg = string.IsNullOrEmpty(maDonHienThi)
                ? "Dữ liệu đơn hàng vừa được cập nhật bởi một phiên thao tác khác. Vui lòng tải lại trang để xem trạng thái mới nhất!"
                : $"Đơn hàng {maDonHienThi} vừa được cập nhật bởi một phiên thao tác khác. Vui lòng tải lại trang để tiếp tục!";
            return (false, msg);
        }
    }
    // =====================================================================
    // CẤU HÌNH THÙ LAO SHIPPER (THEO ĐƠN, CỰ LY KM, KHỐI LƯỢNG & ĐÚNG HẠN)
    // =====================================================================
    // 1. Shipper khu vực (xe máy)
    public const decimal ThuLaoCoBanGiaoKhuVuc = 8_000m;       // Giao thành công mỗi đơn
    public const decimal ThuLaoCoBanHoanKhuVuc = 5_000m;       // Đơn giao hoàn về người gửi
    public const decimal ThuLaoCoBanLayHangKhuVuc = 5_000m;    // Lấy hàng từ khách về bưu cục
    public const decimal PhuCapMoiKgVuotKhuVuc = 1_500m;       // Mỗi kg vượt ngưỡng 2.0 kg
    public const decimal PhuCapLoaiHangKhuVuc = 2_000m;        // Hàng dễ vỡ, điện tử / giá trị cao
    public const decimal ThuongDungHanKhuVuc = 1_000m;         // Giao đúng hoặc trước hạn dự kiến

    // 2. Shipper liên tỉnh (xe tải) theo cự ly 6 bậc
    public const decimal ThuLaoLienTinhNoiTinh = 2_500m;       // Cự ly <= 45 km
    public const decimal ThuLaoLienTinhNoiMienGan = 4_000m;    // Cự ly 45 - 120 km
    public const decimal ThuLaoLienTinhNoiMienTB = 6_500m;     // Cự ly 120 - 300 km
    public const decimal ThuLaoLienTinhLienMienGan = 9_000m;   // Cự ly 300 - 600 km
    public const decimal ThuLaoLienTinhLienMienXa = 12_000m;   // Cự ly 600 - 1000 km
    public const decimal ThuLaoLienTinhBacNam = 15_000m;       // Cự ly > 1000 km
    public const decimal PhuCapMoiKgVuotLienTinh = 500m;       // Mỗi kg vượt ngưỡng 10.0 kg trên xe tải
    public const decimal ThuongDungHanLienTinh = 1_000m;       // Bàn giao đúng lịch trình

    public record ChiTietThuNhapShipper(
        decimal TongCong,
        decimal ThuLaoCoBan,
        decimal PhuCapKhoiLuong,
        decimal PhuCapLoaiHang,
        decimal ThuongDungHan,
        string DienGiai
    );

    /// <summary>
    /// Tính chi tiết thù lao shipper (bóc tách cơ bản, phụ cấp nặng, phụ cấp loại hàng, thưởng đúng hạn).
    /// </summary>
    public static ChiTietThuNhapShipper TinhChiTietThuNhap(
        DonGiaoHang don,
        LoaiShipper loai = LoaiShipper.KhuVuc,
        KetQuaGiao? ketQua = null,
        DateTime? thoiDiemKetThuc = null)
    {
        if (loai == LoaiShipper.KhuVuc)
        {
            decimal coBan = ketQua switch
            {
                KetQuaGiao.DaHoanHang => ThuLaoCoBanHoanKhuVuc,
                KetQuaGiao.DaLayHang => ThuLaoCoBanLayHangKhuVuc,
                _ => ThuLaoCoBanGiaoKhuVuc
            };

            decimal phuKl = 0m;
            if (don.KhoiLuong > TinhPhiXuLy.NguongKhoiLuongKg)
            {
                decimal kgVuot = Math.Ceiling(don.KhoiLuong - TinhPhiXuLy.NguongKhoiLuongKg);
                phuKl = kgVuot * PhuCapMoiKgVuotKhuVuc;
            }

            decimal phuLoai = 0m;
            if (don.LoaiHang != null && don.LoaiHang.HeSoPhuThu > 0)
            {
                phuLoai = PhuCapLoaiHangKhuVuc;
            }

            decimal thuong = 0m;
            DateTime ngayXong = thoiDiemKetThuc ?? DateTime.Now;
            if (don.NgayGiaoDuKien.Date >= ngayXong.Date)
            {
                thuong = ThuongDungHanKhuVuc;
            }

            var parts = new List<string> { $"Thù lao cơ bản: {DinhDang.Tien(coBan)}" };
            if (phuKl > 0) parts.Add($"Phụ cấp nặng: +{DinhDang.Tien(phuKl)}");
            if (phuLoai > 0) parts.Add($"Phụ cấp hàng: +{DinhDang.Tien(phuLoai)}");
            if (thuong > 0) parts.Add($"Thưởng đúng hạn: +{DinhDang.Tien(thuong)}");

            return new ChiTietThuNhapShipper(coBan + phuKl + phuLoai + thuong, coBan, phuKl, phuLoai, thuong, string.Join(" · ", parts));
        }
        else
        {
            double km = 0;
            if (don.BuuCucGui?.ViDo.HasValue == true && don.BuuCucGui?.KinhDo.HasValue == true &&
                don.BuuCucNhan?.ViDo.HasValue == true && don.BuuCucNhan?.KinhDo.HasValue == true)
            {
                km = TinhPhiXuLy.TinhKhoangCachKm(
                    don.BuuCucGui.ViDo.Value, don.BuuCucGui.KinhDo.Value,
                    don.BuuCucNhan.ViDo.Value, don.BuuCucNhan.KinhDo.Value);
            }
            else if (don.MaBuuCucGui.HasValue && don.MaBuuCucNhan.HasValue)
            {
                km = don.MaBuuCucGui == don.MaBuuCucNhan ? 30 : 150;
            }

            decimal coBanKm = km switch
            {
                <= 45 => ThuLaoLienTinhNoiTinh,
                <= 120 => ThuLaoLienTinhNoiMienGan,
                <= 300 => ThuLaoLienTinhNoiMienTB,
                <= 600 => ThuLaoLienTinhLienMienGan,
                <= 1000 => ThuLaoLienTinhLienMienXa,
                _ => ThuLaoLienTinhBacNam
            };

            decimal phuKl = 0m;
            if (don.KhoiLuong > 10m)
            {
                decimal kgVuot = Math.Ceiling(don.KhoiLuong - 10m);
                phuKl = kgVuot * PhuCapMoiKgVuotLienTinh;
            }

            decimal thuong = ThuongDungHanLienTinh;

            var parts = new List<string> { $"Cước cự ly ({km:0.#} km): {DinhDang.Tien(coBanKm)}" };
            if (phuKl > 0) parts.Add($"Phụ cấp tải trọng: +{DinhDang.Tien(phuKl)}");
            if (thuong > 0) parts.Add($"Thưởng lịch trình: +{DinhDang.Tien(thuong)}");

            return new ChiTietThuNhapShipper(coBanKm + phuKl + thuong, coBanKm, phuKl, 0m, thuong, string.Join(" · ", parts));
        }
    }

    /// <summary>
    /// Tính thù lao shipper một đơn (gọi từ các chặng tiếp nhận, bàn giao liên tỉnh, giao thành công).
    /// </summary>
    public static decimal TinhThuNhapShipper(
        DonGiaoHang don,
        LoaiShipper loai = LoaiShipper.KhuVuc,
        KetQuaGiao? ketQua = null,
        DateTime? thoiDiemKetThuc = null) =>
        TinhChiTietThuNhap(don, loai, ketQua, thoiDiemKetThuc).TongCong;


    /// <summary>Vị trí GPS của shipper cũ hơn số phút này thì coi là chưa cập nhật (hiện kèm cảnh báo).</summary>
    public const int PhutViTriConHieuLuc = 30;

    /// <summary>Đồng hồ hệ thống – QuanLyGiaoNhanDbContext.NapDuLieuMauAsync thay bằng thời điểm giả lập khi tạo dữ liệu mẫu.</summary>
    public Func<DateTime> BayGio { get; set; } = () => DateTime.Now;

    // =====================================================================
    // TẠO / SỬA / HỦY ĐƠN
    // =====================================================================

    public async Task<KetQua<DonGiaoHang>> TaoDonAsync(DonGiaoHangFormVM vm, int maKhachHang, string nguoiThucHien, bool laKhachHang = false)
    {
        var khachHang = await db.KhachHangs.FindAsync(maKhachHang);
        if (khachHang is null) return KetQua<DonGiaoHang>.Loi("Khách hàng không tồn tại");
        if (khachHang.TrangThai != TrangThaiHoatDong.HoatDong)
            return KetQua<DonGiaoHang>.Loi($"Khách hàng {khachHang.HoTen} đã ngừng hoạt động – không tạo được đơn mới. Vui lòng liên hệ công ty để mở lại");

        var (loaiHang, khuVuc, loi) = await LayDanhMucAsync(vm.MaLoaiHang, vm.MaKhuVuc, null);
        if (loi is not null) return KetQua<DonGiaoHang>.Loi(loi);
        var (buuCucGui, buuCucNhan, loiBc) = await LayBuuCucAsync(vm.MaBuuCucGui, khuVuc!, null);
        if (loiBc is not null) return KetQua<DonGiaoHang>.Loi(loiBc);

        var ngayTao = BayGio();
        double? km = null;
        if (buuCucGui?.ViDo != null && buuCucGui?.KinhDo != null && buuCucNhan?.ViDo != null && buuCucNhan?.KinhDo != null)
        {
            km = TinhPhiXuLy.TinhKhoangCachKm(buuCucGui.ViDo.Value, buuCucGui.KinhDo.Value, buuCucNhan.ViDo.Value, buuCucNhan.KinhDo.Value);
        }
        var infoCuLy = TinhPhiXuLy.LayPhiCoBanTheoKhoangCach(km, khuVuc!.PhiCoBan);
        var ngayGiaoDuKien = ngayTao.Date.AddDays(infoCuLy.SoNgay);

        if (await KiemTraKhoiLuongAsync(vm.KhoiLuong, buuCucNhan!.MaBuuCuc, vm.MaKhuVuc) is string loiKhoiLuong) return KetQua<DonGiaoHang>.Loi(loiKhoiLuong);

        string? diaChiLay = vm.HinhThucGui == HinhThucGui.LayTanNoi
            ? (string.IsNullOrWhiteSpace(vm.DiaChiLayHang) ? khachHang.DiaChi : vm.DiaChiLayHang.Trim())
            : null;
        if (vm.HinhThucGui == HinhThucGui.LayTanNoi)
        {
            if (string.IsNullOrWhiteSpace(diaChiLay))
                return KetQua<DonGiaoHang>.Loi("Vui lòng nhập địa chỉ lấy hàng hoặc cập nhật địa chỉ khách hàng");

            if (!DiaChiTienIch.KiemTraKhopTinhThanh(diaChiLay, buuCucGui!.TinhThanh))
            {
                var tinhLay = DiaChiTienIch.NhanDienTinhThanh(diaChiLay) ?? "địa phương khác";
                return KetQua<DonGiaoHang>.Loi($"Địa chỉ lấy hàng tại {tinhLay} không thể do {buuCucGui.TenBuuCuc} ({buuCucGui.TinhThanh}) tiếp nhận. Vui lòng chọn bưu cục phụ trách tại {tinhLay}.");
            }
        }

        var trangThaiBanDau = vm.HinhThucGui == HinhThucGui.LayTanNoi ? TrangThaiDon.ChoLayTanNoi : TrangThaiDon.DaTao;
        var don = new DonGiaoHang
        {
            MaKhachHang = maKhachHang, MaLoaiHang = loaiHang!.MaLoaiHang, MaKhuVuc = khuVuc!.MaKhuVuc,
            MaBuuCucGui = buuCucGui!.MaBuuCuc, MaBuuCucNhan = buuCucNhan!.MaBuuCuc,
            HinhThucGui = vm.HinhThucGui, DiaChiLayHang = diaChiLay,
            TenNguoiNhan = vm.TenNguoiNhan.Trim(), SoDienThoaiNguoiNhan = vm.SoDienThoaiNguoiNhan.Trim(),
            DiaChiNhan = vm.DiaChiNhan.Trim(), KhoiLuong = vm.KhoiLuong,
            KichThuocM3 = vm.KichThuocM3 > 0 ? vm.KichThuocM3 : 0.005m,
            NgayTao = ngayTao,
            NgayGiaoDuKien = ngayGiaoDuKien, GhiChu = vm.GhiChu?.Trim(), TrangThai = trangThaiBanDau,
            TienThuHo = vm.TienThuHo, NguoiTraPhi = vm.NguoiTraPhi
        };
        var phi = ApDungPhi(don, khuVuc, loaiHang, buuCucGui, buuCucNhan);
        string noiDungGui = vm.HinhThucGui == HinhThucGui.LayTanNoi
            ? $"shipper đến lấy tận nơi tại {don.DiaChiLayHang}, bưu cục phụ trách {buuCucGui.TenBuuCuc}"
            : $"gửi tại {buuCucGui.TenBuuCuc}";
        string noiDungTao = $"Tạo đơn – {noiDungGui}; phát từ {buuCucNhan.TenBuuCuc}; phí vận chuyển {DinhDang.Tien(phi.Tong)} ({don.NguoiTraPhi.TenHienThi().ToLower()})"
            + (don.TienThuHo > 0 ? $", thu hộ {DinhDang.Tien(don.TienThuHo)}" : "");

        GhiSuKien(don, trangThaiBanDau, noiDungTao, new ThaoTac(nguoiThucHien, LaKhachHang: laKhachHang), maBuuCuc: buuCucGui.MaBuuCuc);

        db.DonGiaoHangs.Add(don);
        await db.SaveChangesAsync();

        // Bắn thông báo cho Quản trị viên, Nhân viên bưu cục gửi và Khách hàng
        BaoDonMoi(don, khachHang, buuCucGui, buuCucNhan, laKhachHang);
        await db.SaveChangesAsync();

        string thongBao = vm.HinhThucGui == HinhThucGui.LayTanNoi
            ? $"Đã tạo đơn {don.MaHienThi} – shipper khu vực sẽ đến lấy hàng tại {don.DiaChiLayHang}"
            : $"Đã tạo đơn {don.MaHienThi} – vui lòng mang hàng tới {buuCucGui.TenBuuCuc} để gửi trong vòng 7 ngày";
        return KetQua<DonGiaoHang>.Dat(don, thongBao);
    }

    /// <summary>
    /// Khách hàng sửa đơn trước khi hàng rời tay (Đã tạo / Chờ lấy tận nơi); quản trị / điều phối sửa thêm được khi hàng vừa được
    /// bưu cục gửi tiếp nhận, chưa phân chặng (VD cân lại khối lượng). Đổi khu vực giao thì đổi luôn bưu cục nhận.
    /// </summary>
    public async Task<KetQua> CapNhatDonAsync(int maDon, DonGiaoHangFormVM vm, string nguoiThucHien, int? maKhachHangCuaToi)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null || (maKhachHangCuaToi.HasValue && don.MaKhachHang != maKhachHangCuaToi))
            return KetQua.Loi("Không tìm thấy đơn hàng");
        bool laKhach = maKhachHangCuaToi.HasValue;
        if (!CoTheSua(don, laKhach))
            return KetQua.Loi(laKhach ? "Chỉ sửa được đơn trước khi gửi hàng tới bưu cục hoặc shipper"
                                      : "Chỉ sửa được đơn trước khi bưu cục gửi phân chặng vận chuyển");

        var (loaiHang, khuVuc, loi) = await LayDanhMucAsync(vm.MaLoaiHang, vm.MaKhuVuc, don);
        if (loi is not null) return KetQua.Loi(loi);
        // Hàng đã ở bưu cục gửi thì không đổi được bưu cục gửi
        bool chuaGui = don.TrangThai is TrangThaiDon.DaTao or TrangThaiDon.ChoLayTanNoi;
        int? maBuuCucGui = chuaGui ? vm.MaBuuCucGui : don.MaBuuCucGui;
        var (buuCucGui, buuCucNhan, loiBc) = await LayBuuCucAsync(maBuuCucGui, khuVuc!, don);
        if (loiBc is not null) return KetQua.Loi(loiBc);

        if (chuaGui && vm.HinhThucGui == HinhThucGui.LayTanNoi)
        {
            string? diaChiLayMoi = string.IsNullOrWhiteSpace(vm.DiaChiLayHang) ? don.DiaChiLayHang : vm.DiaChiLayHang.Trim();
            if (string.IsNullOrWhiteSpace(diaChiLayMoi))
                return KetQua.Loi("Vui lòng nhập địa chỉ lấy hàng");

            if (!DiaChiTienIch.KiemTraKhopTinhThanh(diaChiLayMoi, buuCucGui!.TinhThanh))
            {
                var tinhLay = DiaChiTienIch.NhanDienTinhThanh(diaChiLayMoi) ?? "địa phương khác";
                return KetQua.Loi($"Địa chỉ lấy hàng tại '{tinhLay}' không thể do '{buuCucGui.TenBuuCuc} ({buuCucGui.TinhThanh})' tiếp nhận. Vui lòng chọn bưu cục phụ trách tại {tinhLay}.");
            }
        }
        double? km = null;
        if (buuCucGui?.ViDo != null && buuCucGui?.KinhDo != null && buuCucNhan?.ViDo != null && buuCucNhan?.KinhDo != null)
        {
            km = TinhPhiXuLy.TinhKhoangCachKm(buuCucGui.ViDo.Value, buuCucGui.KinhDo.Value, buuCucNhan.ViDo.Value, buuCucNhan.KinhDo.Value);
        }
        var infoCuLy = TinhPhiXuLy.LayPhiCoBanTheoKhoangCach(km, khuVuc!.PhiCoBan);
        don.NgayGiaoDuKien = don.NgayTao.Date.AddDays(infoCuLy.SoNgay);

        if (await KiemTraKhoiLuongAsync(vm.KhoiLuong, buuCucNhan!.MaBuuCuc, vm.MaKhuVuc) is string loiKhoiLuong) return KetQua.Loi(loiKhoiLuong);

        decimal phiCu = don.PhiVanChuyen;
        // Chỉ báo giá lại (theo bảng giá hiện hành) khi đổi yếu tố tính phí; sửa SĐT / địa chỉ / ghi chú giữ nguyên phí đã báo
        bool tinhLaiPhi = vm.MaKhuVuc != don.MaKhuVuc || vm.MaLoaiHang != don.MaLoaiHang || vm.KhoiLuong != don.KhoiLuong || vm.KichThuocM3 != don.KichThuocM3;
        bool doiBuuCuc = buuCucGui!.MaBuuCuc != don.MaBuuCucGui || buuCucNhan!.MaBuuCuc != don.MaBuuCucNhan;
        don.MaLoaiHang = loaiHang!.MaLoaiHang;
        don.MaKhuVuc = khuVuc!.MaKhuVuc;
        don.MaBuuCucGui = buuCucGui.MaBuuCuc;
        don.MaBuuCucNhan = buuCucNhan!.MaBuuCuc;
        don.TenNguoiNhan = vm.TenNguoiNhan.Trim();
        don.SoDienThoaiNguoiNhan = vm.SoDienThoaiNguoiNhan.Trim();
        don.DiaChiNhan = vm.DiaChiNhan.Trim();
        don.KhoiLuong = vm.KhoiLuong;
        don.KichThuocM3 = vm.KichThuocM3 > 0 ? vm.KichThuocM3 : 0.005m;
        don.GhiChu = vm.GhiChu?.Trim();
        decimal thuHoCu = don.TienThuHo;
        don.TienThuHo = vm.TienThuHo;
        don.NguoiTraPhi = vm.NguoiTraPhi;

        if (chuaGui)
        {
            don.HinhThucGui = vm.HinhThucGui;
            don.DiaChiLayHang = vm.HinhThucGui == HinhThucGui.LayTanNoi ? vm.DiaChiLayHang?.Trim() : null;
            don.TrangThai = vm.HinhThucGui == HinhThucGui.LayTanNoi ? TrangThaiDon.ChoLayTanNoi : TrangThaiDon.DaTao;
        }

        if (tinhLaiPhi || doiBuuCuc) ApDungPhi(don, khuVuc, loaiHang, buuCucGui, buuCucNhan);

        string noiDung = phiCu == don.PhiVanChuyen
            ? "Cập nhật thông tin đơn"
            : $"Cập nhật thông tin đơn – phí {DinhDang.Tien(phiCu)} → {DinhDang.Tien(don.PhiVanChuyen)}";
        if (thuHoCu != don.TienThuHo) noiDung += $" – thu hộ {DinhDang.Tien(thuHoCu)} → {DinhDang.Tien(don.TienThuHo)}";
        if (doiBuuCuc) noiDung += $" – tuyến {buuCucGui.TenBuuCuc} → {buuCucNhan.TenBuuCuc}";
        var tt = new ThaoTac(nguoiThucHien);
        GhiSuKien(don, don.TrangThai, noiDung, tt, maBuuCuc: don.MaBuuCucHienTai);
        // Hàng đã ở bưu cục gửi mà khu vực mới do chính bưu cục này phát → đơn nội vùng, chờ phân shipper khu vực
        if (don.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui && don.MaBuuCucGui == don.MaBuuCucNhan)
            GhiSuKien(don, TrangThaiDon.DaDenBuuCucNhan, $"Đơn nội vùng – {buuCucNhan.TenBuuCuc} phát hàng, chờ phân shipper khu vực",
                      tt, maBuuCuc: don.MaBuuCucNhan);
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã cập nhật đơn {don.MaHienThi}");
    }

    /// <summary>
    /// Hủy đơn: khách hủy khi chưa mang hàng tới bưu cục; bưu cục / điều phối hủy thêm được khi hàng đang nằm ở bưu cục
    /// (hủy = chuyển hoàn: hàng ở lại bưu cục chờ trả người gửi, XacNhanHoanHangAsync). Hàng đang trong tay shipper
    /// thì phải trả về bưu cục trước.
    /// </summary>
    public async Task<KetQua> HuyDonAsync(int maDon, string? lyDo, ThaoTac tt, int? maKhachHangCuaToi)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).Include(d => d.BuuCucHienTai).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null || (maKhachHangCuaToi.HasValue && don.MaKhachHang != maKhachHangCuaToi))
            return KetQua.Loi("Không tìm thấy đơn hàng");
        bool laKhach = maKhachHangCuaToi.HasValue;
        if (!CoTheHuy(don, laKhach))
            return KetQua.Loi(laKhach
                ? "Chỉ hủy được đơn trước khi mang hàng tới bưu cục gửi – đơn đã ở bưu cục vui lòng liên hệ tổng đài"
                : don.MaNhanVienDangGiu is not null
                    ? "Hàng đang trong tay shipper – cần trả hàng về bưu cục trước khi hủy"
                    : $"Không thể hủy đơn ở trạng thái '{don.TrangThai.TenHienThi()}'");
        if (KiemTraBuuCuc(tt, don.MaBuuCucHienTai ?? don.MaBuuCucGui) is string loiBc) return KetQua.Loi(loiBc);

        bool chuyenHoan = don.MaBuuCucHienTai is not null;   // hàng đang ở bưu cục → chờ trả về người gửi
        var phanCong = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (phanCong is not null)
        {
            phanCong.TrangThai = TrangThaiPhanCong.KetThuc;
            phanCong.KetQua = KetQuaGiao.DaHuyDon;
            phanCong.NgayKetThuc = BayGio();
        }
        don.MaChuyenXe = null;
        GhiSuKien(don, TrangThaiDon.DaHuy,
            (chuyenHoan ? $"Hủy đơn – chuyển hoàn: hàng tại {don.BuuCucHienTai?.TenBuuCuc} chờ trả người gửi" : "Hủy đơn") +
            (string.IsNullOrWhiteSpace(lyDo) ? "" : $": {lyDo.Trim()}"), tt, maBuuCuc: don.MaBuuCucHienTai, pc: phanCong);
        await db.SaveChangesAsync();
        if (phanCong is not null) await CapNhatNguonLucAsync([phanCong.MaNhanVien], [phanCong.MaPhuongTien]);
        return KetQua.Dat(chuyenHoan ? $"Đã hủy đơn {don.MaHienThi} – hàng chờ chuyển hoàn về người gửi" : $"Đã hủy đơn {don.MaHienThi}");
    }

    /// <summary>
    /// Tự động hủy các đơn hàng tạo hình thức gửi tại bưu cục (GuiTaiBuuCuc) quá 7 ngày
    /// mà khách hàng chưa mang hàng tới bưu cục gửi (vẫn ở trạng thái DaTao).
    /// Giúp tránh làm tồn đọng rác dữ liệu trong hệ thống.
    /// Tự động ghi lịch sử và gửi thông báo đến tài khoản khách hàng ở mục thông báo.
    /// </summary>
    public async Task<int> TuDongHuyDonGuiTaiBuuCucQuaHanAsync(int soNgayQuaHan = 7)
    {
        var bayGio = BayGio();
        var mocThoiGian = bayGio.AddDays(-soNgayQuaHan);

        var dsDonQuaHan = await db.DonGiaoHangs
            .Include(d => d.BuuCucGui)
            .Include(d => d.PhanCongs)
            .Where(d => (d.HinhThucGui == HinhThucGui.GuiTaiBuuCuc && d.TrangThai == TrangThaiDon.DaTao && d.NgayTao <= mocThoiGian)
                     || (d.HinhThucGui == HinhThucGui.LayTanNoi && d.TrangThai == TrangThaiDon.ChoLayTanNoi && d.NgayTao <= mocThoiGian
                         && !d.PhanCongs.Any(p => p.LoaiChang == LoaiShipper.KhuVuc && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai))))
            .ToListAsync();

        if (dsDonQuaHan.Count == 0) return 0;

        var thaoTacHeThong = new ThaoTac("Hệ thống tự động", LaKhachHang: false);

        foreach (var don in dsDonQuaHan)
        {
            var phanCong = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (phanCong is not null)
            {
                phanCong.TrangThai = TrangThaiPhanCong.KetThuc;
                phanCong.KetQua = KetQuaGiao.DaHuyDon;
                phanCong.NgayKetThuc = bayGio;
            }

            string tenBc = don.BuuCucGui?.TenBuuCuc ?? "bưu cục gửi";
            string lyDo = don.HinhThucGui == HinhThucGui.LayTanNoi
                ? $"Hệ thống tự động hủy đơn do quá {soNgayQuaHan} ngày kể từ khi tạo ({don.NgayTao:dd/MM/yyyy}) chưa thể lấy hàng tận nơi."
                : $"Hệ thống tự động hủy đơn do quá {soNgayQuaHan} ngày kể từ khi tạo ({don.NgayTao:dd/MM/yyyy}) bạn chưa mang kiện hàng tới {tenBc} để gửi.";

            GhiSuKien(don, TrangThaiDon.DaHuy, lyDo, thaoTacHeThong, maBuuCuc: don.MaBuuCucGui, pc: phanCong);
        }

        await db.SaveChangesAsync();
        return dsDonQuaHan.Count;
    }

    /// <summary>Đơn chờ chuyển hoàn / đang hoàn hàng / đã hủy mà hàng còn ở bưu cục, chưa trả cho người gửi.</summary>
    public static bool ChoHoanHang(DonGiaoHang don) =>
        don.TrangThai is TrangThaiDon.ChoChuyenHoan or TrangThaiDon.DangChuyenHoan or TrangThaiDon.DaVeBuuCucGui or TrangThaiDon.DangGiaoHoan
        || (don.TrangThai == TrangThaiDon.DaHuy && don.NgayHoanHang is null && don.MaBuuCucHienTai is not null);

    /// <summary>Truy vấn các đơn đang chờ chuyển hoàn (dịch sang SQL).</summary>
    public static IQueryable<DonGiaoHang> DonChoHoanHang(IQueryable<DonGiaoHang> truyVan) =>
        truyVan.Where(d => (d.TrangThai == TrangThaiDon.ChoChuyenHoan || d.TrangThai == TrangThaiDon.DangChuyenHoan
                         || d.TrangThai == TrangThaiDon.DaVeBuuCucGui || d.TrangThai == TrangThaiDon.DangGiaoHoan
                         || (d.TrangThai == TrangThaiDon.DaHuy && d.NgayHoanHang == null)) && d.MaBuuCucHienTai != null);

    /// <summary>Bưu cục đang giữ hàng xác nhận chuyển hoàn hoặc hoàn trả người gửi.</summary>
    public async Task<KetQua> XacNhanHoanHangAsync(int maDon, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucHienTai).Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!ChoHoanHang(don)) return KetQua.Loi($"Đơn {don.MaHienThi} không có hàng cần chuyển hoàn");
        if (KiemTraBuuCuc(tt, don.MaBuuCucHienTai ?? don.MaBuuCucGui) is string loiBc) return KetQua.Loi(loiBc);

        // Nếu đơn đang ở bưu cục nhận khác bưu cục gửi -> Bắt đầu quy trình chuyển hoàn có xe tải và lộ trình
        if (don.MaBuuCucHienTai != don.MaBuuCucGui && don.MaBuuCucGui is not null)
        {
            return await YeuCauChuyenHoanAsync(maDon, "Bưu cục xác nhận chuyển hoàn về bưu cục gửi", tt);
        }

        // Hàng đã ở bưu cục gửi -> Hoàn trả người gửi tại quầy
        return await HoanHangTaiQuayAsync(maDon, true, tt);
    }

    /// <summary>Hàng đã rời tay người gửi (đã được bưu cục hoặc shipper tiếp nhận).</summary>
    public static bool DaLayHang(DonGiaoHang don) =>
        don.TrangThai != TrangThaiDon.DaTao && don.TrangThai != TrangThaiDon.ChoLayTanNoi;

    /// <summary>Khách sửa khi hàng chưa gửi đi; quản trị / điều phối sửa thêm khi hàng đang ở bưu cục gửi, chưa phân chặng.</summary>
    public static bool CoTheSua(DonGiaoHang don, bool laKhachHang) =>
        don.TrangThai is TrangThaiDon.DaTao or TrangThaiDon.ChoLayTanNoi || (!laKhachHang && don.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui);

    /// <summary>Khách hủy khi hàng chưa gửi đi; bưu cục / điều phối hủy khi hàng đang nằm ở bưu cục (chuyển hoàn).</summary>
    public static bool CoTheHuy(DonGiaoHang don, bool laKhachHang) =>
        don.TrangThai is TrangThaiDon.DaTao or TrangThaiDon.ChoLayTanNoi
        || (!laKhachHang && HienThi.NhomTaiBuuCuc.Contains(don.TrangThai) && don.MaBuuCucHienTai is not null);

    /// <summary>Chặng cần phân công tiếp theo của đơn; null = hiện không phân công được.</summary>
    public static LoaiShipper? ChangCanPhanCong(DonGiaoHang don) => don.TrangThai switch
    {
        TrangThaiDon.ChoLayTanNoi => LoaiShipper.KhuVuc,
        TrangThaiDon.DaNhanTaiBuuCucGui when don.MaBuuCucGui != don.MaBuuCucNhan => LoaiShipper.LienTinh,
        TrangThaiDon.DaDenBuuCucNhan => LoaiShipper.KhuVuc,
        TrangThaiDon.ChoChuyenHoan => LoaiShipper.LienTinh,
        TrangThaiDon.DaVeBuuCucGui => LoaiShipper.KhuVuc,
        _ => null
    };

    /// <summary>Phân công được (cần Include PhanCongs để đếm số lần giao thất bại của chặng khu vực).</summary>
    public static bool CoThePhanCong(DonGiaoHang don) => ChangCanPhanCong(don) switch
    {
        _ when don.TrangThai == TrangThaiDon.ChoLayTanNoi => !don.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
        _ when don.TrangThai == TrangThaiDon.ChoChuyenHoan => !don.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
        _ when don.TrangThai == TrangThaiDon.DaVeBuuCucGui => !don.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)),
        LoaiShipper.LienTinh => true,
        LoaiShipper.KhuVuc => SoLanThatBai(don) < SoLanGiaoToiDa,
        _ => false
    };

    /// <summary>Đổi shipper khi shipper được phân chưa đến nhận hàng (phương tiện gắn cố định theo shipper).</summary>
    public static bool CoTheDoiPhanCong(TrangThaiDon trangThai) =>
        trangThai is TrangThaiDon.ChoLayLienTinh or TrangThaiDon.ChoLayGiaoHang or TrangThaiDon.ChoLayTanNoi or TrangThaiDon.ChoChuyenHoan or TrangThaiDon.DaVeBuuCucGui;

    // =====================================================================
    // TRUY VẤN DÙNG CHUNG
    // =====================================================================

    /// <summary>
    /// [NHIỆM VỤ: TÌM KIẾM - LỌC ĐƠN HÀNG]
    /// Hàm xử lý điều kiện lọc và tìm kiếm động trên tầng dữ liệu (IQueryable - Deferred Execution).
    /// Toàn bộ các tiêu chí bên dưới được EF Core dịch trực tiếp thành các mệnh đề WHERE trong SQL Server:
    /// - Tìm kiếm từ khóa đa năng: Mã đơn (số hoặc chữ ĐH...), Tên người nhận (bỏ dấu/không phân biệt hoa-thường),
    ///   Số điện thoại người nhận (tự lọc ký tự số), Tên khách hàng (người gửi).
    /// - Bộ lọc danh mục: Theo Loại hàng hóa (MaLoaiHang), Khu vực giao (MaKhuVuc).
    /// - Bộ lọc bưu cục: Bưu cục gửi, Bưu cục nhận hoặc Bưu cục đang giữ hàng hiện tại.
    /// - Bộ lọc thời gian: Khoảng thời gian theo Ngày tạo (NgayTao) hoặc Ngày giao dự kiến (NgayGiaoDuKien).
    /// (Lưu ý: Không lọc theo trạng thái ở đây để Controller còn GroupBy đếm số lượng cho từng tab trạng thái).
    /// </summary>
    public static IQueryable<DonGiaoHang> LocDonHang(IQueryable<DonGiaoHang> truyVan, BoLocDonHang boLoc)
    {
        if (!string.IsNullOrWhiteSpace(boLoc.TuKhoa))
        {
            var tuKhoa = boLoc.TuKhoa.Trim();
            int? maDon = DinhDang.TachMaDon(tuKhoa);
            // Tên so khớp không phân biệt hoa thường và dấu: gõ "hoang" vẫn ra "Hoàng"
            string soDienThoai = new(tuKhoa.Where(char.IsDigit).ToArray());
            truyVan = truyVan.Where(d => (maDon != null && d.MaDon == maDon)
                                      || EF.Functions.Collate(d.TenNguoiNhan, "Latin1_General_100_CI_AI").Contains(tuKhoa)
                                      || (soDienThoai.Length >= 3 && d.SoDienThoaiNguoiNhan.Contains(soDienThoai))
                                      || EF.Functions.Collate(d.KhachHang!.HoTen, "Latin1_General_100_CI_AI").Contains(tuKhoa));
        }
        if (boLoc.MaLoaiHang.HasValue) truyVan = truyVan.Where(d => d.MaLoaiHang == boLoc.MaLoaiHang);
        if (boLoc.MaKhuVuc.HasValue) truyVan = truyVan.Where(d => d.MaKhuVuc == boLoc.MaKhuVuc);
        if (boLoc.MaBuuCuc.HasValue)
            truyVan = truyVan.Where(d => d.MaBuuCucGui == boLoc.MaBuuCuc || d.MaBuuCucNhan == boLoc.MaBuuCuc || d.MaBuuCucHienTai == boLoc.MaBuuCuc);
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

    /// <summary>
    /// [NHIỆM VỤ: SẮP XẾP ĐƠN HÀNG]
    /// Ghép mệnh đề ORDER BY vào truy vấn SQL: Mới nhất, Cũ nhất, Khối lượng tăng/giảm, Cước phí tăng/giảm, Tên A-Z / Z-A.
    /// </summary>
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

    /// <summary>Chi tiết đơn kèm khách hàng, danh mục, bưu cục, người đang giữ, các chặng và tracking event.</summary>
    public Task<DonGiaoHang?> LayChiTietDonAsync(int maDon) =>
        db.DonGiaoHangs.AsNoTracking()
          .Include(d => d.KhachHang).Include(d => d.LoaiHang).Include(d => d.KhuVuc)
          .Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.BuuCucHienTai)
          .Include(d => d.NhanVienDangGiu!).ThenInclude(n => n.PhuongTien)
          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien!).ThenInclude(n => n.TaiKhoan)
          .Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
          .Include(d => d.LichSus).ThenInclude(l => l.BuuCuc)
          .Include(d => d.LichSus).ThenInclude(l => l.NhanVien)
          .Include(d => d.LichSus).ThenInclude(l => l.PhuongTien)
          .Include(d => d.ChuyenXe!).ThenInclude(c => c.BuuCucGui)
          .Include(d => d.ChuyenXe!).ThenInclude(c => c.BuuCucNhan)
          .Include(d => d.ChuyenXe!).ThenInclude(c => c.BuuCucTrungGian)
          .Include(d => d.ChuyenXe!).ThenInclude(c => c.PhuongTien)
          .AsSplitQuery()
          .FirstOrDefaultAsync(d => d.MaDon == maDon);

    /// <summary>
    /// Vị trí hiện tại của đơn – đơn không có GPS riêng, suy ra từ đối tượng đang giữ:
    /// bưu cục (toạ độ bưu cục), shipper (GPS gần nhất của shipper + phương tiện), đã giao (địa chỉ nhận).
    /// Cần Include BuuCucGui, BuuCucHienTai, NhanVienDangGiu.PhuongTien.
    /// </summary>
    public ViTriDon XacDinhViTri(DonGiaoHang don)
    {
        if (don.MaBuuCucHienTai is not null && don.BuuCucHienTai is { } bc)
            return new ViTriDon("buucuc", $"Tại {bc.TenBuuCuc}", $"{bc.DiaChi}, {bc.TinhThanh}", bc.ViDo, bc.KinhDo, null, null);
        if (don.MaNhanVienDangGiu is not null && don.NhanVienDangGiu is { } nv)
        {
            string xe = nv.PhuongTien is null ? "" : $"{nv.PhuongTien.MaHienThi} – {nv.PhuongTien.BienSo}";
            string vaiTro = don.TrangThai is TrangThaiDon.DaLayLienTinh or TrangThaiDon.DangTrungChuyen or TrangThaiDon.DangChuyenHoan ? "shipper liên tỉnh" : "shipper khu vực";
            bool coViTri = nv.ViDo is not null && nv.KinhDo is not null && nv.ThoiGianViTri is not null;
            return new ViTriDon("shipper", $"Đang được {vaiTro} {nv.HoTen} vận chuyển",
                coViTri ? $"Vị trí GPS cập nhật {DinhDang.ThoiGianTuongDoi(nv.ThoiGianViTri!.Value)}"
                        : "Shipper chưa chia sẻ vị trí", nv.ViDo, nv.KinhDo, nv.ThoiGianViTri, xe);
        }
        return don.TrangThai switch
        {
            TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat => new ViTriDon("dagiao", "Đã giao tới người nhận", don.DiaChiNhan, null, null, null, null),
            TrangThaiDon.DaHoanHang => new ViTriDon("khach", "Đã hoàn trả người gửi", DinhDang.NgayGio(don.NgayHoanHang), null, null, null, null),
            TrangThaiDon.DaTao => new ViTriDon("khach", "Người gửi đang giữ hàng", $"Chờ mang tới {don.BuuCucGui?.TenBuuCuc ?? "bưu cục gửi"}", null, null, null, null),
            TrangThaiDon.ChoLayTanNoi => new ViTriDon("khach", "Chờ shipper đến lấy hàng", $"Địa chỉ: {don.DiaChiLayHang ?? "người gửi"} – Bưu cục phụ trách: {don.BuuCucGui?.TenBuuCuc}", null, null, null, null),
            TrangThaiDon.ChoChuyenHoan => new ViTriDon("buucuc", $"Tại {don.BuuCucHienTai?.TenBuuCuc ?? "bưu cục"}", "Chờ shipper liên tỉnh nhận chuyển hoàn", don.BuuCucHienTai?.ViDo, don.BuuCucHienTai?.KinhDo, null, null),
            TrangThaiDon.DaVeBuuCucGui => new ViTriDon("buucuc", $"Tại {don.BuuCucGui?.TenBuuCuc ?? "bưu cục gửi"}", "Chờ giao hoàn về người gửi", don.BuuCucGui?.ViDo, don.BuuCucGui?.KinhDo, null, null),
            TrangThaiDon.DaHuy when don.NgayHoanHang is not null => new ViTriDon("khach", "Đã hoàn trả người gửi", DinhDang.NgayGio(don.NgayHoanHang), null, null, null, null),
            TrangThaiDon.ThatLac => new ViTriDon("loi", "Không xác định", "Đơn hàng thất lạc", null, null, null, null),
            _ => new ViTriDon("khac", "Chưa có thông tin vị trí", "", null, null, null, null)
        };
    }

    /// <summary>Số liệu tổng quan của một khách hàng (dùng ở KhachHangTrangChu và QuanTriKhachHang/ChiTiet).</summary>
    public async Task<TongQuanKhachHangVM> TongQuanKhachHangAsync(KhachHang kh, int soDonGanDay)
    {
        var donCuaKhach = db.DonGiaoHangs.AsNoTracking().Where(d => d.MaKhachHang == kh.MaKhachHang);
        var dangXuLy = HienThi.NhomDangXuLy;
        return new TongQuanKhachHangVM
        {
            KhachHang = kh,
            TongDon = await donCuaKhach.CountAsync(),
            DangXuLy = await donCuaKhach.CountAsync(d => dangXuLy.Contains(d.TrangThai) && d.TrangThai != TrangThaiDon.GiaoKhongThanhCong),
            DaGiao = await donCuaKhach.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoThanhCong || d.TrangThai == TrangThaiDon.HoanTat),
            ThatBai = await donCuaKhach.CountAsync(d => d.TrangThai == TrangThaiDon.GiaoKhongThanhCong || d.TrangThai == TrangThaiDon.ThatLac || d.TrangThai == TrangThaiDon.HuHong),
            DaHuy = await donCuaKhach.CountAsync(d => d.TrangThai == TrangThaiDon.DaHuy),
            TongPhi = await donCuaKhach.Where(d => d.TrangThai != TrangThaiDon.DaHuy).SumAsync(d => (decimal?)d.PhiVanChuyen) ?? 0,
            DonGanDay = await donCuaKhach.Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                                         .OrderByDescending(d => d.NgayTao).Take(soDonGanDay).ToListAsync(),
            DonDangVanChuyen = await donCuaKhach.Include(d => d.KhuVuc)
                                         .Where(d => dangXuLy.Contains(d.TrangThai))
                                         .OrderByDescending(d => d.NgayTao).Take(5).ToListAsync(),
            SoTheoTrangThai = await donCuaKhach.GroupBy(d => d.TrangThai).ToDictionaryAsync(g => g.Key, g => g.Count()),
            DonNoPhi = await DonConNoPhi(donCuaKhach).OrderBy(d => d.NgayHoanTat).ToListAsync(),
            TheoThang = await PhiTheoThangAsync(donCuaKhach, 6)
        };
    }

    // =====================================================================
    // HÀM DÙNG CHUNG
    // =====================================================================

    /// <summary>Số lần giao không thành công của đơn (cần Include PhanCongs).</summary>
    public static int SoLanThatBai(DonGiaoHang don) => don.PhanCongs.Count(p => p.KetQua == KetQuaGiao.GiaoKhongThanhCong);

    /// <summary>Khối lượng đơn không được vượt tải trọng phương tiện lớn nhất hệ thống và đội xe phát khu vực nhận.</summary>
    private async Task<string?> KiemTraKhoiLuongAsync(decimal khoiLuong, int? maBuuCucNhan = null, int? maKhuVuc = null)
    {
        decimal lonNhat = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                              .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        if (khoiLuong > lonNhat)
            return $"Khối lượng {DinhDang.KhoiLuong(khoiLuong)} vượt tải trọng phương tiện lớn nhất ({DinhDang.KhoiLuong(lonNhat)}) – vui lòng chia thành nhiều đơn";

        if (maBuuCucNhan.HasValue)
        {
            var xeKhuVuc = db.NhanVienGiaoHangs
                .Where(n => n.LoaiShipper == LoaiShipper.KhuVuc && n.MaBuuCuc == maBuuCucNhan.Value
                         && n.TrangThai != TrangThaiNhanVien.NgungHoatDong && n.PhuongTien != null
                         && n.PhuongTien.TrangThai != TrangThaiPhuongTien.NgungHoatDong);

            if (maKhuVuc.HasValue && await xeKhuVuc.AnyAsync(n => n.MaKhuVucPhuTrach == maKhuVuc.Value))
                xeKhuVuc = xeKhuVuc.Where(n => n.MaKhuVucPhuTrach == maKhuVuc.Value);

            decimal? maxXePhat = await xeKhuVuc.MaxAsync(n => (decimal?)n.PhuongTien!.TaiTrongToiDa);
            if (maxXePhat.HasValue && khoiLuong > maxXePhat.Value)
                return $"Khối lượng {DinhDang.KhoiLuong(khoiLuong)} vượt tải trọng tối đa của đội xe phát hàng khu vực này ({DinhDang.KhoiLuong(maxXePhat.Value)}) – vui lòng chia thành nhiều đơn";
        }

        return null;
    }

    /// <summary>
    /// Sau khi giảm tải trọng / ngừng / xóa phương tiện: đơn đang ở bưu cục chờ phân chặng nặng hơn
    /// xe lớn nhất còn hoạt động thì không phân công được nữa → trả về cảnh báo; null = không có đơn nào bị ảnh hưởng.
    /// </summary>
    public async Task<string?> CanhBaoDonQuaTaiAsync()
    {
        decimal lonNhat = await db.PhuongTiens.Where(p => p.TrangThai != TrangThaiPhuongTien.NgungHoatDong)
                                              .MaxAsync(p => (decimal?)p.TaiTrongToiDa) ?? 0;
        var dsDon = await db.DonGiaoHangs.AsNoTracking()
            .Where(d => (d.TrangThai == TrangThaiDon.DaTao || d.TrangThai == TrangThaiDon.DaNhanTaiBuuCucGui
                         || d.TrangThai == TrangThaiDon.DaDenBuuCucNhan) && d.KhoiLuong > lonNhat)
            .OrderBy(d => d.MaDon).ToListAsync();
        if (dsDon.Count == 0) return null;
        return $"Cảnh báo: {dsDon.Count} đơn đang chờ vận chuyển nặng hơn tải trọng xe lớn nhất còn hoạt động ({DinhDang.KhoiLuong(lonNhat)}) – " +
               $"không phân công được: {string.Join(", ", dsDon.Take(5).Select(d => d.MaHienThi))}{(dsDon.Count > 5 ? "…" : "")}";
    }

    public IQueryable<PhanCongGiaoHang> PhanCongHieuLuc() =>
        db.PhanCongGiaoHangs.Where(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));

    /// <summary>Tổng khối lượng các đơn đang hiệu lực trên một phương tiện (LINQ Sum).</summary>
    public async Task<decimal> KhoiLuongDangChoAsync(int maPhuongTien, int? boQuaMaPhanCong) =>
        await PhanCongHieuLuc().Where(p => p.MaPhuongTien == maPhuongTien && p.MaPhanCong != boQuaMaPhanCong)
                               .SumAsync(p => (decimal?)p.DonGiaoHang!.KhoiLuong) ?? 0;

    /// <summary>
    /// Trạng thái sẵn sàng: nhân viên có chặng đang chạy / đang giao → Đang giao hàng, không còn → Sẵn sàng;
    /// phương tiện còn chặng hiệu lực → Đang sử dụng, không còn → Sẵn sàng. Không đụng Tạm nghỉ / Bảo trì / Ngừng.
    /// </summary>
    public async Task CapNhatNguonLucAsync(IEnumerable<int> dsMaNhanVien, IEnumerable<int> dsMaPhuongTien)
    {
        foreach (var ma in dsMaNhanVien.Distinct())
        {
            var nv = await db.NhanVienGiaoHangs.FindAsync(ma);
            if (nv is null || nv.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong) continue;
            bool dangChayChuyen = await db.ChuyenXes.AnyAsync(c => c.MaNhanVien == ma
                && c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy);
            bool dangGiao = dangChayChuyen || await db.PhanCongGiaoHangs.AnyAsync(p => p.MaNhanVien == ma && p.TrangThai == TrangThaiPhanCong.DangGiao);
            nv.TrangThai = dangGiao ? TrangThaiNhanVien.DangGiaoHang : TrangThaiNhanVien.SanSang;
        }
        foreach (var ma in dsMaPhuongTien.Distinct())
        {
            var pt = await db.PhuongTiens.FindAsync(ma);
            if (pt is null || pt.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong) continue;
            bool xeDangChayChuyen = await db.ChuyenXes.AnyAsync(c => c.MaPhuongTien == ma
                && c.TrangThai != TrangThaiChuyenXe.DaDenNoi && c.TrangThai != TrangThaiChuyenXe.DaHuy);
            bool dangDung = xeDangChayChuyen || await PhanCongHieuLuc().AnyAsync(p => p.MaPhuongTien == ma);
            pt.TrangThai = dangDung ? TrangThaiPhuongTien.DangSuDung : TrangThaiPhuongTien.SanSang;
        }
        await db.SaveChangesAsync();
    }

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

    /// <summary>Cập nhật đối tượng đang giữ hàng: một bưu cục HOẶC một shipper (hoặc không ai – đã giao / chuyển hoàn xong).</summary>
    private static void DatNguoiGiu(DonGiaoHang don, int? maBuuCuc, int? maNhanVien)
    {
        don.MaBuuCucHienTai = maBuuCuc;
        don.MaNhanVienDangGiu = maBuuCuc is null ? maNhanVien : null;
    }

    /// <summary>Nhân viên bưu cục chỉ thao tác trên đơn thuộc bưu cục của mình; quản trị / điều phối (MaBuuCuc = null) mọi bưu cục.</summary>
    private static string? KiemTraBuuCuc(ThaoTac tt, int? maBuuCucCuaDon) =>
        tt.MaBuuCuc is null || tt.MaBuuCuc == maBuuCucCuaDon ? null : "Đơn không thuộc bưu cục của bạn";

    /// <summary>Loại hàng / khu vực phải đang hoạt động; khi sửa đơn cũ thì cho giữ nguyên danh mục đã chọn trước đó.</summary>
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

    /// <summary>Bưu cục gửi (khách chọn) và bưu cục nhận (bưu cục phụ trách khu vực giao) phải tồn tại và đang hoạt động.</summary>
    private async Task<(BuuCuc?, BuuCuc?, string?)> LayBuuCucAsync(int? maBuuCucGui, KhuVuc khuVuc, DonGiaoHang? donCu)
    {
        if (maBuuCucGui is null) return (null, null, "Chọn bưu cục gửi hàng");
        var gui = await db.BuuCucs.FindAsync(maBuuCucGui);
        if (gui is null) return (null, null, "Bưu cục gửi không tồn tại");
        if (gui.TrangThai != TrangThaiHoatDong.HoatDong && donCu?.MaBuuCucGui != gui.MaBuuCuc)
            return (null, null, $"{gui.TenBuuCuc} đã ngừng nhận hàng");
        if (khuVuc.MaBuuCuc is null) return (null, null, $"Khu vực '{khuVuc.TenKhuVuc}' chưa có bưu cục phụ trách giao hàng");
        var nhan = await db.BuuCucs.FindAsync(khuVuc.MaBuuCuc);
        if (nhan is null || (nhan.TrangThai != TrangThaiHoatDong.HoatDong && donCu?.MaBuuCucNhan != nhan.MaBuuCuc))
            return (null, null, $"Bưu cục phụ trách khu vực '{khuVuc.TenKhuVuc}' đang tạm ngừng hoạt động");
        return (gui, nhan, null);
    }

    // =====================================================================
    // PHẦN VẬN CHUYỂN & QUÉT MÃ BƯU CỤC
    // =====================================================================
    // =====================================================================
    // QUÉT MÃ TẠI BƯU CỤC
    // =====================================================================

    /// <summary>Mã quét phải là mã vận đơn của chính đơn này (đối chiếu cả phần ngày nếu là mã vận đơn đầy đủ).</summary>
    public static bool KhopMa(DonGiaoHang don, string? maQuet)
    {
        var (ma, ngay) = DinhDang.TachMaVanDon(maQuet);
        return ma == don.MaDon && (ngay is null || ngay.Value.Date == don.NgayTao.Date);
    }

    /// <summary>Tìm đơn theo mã quét (để hiện thông tin + thao tác được phép trên trang quét).</summary>
    public async Task<DonGiaoHang?> TimTheoMaQuetAsync(string? maQuet)
    {
        var (ma, _) = DinhDang.TachMaVanDon(maQuet);
        if (ma is null) return null;
        var don = await LayChiTietDonAsync(ma.Value);
        return don is not null && KhopMa(don, maQuet) ? don : null;
    }

    /// <summary>
    /// RECEIVED_AT_ORIGIN_HUB: nhân viên bưu cục quét mã khi khách mang hàng tới. Khách gửi ở bưu cục khác bưu cục đã chọn
    /// thì cập nhật lại bưu cục gửi. Đơn nội vùng (gửi = nhận) chuyển thẳng sang chờ phân shipper khu vực.
    /// </summary>
    public async Task<KetQua> TiepNhanAsync(int maDon, string? maQuet, ThaoTac tt, bool daThuCuoc = false)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan)
                          .Include(d => d.KhuVuc).Include(d => d.LoaiHang)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!KhopMa(don, maQuet)) return KetQua.Loi("Mã quét không khớp mã vận đơn");
        if (don.TrangThai != TrangThaiDon.DaTao && don.TrangThai != TrangThaiDon.DaLayTanNoi)
            return KetQua.Loi($"Đơn {don.MaHienThi} đã được tiếp nhận trước đó (đang '{don.TrangThai.TenHienThi()}')");
        if (don.MaBuuCucNhan is null) return KetQua.Loi("Đơn chưa xác định bưu cục nhận – kiểm tra khu vực giao");

        string ghiChuDoi = "";
        if (tt.MaBuuCuc is not null && tt.MaBuuCuc != don.MaBuuCucGui)
        {
            var bcMoi = await db.BuuCucs.FindAsync(tt.MaBuuCuc);
            ghiChuDoi = $" (khách đăng ký gửi tại {don.BuuCucGui?.TenBuuCuc}, đổi sang {bcMoi?.TenBuuCuc})";
            don.MaBuuCucGui = tt.MaBuuCuc;
            don.BuuCucGui = bcMoi;

            if (don.MaBuuCucNhan.HasValue && don.NgayThuPhiNguoiGui == null && don.KhuVuc != null && don.LoaiHang != null)
            {
                var bcNhan = don.BuuCucNhan ?? await db.BuuCucs.FindAsync(don.MaBuuCucNhan.Value);
                decimal cuocCu = don.PhiVanChuyen;
                var phiMoi = ApDungPhi(don, don.KhuVuc, don.LoaiHang, bcMoi, bcNhan);
                if (phiMoi.Tong != cuocCu)
                {
                    ghiChuDoi += $" · Điều chỉnh cước theo bưu cục mới: {DinhDang.Tien(cuocCu)} ➔ {DinhDang.Tien(phiMoi.Tong)}";
                    don.PhiChuyenHoan = phiMoi.Tong * 0.5m;
                }
            }
        }
        int maBuuCuc = don.MaBuuCucGui!.Value;

        PhanCongGiaoHang? pcLay = null;
        if (don.TrangThai == TrangThaiDon.DaLayTanNoi)
        {
            pcLay = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.KhuVuc && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pcLay is not null)
            {
                pcLay.TrangThai = TrangThaiPhanCong.KetThuc;
                pcLay.KetQua = KetQuaGiao.DaLayHang;
                pcLay.NgayKetThuc = BayGio();
                pcLay.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.KhuVuc, KetQuaGiao.DaLayHang, pcLay.NgayKetThuc);
            }
        }

        if (daThuCuoc && don.PhiNguoiGuiPhaiTra > 0 && don.NgayThuPhiNguoiGui is null)
        {
            don.NgayThuPhiNguoiGui = BayGio();
            ghiChuDoi += $" · Đã thu cước người gửi {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} tại quầy";
        }

        string noiDungSuKien = pcLay is not null
            ? $"Tiếp nhận hàng từ shipper {pcLay.NhanVien?.HoTen} tại {don.BuuCucGui?.TenBuuCuc}{ghiChuDoi}"
            : $"Tiếp nhận hàng tại {don.BuuCucGui?.TenBuuCuc}{ghiChuDoi}";

        GhiSuKien(don, TrangThaiDon.DaNhanTaiBuuCucGui, noiDungSuKien,
                  tt, maBuuCuc: maBuuCuc, pc: pcLay, quetMa: true);
        DatNguoiGiu(don, maBuuCuc, null);
        if (don.MaBuuCucGui == don.MaBuuCucNhan)
            GhiSuKien(don, TrangThaiDon.DaDenBuuCucNhan, $"Đơn nội vùng – {don.BuuCucNhan?.TenBuuCuc} phát hàng, chờ phân shipper khu vực",
                      tt, maBuuCuc: maBuuCuc);
        var (luuOk, loiLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
        if (!luuOk) return KetQua.Loi(loiLuu!);
        if (pcLay is not null) await CapNhatNguonLucAsync([pcLay.MaNhanVien], [pcLay.MaPhuongTien]);
        return KetQua.Dat($"Đã tiếp nhận đơn {don.MaHienThi}" +
                          (don.TrangThai == TrangThaiDon.DaDenBuuCucNhan ? " – đơn nội vùng, phân shipper khu vực để giao" : " – chờ phân shipper liên tỉnh"));
    }

    /// <summary>
    /// ARRIVED_AT_DESTINATION_HUB: nhân viên bưu cục nhận quét mã khi shipper liên tỉnh bàn giao hàng.
    /// Chặng liên tỉnh kết thúc (Đã bàn giao bưu cục nhận) – chốt thù lao liên tỉnh.
    /// </summary>
    public async Task<KetQua> NhanHangDenAsync(int maDon, string? maQuet, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs
                          .Include(d => d.BuuCucGui)
                          .Include(d => d.BuuCucNhan)
                          .Include(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucGui)
                          .Include(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucNhan)
                          .Include(d => d.ChuyenXe).ThenInclude(c => c!.BuuCucTrungGian)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                          .Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!KhopMa(don, maQuet)) return KetQua.Loi("Mã quét không khớp mã vận đơn");

        if (don.TrangThai == TrangThaiDon.DangChuyenHoan)
        {
            if (KiemTraBuuCuc(tt, don.MaBuuCucGui) is string loiBcGui) return KetQua.Loi($"{loiBcGui} – đơn chuyển hoàn về {don.BuuCucGui?.TenBuuCuc}");
            var pcChuyenHoan = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
            if (pcChuyenHoan is null) return KetQua.Loi("Không tìm thấy chặng liên tỉnh chuyển hoàn đang hiệu lực của đơn");

            pcChuyenHoan.TrangThai = TrangThaiPhanCong.KetThuc;
            pcChuyenHoan.KetQua = KetQuaGiao.DaBanGiaoBuuCuc;
            pcChuyenHoan.NgayKetThuc = BayGio();
            pcChuyenHoan.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.LienTinh, KetQuaGiao.DaBanGiaoBuuCuc, pcChuyenHoan.NgayKetThuc);
            GhiSuKien(don, TrangThaiDon.DaVeBuuCucGui,
                $"{don.BuuCucGui?.TenBuuCuc} nhận hàng chuyển hoàn từ shipper liên tỉnh {pcChuyenHoan.NhanVien?.HoTen} – {pcChuyenHoan.PhuongTien?.MaHienThi} {pcChuyenHoan.PhuongTien?.BienSo}",
                tt, maBuuCuc: don.MaBuuCucGui, pc: pcChuyenHoan, quetMa: true);
            DatNguoiGiu(don, don.MaBuuCucGui, null);
            var (luuHoanOk, loiHoanLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
            if (!luuHoanOk) return KetQua.Loi(loiHoanLuu!);
            await CapNhatNguonLucAsync([pcChuyenHoan.MaNhanVien], [pcChuyenHoan.MaPhuongTien]);
            return KetQua.Dat($"Đã nhận đơn chuyển hoàn {don.MaHienThi} tại {don.BuuCucGui?.TenBuuCuc} – có thể phân shipper giao hoàn tận nơi hoặc hoàn trả tại quầy");
        }

        if (don.TrangThai is not (TrangThaiDon.DangTrungChuyen or TrangThaiDon.DaLayLienTinh))
            return KetQua.Loi($"Đơn không trong chặng liên tỉnh (đang '{don.TrangThai.TenHienThi()}')");

        var bcThaoTac = tt.MaBuuCuc.HasValue ? await db.BuuCucs.FindAsync(tt.MaBuuCuc.Value) : null;
        bool laHub = bcThaoTac != null && LaHubTrungTam(bcThaoTac);
        bool laDichChuyenXe = don.ChuyenXe != null && (don.ChuyenXe.MaBuuCucNhan == tt.MaBuuCuc || don.ChuyenXe.MaBuuCucTrungGian == tt.MaBuuCuc);
        bool laDichCuoi = tt.MaBuuCuc == null || tt.MaBuuCuc == don.MaBuuCucNhan;

        // Nếu kiện hàng đang đi trên chuyến xe cụ thể: CHỈ bưu cục đích của chuyến xe (hoặc trạm ghé trung gian) mới được quét dỡ hàng
        if (don.ChuyenXe != null)
        {
            if (!laDichChuyenXe)
                return KetQua.Loi($"Kiện hàng hiện đang trên Chuyến xe {don.ChuyenXe.MaHienThi} di chuyển đến {don.ChuyenXe.BuuCucNhan?.TenBuuCuc}. Bưu cục {bcThaoTac?.TenBuuCuc} chỉ có thể tiếp nhận khi hàng được trung chuyển tới.");
        }
        else
        {
            if (!laDichCuoi && !laHub)
                return KetQua.Loi($"Đơn phát tại {don.BuuCucNhan?.TenBuuCuc} (Bưu cục của bạn không phải bưu cục phát hay Hub/trạm trung chuyển của đơn)");
        }

        var pc = don.PhanCongs.FirstOrDefault(p => p.LoaiChang == LoaiShipper.LienTinh && PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc is null) return KetQua.Loi("Không tìm thấy chặng liên tỉnh đang hiệu lực của đơn");

        pc.TrangThai = TrangThaiPhanCong.KetThuc;
        pc.KetQua = KetQuaGiao.DaBanGiaoBuuCuc;
        pc.NgayKetThuc = BayGio();
        pc.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.LienTinh, KetQuaGiao.DaBanGiaoBuuCuc, pc.NgayKetThuc);

        int maBcTiepNhan = tt.MaBuuCuc ?? don.MaBuuCucNhan ?? 0;
        int? maChuyenXeHienTai = don.MaChuyenXe;

        bool laDichPhatCuoi = laDichCuoi && (don.ChuyenXe == null || don.ChuyenXe.MaBuuCucNhan == don.MaBuuCucNhan);

        if (laDichPhatCuoi)
        {
            // Đã đến bưu cục phát đích cuối cùng
            don.TrangThai = TrangThaiDon.DaDenBuuCucNhan;
            don.MaChuyenXe = null;
            DatNguoiGiu(don, don.MaBuuCucNhan, null);

            GhiSuKien(don, TrangThaiDon.DaDenBuuCucNhan,
                $"{don.BuuCucNhan?.TenBuuCuc} nhận hàng từ shipper liên tỉnh {pc.NhanVien?.HoTen} – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}",
                tt, maBuuCuc: don.MaBuuCucNhan, pc: pc, quetMa: true);
        }
        else
        {
            // Đến Hub trung tâm phân loại hoặc trạm trung chuyển trung gian
            don.TrangThai = TrangThaiDon.DaNhanTaiBuuCucGui; // Lưu kho tại Hub, sẵn sàng xếp chuyến tiếp
            don.MaChuyenXe = null;
            DatNguoiGiu(don, maBcTiepNhan, null);

            string tenBcTiepNhan = bcThaoTac?.TenBuuCuc ?? "Hub trung chuyển";
            GhiSuKien(don, TrangThaiDon.DaNhanTaiBuuCucGui,
                $"{tenBcTiepNhan} (Hub Phân loại) đã quét nhận kiện từ shipper liên tỉnh {pc.NhanVien?.HoTen} – Lưu kho phân loại để trung chuyển tiếp đến {don.BuuCucNhan?.TenBuuCuc}",
                tt, maBuuCuc: maBcTiepNhan, pc: pc, quetMa: true);
        }

        // Tự động đóng chuyến xe nếu tất cả kiện trên chuyến đều đã dỡ/tiếp nhận xong
        if (maChuyenXeHienTai.HasValue)
        {
            bool conDonTrenChuyen = await db.DonGiaoHangs.AnyAsync(d => d.MaDon != don.MaDon && d.MaChuyenXe == maChuyenXeHienTai.Value);
            if (!conDonTrenChuyen)
            {
                var cx = await db.ChuyenXes.Include(c => c.PhuongTien).FirstOrDefaultAsync(c => c.MaChuyen == maChuyenXeHienTai.Value);
                if (cx != null && cx.TrangThai == TrangThaiChuyenXe.DangVanChuyen)
                {
                    cx.TrangThai = TrangThaiChuyenXe.DaDenNoi;
                    cx.NgayDenNoi = BayGio();
                    if (cx.PhuongTien != null) cx.PhuongTien.TrangThai = TrangThaiPhuongTien.SanSang;
                }
            }
        }

        var (luuDenOk, loiDenLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
        if (!luuDenOk) return KetQua.Loi(loiDenLuu!);
        await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);

        return laDichCuoi
            ? KetQua.Dat($"Đã nhận đơn {don.MaHienThi} tại {don.BuuCucNhan?.TenBuuCuc} – phân shipper khu vực để giao")
            : KetQua.Dat($"Hub {bcThaoTac?.TenBuuCuc} đã nhận kiện trung chuyển {don.MaHienThi} – sẵn sàng xếp chuyến tiếp đến {don.BuuCucNhan?.TenBuuCuc}");
    }

    /// <summary>
    /// [NHIỆM VỤ: XỬ LÝ GIAO KHÔNG THÀNH CÔNG - TRẢ HÀNG VỀ BƯU CỤC]
    /// Shipper mang trả kiện hàng về bưu cục nhận khi giao thất bại (quá 3 lần hoặc không hẹn lại được):
    /// - Nhân viên bưu cục quét mã barcode/QR để tiếp nhận lại kiện hàng vào kho.
    /// - Kết thúc phân công của Shipper: KetQua = TraVeBuuCuc.
    /// - Chuyển quyền lưu giữ đơn về cho Bưu cục nhận (MaBuuCucHienTai = maBc, MaNhanVienDangGiu = null).
    /// - Chuyển trạng thái đơn về DaDenBuuCucNhan (chờ điều phối lại) hoặc DaVeBuuCucGui (nếu là đơn chuyển hoàn).
    /// </summary>
    public async Task<KetQua> NhanLaiTuShipperAsync(int maDon, string? maQuet, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucNhan).Include(d => d.BuuCucGui).Include(d => d.NhanVienDangGiu)
                          .Include(d => d.PhanCongs).Include(d => d.LichSus).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!KhopMa(don, maQuet)) return KetQua.Loi("Mã quét không khớp mã vận đơn");
        if (don.TrangThai is not (TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.HenGiaoLai or TrangThaiDon.DaLayGiaoHang or TrangThaiDon.DangGiaoHoan))
            return KetQua.Loi($"Chỉ nhận lại hàng shipper khu vực đang giữ (đơn đang '{don.TrangThai.TenHienThi()}')");

        bool laHangHoan = don.TrangThai == TrangThaiDon.DangGiaoHoan || don.LaDonHoan || don.PhiChuyenHoan > 0
                          || don.LichSus.Any(l => l.TrangThaiCu == TrangThaiDon.DangGiaoHoan || l.TrangThaiMoi == TrangThaiDon.DangGiaoHoan || l.TrangThaiMoi == TrangThaiDon.DaVeBuuCucGui || l.TrangThaiMoi == TrangThaiDon.ChoChuyenHoan);
        int? maBcTraVe = laHangHoan ? don.MaBuuCucGui : don.MaBuuCucNhan;
        if (KiemTraBuuCuc(tt, maBcTraVe) is string loiBc) return KetQua.Loi(loiBc);

        var pc = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc is not null)
        {
            pc.TrangThai = TrangThaiPhanCong.KetThuc;
            pc.KetQua = KetQuaGiao.TraVeBuuCuc;
            pc.NgayKetThuc = BayGio();
        }
        // Event ghi shipper đã bàn giao lại (kể cả khi lần giao thất bại đã kết thúc chặng trước đó)
        var pcGhi = pc ?? don.PhanCongs.Where(p => p.MaNhanVien == don.MaNhanVienDangGiu).OrderByDescending(p => p.MaPhanCong).FirstOrDefault();
        TrangThaiDon trangThaiTraVe = laHangHoan ? TrangThaiDon.DaVeBuuCucGui : TrangThaiDon.DaDenBuuCucNhan;
        var tenBuuCuc = (laHangHoan ? don.BuuCucGui?.TenBuuCuc : don.BuuCucNhan?.TenBuuCuc) ?? "Bưu cục";
        GhiSuKien(don, trangThaiTraVe,
            $"{tenBuuCuc} nhận lại hàng{(laHangHoan ? " hoàn" : "")} từ shipper {don.NhanVienDangGiu?.HoTen}" +
            $" ({SoLanThatBai(don)}/{SoLanGiaoToiDa} lần giao không thành công)", tt, maBuuCuc: maBcTraVe, pc: pcGhi, quetMa: true);
        DatNguoiGiu(don, maBcTraVe, null);
        var (luuLaiOk, loiLaiLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
        if (!luuLaiOk) return KetQua.Loi(loiLaiLuu!);
        if (pcGhi is not null) await CapNhatNguonLucAsync([pcGhi.MaNhanVien], [pcGhi.MaPhuongTien]);
        return KetQua.Dat($"Đã nhận lại đơn {don.MaHienThi} về bưu cục" +
                          (SoLanThatBai(don) >= SoLanGiaoToiDa ? " – đã giao lỗi tối đa, hãy liên hệ người gửi để hủy / chuyển hoàn" : ""));
    }

    // =====================================================================
    // PHÂN CÔNG / ĐỔI PHÂN CÔNG (chặng liên tỉnh / khu vực)
    // =====================================================================

    /// <summary>
    /// [NHIỆM VỤ: PHÂN CÔNG GIAO HÀNG]
    /// Điều phối viên hoặc Bưu cục phân công nhân viên giao hàng (Shipper) cho chặng tiếp theo:
    /// - Chặng lấy hàng tận nơi: Shipper khu vực đến địa chỉ người gửi lấy kiện hàng.
    /// - Chặng liên tỉnh: Shipper lái xe tải vận chuyển hàng giữa bưu cục gửi và bưu cục nhận (hoặc qua Hub).
    /// - Chặng khu vực: Shipper khu vực mang hàng từ bưu cục nhận đi giao tới người nhận (hoặc giao lại).
    /// - Chặng chuyển hoàn / giao hoàn: Vận chuyển kiện hàng trả về cho người gửi.
    /// Kiểm tra: Trạng thái đơn hợp lệ, Shipper sẵn sàng, xe không quá tải trọng, số lần giao lỗi < 3.
    /// Tạo bản ghi PhanCongGiaoHang mới (TrangThai = DaPhanCong) và ghi nhật ký sự kiện đơn hàng.
    /// </summary>
    public async Task<KetQua> PhanCongAsync(int maDon, int maNhanVien, string? ghiChu, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.BuuCucHienTai)
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        var loai = ChangCanPhanCong(don);
        if (loai == LoaiShipper.LienTinh)
            return KetQua.Loi($"Đơn hàng {don.MaHienThi} là chặng vận chuyển liên tỉnh. Vận chuyển liên tỉnh bắt buộc phải được điều phối và xếp vào Chuyến xe tải (Hub & Spoke). Vui lòng sử dụng tính năng Chuyến xe liên tỉnh!");
        if (loai is null)
            return KetQua.Loi($"Không thể phân công đơn ở trạng thái '{don.TrangThai.TenHienThi()}' " +
                              "(chỉ đơn đang ở bưu cục gửi chờ đi liên tỉnh, đã đến bưu cục nhận chờ giao, hoặc đang chờ chuyển hoàn)");
        if (loai == LoaiShipper.KhuVuc && don.TrangThai == TrangThaiDon.DaDenBuuCucNhan && SoLanThatBai(don) >= SoLanGiaoToiDa)
            return KetQua.Loi($"Đơn đã giao không thành công {SoLanGiaoToiDa} lần – không giao lại nữa, hãy liên hệ người gửi để hủy / chuyển hoàn");
        if (don.PhanCongs.Any(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai)))
            return KetQua.Loi("Đơn đã có một phân công đang hiệu lực");
        int? buuCucHienTai = don.TrangThai == TrangThaiDon.ChoLayTanNoi ? don.MaBuuCucGui : (don.MaBuuCucHienTai ?? don.MaBuuCucGui);
        if (KiemTraBuuCuc(tt, buuCucHienTai) is string loiBc) return KetQua.Loi(loiBc);
        if (await KiemTraNhanVienAsync(maNhanVien, don, loai.Value) is string loi) return KetQua.Loi(loi);

        var nv = await db.NhanVienGiaoHangs.Include(n => n.PhuongTien).FirstAsync(n => n.MaNhanVien == maNhanVien);
        var pc = new PhanCongGiaoHang
        {
            MaDon = maDon, MaNhanVien = maNhanVien, MaPhuongTien = nv.MaPhuongTien!.Value, LoaiChang = loai.Value,
            NgayPhanCong = BayGio(), TrangThai = TrangThaiPhanCong.DaPhanCong, GhiChu = DinhDang.Cat(ghiChu, DoDaiGhiChu), NguoiPhanCong = tt.NguoiThucHien
        };
        db.PhanCongGiaoHangs.Add(pc);
        int lanGiao = SoLanThatBai(don) + 1;
        string noiDung = don.TrangThai switch
        {
            TrangThaiDon.ChoLayTanNoi => $"Phân shipper khu vực {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo} đến lấy hàng tại {don.DiaChiLayHang ?? "địa chỉ người gửi"}",
            TrangThaiDon.ChoChuyenHoan => $"Phân shipper liên tỉnh {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo}, chuyển hoàn {don.BuuCucHienTai?.TenBuuCuc ?? don.BuuCucNhan?.TenBuuCuc} → {don.BuuCucGui?.TenBuuCuc}",
            TrangThaiDon.DaVeBuuCucGui => $"Phân shipper khu vực {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo} giao hoàn kiện hàng về cho người gửi",
            _ => (loai == LoaiShipper.LienTinh
                ? $"Phân shipper liên tỉnh {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo}, tuyến {don.BuuCucGui?.TenBuuCuc} → {don.BuuCucNhan?.TenBuuCuc}"
                : $"Phân shipper khu vực {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo}" + (lanGiao > 1 ? $" – giao lại lần {lanGiao}" : ""))
        };
        await db.SaveChangesAsync();   // lấy MaPhanCong trước khi ghi event

        var trangThaiMoi = don.TrangThai switch
        {
            TrangThaiDon.ChoLayTanNoi => TrangThaiDon.ChoLayTanNoi,
            TrangThaiDon.ChoChuyenHoan => TrangThaiDon.ChoChuyenHoan,
            TrangThaiDon.DaVeBuuCucGui => TrangThaiDon.DaVeBuuCucGui,
            _ => (loai == LoaiShipper.LienTinh ? TrangThaiDon.ChoLayLienTinh : TrangThaiDon.ChoLayGiaoHang)
        };

        GhiSuKien(don, trangThaiMoi, noiDung, tt, maBuuCuc: buuCucHienTai, pc: pc);
        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([maNhanVien], [pc.MaPhuongTien]);
        string thongBaoPc = don.TrangThai switch
        {
            TrangThaiDon.ChoLayTanNoi => $"Đã phân đơn {don.MaHienThi} cho {nv.HoTen} – shipper đến địa chỉ người gửi để lấy hàng",
            TrangThaiDon.ChoChuyenHoan => $"Đã phân shipper chuyển hoàn {don.MaHienThi} cho {nv.HoTen} – shipper tới {don.BuuCucHienTai?.TenBuuCuc ?? don.BuuCucNhan?.TenBuuCuc} quét mã nhận hàng",
            TrangThaiDon.DaVeBuuCucGui => $"Đã phân shipper giao hoàn {don.MaHienThi} cho {nv.HoTen} – shipper tới {don.BuuCucGui?.TenBuuCuc} quét mã nhận hàng để giao trả người gửi",
            _ => $"Đã phân đơn {don.MaHienThi} cho {nv.HoTen} – shipper đến {(loai == LoaiShipper.LienTinh ? don.BuuCucGui : don.BuuCucNhan)?.TenBuuCuc} quét mã nhận hàng"
        };
        return KetQua.Dat(thongBaoPc);
    }

    /// <summary>
    /// [NHIỆM VỤ: ĐỔI PHÂN CÔNG GIAO HÀNG]
    /// Thay đổi người giao hàng khi shipper cũ chưa đến bưu cục nhận hàng:
    /// - Chuyển phân công cũ sang trạng thái 'DaThayDoi' (lưu vết lịch sử phân công).
    /// - Tạo một bản ghi phân công mới gán cho Shipper mới được chỉ định.
    /// </summary>
    public async Task<KetQua> DoiPhanCongAsync(int maDon, int maNhanVien, string? lyDo, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!CoTheDoiPhanCong(don.TrangThai))
            return KetQua.Loi($"Chỉ đổi shipper khi shipper chưa đến nhận hàng (đơn đang '{don.TrangThai.TenHienThi()}')");
        var cu = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (cu is null) return KetQua.Loi("Đơn chưa có phân công đang hiệu lực");
        if (cu.MaNhanVien == maNhanVien) return KetQua.Loi("Shipper mới trùng với shipper hiện tại");
        if (string.IsNullOrWhiteSpace(lyDo)) return KetQua.Loi("Nhập lý do đổi shipper");
        int? buuCucHienTai = don.TrangThai == TrangThaiDon.ChoLayTanNoi ? don.MaBuuCucGui : don.MaBuuCucHienTai;
        if (KiemTraBuuCuc(tt, buuCucHienTai) is string loiBc) return KetQua.Loi(loiBc);
        if (await KiemTraNhanVienAsync(maNhanVien, don, cu.LoaiChang, cu.MaPhanCong) is string loi) return KetQua.Loi(loi);

        var bayGio = BayGio();
        cu.TrangThai = TrangThaiPhanCong.DaThayDoi;
        cu.NgayKetThuc = bayGio;
        cu.GhiChu = DinhDang.Cat(string.Join(" | ", new[] { cu.GhiChu, $"Đổi shipper: {lyDo.Trim()}" }.Where(s => !string.IsNullOrEmpty(s))), DoDaiGhiChu);

        var nv = await db.NhanVienGiaoHangs.Include(n => n.PhuongTien).FirstAsync(n => n.MaNhanVien == maNhanVien);
        var moi = new PhanCongGiaoHang
        {
            MaDon = maDon, MaNhanVien = maNhanVien, MaPhuongTien = nv.MaPhuongTien!.Value, LoaiChang = cu.LoaiChang, NgayPhanCong = bayGio,
            TrangThai = TrangThaiPhanCong.DaPhanCong, GhiChu = $"Thay cho phân công #{cu.MaPhanCong}", NguoiPhanCong = tt.NguoiThucHien
        };
        db.PhanCongGiaoHangs.Add(moi);
        await db.SaveChangesAsync();
        GhiSuKien(don, don.TrangThai,
            $"Đổi shipper: {cu.NhanVien!.HoTen} → {nv.HoTen} ({nv.MaHienThi}) – {nv.PhuongTien!.MaHienThi} {nv.PhuongTien.BienSo}. Lý do: {lyDo.Trim()}",
            tt, maBuuCuc: buuCucHienTai, pc: moi);
        BaoDoiPhanCong(don, cu, moi, nv, lyDo.Trim());
        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([cu.MaNhanVien, maNhanVien], [cu.MaPhuongTien, moi.MaPhuongTien]);
        return KetQua.Dat($"Đã đổi shipper đơn {don.MaHienThi} sang {nv.HoTen}");
    }

    // =====================================================================
    // SHIPPER: NHẬN HÀNG (quét / tận nơi) → CHẠY / GIAO → KẾT QUẢ
    // =====================================================================

    /// <summary>
    /// [NHIỆM VỤ: NHẬN HÀNG]
    /// Shipper xác nhận tiếp nhận kiện hàng (qua quét mã barcode/QR hoặc xác nhận lấy tại nhà khách):
    /// - Kiểm tra mã quét khớp với mã đơn hàng và Shipper được phân đúng đơn.
    /// - Chuyển trạng thái phân công: DaPhanCong -> DaNhanHang (ghi nhận thời gian nhận hàng).
    /// - Cập nhật trạng thái đơn: DaLayTanNoi / DaLayLienTinh / DaLayGiaoHang / DangChuyenHoan / DangGiaoHoan.
    /// - Bàn giao quyền lưu giữ kiện hàng cho Shipper (MaNhanVienDangGiu = maNhanVien, MaBuuCucHienTai = null).
    /// </summary>
    public async Task<KetQua> LayHangAsync(int maDon, string? maQuet, int maNhanVien, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucGui).Include(d => d.BuuCucHienTai).Include(d => d.BuuCucNhan).Include(d => d.PhanCongs).ThenInclude(p => p.PhuongTien)
                          .FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (!KhopMa(don, maQuet)) return KetQua.Loi("Mã quét không khớp mã vận đơn");
        var pc = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc is null || pc.MaNhanVien != maNhanVien) return KetQua.Loi("Bạn không được phân công nhận đơn này");
        if (pc.TrangThai != TrangThaiPhanCong.DaPhanCong) return KetQua.Loi("Bạn đã nhận đơn này trước đó");
        var moi = don.TrangThai switch
        {
            TrangThaiDon.ChoLayTanNoi => TrangThaiDon.DaLayTanNoi,
            TrangThaiDon.ChoLayLienTinh => TrangThaiDon.DaLayLienTinh,
            TrangThaiDon.ChoLayGiaoHang => TrangThaiDon.DaLayGiaoHang,
            TrangThaiDon.ChoChuyenHoan => TrangThaiDon.DangChuyenHoan,
            TrangThaiDon.DaVeBuuCucGui => TrangThaiDon.DangGiaoHoan,
            _ => (TrangThaiDon?)null
        };
        if (moi is null) return KetQua.Loi($"Đơn đang '{don.TrangThai.TenHienThi()}' – không nhận hàng được");

        pc.NgayNhanHang = BayGio();
        pc.TrangThai = TrangThaiPhanCong.DaNhanHang;
        string noiDungNhan = moi switch
        {
            TrangThaiDon.DaLayTanNoi => $"Shipper {tt.NguoiThucHien.Split(" (")[0]} đã lấy hàng tại {don.DiaChiLayHang ?? "địa chỉ người gửi"} – đang mang về {don.BuuCucGui?.TenBuuCuc} ({pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo})",
            TrangThaiDon.DangChuyenHoan => $"Shipper liên tỉnh {tt.NguoiThucHien.Split(" (")[0]} nhận hàng chuyển hoàn tại {don.BuuCucHienTai?.TenBuuCuc ?? don.BuuCucNhan?.TenBuuCuc} – xe {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}",
            TrangThaiDon.DangGiaoHoan => $"Shipper khu vực {tt.NguoiThucHien.Split(" (")[0]} nhận hàng tại {don.BuuCucGui?.TenBuuCuc} để đi giao hoàn trả người gửi – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}",
            _ => $"Shipper {tt.NguoiThucHien.Split(" (")[0]} nhận hàng tại {don.BuuCucHienTai?.TenBuuCuc} – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}"
        };

        GhiSuKien(don, moi.Value, noiDungNhan, tt, maBuuCuc: don.MaBuuCucHienTai ?? don.MaBuuCucGui, pc: pc, quetMa: true);
        DatNguoiGiu(don, null, maNhanVien);
        await db.SaveChangesAsync();
        string thongBaoNhan = moi switch
        {
            TrangThaiDon.DaLayTanNoi => $"Đã lấy đơn {don.MaHienThi} – mang về {don.BuuCucGui?.TenBuuCuc} để bưu cục tiếp nhận",
            TrangThaiDon.DaLayLienTinh => $"Đã nhận đơn {don.MaHienThi} – bấm Bắt đầu vận chuyển khi xuất bến",
            TrangThaiDon.DangChuyenHoan => $"Đã nhận đơn chuyển hoàn {don.MaHienThi} – bấm Bắt đầu vận chuyển khi xuất bến",
            TrangThaiDon.DangGiaoHoan => $"Đã nhận đơn {don.MaHienThi} – đi giao hoàn trả người gửi",
            _ => $"Đã nhận đơn {don.MaHienThi} – bấm Bắt đầu giao khi đi giao"
        };
        return KetQua.Dat(thongBaoNhan);
    }

    /// <summary>
    /// [NHIỆM VỤ: GIAO HÀNG - BẮT ĐẦU VẬN CHUYỂN / ĐI GIAO]
    /// Shipper bấm xuất phát vận chuyển đơn hàng:
    /// - Chuyển trạng thái phân công: DaNhanHang -> DangGiao.
    /// - Chuyển trạng thái đơn: DangTrungChuyen (nếu là xe tải liên tỉnh) hoặc DangGiao (nếu là shipper khu vực).
    /// </summary>
    public async Task<KetQua> BatDauAsync(int maPhanCong, int maNhanVien, ThaoTac tt)
    {
        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        var don = pc.DonGiaoHang!;
        if (pc.TrangThai != TrangThaiPhanCong.DaNhanHang) return KetQua.Loi("Chỉ bắt đầu được khi đã nhận hàng và chưa xuất phát");
        TrangThaiDon moi;
        string noiDung;
        if (pc.LoaiChang == LoaiShipper.LienTinh)
        {
            if (don.TrangThai != TrangThaiDon.DaLayLienTinh && don.TrangThai != TrangThaiDon.DangChuyenHoan) return KetQua.Loi($"Đơn đang '{don.TrangThai.TenHienThi()}'");
            if (don.TrangThai == TrangThaiDon.DangChuyenHoan)
            {
                moi = TrangThaiDon.DangChuyenHoan;
                noiDung = $"Bắt đầu chuyển hoàn {don.BuuCucNhan?.TenBuuCuc} → {don.BuuCucGui?.TenBuuCuc} – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}";
            }
            else
            {
                moi = TrangThaiDon.DangTrungChuyen;
                noiDung = $"Bắt đầu vận chuyển {don.BuuCucGui?.TenBuuCuc} → {don.BuuCucNhan?.TenBuuCuc} – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}";
            }
        }
        else
        {
            if (don.TrangThai is not (TrangThaiDon.DaLayGiaoHang or TrangThaiDon.HenGiaoLai or TrangThaiDon.DangGiaoHoan)) return KetQua.Loi($"Đơn đang '{don.TrangThai.TenHienThi()}'");
            if (don.TrangThai == TrangThaiDon.DangGiaoHoan)
            {
                moi = TrangThaiDon.DangGiaoHoan;
                noiDung = $"Bắt đầu giao hoàn kiện hàng về cho người gửi – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}";
            }
            else
            {
                moi = TrangThaiDon.DangGiao;
                noiDung = (don.TrangThai == TrangThaiDon.HenGiaoLai ? "Đi giao lại" : "Bắt đầu giao hàng") +
                          $" – khu vực {don.KhuVuc?.TenKhuVuc} – {pc.PhuongTien?.MaHienThi} {pc.PhuongTien?.BienSo}";
            }
        }
        pc.NgayBatDauGiao = BayGio();
        pc.TrangThai = TrangThaiPhanCong.DangGiao;
        GhiSuKien(don, moi, noiDung, tt, pc: pc);
        await db.SaveChangesAsync();
        await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
        return KetQua.Dat($"{don.MaHienThi}: {moi.TenHienThi()}");
    }

    /// <summary>Shipper liên tỉnh xuất bến: bắt đầu vận chuyển mọi đơn đã nhận, chưa chạy.</summary>
    public async Task<KetQua> BatDauTatCaLienTinhAsync(int maNhanVien, ThaoTac tt)
    {
        var dsMa = await PhanCongHieuLuc().Where(p => p.MaNhanVien == maNhanVien && p.LoaiChang == LoaiShipper.LienTinh
                                                    && p.TrangThai == TrangThaiPhanCong.DaNhanHang)
                                          .Select(p => p.MaPhanCong).ToListAsync();
        if (dsMa.Count == 0) return KetQua.Loi("Không có đơn nào đã nhận mà chưa xuất bến");
        foreach (var ma in dsMa)
        {
            var kq = await BatDauAsync(ma, maNhanVien, tt);
            if (!kq.ThanhCong) return kq;
        }
        return KetQua.Dat($"Đã bắt đầu vận chuyển {dsMa.Count} đơn");
    }

    /// <summary>
    /// [NHIỆM VỤ: GIAO HÀNG - XÁC NHẬN GIAO THÀNH CÔNG]
    /// Shipper khu vực xác nhận đã giao kiện hàng tận tay người nhận:
    /// - Hoàn tất phân công: TrangThai = KetThuc, KetQua = GiaoThanhCong.
    /// - Chốt thù lao / hoa hồng của Shipper cho đơn hàng (ThuNhapShipper).
    /// - Cập nhật trạng thái đơn thành GiaoThanhCong, giải phóng quyền giữ hàng (MaNhanVienDangGiu = null).
    /// - Ghi nhận số tiền Shipper đã thu hộ (COD + cước người nhận trả) vào công nợ / ví của Shipper.
    /// - Nếu đơn không cần thu tiền người nhận (COD = 0), hệ thống tự động hoàn tất và đối soát đơn hàng.
    /// </summary>
    public async Task<KetQua> GiaoThanhCongAsync(int maPhanCong, int maNhanVien, string? ghiChu, ThaoTac tt)
    {
        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        var don = pc.DonGiaoHang!;
        if (pc.LoaiChang != LoaiShipper.KhuVuc || pc.TrangThai != TrangThaiPhanCong.DangGiao
            || (don.TrangThai != TrangThaiDon.DangGiao && don.TrangThai != TrangThaiDon.DangGiaoHoan))
            return KetQua.Loi("Chỉ đơn Đang giao hàng mới được xác nhận giao thành công");

        if (don.TrangThai == TrangThaiDon.DangGiaoHoan)
        {
            pc.NgayKetThuc = BayGio();
            pc.TrangThai = TrangThaiPhanCong.KetThuc;
            pc.KetQua = KetQuaGiao.DaHoanHang;
            pc.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.KhuVuc, KetQuaGiao.DaHoanHang, pc.NgayKetThuc);
            if (!string.IsNullOrWhiteSpace(ghiChu)) pc.GhiChu = DinhDang.Cat(ghiChu, DoDaiGhiChu);

            don.NgayHoanHang = BayGio();
            string daThuHoan = "";
            decimal phaiThu = don.PhiHoanChuaThu;
            if (phaiThu > 0)
            {
                don.NgayThuPhiNguoiGui ??= BayGio();
                don.NgayThuPhiHoan ??= BayGio();
                daThuHoan = $" – shipper đã thu cước người gửi {DinhDang.Tien(phaiThu)}";
            }

            GhiSuKien(don, TrangThaiDon.DaHoanHang, $"Giao hoàn hàng thành công về người gửi{daThuHoan}" +
                      (string.IsNullOrWhiteSpace(ghiChu) ? "" : $" – {ghiChu.Trim()}"), tt, pc: pc);
            DatNguoiGiu(don, null, null);
            var (luuHoanOk, loiHoanLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
            if (!luuHoanOk) return KetQua.Loi(loiHoanLuu!);
            await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
            return KetQua.Dat($"{don.MaHienThi}: Giao hoàn thành công về người gửi");
        }

        pc.NgayKetThuc = BayGio();
        pc.TrangThai = TrangThaiPhanCong.KetThuc;
        pc.KetQua = KetQuaGiao.GiaoThanhCong;
        pc.ThuNhapShipper = TinhThuNhapShipper(don, LoaiShipper.KhuVuc, KetQuaGiao.GiaoThanhCong, pc.NgayKetThuc);   // chốt thù lao tại thời điểm giao
        if (!string.IsNullOrWhiteSpace(ghiChu)) pc.GhiChu = DinhDang.Cat(ghiChu, DoDaiGhiChu);
        string daThu = don.TongThuNguoiNhan > 0 ? $" – đã thu người nhận {DinhDang.Tien(don.TongThuNguoiNhan)}" : "";
        GhiSuKien(don, TrangThaiDon.GiaoThanhCong, $"Giao hàng thành công tới {don.DiaChiNhan}{daThu}" +
                  (string.IsNullOrWhiteSpace(ghiChu) ? "" : $" – {ghiChu.Trim()}"), tt, pc: pc);
        DatNguoiGiu(don, null, null);
        var (luuGiaoOk, loiGiaoLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
        if (!luuGiaoOk) return KetQua.Loi(loiGiaoLuu!);
        await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);

        // Đơn không có tiền thu của người nhận (0 đ): tự động đối soát & hoàn tất
        if (don.TongThuNguoiNhan == 0)
        {
            await HoanTatAsync(don.MaDon, $"Hệ thống tự động – {don.MaHienThi} không có tiền thu của người nhận", tuGiaoDichVi: false);
            return KetQua.Dat($"{don.MaHienThi}: Giao thành công & tự động hoàn tất");
        }

        return KetQua.Dat($"{don.MaHienThi}: Giao thành công");
    }

    /// <summary>
    /// [NHIỆM VỤ: XỬ LÝ GIAO KHÔNG THÀNH CÔNG - GHI NHẬN THẤT BẠI]
    /// Shipper ghi nhận giao hàng thất bại khi không giao được cho khách:
    /// - Bắt buộc chọn Lý do thất bại (Khách không nghe máy, sai địa chỉ, khách hẹn lại, từ chối nhận...).
    /// - Kết thúc lượt giao hiện tại: TrangThai = KetThuc, KetQua = GiaoKhongThanhCong.
    /// - Cập nhật trạng thái đơn thành GiaoKhongThanhCong; Shipper VẪN TẠM GIỮ HÀNG trên xe (chưa trả về bưu cục).
    /// - Tính số lần thất bại (tối đa 3 lần): Cho phép shipper hẹn ngày giao lại hoặc mang trả kiện hàng về bưu cục.
    /// </summary>
    public async Task<KetQua> GiaoThatBaiAsync(int maPhanCong, int maNhanVien, LyDoThatBai? lyDo, string? chiTiet, ThaoTac tt)
    {
        if (lyDo is null) return KetQua.Loi("Bắt buộc chọn lý do giao không thành công");
        if (lyDo == LyDoThatBai.Khac && string.IsNullOrWhiteSpace(chiTiet))
            return KetQua.Loi("Vui lòng mô tả lý do khi chọn 'Lý do khác'");

        var (pc, loi) = await LayPhanCongCuaNhanVienAsync(maPhanCong, maNhanVien);
        if (pc is null) return KetQua.Loi(loi!);
        var don = pc.DonGiaoHang!;
        if (pc.LoaiChang != LoaiShipper.KhuVuc || pc.TrangThai != TrangThaiPhanCong.DangGiao
            || (don.TrangThai != TrangThaiDon.DangGiao && don.TrangThai != TrangThaiDon.DangGiaoHoan))
            return KetQua.Loi("Chỉ đơn Đang giao hàng hoặc Đang giao hoàn mới được ghi nhận giao không thành công");

        pc.NgayKetThuc = BayGio();
        pc.TrangThai = TrangThaiPhanCong.KetThuc;
        pc.KetQua = KetQuaGiao.GiaoKhongThanhCong;
        pc.LyDoThatBai = lyDo;
        pc.GhiChu = chiTiet?.Trim();
        string moTa = lyDo.Value.TenHienThi() + (string.IsNullOrWhiteSpace(chiTiet) ? "" : $" – {chiTiet.Trim()}");
        GhiSuKien(don, TrangThaiDon.GiaoKhongThanhCong, $"Giao không thành công. Lý do: {moTa}", tt, pc: pc);
        // shipper vẫn giữ hàng (MaNhanVienDangGiu không đổi)
        var (luuTbOk, loiTbLuu) = await LuuThayDoiAnToanAsync(don.MaHienThi);
        if (!luuTbOk) return KetQua.Loi(loiTbLuu!);
        await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
        int conLai = SoLanGiaoToiDa - await db.PhanCongGiaoHangs.CountAsync(p => p.MaDon == don.MaDon && p.KetQua == KetQuaGiao.GiaoKhongThanhCong);
        return KetQua.Dat($"{don.MaHienThi}: Giao không thành công – " +
                          (conLai > 0 ? $"hẹn giao lại (còn {conLai} lần) hoặc trả hàng về bưu cục" : "đã hết lượt giao, trả hàng về bưu cục"));
    }

    /// <summary>
    /// [NHIỆM VỤ: XỬ LÝ GIAO KHÔNG THÀNH CÔNG - HẸN GIAO LẠI (RESCHEDULED)]
    /// Sau lần giao thất bại, Shipper liên hệ được với người nhận và hẹn ngày giao lại:
    /// - Cập nhật ngày hẹn giao dự kiến mới (NgayGiaoDuKien).
    /// - Tạo bản ghi phân công mới cho đợt giao tiếp theo ở trạng thái 'DaNhanHang' (vì shipper đã có sẵn hàng trên xe).
    /// - Chuyển trạng thái đơn thành 'HenGiaoLai', chờ tới ngày hẹn shipper bấm 'Bắt đầu giao' để đi giao tiếp.
    /// </summary>
    public async Task<KetQua> HenGiaoLaiAsync(int maDon, int maNhanVien, DateTime? ngayHen, string? ghiChu, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai != TrangThaiDon.GiaoKhongThanhCong || don.MaNhanVienDangGiu != maNhanVien)
            return KetQua.Loi("Chỉ hẹn giao lại đơn bạn đang giữ sau lần giao không thành công");
        if (SoLanThatBai(don) >= SoLanGiaoToiDa)
            return KetQua.Loi($"Đơn đã giao không thành công {SoLanGiaoToiDa} lần – trả hàng về bưu cục");
        var homNay = BayGio().Date;
        if (ngayHen is not null && ngayHen.Value.Date < homNay) return KetQua.Loi("Ngày hẹn không được trước hôm nay");
        var nv = await db.NhanVienGiaoHangs.Include(n => n.PhuongTien).FirstAsync(n => n.MaNhanVien == maNhanVien);
        var lanTruoc = don.PhanCongs.Where(p => p.MaNhanVien == maNhanVien).OrderByDescending(p => p.MaPhanCong).First();

        var bayGio = BayGio();
        var pc = new PhanCongGiaoHang
        {
            MaDon = maDon, MaNhanVien = maNhanVien, MaPhuongTien = nv.MaPhuongTien ?? lanTruoc.MaPhuongTien, LoaiChang = LoaiShipper.KhuVuc,
            NgayPhanCong = bayGio, NgayNhanHang = bayGio, TrangThai = TrangThaiPhanCong.DaNhanHang, NguoiPhanCong = tt.NguoiThucHien,
            GhiChu = DinhDang.Cat($"Hẹn giao lại sau phân công #{lanTruoc.MaPhanCong}" + (string.IsNullOrWhiteSpace(ghiChu) ? "" : $": {ghiChu.Trim()}"), DoDaiGhiChu)
        };
        db.PhanCongGiaoHangs.Add(pc);
        if (ngayHen is not null) don.NgayGiaoDuKien = ngayHen.Value.Date;
        await db.SaveChangesAsync();
        GhiSuKien(don, TrangThaiDon.HenGiaoLai,
            $"Hẹn giao lại {(ngayHen is null ? "" : $"ngày {ngayHen:dd/MM/yyyy}")} – lần {SoLanThatBai(don) + 1}/{SoLanGiaoToiDa}" +
            (string.IsNullOrWhiteSpace(ghiChu) ? "" : $" – {ghiChu.Trim()}"), tt, pc: pc);
        await db.SaveChangesAsync();
        return KetQua.Dat($"{don.MaHienThi}: đã hẹn giao lại – bấm Bắt đầu giao khi đi giao");
    }

    // =====================================================================
    // SỰ CỐ: THẤT LẠC / HƯ HỎNG
    // =====================================================================

    /// <summary>LOST / DAMAGED: đơn đang ở bưu cục hoặc trong tay shipper gặp sự cố – kết thúc vận chuyển.</summary>
    public async Task<KetQua> BaoSuCoAsync(int maDon, TrangThaiDon loai, string? moTa, ThaoTac tt)
    {
        if (loai is not (TrangThaiDon.ThatLac or TrangThaiDon.HuHong)) return KetQua.Loi("Loại sự cố không hợp lệ");
        if (string.IsNullOrWhiteSpace(moTa)) return KetQua.Loi("Mô tả sự cố");
        var don = await db.DonGiaoHangs.Include(d => d.PhanCongs).Include(d => d.NhanVienDangGiu).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai == TrangThaiDon.DaTao || don.TrangThai.DaKetThuc())
            return KetQua.Loi($"Không ghi nhận sự cố cho đơn đang '{don.TrangThai.TenHienThi()}'");
        // Nhân viên bưu cục: hàng đang ở bưu cục mình, hoặc shipper của bưu cục mình đang giữ
        if (tt.MaBuuCuc is not null && don.MaBuuCucHienTai != tt.MaBuuCuc && don.NhanVienDangGiu?.MaBuuCuc != tt.MaBuuCuc)
            return KetQua.Loi("Đơn không thuộc bưu cục của bạn");

        var pc = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc is not null)
        {
            pc.TrangThai = TrangThaiPhanCong.KetThuc;
            pc.KetQua = KetQuaGiao.SuCo;
            pc.NgayKetThuc = BayGio();
            pc.GhiChu = DinhDang.Cat(moTa, DoDaiGhiChu);
        }
        string noiGiu = don.MaBuuCucHienTai is not null ? "tại bưu cục" : don.NhanVienDangGiu is { } nv ? $"khi shipper {nv.HoTen} đang giữ" : "";
        GhiSuKien(don, loai, $"{loai.TenHienThi()} {noiGiu}: {moTa.Trim()}", tt, maBuuCuc: don.MaBuuCucHienTai, pc: pc);
        // Hàng hư hỏng vẫn nằm ở bưu cục; thất lạc / hư hỏng trên đường thì không còn ai giữ
        if (loai == TrangThaiDon.ThatLac || don.MaBuuCucHienTai is null) DatNguoiGiu(don, null, null);
        await db.SaveChangesAsync();


        if (pc is not null) await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
        return KetQua.Dat($"Đã ghi nhận đơn {don.MaHienThi}: {loai.TenHienThi()}");
    }

    // =====================================================================
    // KIỂM TRA SHIPPER NHẬN ĐƯỢC CHẶNG (LINQ)
    // =====================================================================

    /// <summary>Lý do shipper KHÔNG nhận được chặng của đơn; null = hợp lệ.</summary>
    public async Task<string?> KiemTraNhanVienAsync(int maNhanVien, DonGiaoHang don, LoaiShipper loai, int? boQuaMaPhanCong = null)
    {
        var nv = await db.NhanVienGiaoHangs.Include(n => n.TaiKhoan).Include(n => n.PhuongTien).Include(n => n.KhuVucPhuTrach)
                         .FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);
        if (nv is null) return "Nhân viên không tồn tại";
        var hieuLuc = await PhanCongHieuLuc().Where(p => p.MaNhanVien == maNhanVien && p.MaPhanCong != boQuaMaPhanCong)
                                             .Select(p => p.DonGiaoHang!.KhoiLuong).ToListAsync();
        return LyDoKhongNhan(nv, don, loai, hieuLuc.Count, hieuLuc.Sum());
    }

    /// <summary>Quy tắc chọn shipper cho một chặng (dùng chung cho kiểm tra khi lưu và danh sách lựa chọn trên form).</summary>
    private static string? LyDoKhongNhan(NhanVienGiaoHang nv, DonGiaoHang don, LoaiShipper loai, int soDonDangGiu, decimal khoiLuongDangCho)
    {
        if (nv.TrangThai is TrangThaiNhanVien.TamNghi or TrangThaiNhanVien.NgungHoatDong) return $"Đang {nv.TrangThai.TenHienThi().ToLower()}";
        if (nv.TaiKhoan?.TrangThai == TrangThaiTaiKhoan.BiKhoa) return "Tài khoản bị khóa";
        if (nv.LoaiShipper != loai) return $"Là {nv.LoaiShipper.TenHienThi().ToLower()}";
        int? buuCucCan = don.TrangThai == TrangThaiDon.ChoChuyenHoan
            ? (don.MaBuuCucHienTai ?? don.MaBuuCucNhan)
            : ((don.TrangThai == TrangThaiDon.DaVeBuuCucGui || loai == LoaiShipper.LienTinh || don.TrangThai == TrangThaiDon.ChoLayTanNoi)
                ? don.MaBuuCucGui
                : don.MaBuuCucNhan);
        if (nv.MaBuuCuc != buuCucCan) return "Không thuộc bưu cục " + (buuCucCan == don.MaBuuCucGui ? "gửi" : (buuCucCan == don.MaBuuCucHienTai ? "hiện tại" : "nhận")) + " của đơn";
        if (don.TrangThai != TrangThaiDon.ChoLayTanNoi && don.TrangThai != TrangThaiDon.DaVeBuuCucGui && loai == LoaiShipper.KhuVuc && nv.MaKhuVucPhuTrach != don.MaKhuVuc)
            return $"Phụ trách khu vực {nv.KhuVucPhuTrach?.TenKhuVuc ?? "khác"}";
        if (nv.PhuongTien is null) return "Chưa được gán phương tiện";
        if (nv.PhuongTien.TrangThai is TrangThaiPhuongTien.BaoTri or TrangThaiPhuongTien.NgungHoatDong)
            return $"Phương tiện {nv.PhuongTien.BienSo} đang {nv.PhuongTien.TrangThai.TenHienThi().ToLower()}";
        if (loai == LoaiShipper.KhuVuc && soDonDangGiu >= SoDonToiDa) return $"Đã giữ tối đa {SoDonToiDa} đơn";
        if (khoiLuongDangCho + don.KhoiLuong > nv.PhuongTien.TaiTrongToiDa)
            return $"Quá tải: đang chở {DinhDang.KhoiLuong(khoiLuongDangCho)} + {DinhDang.KhoiLuong(don.KhoiLuong)} > {DinhDang.KhoiLuong(nv.PhuongTien.TaiTrongToiDa)}";
        return null;
    }

    /// <summary>
    /// Danh sách shipper cho form phân công: chỉ shipper đúng loại chặng thuộc bưu cục cần (liên tỉnh hoặc lấy tận nơi: bưu cục gửi;
    /// khu vực giao hàng: bưu cục nhận), kèm phương tiện được gán và lý do không chọn được.
    /// </summary>
    public async Task<List<LuaChonNhanVien>> LayLuaChonAsync(DonGiaoHang don, LoaiShipper loai, int? boQuaMaPhanCong)
    {
        int? buuCucCan = don.TrangThai == TrangThaiDon.ChoChuyenHoan
            ? (don.MaBuuCucHienTai ?? don.MaBuuCucNhan)
            : ((don.TrangThai == TrangThaiDon.DaVeBuuCucGui || loai == LoaiShipper.LienTinh || don.TrangThai == TrangThaiDon.ChoLayTanNoi)
                ? don.MaBuuCucGui
                : don.MaBuuCucNhan);
        var hieuLuc = await PhanCongHieuLuc().Where(p => p.MaPhanCong != boQuaMaPhanCong)
            .Select(p => new { p.MaNhanVien, p.DonGiaoHang!.KhoiLuong }).ToListAsync();
        var dsNhanVien = await db.NhanVienGiaoHangs.AsNoTracking()
            .Include(n => n.KhuVucPhuTrach).Include(n => n.TaiKhoan).Include(n => n.PhuongTien)
            .Where(n => n.LoaiShipper == loai && n.MaBuuCuc == buuCucCan)
            .OrderBy(n => n.HoTen).ToListAsync();
        return dsNhanVien.Select(n =>
        {
            var cuaNv = hieuLuc.Where(h => h.MaNhanVien == n.MaNhanVien).ToList();
            decimal dangCho = cuaNv.Sum(h => h.KhoiLuong);
            string? lyDo = LyDoKhongNhan(n, don, loai, cuaNv.Count, dangCho);
            bool cungKv = don.TrangThai is TrangThaiDon.ChoLayTanNoi or TrangThaiDon.DaVeBuuCucGui || (n.MaKhuVucPhuTrach == don.MaKhuVuc);
            return new LuaChonNhanVien(n.MaNhanVien, n.HoTen, n.MaHienThi, n.KhuVucPhuTrach?.TenKhuVuc, cungKv,
                                       n.TrangThai, cuaNv.Count, n.PhuongTien?.MaHienThi, n.PhuongTien?.BienSo, n.PhuongTien?.LoaiPhuongTien,
                                       n.PhuongTien?.TaiTrongToiDa ?? 0, dangCho, lyDo is null, lyDo);
        }).OrderByDescending(x => x.DuocChon).ThenByDescending(x => x.CungKhuVuc).ThenBy(x => x.SoDonDangGiu).ToList();
    }

    private async Task<(PhanCongGiaoHang?, string?)> LayPhanCongCuaNhanVienAsync(int maPhanCong, int maNhanVien)
    {
        var pc = await db.PhanCongGiaoHangs.Include(p => p.PhuongTien)
                         .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucGui)
                         .Include(p => p.DonGiaoHang!).ThenInclude(d => d.BuuCucNhan)
                         .Include(p => p.DonGiaoHang!).ThenInclude(d => d.KhuVuc)
                         .FirstOrDefaultAsync(p => p.MaPhanCong == maPhanCong);
        if (pc is null) return (null, "Không tìm thấy phân công");
        if (pc.MaNhanVien != maNhanVien) return (null, "Bạn không được phân công đơn này");
        if (!PhanCongGiaoHang.TrangThaiHieuLuc.Contains(pc.TrangThai)) return (null, "Phân công này đã kết thúc");
        return (pc, null);
    }

    // =====================================================================
    // PHẦN TÍNH PHÍ, ĐỐI SOÁT & TRA CỨU
    // =====================================================================
    // =====================================================================
    // TÍNH PHÍ
    // =====================================================================

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

    // =====================================================================
    // HOÀN TẤT & ĐỐI SOÁT THU HỘ
    // =====================================================================

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
        if (don.NguoiTraPhi == NguoiTraPhi.NguoiGui && don.NgayThuPhiNguoiGui == null && don.TienThuHo >= don.PhiVanChuyen)
        {
            don.NgayThuPhiNguoiGui = don.NgayHoanTat;
        }
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

    /// <summary>
    /// Yêu cầu chuyển hoàn đơn giao không thành công:
    /// Tính phí chuyển hoàn (50% phí chiều đi, làm tròn lên 1.000 đ).
    /// Nếu đơn liên tỉnh đang ở bưu cục nhận -> ChoChuyenHoan (chờ xe tải liên tỉnh chuyển hoàn về bưu cục gửi).
    /// Nếu đơn nội tỉnh (cùng bưu cục gửi & nhận) -> DaVeBuuCucGui (đang ở bưu cục gửi rồi, chờ giao hoàn cho người gửi).
    /// </summary>
    public async Task<KetQua> YeuCauChuyenHoanAsync(int maDon, string? lyDo, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucGui).Include(d => d.BuuCucNhan).Include(d => d.BuuCucHienTai)
                          .Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai != TrangThaiDon.DaDenBuuCucNhan && don.TrangThai != TrangThaiDon.GiaoKhongThanhCong && don.TrangThai != TrangThaiDon.HenGiaoLai && don.TrangThai != TrangThaiDon.DaHuy)
            return KetQua.Loi($"Chỉ chuyển hoàn được đơn giao không thành công hoặc đã hủy (đơn đang '{don.TrangThai.TenHienThi()}')");
        if (don.MaNhanVienDangGiu is not null)
            return KetQua.Loi("Shipper đang giữ hàng – cần bưu cục nhận lại hàng từ shipper trước khi chuyển hoàn");
        if (KiemTraBuuCuc(tt, don.MaBuuCucHienTai ?? don.MaBuuCucNhan ?? don.MaBuuCucGui) is string loiBc) return KetQua.Loi(loiBc);

        // Chấm dứt các phân công còn hiệu lực nếu có
        var pcCu = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pcCu is not null)
        {
            pcCu.TrangThai = TrangThaiPhanCong.KetThuc;
            pcCu.KetQua = KetQuaGiao.TraVeBuuCuc;
            pcCu.NgayKetThuc = BayGio();
        }

        bool laLienTinh = don.MaBuuCucGui != don.MaBuuCucNhan && don.MaBuuCucHienTai != don.MaBuuCucGui;
        // Tính phí chuyển hoàn: 50% cước chiều đi nếu liên tỉnh, làm tròn lên 1000đ; miễn phí nếu chưa rời bưu cục gửi
        don.PhiChuyenHoan = laLienTinh ? Math.Ceiling((don.PhiVanChuyen * 0.5m) / 1000m) * 1000m : 0;

        TrangThaiDon trangThaiMoi = laLienTinh ? TrangThaiDon.ChoChuyenHoan : TrangThaiDon.DaVeBuuCucGui;
        int? maBc = laLienTinh ? (don.MaBuuCucHienTai ?? don.MaBuuCucNhan) : don.MaBuuCucGui;
        don.MaBuuCucHienTai = maBc;

        string moTa = laLienTinh
            ? $"Yêu cầu chuyển hoàn về bưu cục gửi {don.BuuCucGui?.TenBuuCuc} – Phí chuyển hoàn: {DinhDang.Tien(don.PhiChuyenHoan)}" +
              (string.IsNullOrWhiteSpace(lyDo) ? "" : $". Lý do: {lyDo.Trim()}")
            : $"Hàng lưu tại bưu cục gửi {don.BuuCucGui?.TenBuuCuc} – Miễn phí chuyển hoàn (chưa luân chuyển)" +
              (string.IsNullOrWhiteSpace(lyDo) ? "" : $". Lý do: {lyDo.Trim()}");

        GhiSuKien(don, trangThaiMoi, moTa, tt, maBuuCuc: maBc, pc: pcCu);
        await db.SaveChangesAsync();
        if (pcCu is not null) await CapNhatNguonLucAsync([pcCu.MaNhanVien], [pcCu.MaPhuongTien]);

        return KetQua.Dat(laLienTinh
            ? $"Đã yêu cầu chuyển hoàn đơn {don.MaHienThi} – phí chuyển hoàn {DinhDang.Tien(don.PhiChuyenHoan)}. Bưu cục {don.BuuCucHienTai?.TenBuuCuc} phân shipper liên tỉnh xe tải chuyển về {don.BuuCucGui?.TenBuuCuc}."
            : $"Đã chuyển đơn {don.MaHienThi} sang trạng thái chờ giao hoàn tại {don.BuuCucGui?.TenBuuCuc} – miễn cước chuyển hoàn.");
    }

    /// <summary>
    /// RETURN_COUNTER: Người gửi đến bưu cục gửi nhận lại hàng trực tiếp tại quầy.
    /// Có thể thu cước tại quầy (tiền mặt) hoặc ghi công nợ nếu khách thanh toán online BIDV VietQR sau.
    /// </summary>
    public async Task<KetQua> HoanHangTaiQuayAsync(int maDon, bool daThuCuoc, ThaoTac tt)
    {
        var don = await db.DonGiaoHangs.Include(d => d.BuuCucGui).Include(d => d.BuuCucHienTai)
                          .Include(d => d.PhanCongs).FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.TrangThai != TrangThaiDon.DaVeBuuCucGui && !ChoHoanHang(don))
            return KetQua.Loi($"Đơn không ở trạng thái chờ hoàn hàng tại quầy (đang '{don.TrangThai.TenHienThi()}')");
        if (KiemTraBuuCuc(tt, don.MaBuuCucHienTai ?? don.MaBuuCucGui) is string loiBc) return KetQua.Loi(loiBc);

        var pc = don.PhanCongs.FirstOrDefault(p => PhanCongGiaoHang.TrangThaiHieuLuc.Contains(p.TrangThai));
        if (pc is not null)
        {
            pc.TrangThai = TrangThaiPhanCong.KetThuc;
            pc.KetQua = KetQuaGiao.TraVeBuuCuc;
            pc.NgayKetThuc = BayGio();
        }

        don.NgayHoanHang = BayGio();
        string ghiChuThu = "";
        if (daThuCuoc && don.PhiHoanChuaThu > 0)
        {
            decimal tienThu = don.PhiHoanChuaThu;
            don.NgayThuPhiNguoiGui ??= BayGio();
            don.NgayThuPhiHoan ??= BayGio();
            ghiChuThu = $" – đã thu {DinhDang.Tien(tienThu)} cước tại quầy";
        }
        else if (don.PhiHoanChuaThu > 0)
        {
            ghiChuThu = $" – cước {DinhDang.Tien(don.PhiHoanChuaThu)} ghi nhận vào công nợ người gửi";
        }

        GhiSuKien(don, TrangThaiDon.DaHoanHang,
            $"Đã hoàn hàng cho người gửi tại quầy {don.BuuCucHienTai?.TenBuuCuc ?? don.BuuCucGui?.TenBuuCuc}{ghiChuThu}",
            tt, maBuuCuc: don.MaBuuCucHienTai ?? don.MaBuuCucGui, pc: pc, quetMa: true);
        DatNguoiGiu(don, null, null);
        await db.SaveChangesAsync();
        if (pc is not null) await CapNhatNguonLucAsync([pc.MaNhanVien], [pc.MaPhuongTien]);
        return KetQua.Dat($"Đã xác nhận hoàn trả đơn {don.MaHienThi} cho người gửi tại quầy{ghiChuThu}");
    }

    /// <summary>Quản trị xác nhận đã thu phần phí người gửi còn nợ của một đơn đã hoàn tất (gạch công nợ).</summary>
    public async Task<KetQua> ThuPhiNguoiGuiAsync(int maDon, string nguoiThucHien)
    {
        var don = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");
        if (don.PhiNguoiGuiPhaiTra <= 0) return KetQua.Loi($"Đơn {don.MaHienThi} không có phí người gửi phải thanh toán riêng");
        if (don.NgayThuPhiNguoiGui is not null && (don.NgayThuPhiHoan is not null || !don.LaDonHoan))
            return KetQua.Loi($"Đã thu phí đơn {don.MaHienThi} ngày {DinhDang.Ngay(don.NgayThuPhiNguoiGui)}");
        if (don.TrangThai == TrangThaiDon.DaHuy && don.NgayHoanHang == null)
            return KetQua.Loi("Đơn đã hủy nhưng chưa hoàn hàng về cho người gửi – chưa tính phí chuyển hoàn");

        don.NgayThuPhiNguoiGui = BayGio();
        if (don.LaDonHoan) don.NgayThuPhiHoan = BayGio();

        string ghiChuThu = don.NgayYeuCauThanhToan != null
            ? $"Quản trị viên đã duyệt & xác nhận nhận đủ tiền cước {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} qua chuyển khoản ngân hàng – gạch công nợ"
            : $"Đã thu phí người gửi {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} – gạch công nợ";
        GhiSuKien(don, don.TrangThai, ghiChuThu, new ThaoTac(nguoiThucHien));
        TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.Vi,
            $"Xác nhận thanh toán cước: {don.MaHienThi}",
            $"Quản trị viên đã xác nhận thu cước {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} cho đơn {don.MaHienThi}. Công nợ đã được gạch trừ.",
            "/KhachHangDonHang/CongNo", don.MaDon);
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã thu {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} phí người gửi của đơn {don.MaHienThi}");
    }

    /// <summary>Quản trị từ chối yêu cầu thanh toán chuyển khoản của khách hàng.</summary>
    public async Task<KetQua> TuChoiThuPhiNguoiGuiAsync(int maDon, string nguoiThucHien, string? lyDo)
    {
        var don = await db.DonGiaoHangs.FirstOrDefaultAsync(d => d.MaDon == maDon);
        if (don is null) return KetQua.Loi("Không tìm thấy đơn hàng");

        don.NgayYeuCauThanhToan = null;
        string lyDoTuChoi = string.IsNullOrWhiteSpace(lyDo) ? "Chưa nhận được tiền vào tài khoản ngân hàng" : lyDo.Trim();
        GhiSuKien(don, don.TrangThai, $"Quản trị viên từ chối duyệt thanh toán cước (Lý do: {lyDoTuChoi})", new ThaoTac(nguoiThucHien));
        TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.Vi,
            $"Từ chối thanh toán cước: {don.MaHienThi}",
            $"Yêu cầu thanh toán cước cho đơn {don.MaHienThi} bị từ chối. Lý do: {lyDoTuChoi}.",
            "/KhachHangDonHang/CongNo", don.MaDon);
        await db.SaveChangesAsync();
        return KetQua.Dat($"Đã từ chối duyệt thanh toán đơn {don.MaHienThi}");
    }

    /// <summary>Đơn của khách còn nợ phí người gửi (đã hoàn tất hoặc đã hoàn hàng, chưa thu) – dùng cho tổng quan khách hàng và thống kê.</summary>
    public static IQueryable<DonGiaoHang> DonConNoPhi(IQueryable<DonGiaoHang> truyVan) =>
        truyVan.Where(d => (d.NgayThuPhiNguoiGui == null && d.TrangThai == TrangThaiDon.HoanTat && d.NguoiTraPhi == NguoiTraPhi.NguoiGui && d.PhiVanChuyen > d.TienThuHo)
                        || (d.NgayThuPhiNguoiGui == null && d.TrangThai == TrangThaiDon.DaHuy && d.NgayHoanHang != null && d.PhiVanChuyen > 0)
                        || ((d.TrangThai == TrangThaiDon.DaHoanHang || d.TrangThai == TrangThaiDon.ChoChuyenHoan || d.TrangThai == TrangThaiDon.DangChuyenHoan || d.TrangThai == TrangThaiDon.DaVeBuuCucGui || d.TrangThai == TrangThaiDon.DangGiaoHoan)
                            && (d.NgayThuPhiNguoiGui == null || d.NgayThuPhiHoan == null)));

    // =====================================================================
    // LỊCH SỬ GIAO NHẬN
    // =====================================================================

    /// <summary>Một dòng lịch sử giao nhận: mọi thay đổi trạng thái đơn đều được ghi lại (ai, lúc nào, nội dung).</summary>
    // Ghi tracking event: GhiSuKien (DonHangXuLy.cs) – mọi thay đổi trạng thái đều được ghi lại (ai, lúc nào, ở đâu, nội dung).

    // =====================================================================
    // THỐNG KÊ, BẢNG GIÁ, TRA CỨU
    // =====================================================================


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

/// <summary>
/// Người / nơi thực hiện một thao tác: tên ghi vào tracking event, bưu cục của nhân viên bưu cục
/// (null = quản trị / điều phối – mọi bưu cục), toạ độ trình duyệt gửi kèm lúc quét (nếu người dùng cho phép).
/// </summary>
public record ThaoTac(string NguoiThucHien, int? MaBuuCuc = null, double? ViDo = null, double? KinhDo = null, bool LaKhachHang = false);
