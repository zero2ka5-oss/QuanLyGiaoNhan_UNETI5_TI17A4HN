// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 5 - Định dạng hiển thị cho Lịch sử giao nhận (mô tả công khai, thời gian tương đối, người thực hiện, thanh tiến trình).

using System.Globalization;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Phần Module 5 của DinhDang (partial).</summary>
public static partial class DinhDang
{
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
