using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public static class HienThi
{
    // ----- Đơn giao hàng -----
    public static string TenHienThi(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaTao => "Đã tạo đơn",
        TrangThaiDon.ChoLayTanNoi => "Chờ shipper lấy hàng",
        TrangThaiDon.DaLayTanNoi => "Shipper đã lấy hàng",
        TrangThaiDon.DaNhanTaiBuuCucGui => "Đã tiếp nhận tại bưu cục gửi",
        TrangThaiDon.ChoLayLienTinh => "Chờ shipper liên tỉnh",
        TrangThaiDon.DaLayLienTinh => "Shipper liên tỉnh đã nhận",
        TrangThaiDon.DangTrungChuyen => "Đang trung chuyển",
        TrangThaiDon.DaDenBuuCucNhan => "Đã đến bưu cục nhận",
        TrangThaiDon.ChoLayGiaoHang => "Chờ shipper khu vực",
        TrangThaiDon.DaLayGiaoHang => "Shipper khu vực đã nhận",
        TrangThaiDon.DangGiao => "Đang giao hàng",
        TrangThaiDon.GiaoThanhCong => "Giao thành công",
        TrangThaiDon.GiaoKhongThanhCong => "Giao không thành công",
        TrangThaiDon.HenGiaoLai => "Hẹn giao lại",
        TrangThaiDon.HoanTat => "Hoàn tất",
        TrangThaiDon.DaHuy => "Đã hủy",
        TrangThaiDon.ThatLac => "Thất lạc",
        TrangThaiDon.HuHong => "Hư hỏng",
        _ => t.ToString()
    };

    /// <summary>Mã sự kiện theo đặc tả (CREATED, RECEIVED_AT_ORIGIN_HUB, ...) – hiện kèm trên trang tracking nội bộ.</summary>
    public static string MaSuKien(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaTao => "CREATED",
        TrangThaiDon.ChoLayTanNoi => "ASSIGNED_FOR_PICKUP",
        TrangThaiDon.DaLayTanNoi => "PICKED_UP_FROM_SENDER",
        TrangThaiDon.DaNhanTaiBuuCucGui => "RECEIVED_AT_ORIGIN_HUB",
        TrangThaiDon.ChoLayLienTinh => "ASSIGNED_TO_INTERPROVINCIAL_COURIER",
        TrangThaiDon.DaLayLienTinh => "PICKED_UP_FOR_INTERPROVINCIAL",
        TrangThaiDon.DangTrungChuyen => "IN_TRANSIT",
        TrangThaiDon.DaDenBuuCucNhan => "ARRIVED_AT_DESTINATION_HUB",
        TrangThaiDon.ChoLayGiaoHang => "ASSIGNED_TO_LOCAL_COURIER",
        TrangThaiDon.DaLayGiaoHang => "PICKED_UP_BY_LOCAL_COURIER",
        TrangThaiDon.DangGiao => "OUT_FOR_DELIVERY",
        TrangThaiDon.GiaoThanhCong => "DELIVERED",
        TrangThaiDon.GiaoKhongThanhCong => "DELIVERY_FAILED",
        TrangThaiDon.HenGiaoLai => "RESCHEDULED",
        TrangThaiDon.HoanTat => "SETTLED",
        TrangThaiDon.DaHuy => "CANCELLED",
        TrangThaiDon.ThatLac => "LOST",
        TrangThaiDon.HuHong => "DAMAGED",
        _ => t.ToString()
    };

    public static string LopBadge(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaTao or TrangThaiDon.ChoLayTanNoi or TrangThaiDon.HenGiaoLai => "tt-cho",
        TrangThaiDon.DaNhanTaiBuuCucGui or TrangThaiDon.ChoLayLienTinh or TrangThaiDon.DaDenBuuCucNhan or TrangThaiDon.ChoLayGiaoHang => "tt-xuly",
        TrangThaiDon.DaLayTanNoi or TrangThaiDon.DaLayLienTinh or TrangThaiDon.DangTrungChuyen or TrangThaiDon.DaLayGiaoHang or TrangThaiDon.DangGiao => "tt-dang",
        TrangThaiDon.GiaoThanhCong => "tt-thanhcong",
        TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.ThatLac or TrangThaiDon.HuHong => "tt-thatbai",
        TrangThaiDon.HoanTat => "tt-hoantat",
        _ => "tt-huy"
    };

    public static string BieuTuong(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaTao => "bi-receipt",
        TrangThaiDon.ChoLayTanNoi => "bi-person-walking",
        TrangThaiDon.DaLayTanNoi => "bi-box-seam",
        TrangThaiDon.DaNhanTaiBuuCucGui => "bi-building-check",
        TrangThaiDon.ChoLayLienTinh or TrangThaiDon.ChoLayGiaoHang => "bi-person-check",
        TrangThaiDon.DaLayLienTinh => "bi-box-arrow-right",
        TrangThaiDon.DangTrungChuyen => "bi-truck-front",
        TrangThaiDon.DaDenBuuCucNhan => "bi-building-down",
        TrangThaiDon.DaLayGiaoHang => "bi-box-seam",
        TrangThaiDon.DangGiao => "bi-truck",
        TrangThaiDon.GiaoThanhCong => "bi-check2-circle",
        TrangThaiDon.GiaoKhongThanhCong => "bi-exclamation-octagon",
        TrangThaiDon.HenGiaoLai => "bi-calendar-event",
        TrangThaiDon.HoanTat => "bi-patch-check-fill",
        TrangThaiDon.ThatLac => "bi-question-octagon",
        TrangThaiDon.HuHong => "bi-heartbreak",
        _ => "bi-x-circle"
    };

    /// <summary>Màu cố định của từng trạng thái trên biểu đồ (theo nhóm: chờ – ở bưu cục – đang chạy – xong – lỗi – hủy).</summary>
    public static string MauBieuDo(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaTao or TrangThaiDon.ChoLayTanNoi => "#F59E0B",
        TrangThaiDon.DaLayTanNoi => "#3B82F6",
        TrangThaiDon.DaNhanTaiBuuCucGui or TrangThaiDon.ChoLayLienTinh => "#93C5FD",
        TrangThaiDon.DaLayLienTinh or TrangThaiDon.DangTrungChuyen => "#6366F1",
        TrangThaiDon.DaDenBuuCucNhan or TrangThaiDon.ChoLayGiaoHang => "#60A5FA",
        TrangThaiDon.DaLayGiaoHang or TrangThaiDon.DangGiao => "#3B82F6",
        TrangThaiDon.HenGiaoLai => "#FBBF24",
        TrangThaiDon.GiaoThanhCong => "#22C55E",
        TrangThaiDon.HoanTat => "#059669",
        TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.ThatLac or TrangThaiDon.HuHong => "#EF4444",
        _ => "#9CA3AF"
    };

    /// <summary>Hàng đang nằm ở một bưu cục (chờ xử lý / chờ shipper đến nhận).</summary>
    public static readonly TrangThaiDon[] NhomTaiBuuCuc =
        [TrangThaiDon.DaNhanTaiBuuCucGui, TrangThaiDon.ChoLayLienTinh, TrangThaiDon.DaDenBuuCucNhan, TrangThaiDon.ChoLayGiaoHang];
    /// <summary>Hàng đang trong tay shipper (liên tỉnh / khu vực / lấy tận nơi).</summary>
    public static readonly TrangThaiDon[] NhomShipperGiu =
        [TrangThaiDon.DaLayTanNoi, TrangThaiDon.DaLayLienTinh, TrangThaiDon.DangTrungChuyen, TrangThaiDon.DaLayGiaoHang, TrangThaiDon.DangGiao,
         TrangThaiDon.GiaoKhongThanhCong, TrangThaiDon.HenGiaoLai];
    /// <summary>Đơn còn trong quy trình vận chuyển (chưa giao xong / hủy / sự cố).</summary>
    public static readonly TrangThaiDon[] NhomDangXuLy = [TrangThaiDon.DaTao, TrangThaiDon.ChoLayTanNoi, .. NhomTaiBuuCuc, .. NhomShipperGiu];

    /// <summary>Trạng thái kết thúc vận chuyển – không còn thao tác quét / bàn giao.</summary>
    public static bool DaKetThuc(this TrangThaiDon t) =>
        t is TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat or TrangThaiDon.DaHuy or TrangThaiDon.ThatLac or TrangThaiDon.HuHong;

    public static readonly TrangThaiDon[] TatCaTrangThaiDon = Enum.GetValues<TrangThaiDon>();

    // ----- Nhân viên / phương tiện / phân công -----
    public static string TenHienThi(this TrangThaiNhanVien t) => t switch
    {
        TrangThaiNhanVien.SanSang => "Sẵn sàng",
        TrangThaiNhanVien.DangGiaoHang => "Đang giao hàng",
        TrangThaiNhanVien.TamNghi => "Tạm nghỉ",
        _ => "Ngừng hoạt động"
    };

    public static string LopBadge(this TrangThaiNhanVien t) => t switch
    {
        TrangThaiNhanVien.SanSang => "tt-thanhcong",
        TrangThaiNhanVien.DangGiaoHang => "tt-dang",
        TrangThaiNhanVien.TamNghi => "tt-cho",
        _ => "tt-huy"
    };

    public static string TenHienThi(this TrangThaiPhuongTien t) => t switch
    {
        TrangThaiPhuongTien.SanSang => "Sẵn sàng",
        TrangThaiPhuongTien.DangSuDung => "Đang sử dụng",
        TrangThaiPhuongTien.BaoTri => "Bảo trì",
        _ => "Ngừng hoạt động"
    };

    public static string LopBadge(this TrangThaiPhuongTien t) => t switch
    {
        TrangThaiPhuongTien.SanSang => "tt-thanhcong",
        TrangThaiPhuongTien.DangSuDung => "tt-dang",
        TrangThaiPhuongTien.BaoTri => "tt-cho",
        _ => "tt-huy"
    };

    public static string TenHienThi(this TrangThaiPhanCong t) => t switch
    {
        TrangThaiPhanCong.DaPhanCong => "Đã phân công",
        TrangThaiPhanCong.DaNhanHang => "Đã nhận hàng",
        TrangThaiPhanCong.DangGiao => "Đang giao",
        TrangThaiPhanCong.KetThuc => "Kết thúc",
        _ => "Đã thay đổi"
    };

    public static string LopBadge(this TrangThaiPhanCong t) => t switch
    {
        TrangThaiPhanCong.DaPhanCong or TrangThaiPhanCong.DaNhanHang => "tt-xuly",
        TrangThaiPhanCong.DangGiao => "tt-dang",
        TrangThaiPhanCong.KetThuc => "tt-hoantat",
        _ => "tt-huy"
    };

    public static string TenHienThi(this KetQuaGiao k) => k switch
    {
        KetQuaGiao.GiaoThanhCong => "Giao thành công",
        KetQuaGiao.GiaoKhongThanhCong => "Giao không thành công",
        KetQuaGiao.DaBanGiaoBuuCuc => "Đã bàn giao bưu cục nhận",
        KetQuaGiao.TraVeBuuCuc => "Trả hàng về bưu cục",
        KetQuaGiao.SuCo => "Sự cố (thất lạc / hư hỏng)",
        KetQuaGiao.DaLayHang => "Đã lấy hàng về bưu cục",
        _ => "Đơn bị hủy"
    };

    public static string TenHienThi(this HinhThucGui h) => h switch
    {
        HinhThucGui.LayTanNoi => "Lấy hàng tận nơi",
        _ => "Gửi tại bưu cục"
    };

    public static string LopBadge(this HinhThucGui h) => h switch
    {
        HinhThucGui.LayTanNoi => "tt-dang",
        _ => "tt-xuly"
    };

    public static string TenHienThi(this LoaiShipper l) => l == LoaiShipper.LienTinh ? "Shipper liên tỉnh" : "Shipper khu vực";
    public static string TenChang(this LoaiShipper l) => l == LoaiShipper.LienTinh ? "Chặng liên tỉnh" : "Chặng khu vực";

    public static string TenHienThi(this LyDoThatBai l) => l switch
    {
        LyDoThatBai.NguoiNhanVangMat => "Người nhận vắng mặt",
        LyDoThatBai.SaiDiaChi => "Sai địa chỉ",
        LyDoThatBai.TuChoiNhan => "Người nhận từ chối nhận",
        LyDoThatBai.KhongLienLacDuoc => "Không liên lạc được người nhận",
        _ => "Lý do khác"
    };

    public static string TenHienThi(this LoaiGiaoDich l) => l == LoaiGiaoDich.NopTienThuHo ? "Nộp tiền thu hộ" : "Rút tiền ship";

    public static string TenHienThi(this TrangThaiGiaoDich t) => t switch
    {
        TrangThaiGiaoDich.ChoXacNhan => "Chờ xác nhận",
        TrangThaiGiaoDich.DaXacNhan => "Đã xác nhận",
        _ => "Bị từ chối"
    };

    public static string LopBadge(this TrangThaiGiaoDich t) => t switch
    {
        TrangThaiGiaoDich.ChoXacNhan => "tt-cho",
        TrangThaiGiaoDich.DaXacNhan => "tt-thanhcong",
        _ => "tt-thatbai"
    };

    public static string TenHienThi(this NguoiTraPhi n) => n == NguoiTraPhi.NguoiNhan ? "Người nhận trả" : "Người gửi trả";

    // ----- Danh mục / tài khoản -----
    public static string TenHienThi(this TrangThaiHoatDong t) => t == TrangThaiHoatDong.HoatDong ? "Đang hoạt động" : "Ngừng hoạt động";
    public static string LopBadge(this TrangThaiHoatDong t) => t == TrangThaiHoatDong.HoatDong ? "tt-thanhcong" : "tt-huy";
    public static string TenHienThi(this TrangThaiTaiKhoan t) => t == TrangThaiTaiKhoan.HoatDong ? "Hoạt động" : "Bị khóa";
    public static string LopBadge(this TrangThaiTaiKhoan t) => t == TrangThaiTaiKhoan.HoatDong ? "tt-thanhcong" : "tt-thatbai";

    public static string TenVaiTro(string? vaiTro) => vaiTro switch
    {
        VaiTroNguoiDung.QuanTri => "Quản trị viên",
        VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc" => "Nhân viên",
        VaiTroNguoiDung.GiaoHang => "Shipper",
        VaiTroNguoiDung.KhachHang => "Khách hàng",
        _ => vaiTro ?? ""
    };

    public static string TenVaiTroNgan(string? vaiTro) => vaiTro switch
    {
        VaiTroNguoiDung.QuanTri => "Quản trị",
        VaiTroNguoiDung.NhanVien or "DieuPhoi" or "BuuCuc" => "Nhân viên",
        VaiTroNguoiDung.GiaoHang => "Shipper",
        VaiTroNguoiDung.KhachHang => "Khách hàng",
        _ => "Hệ thống"
    };

    /// <summary>Chữ cái đầu của tên (từ cuối trong họ tên Việt) cho ảnh đại diện, VD: "Nguyễn Văn An" → "A".</summary>
    public static string ChuCaiDau(string? hoTen)
    {
        var tu = (hoTen ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tu.Length == 0 ? "?" : tu[^1][..1].ToUpper();
    }

    /// <summary>
    /// Ảnh đại diện: có ảnh thì &lt;img&gt;, chưa có thì ô tròn chữ cái đầu. lop = "avt" / "avt avt-sm" / "avt avt-lg"...
    /// Đường dẫn và tên đều được mã hóa HTML.
    /// </summary>
    public static IHtmlContent AnhDaiDien(string? anh, string? hoTen, string lop = "avt")
    {
        var e = HtmlEncoder.Default;
        return new HtmlString(string.IsNullOrEmpty(anh)
            ? $"<span class=\"{e.Encode(lop)}\">{e.Encode(ChuCaiDau(hoTen))}</span>"
            : $"<img src=\"{e.Encode(anh)}\" alt=\"{e.Encode(hoTen ?? "")}\" class=\"{e.Encode(lop)} avt-anh\" loading=\"lazy\" />");
    }

    // ----- Hành trình đơn (thanh 6 bước dùng chung cho tra cứu, chi tiết đơn, danh sách đơn) -----

    /// <summary>Các bước hành trình: tạo đơn → bưu cục gửi → trung chuyển → bưu cục nhận → đang giao → đã giao.</summary>
    public static readonly (string Ten, string Icon)[] BuocHanhTrinh =
    [
        ("Đã tạo đơn", "bi-receipt"), ("Bưu cục gửi", "bi-building-check"), ("Trung chuyển", "bi-truck-front"),
        ("Bưu cục nhận", "bi-building-down"), ("Đang giao", "bi-truck"), ("Đã giao", "bi-house-check")
    ];

    /// <summary>Bậc hành trình 0–5 theo BuocHanhTrinh (giao lỗi / hẹn giao lại tính ở bước đang giao).</summary>
    public static int BacHanhTrinh(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaNhanTaiBuuCucGui or TrangThaiDon.ChoLayLienTinh => 1,
        TrangThaiDon.DaLayLienTinh or TrangThaiDon.DangTrungChuyen => 2,
        TrangThaiDon.DaDenBuuCucNhan or TrangThaiDon.ChoLayGiaoHang => 3,
        TrangThaiDon.DaLayGiaoHang or TrangThaiDon.DangGiao or TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.HenGiaoLai => 4,
        TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat => 5,
        _ => 0
    };

    /// <summary>Màu thẻ trạng thái lớn: "xong" (đã giao / hoàn tất), "loi" (giao lỗi / hủy / sự cố), "dang" (đang xử lý).</summary>
    public static string LopTheTrangThai(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat => "xong",
        TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.DaHuy or TrangThaiDon.ThatLac or TrangThaiDon.HuHong => "loi",
        _ => "dang"
    };

    /// <summary>Bậc thực tế của một đơn; đơn hủy / sự cố lấy bậc cao nhất đã đạt trước đó (theo lịch sử giao nhận).</summary>
    public static int BacHanhTrinh(DonGiaoHang don) => don.TrangThai is TrangThaiDon.DaHuy or TrangThaiDon.ThatLac or TrangThaiDon.HuHong
        ? don.LichSus.Where(l => !l.TrangThaiMoi.DaKetThuc()).Select(l => l.TrangThaiMoi.BacHanhTrinh()).DefaultIfEmpty(0).Max()
        : don.TrangThai.BacHanhTrinh();
}
