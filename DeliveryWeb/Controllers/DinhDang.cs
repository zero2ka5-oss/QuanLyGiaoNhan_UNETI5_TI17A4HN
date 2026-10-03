// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Định dạng tiền tệ, khối lượng, ngày giờ hiển thị trên View.

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

    /// <summary>0912345678 → 0912 345 678</summary>
    public static string SoDienThoai(string? sdt) =>
        sdt is { Length: 10 } ? $"{sdt[..4]} {sdt[4..7]} {sdt[7..]}" : sdt ?? "";

    /// <summary>"DH000012", "dh12", "12" → 12; không phải mã đơn → null.</summary>
    public static int? TachMaDon(string? chuoi)
    {
        if (string.IsNullOrWhiteSpace(chuoi)) return null;
        var s = chuoi.Trim().ToUpperInvariant();
        if (s.StartsWith("DH")) s = s[2..];
        return int.TryParse(s, out var ma) && ma > 0 ? ma : null;
    }

    /// <summary>0912345678 → 09xxxxx678 (tra cứu công khai)</summary>
    public static string CheSoDienThoai(string sdt) =>
        sdt.Length < 6 ? sdt : sdt[..2] + new string('x', sdt.Length - 5) + sdt[^3..];

    /// <summary>Che họ tên trên trang tra cứu công khai, chỉ giữ chữ cái đầu mỗi từ: "Ngô Quang Huy" → "N** Q**** H**".</summary>
    public static string CheTen(string? hoTen) =>
        string.Join(' ', (hoTen ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(tu => tu[..1] + new string('*', Math.Max(tu.Length - 1, 1))));

    /// <summary>
    /// Mô tả chung cho từng mốc trạng thái – dùng trên trang tra cứu công khai thay cho nội dung nội bộ
    /// (không lộ tên nhân viên, biển số xe, lý do hủy…), giống cách các hãng giao hàng hiển thị hành trình.
    /// </summary>
    public static string MoTaCongKhai(TrangThaiDon trangThai) => trangThai switch
    {
        TrangThaiDon.ChoPhanCong => "Đơn hàng đã được tạo, đang chờ sắp xếp nhân viên lấy hàng",
        TrangThaiDon.DaPhanCong => "Đã sắp xếp nhân viên đến lấy hàng",
        TrangThaiDon.DaNhanHang => "Nhân viên đã lấy hàng thành công",
        TrangThaiDon.DangGiao => "Đơn hàng đang được giao đến người nhận",
        TrangThaiDon.GiaoThanhCong => "Giao hàng thành công",
        TrangThaiDon.GiaoKhongThanhCong => "Giao hàng chưa thành công, đơn sẽ được giao lại",
        TrangThaiDon.HoanTat => "Đơn hàng đã hoàn tất",
        TrangThaiDon.DaHuy => "Đơn hàng đã bị hủy",
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

    /// <summary>Tách chuỗi người thực hiện "Nguyễn Đức Anh (Nhân viên giao hàng) – GD000011" thành tên, vai trò, mã tham chiếu.
    /// Lịch sử cũ ghi vai trò viết tắt "NV giao hàng" được đổi sang tên đầy đủ khi hiển thị.</summary>
    public static (string Ten, string? VaiTro, string? ThamChieu) TachNguoiThucHien(string? chuoi)
    {
        var m = System.Text.RegularExpressions.Regex.Match(chuoi ?? "", @"^(.*?)\s*\(([^)]*)\)\s*(?:–\s*(.+))?$");
        if (!m.Success) return (chuoi ?? "", null, null);
        string vaiTro = m.Groups[2].Value == "NV giao hàng" ? "Nhân viên giao hàng" : m.Groups[2].Value;
        return (m.Groups[1].Value, vaiTro, m.Groups[3].Success ? m.Groups[3].Value : null);
    }

    /// <summary>Lớp màu theo vai trò của người thực hiện.</summary>
    public static string LopVaiTro(string? vaiTro) => vaiTro switch
    {
        "Quản trị" => "vt-quantri",
        "Điều phối" => "vt-dieuphoi",
        "Nhân viên giao hàng" or "NV giao hàng" => "vt-giaohang",
        "Khách hàng" => "vt-khach",
        _ => "vt-hethong"
    };

    /// <summary>Các bước của thanh tiến trình đơn: Phân công → Nhận hàng → Đang giao → Giao xong → Hoàn tất.</summary>
    public static readonly string[] BuocTienTrinh = ["Phân công", "Nhận hàng", "Đang giao", "Giao xong", "Hoàn tất"];

    /// <summary>
    /// Trạng thái từng bước của thanh tiến trình tại một mốc lịch sử: "xong" (đã tích), "moi" (bước vừa đạt ở mốc này),
    /// "loi" (dừng ở bước này: giao không thành công / hủy), "" (chưa tới). Kèm nhãn thay thế cho bước lỗi.
    /// </summary>
    public static (string Lop, string Nhan)[] TienTrinh(TrangThaiDon moi, TrangThaiDon? cu)
    {
        static int Bac(TrangThaiDon t) => t switch
        {
            TrangThaiDon.DaPhanCong => 1, TrangThaiDon.DaNhanHang => 2, TrangThaiDon.DangGiao or TrangThaiDon.GiaoKhongThanhCong => 3,
            TrangThaiDon.GiaoThanhCong => 4, TrangThaiDon.HoanTat => 5, _ => 0
        };
        int bac = moi == TrangThaiDon.DaHuy ? Bac(cu ?? TrangThaiDon.ChoPhanCong) : Bac(moi);
        int buocLoi = moi is TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.DaHuy ? bac + 1 : 0;
        bool vuaDoi = cu != moi && buocLoi == 0;
        return BuocTienTrinh.Select((ten, i) =>
        {
            int so = i + 1;
            if (so == buocLoi) return ("loi", moi == TrangThaiDon.DaHuy ? "Đã hủy" : "Giao lỗi");
            if (so == bac && vuaDoi) return ("moi", ten);
            return (so <= bac ? "xong" : "", ten);
        }).ToArray();
    }
}
