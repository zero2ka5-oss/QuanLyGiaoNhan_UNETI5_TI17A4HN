// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Chuyển enum trạng thái sang chữ tiếng Việt, màu badge và biểu tượng dùng trên View.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

public static class HienThi
{
    // ----- Đơn giao hàng -----
    public static string TenHienThi(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.ChoPhanCong => "Chờ phân công",
        TrangThaiDon.DaPhanCong => "Đã phân công",
        TrangThaiDon.DaNhanHang => "Đã nhận hàng",
        TrangThaiDon.DangGiao => "Đang giao",
        TrangThaiDon.GiaoThanhCong => "Giao thành công",
        TrangThaiDon.GiaoKhongThanhCong => "Giao không thành công",
        TrangThaiDon.HoanTat => "Hoàn tất",
        TrangThaiDon.DaHuy => "Đã hủy",
        _ => t.ToString()
    };

    public static string LopBadge(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.ChoPhanCong => "tt-cho",
        TrangThaiDon.DaPhanCong or TrangThaiDon.DaNhanHang => "tt-xuly",
        TrangThaiDon.DangGiao => "tt-dang",
        TrangThaiDon.GiaoThanhCong => "tt-thanhcong",
        TrangThaiDon.GiaoKhongThanhCong => "tt-thatbai",
        TrangThaiDon.HoanTat => "tt-hoantat",
        _ => "tt-huy"
    };

    public static string BieuTuong(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.ChoPhanCong => "bi-hourglass-split",
        TrangThaiDon.DaPhanCong => "bi-person-check",
        TrangThaiDon.DaNhanHang => "bi-box-seam",
        TrangThaiDon.DangGiao => "bi-truck",
        TrangThaiDon.GiaoThanhCong => "bi-check2-circle",
        TrangThaiDon.GiaoKhongThanhCong => "bi-exclamation-octagon",
        TrangThaiDon.HoanTat => "bi-patch-check-fill",
        _ => "bi-x-circle"
    };

    public static readonly TrangThaiDon[] TatCaTrangThaiDon = Enum.GetValues<TrangThaiDon>();

    // ----- Nhân viên / phương tiện / phân công -----
    public static string TenHienThi(this TrangThaiNhanVien t) => t switch
    {
        TrangThaiNhanVien.SanSang => "Sẵn sàng",
        TrangThaiNhanVien.DangGiaoHang => "Đang giao hàng",
        TrangThaiNhanVien.TamNghi => "Tạm nghỉ",
        _ => "Ngừng hoạt động"
    };

    public static string LopBadge(this TrangThaiNhanVien t) => t switch
    {
        TrangThaiNhanVien.SanSang => "tt-thanhcong",
        TrangThaiNhanVien.DangGiaoHang => "tt-dang",
        TrangThaiNhanVien.TamNghi => "tt-cho",
        _ => "tt-huy"
    };

    public static string TenHienThi(this TrangThaiPhuongTien t) => t switch
    {
        TrangThaiPhuongTien.SanSang => "Sẵn sàng",
        TrangThaiPhuongTien.DangSuDung => "Đang sử dụng",
        TrangThaiPhuongTien.BaoTri => "Bảo trì",
        _ => "Ngừng hoạt động"
    };

    public static string LopBadge(this TrangThaiPhuongTien t) => t switch
    {
        TrangThaiPhuongTien.SanSang => "tt-thanhcong",
        TrangThaiPhuongTien.DangSuDung => "tt-dang",
        TrangThaiPhuongTien.BaoTri => "tt-cho",
        _ => "tt-huy"
    };

    public static string TenHienThi(this TrangThaiPhanCong t) => t switch
    {
        TrangThaiPhanCong.DaPhanCong => "Đã phân công",
        TrangThaiPhanCong.DaNhanHang => "Đã nhận hàng",
        TrangThaiPhanCong.DangGiao => "Đang giao",
        TrangThaiPhanCong.KetThuc => "Kết thúc",
        _ => "Đã thay đổi"
    };

    public static string LopBadge(this TrangThaiPhanCong t) => t switch
    {
        TrangThaiPhanCong.DaPhanCong or TrangThaiPhanCong.DaNhanHang => "tt-xuly",
        TrangThaiPhanCong.DangGiao => "tt-dang",
        TrangThaiPhanCong.KetThuc => "tt-hoantat",
        _ => "tt-huy"
    };

    public static string TenHienThi(this KetQuaGiao k) => k switch
    {
        KetQuaGiao.GiaoThanhCong => "Giao thành công",
        KetQuaGiao.GiaoKhongThanhCong => "Giao không thành công",
        _ => "Đơn bị hủy"
    };

    public static string TenHienThi(this LyDoThatBai l) => l switch
    {
        LyDoThatBai.NguoiNhanVangMat => "Người nhận vắng mặt",
        LyDoThatBai.SaiDiaChi => "Sai địa chỉ",
        LyDoThatBai.TuChoiNhan => "Người nhận từ chối nhận",
        LyDoThatBai.KhongLienLacDuoc => "Không liên lạc được người nhận",
        _ => "Lý do khác"
    };

    public static string TenHienThi(this LoaiGiaoDich l) => l == LoaiGiaoDich.NopTienThuHo ? "Nộp tiền thu hộ" : "Rút tiền ship";

