using System.ComponentModel.DataAnnotations;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng ThongBao – thông báo trong ứng dụng gửi tới từng tài khoản (chuông thông báo trên mọi giao diện).
 * Tạo tự động trong cùng giao tác với thao tác nghiệp vụ (ThongBaoXuLy) nên không có thông báo "ma" khi thao tác thất bại:
 *  - Khách hàng: các mốc hành trình (tiếp nhận, trung chuyển, đến bưu cục phát, đang giao, giao xong / lỗi, hủy, hoàn hàng), ví.
 *  - Shipper: được phân đơn / bị đổi đơn, lệnh nộp – rút tiền được xử lý.
 *  - Nhân viên bưu cục: hàng đang chuyển đến, đơn trả về sau khi giao lỗi hết lượt.
 *  - Quản trị / điều phối: sự cố, lệnh nộp / rút tiền chờ xác nhận.
 * DuongDan: trang mở khi bấm vào thông báo (luôn là đường dẫn nội bộ).
 */
public class ThongBao
{
    [Key]
    public int MaThongBao { get; set; }

    public int MaTaiKhoan { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    public LoaiThongBao Loai { get; set; }

    [Required, StringLength(150)]
    public string TieuDe { get; set; } = "";

    [Required, StringLength(500)]
    public string NoiDung { get; set; } = "";

    [StringLength(300)]
    public string? DuongDan { get; set; }

    public int? MaDon { get; set; }

    public bool DaDoc { get; set; }

    public DateTime NgayTao { get; set; }
}

public enum LoaiThongBao : byte { DonHang = 0, PhanCong = 1, Vi = 2, SuCo = 3, HeThong = 4, HoTro = 5 }
