using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

/*
 * TinhPhiXuLy — công thức tính phí vận chuyển dựa trên khoảng cách giữa các tỉnh thành
 * -------------------------------------------------------------------------------------
 *   Phí vận chuyển = Phí cơ bản (theo cự ly khoảng cách) + Phụ phí theo khối lượng + Phụ phí loại hàng
 *
 *   1. Phí cơ bản theo khoảng cách (Haversine giữa bưu cục gửi và bưu cục nhận):
 *      - Nội tỉnh / cùng bưu cục (<= 45 km):                  16.500 đ (giao trong ngày / 24h)
 *      - Nội miền gần (45 - 120 km, vd HN - Bắc Ninh/Hưng Yên): 26.000 đ (giao 1 – 2 ngày)
 *      - Nội miền trung bình (120 - 300 km, vd HN - Hải Phòng): 34.000 đ (giao 2 ngày)
 *      - Liên miền gần (300 - 600 km, vd HN - Nghệ An/Hà Tĩnh): 42.000 đ (giao 2 – 3 ngày)
 *      - Liên miền xa (600 - 1000 km, vd HN - Đà Nẵng):        50.000 đ (giao 3 – 4 ngày)
 *      - Tuyến Bắc – Nam trục chính (> 1000 km, vd HN - TP.HCM): 58.000 đ (giao 3 – 5 ngày)
 *      (Nếu không có tọa độ bưu cục gửi, áp dụng phí cơ bản mặc định của khu vực nhận)
 *
 *   2. Phụ phí khối lượng:
 *      - Ngưỡng miễn phụ phí: 2 kg đầu.
 *      - Phần vượt làm tròn LÊN từng kg × 5.000 đ/kg.
 *
 *   3. Phụ phí kích thước (hàng cồng kềnh / quá khổ):
 *      - Tiêu chuẩn (<= 0.01 m³, tương đương thùng 25x20x20 cm): Miễn phụ thu (0 đ)
 *      - Cồng kềnh vừa (0.01 - 0.03 m³, thùng mì, quạt lửng, nồi chiên): +10.000 đ
 *      - Cồng kềnh lớn (0.03 - 0.08 m³, case PC, quạt cây, lò vi sóng): +20.000 đ
 *      - Quá khổ (> 0.08 m³, tủ lạnh mini, TV, máy lọc nước, bàn ghế): +35.000 đ + 5.000 đ / mỗi 0.01 m³ vượt
 *
 *   4. Phụ phí loại hàng:
 *      - (Phí cơ bản + Phụ phí khối lượng + Phụ phí kích thước) × LoaiHang.HeSoPhuThu, làm tròn lên bội 1.000 đ.
 */
public static class TinhPhiXuLy
{
    public const decimal NguongKhoiLuongKg = 2m;
    public const decimal DonGiaVuotMoiKg = 5_000m;

    // Ngưỡng phụ phí kích thước (m³)
    public const decimal NguongKichThuocTieuChuanM3 = 0.01m; // 10 lít
    public const decimal NguongKichThuocVuaM3 = 0.03m;       // 30 lít
    public const decimal NguongKichThuocLonM3 = 0.08m;       // 80 lít
    public const decimal PhuPhiKichThuocVua = 10_000m;
    public const decimal PhuPhiKichThuocLon = 20_000m;
    public const decimal PhuPhiKichThuocQuaKhoCoBan = 35_000m;
    public const decimal PhuPhiQuaKhoMoi001M3 = 5_000m;

    /// <summary>Bảng giá cước cơ bản theo các khung cự ly khoảng cách giữa các tỉnh thành.</summary>
    public static readonly List<KhungCuLy> BangGiaCuLy =
    [
        new("Nội tỉnh / Cùng bưu cục", 0, 45, 16_500m, 1, "Giao trong ngày hoặc 24h"),
        new("Nội miền gần", 45, 120, 26_000m, 1, "Giao 1 – 2 ngày"),
        new("Nội miền trung bình", 120, 300, 34_000m, 2, "Giao 2 ngày"),
        new("Liên miền gần", 300, 600, 42_000m, 3, "Giao 2 – 3 ngày"),
        new("Liên miền xa", 600, 1000, 50_000m, 4, "Giao 3 – 4 ngày"),
        new("Tuyến Bắc – Nam", 1000, 99_999, 58_000m, 5, "Tuyến trục Bắc – Nam, giao 3 – 5 ngày")
    ];

    /// <summary>
    /// Tính khoảng cách địa lý (km) giữa 2 tọa độ bưu cục theo công thức Haversine.
    /// </summary>
    public static double TinhKhoangCachKm(double viDo1, double kinhDo1, double viDo2, double kinhDo2)
    {
        const double r = 6371.0; // Bán kính Trái Đất (km)
        double dLat = (viDo2 - viDo1) * Math.PI / 180.0;
        double dLon = (kinhDo2 - kinhDo1) * Math.PI / 180.0;
        double lat1Rad = viDo1 * Math.PI / 180.0;
        double lat2Rad = viDo2 * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(r * c, 1);
    }