    public static string TenHienThi(this TrangThaiGiaoDich t) => t switch
    {
        TrangThaiGiaoDich.ChoXacNhan => "Chờ xác nhận",
        TrangThaiGiaoDich.DaXacNhan => "Đã xác nhận",
        _ => "Bị từ chối"
    };

    public static string LopBadge(this TrangThaiGiaoDich t) => t switch
    {
        TrangThaiGiaoDich.ChoXacNhan => "tt-cho",
        TrangThaiGiaoDich.DaXacNhan => "tt-thanhcong",
        _ => "tt-thatbai"
    };

    public static string TenHienThi(this NguoiTraPhi n) => n == NguoiTraPhi.NguoiNhan ? "Người nhận trả" : "Người gửi trả";

    // ----- Danh mục / tài khoản -----
    public static string TenHienThi(this TrangThaiHoatDong t) => t == TrangThaiHoatDong.HoatDong ? "Đang hoạt động" : "Ngừng hoạt động";
    public static string LopBadge(this TrangThaiHoatDong t) => t == TrangThaiHoatDong.HoatDong ? "tt-thanhcong" : "tt-huy";
    public static string TenHienThi(this TrangThaiTaiKhoan t) => t == TrangThaiTaiKhoan.HoatDong ? "Hoạt động" : "Bị khóa";
    public static string LopBadge(this TrangThaiTaiKhoan t) => t == TrangThaiTaiKhoan.HoatDong ? "tt-thanhcong" : "tt-thatbai";

    public static string TenVaiTro(string? vaiTro) => vaiTro switch
    {
        VaiTroNguoiDung.QuanTri => "Quản trị viên",
        VaiTroNguoiDung.DieuPhoi => "Nhân viên điều phối",
        VaiTroNguoiDung.GiaoHang => "Nhân viên giao hàng",
        VaiTroNguoiDung.KhachHang => "Khách hàng",
        _ => vaiTro ?? ""
    };

    public static string TenVaiTroNgan(string? vaiTro) => vaiTro switch
    {
        VaiTroNguoiDung.QuanTri => "Quản trị",
        VaiTroNguoiDung.DieuPhoi => "Điều phối",
        VaiTroNguoiDung.GiaoHang => "Nhân viên giao hàng",
        VaiTroNguoiDung.KhachHang => "Khách hàng",
        _ => "Hệ thống"
    };

    /// <summary>Chữ cái đầu của tên (từ cuối trong họ tên Việt) cho ảnh đại diện, VD: "Nguyễn Văn An" → "A".</summary>
    public static string ChuCaiDau(string? hoTen)
    {
        var tu = (hoTen ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tu.Length == 0 ? "?" : tu[^1][..1].ToUpper();
    }

    /// <summary>
    /// Ảnh đại diện: có ảnh thì &lt;img&gt;, chưa có thì ô tròn chữ cái đầu. lop = "avt" / "avt avt-sm" / "avt avt-lg"...
    /// Đường dẫn và tên đều được mã hóa HTML.
    /// </summary>
    public static IHtmlContent AnhDaiDien(string? anh, string? hoTen, string lop = "avt")
    {
        var e = HtmlEncoder.Default;
        return new HtmlString(string.IsNullOrEmpty(anh)
            ? $"<span class=\"{e.Encode(lop)}\">{e.Encode(ChuCaiDau(hoTen))}</span>"
            : $"<img src=\"{e.Encode(anh)}\" alt=\"{e.Encode(hoTen ?? "")}\" class=\"{e.Encode(lop)} avt-anh\" loading=\"lazy\" />");
    }

    // ----- Hành trình đơn (thanh 5 bước / 4 đoạn dùng chung cho tra cứu, chi tiết đơn, danh sách đơn) -----

    /// <summary>Bậc hành trình 0–4: tạo đơn → phân công → lấy hàng → đang giao → đã giao (giao lỗi tính ở bước đang giao).</summary>
    public static int BacHanhTrinh(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.DaPhanCong => 1,
        TrangThaiDon.DaNhanHang => 2,
        TrangThaiDon.DangGiao or TrangThaiDon.GiaoKhongThanhCong => 3,
        TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat => 4,
        _ => 0
    };

    /// <summary>Màu thẻ trạng thái lớn: "xong" (đã giao / hoàn tất), "loi" (giao lỗi / hủy), "dang" (đang xử lý).</summary>
    public static string LopTheTrangThai(this TrangThaiDon t) => t switch
    {
        TrangThaiDon.GiaoThanhCong or TrangThaiDon.HoanTat => "xong",
        TrangThaiDon.GiaoKhongThanhCong or TrangThaiDon.DaHuy => "loi",
        _ => "dang"
    };

    /// <summary>Bậc thực tế của một đơn; đơn đã hủy lấy bậc cao nhất đã đạt trước khi hủy (theo lịch sử giao nhận).</summary>
    public static int BacHanhTrinh(DonGiaoHang don) => don.TrangThai == TrangThaiDon.DaHuy
        ? don.LichSus.Where(l => l.TrangThaiMoi != TrangThaiDon.DaHuy).Select(l => l.TrangThaiMoi.BacHanhTrinh()).DefaultIfEmpty(0).Max()
        : don.TrangThai.BacHanhTrinh();
}
