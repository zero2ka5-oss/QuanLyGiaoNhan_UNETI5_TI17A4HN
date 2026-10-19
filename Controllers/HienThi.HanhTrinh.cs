// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Các bước và bậc hành trình đơn cho thanh tiến độ (_HanhTrinhDon, _TienDoNgan).

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của HienThi (partial).</summary>
public static partial class HienThi
{
    /// <summary>Các bước hành trình: tạo đơn → bưu cục gửi → trung chuyển → bưu cục nhận → đang giao → đã giao.</summary>
    public static readonly (string Ten, string Icon)[] BuocHanhTrinh =
    [
        ("Đã tạo đơn", "bi-receipt"), ("Bưu cục gửi", "bi-building-check"), ("Trung chuyển", "bi-truck-front"),
        ("Bưu cục nhận", "bi-building-down"), ("Đang giao", "bi-truck"), ("Đã giao", "bi-house-check")
    ];

    /// <summary>Các bước hành trình chuyển hoàn: yêu cầu hoàn → chuyển hoàn liên tỉnh → về bưu cục gửi → đang giao hoàn → đã hoàn hàng.</summary>
    public static readonly (string Ten, string Icon)[] BuocHanhTrinhHoan =
    [
        ("Yêu cầu hoàn", "bi-arrow-return-left"),
        ("Chuyển hoàn", "bi-truck-front"),
        ("Về bưu cục gửi", "bi-building-check"),
        ("Giao hoàn", "bi-person-walking"),
        ("Đã hoàn hàng", "bi-house-check")
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

    /// <summary>Bậc hành trình chuyển hoàn 0–4 theo BuocHanhTrinhHoan.</summary>
    public static int BacHanhTrinhHoan(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.ChoChuyenHoan => 0,
        TrangThaiDon.DangChuyenHoan => 1,
        TrangThaiDon.DaVeBuuCucGui => 2,
        TrangThaiDon.DangGiaoHoan => 3,
        TrangThaiDon.DaHoanHang => 4,
        _ => 0
    };

    /// <summary>Bậc thực tế của một đơn; đơn hủy / sự cố lấy bậc cao nhất đã đạt trước đó (theo lịch sử giao nhận).</summary>
    public static int BacHanhTrinh(DonGiaoHang don) => don.TrangThai is TrangThaiDon.DaHuy or TrangThaiDon.ThatLac or TrangThaiDon.HuHong
        ? don.LichSus.Where(l => !l.TrangThaiMoi.DaKetThuc()).Select(l => l.TrangThaiMoi.BacHanhTrinh()).DefaultIfEmpty(0).Max()
        : don.TrangThai.BacHanhTrinh();
}
