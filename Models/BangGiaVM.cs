// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - ViewModel khối ước lượng phí + bảng giá (trang chủ công khai và trang Bảng giá của khách hàng).

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/// <summary>Khu vực, loại hàng đang hoạt động và khối lượng tối đa một đơn (tải trọng xe lớn nhất).</summary>
public record BangGiaVM(List<KhuVuc> DsKhuVuc, List<LoaiHang> DsLoaiHang, decimal KhoiLuongToiDa, bool LaKhachHang);
