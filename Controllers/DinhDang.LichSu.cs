// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Khung định dạng cho lịch sử giao nhận (bản đơn giản – hoàn thiện tuần 3).

using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public static partial class DinhDang
{
    /// <summary>Khung: dùng tên trạng thái (tuần 3 thay bằng mô tả công khai từng mốc).</summary>
    public static string MoTaCongKhai(TrangThaiDon trangThai) => trangThai.TenHienThi();

    /// <summary>Khung: ngày giờ đầy đủ (tuần 3 đổi sang "5 phút trước", "Hôm qua 08:40"...).</summary>
    public static string ThoiGianTuongDoi(DateTime thoiGian) => NgayGio(thoiGian);

    /// <summary>Khung: giữ nguyên chuỗi người thực hiện (tuần 3 tách tên / vai trò / mã tham chiếu).</summary>
    public static (string Ten, string? VaiTro, string? ThamChieu) TachNguoiThucHien(string? chuoi) => (chuoi ?? "", null, null);

    public static string LopVaiTro(string? vaiTro) => "vt-hethong";
}