    /// <summary>
    /// Xác định khung cự ly và phí cơ bản tương ứng dựa trên khoảng cách km.
    /// </summary>
    public static (decimal PhiCoBan, string TenCuLy, int SoNgay) LayPhiCoBanTheoKhoangCach(double? khoangCachKm, decimal phiCoBanMacDinh = 18_000m)
    {
        if (khoangCachKm is null) return (phiCoBanMacDinh, "Theo khu vực", 2);
        double d = Math.Max(0, khoangCachKm.Value);
        var khung = BangGiaCuLy.FirstOrDefault(k => d >= k.TuKm && d < k.DenKm) ?? BangGiaCuLy.Last();
        return (khung.PhiCoBan, $"{khung.TenKhung} (~{d:0.#} km)", khung.SoNgayGiao);
    }

    /// <summary>Phân loại cự ly hiển thị cho người dùng.</summary>
    public static string PhanLoaiCuLy(double khoangCachKm)
    {
        var khung = BangGiaCuLy.FirstOrDefault(k => khoangCachKm >= k.TuKm && khoangCachKm < k.DenKm) ?? BangGiaCuLy.Last();
        return $"{khung.TenKhung} (~{khoangCachKm:0.#} km, dự kiến {khung.SoNgayGiao} ngày)";
    }

    /// <summary>Tính phụ phí kích thước cồng kềnh theo thể tích m³.</summary>
    public static (decimal PhuPhi, string MoTa) TinhPhuPhiKichThuoc(decimal m3)
    {
        if (m3 <= NguongKichThuocTieuChuanM3)
            return (0m, "Tiêu chuẩn (≤ 0,01 m³)");
        if (m3 <= NguongKichThuocVuaM3)
            return (PhuPhiKichThuocVua, "Cồng kềnh vừa (0,01 – 0,03 m³)");
        if (m3 <= NguongKichThuocLonM3)
            return (PhuPhiKichThuocLon, "Cồng kềnh lớn (0,03 – 0,08 m³)");

        decimal vuot = m3 - NguongKichThuocLonM3;
        decimal blocks = Math.Ceiling(vuot / 0.01m);
        decimal phuPhi = PhuPhiKichThuocQuaKhoCoBan + (blocks * PhuPhiQuaKhoMoi001M3);
        return (phuPhi, $"Quá khổ ({m3:0.####} m³)");
    }

    /// <summary>
    /// Tính phí vận chuyển đầy đủ: Phí cơ bản + Phụ phí khối lượng + Phụ phí kích thước + Phụ phí loại hàng.
    /// </summary>
    public static KetQuaTinhPhi TinhPhi(decimal phiCoBan, decimal khoiLuongKg, decimal kichThuocM3, decimal heSoPhuThu, int soNgayGiao = 2)
    {
        if (phiCoBan < 0 || khoiLuongKg <= 0 || kichThuocM3 <= 0 || heSoPhuThu < 0)
            throw new ArgumentException("Dữ liệu tính phí không hợp lệ");

        decimal soKgVuot = khoiLuongKg > NguongKhoiLuongKg ? Math.Ceiling(khoiLuongKg - NguongKhoiLuongKg) : 0;
        decimal phuPhiKhoiLuong = soKgVuot * DonGiaVuotMoiKg;
        var (phuPhiKichThuoc, moTaKichThuoc) = TinhPhuPhiKichThuoc(kichThuocM3);
        decimal phuPhiLoaiHang = LamTronNghin((phiCoBan + phuPhiKhoiLuong + phuPhiKichThuoc) * heSoPhuThu);
        decimal tong = phiCoBan + phuPhiKhoiLuong + phuPhiKichThuoc + phuPhiLoaiHang;
        return new KetQuaTinhPhi(phiCoBan, soKgVuot, phuPhiKhoiLuong, phuPhiKichThuoc, moTaKichThuoc, heSoPhuThu, phuPhiLoaiHang, Math.Max(0, tong), soNgayGiao);
    }

    /// <summary>Overload tương thích khi không truyền kích thước (mặc định 0.005 m³ = kiện hàng tiêu chuẩn).</summary>
    public static KetQuaTinhPhi TinhPhi(decimal phiCoBan, decimal khoiLuongKg, decimal heSoPhuThu) =>
        TinhPhi(phiCoBan, khoiLuongKg, 0.005m, heSoPhuThu, 2);

    public static decimal LamTronNghin(decimal soTien) => Math.Ceiling(soTien / 1000m) * 1000m;
}

