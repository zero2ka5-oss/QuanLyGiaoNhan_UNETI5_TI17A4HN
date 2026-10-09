using System.Globalization;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public static class DinhDang
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>35000 → "35.000 đ"</summary>
    public static string Tien(decimal soTien) => soTien.ToString("#,##0", Vi) + " đ";

    /// <summary>1.5 → "1,5 kg"</summary>
    public static string KhoiLuong(decimal kg) => kg.ToString("#,##0.##", Vi) + " kg";

    public static string So(decimal so) => so.ToString("#,##0.##", Vi);

    public static string NgayGio(DateTime? thoiGian) => thoiGian?.ToString("HH:mm dd/MM/yyyy") ?? "—";

    public static string Ngay(DateTime? ngay) => ngay?.ToString("dd/MM/yyyy") ?? "—";

    /// <summary>
    /// Cắt bỏ khoảng trắng và giới hạn độ dài chuỗi người dùng nhập trước khi ghi vào cột có độ dài cố định
    /// (VD PhanCongGiaoHang.GhiChu nvarchar(500)) – tránh lỗi SQL "String or binary data would be truncated".
    /// </summary>
    public static string? Cat(string? chuoi, int toiDa)
    {
        if (string.IsNullOrWhiteSpace(chuoi)) return null;
        var s = chuoi.Trim();
        return s.Length <= toiDa ? s : s[..(toiDa - 1)] + "…";
    }

    /// <summary>0912345678 → 0912 345 678</summary>
    public static string SoDienThoai(string? sdt) =>
        sdt is { Length: 10 } ? $"{sdt[..4]} {sdt[4..7]} {sdt[7..]}" : sdt ?? "";

    /// <summary>
    /// Đọc mã đơn từ chuỗi người dùng nhập / máy quét QR-barcode trả về:
    /// mã vận đơn "DH202610060041" (DH + yyyyMMdd + mã đơn) → 41; mã cũ "DH000012", "dh12", "12" → 12; không phải mã đơn → null.
    /// </summary>
    public static int? TachMaDon(string? chuoi) => TachMaVanDon(chuoi).MaDon;

    /// <summary>Như TachMaDon, kèm ngày tạo đọc được từ mã vận đơn mới (để đối chiếu, chống quét nhầm / gõ sai).</summary>
    public static (int? MaDon, DateTime? NgayTao) TachMaVanDon(string? chuoi)
    {
        if (string.IsNullOrWhiteSpace(chuoi)) return (null, null);
        var s = chuoi.Trim().ToUpperInvariant();
        if (s.StartsWith("DH")) s = s[2..];
        if (!s.All(char.IsAsciiDigit)) return (null, null);
        if (s.Length >= 12 && DateTime.TryParseExact(s[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngay)
            && int.TryParse(s[8..], out var maMoi) && maMoi > 0)
            return (maMoi, ngay);
        return int.TryParse(s, out var ma) && ma > 0 ? (ma, null) : (null, null);
    }

    /// <summary>0912345678 → 09xxxxx678 (tra cứu công khai)</summary>
    public static string CheSoDienThoai(string sdt) =>
        sdt.Length < 6 ? sdt : sdt[..2] + new string('x', sdt.Length - 5) + sdt[^3..];

    /// <summary>Che họ tên trên trang tra cứu công khai, chỉ giữ chữ cái đầu mỗi từ: "Ngô Quang Huy" → "N** Q**** H**".</summary>
    public static string CheTen(string? hoTen) =>
        string.Join(' ', (hoTen ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(tu => tu[..1] + new string('*', Math.Max(tu.Length - 1, 1))));

    /// <summary>
    /// Mô tả chung cho từng mốc trạng thái – dùng trên trang tra cứu công khai thay cho nội dung nội bộ.
    /// </summary>
    public static string MoTaCongKhai(TrangThaiDon trangThai) => trangThai switch
    {
        TrangThaiDon.DaTao => "Đơn hàng đã được tạo, chờ người gửi mang hàng tới bưu cục",
        TrangThaiDon.ChoLayTanNoi => "Đơn hàng đã được tạo, chờ shipper khu vực đến lấy hàng",
        TrangThaiDon.DaLayTanNoi => "Shipper đã lấy hàng từ người gửi, đang chuyển về bưu cục",
        TrangThaiDon.DaNhanTaiBuuCucGui => "Bưu cục gửi đã tiếp nhận hàng",
        TrangThaiDon.ChoLayLienTinh => "Đơn hàng đang chờ xuất bến tại bưu cục gửi",
        TrangThaiDon.DaLayLienTinh => "Đơn hàng đã được bàn giao cho đơn vị vận chuyển liên tỉnh",
        TrangThaiDon.DangTrungChuyen => "Đơn hàng đang được trung chuyển giữa các bưu cục",
        TrangThaiDon.DaDenBuuCucNhan => "Đơn hàng đã đến bưu cục phát",
        TrangThaiDon.ChoLayGiaoHang => "Bưu cục phát đang sắp xếp nhân viên giao hàng",
        TrangThaiDon.DaLayGiaoHang => "Shipper khu vực đã nhận đơn tại bưu cục",
        TrangThaiDon.DangGiao => "Đơn hàng đang được giao đến người nhận",
        TrangThaiDon.GiaoThanhCong => "Giao hàng thành công",
        TrangThaiDon.GiaoKhongThanhCong => "Giao hàng chưa thành công",
        TrangThaiDon.HenGiaoLai => "Đã hẹn lịch giao lại với người nhận",
        TrangThaiDon.HoanTat => "Đơn hàng đã hoàn tất",
        TrangThaiDon.DaHuy => "Đơn hàng đã bị hủy",
        TrangThaiDon.ThatLac or TrangThaiDon.HuHong => "Đơn hàng gặp sự cố, vui lòng liên hệ tổng đài",
        _ => trangThai.TenHienThi()
    };

    /// <summary>"Vừa xong", "5 phút trước", "3 giờ trước", "Hôm qua 08:40", sau đó là ngày giờ đầy đủ.</summary>
    public static string ThoiGianTuongDoi(DateTime thoiGian)
    {
        var khoang = DateTime.Now - thoiGian;
        if (khoang.TotalMinutes < 1) return "Vừa xong";
        if (khoang.TotalMinutes < 60) return $"{(int)khoang.TotalMinutes} phút trước";
        if (thoiGian.Date == DateTime.Today) return $"{(int)khoang.TotalHours} giờ trước";
        if (thoiGian.Date == DateTime.Today.AddDays(-1)) return $"Hôm qua {thoiGian:HH:mm}";
        return thoiGian.ToString("HH:mm dd/MM/yyyy");
    }

    /// <summary>Tách chuỗi người thực hiện thành tên, vai trò, mã tham chiếu.</summary>
    public static (string Ten, string? VaiTro, string? ThamChieu) TachNguoiThucHien(string? chuoi)
    {
        var m = System.Text.RegularExpressions.Regex.Match(chuoi ?? "", @"^(.*?)\s*\(([^)]*)\)\s*(?:–\s*(.+))?$");
        if (!m.Success) return (chuoi ?? "", null, null);
        string vaiTro = m.Groups[2].Value is "NV giao hàng" or "Nhân viên giao hàng" ? "Shipper" : m.Groups[2].Value;
        return (m.Groups[1].Value, vaiTro, m.Groups[3].Success ? m.Groups[3].Value : null);
    }

    /// <summary>Lớp màu theo vai trò của người thực hiện.</summary>
    public static string LopVaiTro(string? vaiTro) => vaiTro switch
    {
        "Quản trị" => "vt-quantri",
        "Nhân viên" or "Điều phối" or "Bưu cục" => "vt-dieuphoi",
        "Shipper" or "Nhân viên giao hàng" or "NV giao hàng" => "vt-giaohang",
        "Khách hàng" => "vt-khach",
        _ => "vt-hethong"
    };

    /// <summary>Các bước của thanh tiến trình đơn.</summary>
    public static readonly string[] BuocTienTrinh = ["Bưu cục gửi", "Trung chuyển", "Bưu cục nhận", "Đang giao", "Giao xong", "Hoàn tất"];

    /// <summary>Trạng thái từng bước của thanh tiến trình tại một mốc lịch sử.</summary>
    public static (string Lop, string Nhan)[] TienTrinh(TrangThaiDon moi, TrangThaiDon? cu)
    {
        static int Bac(TrangThaiDon t) => t == TrangThaiDon.HoanTat ? 6 : t.BacHanhTrinh();
        bool dung = moi is TrangThaiDon.DaHuy or TrangThaiDon.ThatLac or TrangThaiDon.HuHong;
        int bac = dung ? Bac(cu ?? TrangThaiDon.DaTao) : Bac(moi);
        int buocLoi = dung || moi == TrangThaiDon.GiaoKhongThanhCong ? bac + 1 : 0;
        bool vuaDoi = cu != moi && buocLoi == 0;
        return BuocTienTrinh.Select((ten, i) =>
        {
            int so = i + 1;
            if (so == buocLoi) return ("loi", moi switch
            {
                TrangThaiDon.DaHuy => "Đã hủy", TrangThaiDon.ThatLac => "Thất lạc", TrangThaiDon.HuHong => "Hư hỏng", _ => "Giao lỗi"
            });
            if (so == bac && vuaDoi) return ("moi", ten);
            return (so <= bac ? "xong" : "", ten);
        }).ToArray();
    }
}
