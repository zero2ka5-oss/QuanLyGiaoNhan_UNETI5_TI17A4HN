// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung công thức tính phí vận chuyển (tạm tính phí cơ bản theo khu vực).

using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * TinhPhiXuLy — KHUNG (tuần 1)
 *   Phí vận chuyển = Phí cơ bản theo khu vực + Phụ phí theo khối lượng + Phụ phí loại hàng
 *   Tuần 1 mới tính phí cơ bản để luồng tạo đơn của nhóm chạy được;
 *   tuần 2 hoàn thiện phụ phí khối lượng vượt ngưỡng và phụ phí loại hàng.
 */
public static class TinhPhiXuLy
{
    public const decimal NguongKhoiLuongKg = 2m;
    public const decimal DonGiaVuotMoiKg = 5_000m;

    public static KetQuaTinhPhi TinhPhi(decimal phiCoBan, decimal khoiLuongKg, decimal heSoPhuThu)
    {
        if (phiCoBan < 0 || khoiLuongKg <= 0 || heSoPhuThu < 0)
            throw new ArgumentException("Dữ liệu tính phí không hợp lệ");
        // TODO tuần 2: phụ phí khối lượng + phụ phí loại hàng
        return new KetQuaTinhPhi(phiCoBan, 0, 0, heSoPhuThu, 0, phiCoBan);
    }
}
