using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * Thông báo theo hành trình đơn — gọi từ GhiSuKien (mọi lần đổi trạng thái đều đi qua đây nên không sót bước nào).
 * Ai nhận gì (giống các ứng dụng giao hàng thực tế: chỉ báo mốc người đó cần biết, không báo bước nội bộ):
 *   Khách hàng : tiếp nhận tại bưu cục gửi · đang trung chuyển · đến bưu cục phát · shipper đang giao (tên + SĐT) ·
 *                giao thành công / không thành công (lý do) · hẹn giao lại · trả về bưu cục · hủy · đã hoàn hàng · sự cố
 *   Shipper    : được phân đơn khu vực · đơn bị chuyển cho shipper khác (DoiPhanCongAsync)
 *   Điều phối bưu cục: đơn giao lỗi đủ số lần · thất lạc / hư hỏng
 *   Quản trị   : thất lạc / hư hỏng
 */
public partial class DonHangXuLy
{
    private ThongBaoXuLy? _thongBao;
    private ThongBaoXuLy TB => _thongBao ??= new ThongBaoXuLy(db, () => BayGio());
    /// <summary>Dòng thông báo phí đổi lúc tiếp nhận (gắn vào thông báo "đã tiếp nhận" gửi khách).</summary>
    private string? _ghiChuPhiKhach;

