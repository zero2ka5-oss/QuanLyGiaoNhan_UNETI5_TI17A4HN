// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Công thức tính phí vận chuyển.

using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * TinhPhiXuLy — công thức thống nhất của nhóm (mục 9.1 của đề)
 * -------------------------------------------------------------
 *   Phí vận chuyển = Phí cơ bản theo khu vực + Phụ phí theo khối lượng + Phụ phí loại hàng
 *
 *   - Phí cơ bản        = KhuVuc.PhiCoBan
 *   - Phụ phí khối lượng: 2 kg đầu không phụ thu; phần vượt làm tròn LÊN từng kg × 5.000 đ/kg
 *   - Phụ phí loại hàng = (Phí cơ bản + Phụ phí khối lượng) × LoaiHang.HeSoPhuThu, làm tròn lên bội 1.000 đ
 *
 *   Ví dụ: khu vực 30.000 đ, 3,5 kg, hàng dễ vỡ hệ số 0,3
 *          → vượt 2 kg (1,5 làm tròn lên) → phụ phí KL 10.000 đ
 *          → phụ phí loại hàng (30.000 + 10.000) × 0,3 = 12.000 đ → tổng 52.000 đ
 */
public static class TinhPhiXuLy
{
    public const decimal NguongKhoiLuongKg = 2m;
    public const decimal DonGiaVuotMoiKg = 5_000m;

    public static KetQuaTinhPhi TinhPhi(decimal phiCoBan, decimal khoiLuongKg, decimal heSoPhuThu)
    {
        if (phiCoBan < 0 || khoiLuongKg <= 0 || heSoPhuThu < 0)
            throw new ArgumentException("Dữ liệu tính phí không hợp lệ");

        decimal soKgVuot = khoiLuongKg > NguongKhoiLuongKg ? Math.Ceiling(khoiLuongKg - NguongKhoiLuongKg) : 0;
        decimal phuPhiKhoiLuong = soKgVuot * DonGiaVuotMoiKg;
        decimal phuPhiLoaiHang = LamTronNghin((phiCoBan + phuPhiKhoiLuong) * heSoPhuThu);
        decimal tong = phiCoBan + phuPhiKhoiLuong + phuPhiLoaiHang;
        return new KetQuaTinhPhi(phiCoBan, soKgVuot, phuPhiKhoiLuong, heSoPhuThu, phuPhiLoaiHang, Math.Max(0, tong));
    }

    public static decimal LamTronNghin(decimal soTien) => Math.Ceiling(soTien / 1000m) * 1000m;
}
