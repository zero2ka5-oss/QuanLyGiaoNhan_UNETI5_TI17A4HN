using System.Globalization;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public static partial class DinhDang
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
}