    private void BaoTheoSuKien(DonGiaoHang don, TrangThaiDon cu, TrangThaiDon moi, string noiDung, ThaoTac tt, PhanCongGiaoHang? pc)
    {
        if (don.MaDon == 0) return;   // đơn vừa tạo: người tạo đã thấy ngay trên màn hình
        string ma = don.MaHienThi;
        bool doKhach = tt.LaKhachHang;

        void Khach(string tieuDe, string nd, LoaiThongBao loai = LoaiThongBao.DonHang)
        {
            if (!doKhach) TB.Gui(TB.TkKhachHang(don.MaKhachHang), loai, tieuDe, nd, ThongBaoXuLy.LinkKhach(don.MaDon), don.MaDon);
        }

        switch (moi)
        {
            // Tiếp nhận
            case TrangThaiDon.DaNhanTaiBuuCucGui when cu == TrangThaiDon.DaTao:
                Khach($"{ma}: đã tiếp nhận tại bưu cục", don.MaBuuCucGui == don.MaBuuCucNhan
                    ? $"{TB.TenBuuCuc(don.MaBuuCucGui)} đã nhận hàng – đơn giao trong khu vực, sắp được phân shipper. Giao dự kiến {DinhDang.Ngay(don.NgayGiaoDuKien)}."
                    : $"{TB.TenBuuCuc(don.MaBuuCucGui)} đã nhận hàng – đơn sẽ được chuyển tới {TB.TenBuuCuc(don.MaBuuCucNhan)}. Giao dự kiến {DinhDang.Ngay(don.NgayGiaoDuKien)}." + (_ghiChuPhiKhach is null ? "" : " " + _ghiChuPhiKhach));
                _ghiChuPhiKhach = null;
                break;
            case TrangThaiDon.DaLayTanNoi:
                Khach($"{ma}: đã lấy hàng tận nơi", $"Shipper đã nhận kiện hàng từ bạn và đang vận chuyển về {TB.TenBuuCuc(don.MaBuuCucGui)}.");
                break;
            case TrangThaiDon.DangTrungChuyen:
                Khach($"{ma}: đang trung chuyển", $"Đơn đang được xe liên tỉnh chở từ {TB.TenBuuCuc(don.MaBuuCucGui)} tới {TB.TenBuuCuc(don.MaBuuCucNhan)}.");
                break;
            case TrangThaiDon.DaDenBuuCucNhan when cu is TrangThaiDon.DaLayLienTinh or TrangThaiDon.DangTrungChuyen:
                Khach($"{ma}: đã đến bưu cục phát", $"{TB.TenBuuCuc(don.MaBuuCucNhan)} đã nhận hàng – đơn sắp được giao tới người nhận.");
                TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucNhan), LoaiThongBao.DonHang, $"Đơn {ma} đã đến bưu cục",
                            $"{TB.TenBuuCuc(don.MaBuuCucNhan)} đã nhận đơn {ma} từ {TB.TenBuuCuc(don.MaBuuCucGui)}. Vui lòng phân công shipper giao hàng.",
                            ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
                break;
            case TrangThaiDon.DaDenBuuCucNhan when cu is TrangThaiDon.DaLayGiaoHang or TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.HenGiaoLai:
                Khach($"{ma}: hàng được trả về bưu cục", $"Shipper đã trả hàng về {TB.TenBuuCuc(don.MaBuuCucNhan)} sau lần giao chưa thành công.");
                break;
            case TrangThaiDon.DangGiao when pc is not null:
                var sp = db.NhanVienGiaoHangs.Where(n => n.MaNhanVien == pc.MaNhanVien).Select(n => new { n.HoTen, n.SoDienThoai }).FirstOrDefault();
                Khach($"{ma}: shipper đang giao hàng",
                      $"{sp?.HoTen} ({DinhDang.SoDienThoai(sp?.SoDienThoai)}) đang giao tới {don.TenNguoiNhan}. Người nhận vui lòng giữ liên lạc.");
                break;
            case TrangThaiDon.GiaoThanhCong:
                Khach($"{ma}: giao thành công",
                      $"Đã giao tới {don.TenNguoiNhan}." + (don.TienTraNguoiGui > 0 ? $" {DinhDang.Tien(don.TienTraNguoiGui)} (thu hộ sau khi trừ phí) sẽ vào ví sau khi đối soát." : ""));
                break;
            case TrangThaiDon.HoanTat:
                if (don.TienTraNguoiGui > 0)
                {
                    TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.Vi, $"{ma}: tiền COD đã vào ví",
                           $"Đã hoàn tất đối soát – số tiền {DinhDang.Tien(don.TienTraNguoiGui)} (thu hộ COD sau khi trừ phí) đã được cộng vào Ví GiaoNhanh của bạn. Bạn có thể rút tiền về ngân hàng bất cứ lúc nào.",
                           "/KhachHangVi", don.MaDon);
                }
                if (don.PhiNguoiGuiPhaiTra > 0)
                {
                    TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.Vi, $"{ma}: phát sinh nợ cước vận chuyển",
                           $"Đơn {ma} đã hoàn tất và phát sinh khoản nợ cước {DinhDang.Tien(don.PhiNguoiGuiPhaiTra)} cần thanh toán.",
                           "/KhachHangDonHang/CongNo", don.MaDon);
                }
                break;
            case TrangThaiDon.GiaoKhongThanhCong:
                Khach($"{ma}: giao không thành công", noiDung);
                if (SoLanThatBai(don) >= SoLanGiaoToiDa)
                {
                    string nd = $"Đã giao lỗi {SoLanGiaoToiDa} lần – shipper sẽ trả hàng về bưu cục, đơn tự chuyển hoàn khi quét nhận lại.";
                    TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucNhan), LoaiThongBao.SuCo, $"{ma}: giao lỗi đủ số lần", nd, ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
                }
                break;
            case TrangThaiDon.HenGiaoLai:
                Khach($"{ma}: đã hẹn giao lại", noiDung);
                break;
            case TrangThaiDon.DaHuy when cu != TrangThaiDon.DaHuy:
                if (!doKhach)
                {
                    string tieuDeHuy = tt.NguoiThucHien.Contains("Hệ thống")
                        ? $"{ma}: đơn tự động bị hủy do quá 7 ngày chưa gửi hàng"
                        : $"{ma}: đơn đã bị hủy";
                    Khach(tieuDeHuy, noiDung);
                }
                else
                {
                    string ndHuy = $"Khách hàng đã hủy đơn {ma}" + (string.IsNullOrWhiteSpace(noiDung) ? "." : $": {noiDung}");
                    TB.GuiNhieu(TB.TkQuanTri(), LoaiThongBao.DonHang, $"Khách hủy đơn: {ma}", ndHuy, ThongBaoXuLy.LinkQuanTri(don.MaDon), don.MaDon);
                    TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucGui), LoaiThongBao.DonHang, $"Khách hủy đơn: {ma}", ndHuy, ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
                }
                break;
            case TrangThaiDon.ThatLac or TrangThaiDon.HuHong when cu != moi:
                Khach($"{ma}: {moi.TenHienThi().ToLower()}", noiDung, LoaiThongBao.SuCo);
                var dsBc = new int?[] { don.MaBuuCucHienTai ?? don.MaBuuCucNhan, don.MaBuuCucGui };
                TB.GuiNhieu(TB.TkQuanTri(), LoaiThongBao.SuCo, $"Sự cố {ma}: {moi.TenHienThi().ToLower()}", noiDung,
                            ThongBaoXuLy.LinkQuanTri(don.MaDon), don.MaDon);
                TB.GuiNhieu(dsBc.SelectMany(TB.TkDieuPhoi), LoaiThongBao.SuCo, $"Sự cố {ma}: {moi.TenHienThi().ToLower()}", noiDung,
                            ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
                break;
        }

        // Thông báo phân công cho Shipper
        if (pc is { TrangThai: TrangThaiPhanCong.DaPhanCong })
        {
            if (moi == TrangThaiDon.ChoLayGiaoHang && cu != moi)
            {
                TB.Gui(TB.TkShipper(pc.MaNhanVien), LoaiThongBao.PhanCong, $"Bạn được phân đơn {ma}",
                       $"Giao tới {don.TenNguoiNhan} – {don.DiaChiNhan}. Tới {TB.TenBuuCuc(don.MaBuuCucHienTai)} quét mã nhận hàng.",
                       ThongBaoXuLy.LinkCongViecShipper, don.MaDon);
            }
            else if (don.TrangThai == TrangThaiDon.ChoLayTanNoi && cu == moi)
            {
                TB.Gui(TB.TkShipper(pc.MaNhanVien), LoaiThongBao.PhanCong, $"Bạn được phân lấy đơn {ma} tận nơi",
                       $"Lấy hàng tại {don.DiaChiLayHang ?? "địa chỉ khách hàng"}. Liên hệ người gửi và quét mã khi nhận hàng.",
                       ThongBaoXuLy.LinkCongViecShipper, don.MaDon);
                var sp = db.NhanVienGiaoHangs.Where(n => n.MaNhanVien == pc.MaNhanVien).Select(n => new { n.HoTen, n.SoDienThoai }).FirstOrDefault();
                Khach($"{ma}: shipper đang đến lấy hàng",
                      $"Shipper {sp?.HoTen} ({DinhDang.SoDienThoai(sp?.SoDienThoai)}) được phân công đến lấy hàng tại {don.DiaChiLayHang}. Bạn vui lòng chuẩn bị kiện hàng.");
            }
            else if (moi == TrangThaiDon.ChoLayLienTinh && cu != moi)
            {
                TB.Gui(TB.TkShipper(pc.MaNhanVien), LoaiThongBao.PhanCong, $"Bạn được phân chở liên tỉnh đơn {ma}",
                       $"Chở từ {TB.TenBuuCuc(don.MaBuuCucGui)} tới {TB.TenBuuCuc(don.MaBuuCucNhan)}. Tới bưu cục gửi quét mã nhận hàng.",
                       ThongBaoXuLy.LinkCongViecShipper, don.MaDon);
            }
        }
    }

    /// <summary>Thông báo khi đổi phân công từ shipper cũ sang shipper mới.</summary>
    public void BaoDoiPhanCong(DonGiaoHang don, PhanCongGiaoHang pcCu, PhanCongGiaoHang pcMoi, NhanVienGiaoHang nvMoi, string lyDo)
    {
        string ma = don.MaHienThi;
        // Báo shipper cũ
        TB.Gui(TB.TkShipper(pcCu.MaNhanVien), LoaiThongBao.PhanCong, $"Đơn {ma} đã chuyển sang shipper khác",
               $"Phân công đơn {ma} của bạn đã chuyển cho {nvMoi.HoTen}. Lý do: {lyDo}.",
               ThongBaoXuLy.LinkCongViecShipper, don.MaDon);

        // Báo shipper mới
        if (don.TrangThai == TrangThaiDon.ChoLayTanNoi)
        {
            TB.Gui(TB.TkShipper(pcMoi.MaNhanVien), LoaiThongBao.PhanCong, $"Bạn được phân lấy đơn {ma} tận nơi",
                   $"Lấy hàng tại {don.DiaChiLayHang ?? "địa chỉ khách hàng"}. Liên hệ người gửi và quét mã khi nhận hàng.",
                   ThongBaoXuLy.LinkCongViecShipper, don.MaDon);
        }
        else
        {
            TB.Gui(TB.TkShipper(pcMoi.MaNhanVien), LoaiThongBao.PhanCong, $"Bạn được phân đơn {ma}",
                   $"Giao tới {don.TenNguoiNhan} – {don.DiaChiNhan}. Tới {TB.TenBuuCuc(don.MaBuuCucHienTai)} quét mã nhận hàng.",
                   ThongBaoXuLy.LinkCongViecShipper, don.MaDon);
        }
    }

    /// <summary>
    /// Gửi thông báo khi đơn mới được tạo:
    /// - Quản trị viên (Admin): luôn nhận thông báo có đơn mới
    /// - Nhân viên / Điều phối tại bưu cục gửi: nhận thông báo để xử lý tiếp nhận hoặc điều shipper lấy hàng
    /// - Khách hàng: nhận thông báo tạo đơn thành công (hoặc thông báo tạo hộ nếu admin/điều phối tạo)
    /// </summary>
    public void BaoDonMoi(DonGiaoHang don, KhachHang khachHang, BuuCuc buuCucGui, BuuCuc buuCucNhan, bool doKhachTao)
    {
        string ma = don.MaHienThi;
        string tenKhach = khachHang.HoTen;

        if (doKhachTao)
        {
            // 1. Quản trị viên (Admin)
            string ndQt = $"Khách hàng {tenKhach} vừa tạo đơn {ma} ({buuCucGui.TenBuuCuc} ➔ {buuCucNhan.TenBuuCuc}, phí {DinhDang.Tien(don.PhiVanChuyen)}).";
            TB.GuiNhieu(TB.TkQuanTri(), LoaiThongBao.DonHang, $"Đơn hàng mới: {ma}", ndQt, ThongBaoXuLy.LinkQuanTri(don.MaDon), don.MaDon);

            // 2. Nhân viên điều phối bưu cục gửi
            if (don.HinhThucGui == HinhThucGui.LayTanNoi)
            {
                string ndBc = $"Khách hàng {tenKhach} yêu cầu lấy hàng tận nơi tại {don.DiaChiLayHang}. Vui lòng phân công shipper lấy hàng.";
                TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucGui), LoaiThongBao.PhanCong, $"Yêu cầu lấy hàng: {ma}", ndBc, ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
            }
            else
            {
                string ndBc = $"Khách hàng {tenKhach} vừa tạo đơn gửi tại {buuCucGui.TenBuuCuc} (chờ mang hàng tới bưu cục).";
                TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucGui), LoaiThongBao.DonHang, $"Đơn hàng mới: {ma}", ndBc, ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
            }

            // 3. Khách hàng
            string ndKhach = don.HinhThucGui == HinhThucGui.LayTanNoi
                ? $"Đơn {ma} đã được tạo thành công. Shipper sẽ đến lấy hàng tận nơi tại {don.DiaChiLayHang}."
                : $"Đơn {ma} đã được tạo thành công. Vui lòng mang hàng tới {buuCucGui.TenBuuCuc} để gửi.";
            TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.DonHang, $"Tạo đơn thành công: {ma}", ndKhach, ThongBaoXuLy.LinkKhach(don.MaDon), don.MaDon);
        }
        else
        {
            // Quản trị viên tạo đơn hộ
            TB.Gui(TB.TkKhachHang(don.MaKhachHang), LoaiThongBao.DonHang, $"{ma}: đơn được tạo hộ tại bưu cục",
                   $"{buuCucGui.TenBuuCuc} đã tạo đơn giao tới {don.TenNguoiNhan} – phí vận chuyển {DinhDang.Tien(don.PhiVanChuyen)}.",
                   ThongBaoXuLy.LinkKhach(don.MaDon), don.MaDon);

            string ndBc = $"Đơn {ma} đã được tạo tại bưu cục gửi tới {don.TenNguoiNhan}.";
            TB.GuiNhieu(TB.TkDieuPhoi(don.MaBuuCucGui), LoaiThongBao.DonHang, $"Đơn hàng mới: {ma}", ndBc, ThongBaoXuLy.LinkBuuCuc(don.MaDon), don.MaDon);
        }
    }
}
