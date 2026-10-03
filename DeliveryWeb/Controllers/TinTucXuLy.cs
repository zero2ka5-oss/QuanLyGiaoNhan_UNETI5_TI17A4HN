// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Tiện ích tin tức (tên / màu chuyên mục, biểu tượng, tách đoạn nội dung) + 6 bài mẫu nạp sẵn vào CSDL.

using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * Bài mẫu (TinTucMau) được nạp một lần qua migration ThemTinTuc (HasData); sau đó Quản trị tự quản lý tại /QuanTriTinTuc.
 * Nội dung bài mẫu bám đúng quy định đang chạy trong hệ thống:
 * phí = phí cơ bản khu vực + phụ phí khối lượng vượt ngưỡng + phụ phí loại hàng (TinhPhiXuLy),
 * giao tối đa SoLanGiaoToiDa lần (DonHangXuLy), khách chỉ sửa / hủy đơn khi chưa lấy hàng,
 * thu hộ được đối soát trả người gửi sau khi shipper nộp tiền và công ty xác nhận.
 */
public static class TinTucXuLy
{
    public static string TenChuyenMuc(this ChuyenMucTinTuc c) => c switch
    {
        ChuyenMucTinTuc.ThongBao => "Thông báo",
        ChuyenMucTinTuc.CamNang => "Cẩm nang gửi hàng",
        _ => "Hướng dẫn"
    };

    public static string LopChuyenMuc(this ChuyenMucTinTuc c) => c switch
    {
        ChuyenMucTinTuc.ThongBao => "cm-thongbao",
        ChuyenMucTinTuc.CamNang => "cm-camnang",
        _ => "cm-huongdan"
    };

    /// <summary>Các biểu tượng cho Quản trị chọn khi đăng bài (Bootstrap Icons).</summary>
    public static readonly string[] DsBieuTuong =
    [
        "bi-newspaper", "bi-megaphone", "bi-calendar-event", "bi-cloud-lightning-rain", "bi-cash-coin", "bi-calculator",
        "bi-box-seam", "bi-arrow-repeat", "bi-pencil-square", "bi-truck", "bi-gift", "bi-shield-check"
    ];

    /// <summary>Bài đang hiển thị công khai: bật hiển thị và đã tới ngày đăng.</summary>
    public static IQueryable<TinTuc> DangHienThi(this IQueryable<TinTuc> truyVan) =>
        truyVan.Where(t => t.HienThi && t.NgayDang <= DateTime.Today);

    /// <summary>Tách nội dung thành các khối: đoạn văn (LaDanhSach = false) hoặc nhóm gạch đầu dòng liên tiếp.</summary>
    public static List<(bool LaDanhSach, List<string> Dong)> TachDoan(string? noiDung)
    {
        var ketQua = new List<(bool, List<string>)>();
        foreach (var dong in (noiDung ?? "").Split('\n').Select(d => d.Trim()).Where(d => d != ""))
        {
            bool gach = dong.StartsWith("- ");
            string chu = gach ? dong[2..].Trim() : dong;
            if (gach && ketQua.Count > 0 && ketQua[^1].Item1) ketQua[^1].Item2.Add(chu);
            else ketQua.Add((gach, [chu]));
        }
        return ketQua;
    }

    public static readonly List<TinTuc> TinTucMau =
    [
        new()
        {
            MaTinTuc = 1, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.ThongBao, NgayDang = new DateTime(2026, 9, 26), BieuTuong = "bi-cloud-lightning-rain",
            TieuDe = "Mùa mưa bão: một số đơn có thể giao chậm 1 – 2 ngày",
            TomTat = "Tại các khu vực chịu ảnh hưởng của mưa lớn, thời gian giao hàng có thể kéo dài hơn dự kiến. Hàng hóa vẫn được bảo quản an toàn.",
            NoiDung = string.Join("\n",
            [
                "Do ảnh hưởng của mưa lớn kéo dài, một số tuyến giao hàng ngoại thành và liên tỉnh có thể chậm hơn ngày giao dự kiến từ 1 đến 2 ngày.",
                "Trong thời gian này, GiaoNhanh áp dụng các biện pháp sau:",
                "- Hàng hóa được bọc chống nước trước khi đưa lên xe.",
                "- Nhân viên giao hàng gọi điện cho người nhận trước khi đến để hẹn thời gian phù hợp.",
                "- Đơn chưa giao được vì thời tiết sẽ được ghi rõ lý do trong lịch sử giao nhận và sắp xếp giao lại sớm nhất.",
                "Quý khách có thể theo dõi trạng thái đơn bất cứ lúc nào tại mục Tra cứu đơn bằng mã đơn và số điện thoại người nhận."
            ])
        },
        new()
        {
            MaTinTuc = 2, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.CamNang, NgayDang = new DateTime(2026, 9, 18), BieuTuong = "bi-cash-coin",
            TieuDe = "Thu hộ tiền hàng: người gửi nhận lại tiền khi nào?",
            TomTat = "Tiền thu hộ là tiền hàng của người gửi. Nhân viên giao hàng chỉ giữ hộ và nộp lại công ty để đối soát trả người gửi.",
            NoiDung = string.Join("\n",
            [
                "Khi tạo đơn, người gửi có thể nhập số tiền thu hộ – đây là tiền hàng mà nhân viên giao hàng sẽ thu của người nhận.",
                "Quy trình đối soát tiền thu hộ tại GiaoNhanh:",
                "- Giao thành công: nhân viên thu tiền thu hộ (cộng phí ship nếu người nhận trả phí).",
                "- Nhân viên nộp số tiền đã thu về công ty qua ví trên ứng dụng.",
                "- Công ty xác nhận đã nhận tiền, đơn chuyển sang Hoàn tất và được đối soát.",
                "- Người gửi nhận lại tiền thu hộ, trừ phí ship nếu người gửi là người trả phí.",
                "Nhân viên giao hàng chỉ được hưởng tiền ship của các đơn giao thành công, không được hưởng tiền thu hộ."
            ])
        },
        new()
        {
            MaTinTuc = 3, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.HuongDan, NgayDang = new DateTime(2026, 9, 10), BieuTuong = "bi-calculator",
            TieuDe = "Cách tính phí vận chuyển minh bạch",
            TomTat = "Phí được hệ thống tự tính ngay khi tạo đơn theo khu vực giao, khối lượng và loại hàng – không phát sinh thêm khi giao.",
            NoiDung = string.Join("\n",
            [
                "Phí vận chuyển của mỗi đơn gồm ba phần và được hiển thị trước khi quý khách xác nhận tạo đơn:",
                $"- Phí cơ bản theo khu vực giao (xem Bảng giá trên trang chủ).",
                $"- Phụ phí khối lượng: phần vượt {DinhDang.KhoiLuong(TinhPhiXuLy.NguongKhoiLuongKg)} tính {DinhDang.Tien(TinhPhiXuLy.DonGiaVuotMoiKg)} cho mỗi kg.",
                "- Phụ phí loại hàng: hàng dễ vỡ, hàng giá trị cao… được cộng thêm theo hệ số của từng loại.",
                "Quý khách có thể chọn người gửi hoặc người nhận trả phí ship. Nếu người nhận trả, nhân viên sẽ thu phí ship cùng tiền thu hộ khi giao."
            ])
        },
        new()
        {
            MaTinTuc = 4, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.CamNang, NgayDang = new DateTime(2026, 8, 28), BieuTuong = "bi-box-seam",
            TieuDe = "Đóng gói hàng dễ vỡ đúng cách để giao an toàn",
            TomTat = "Một vài lưu ý đơn giản khi đóng gói giúp hàng dễ vỡ đến tay người nhận nguyên vẹn.",
            NoiDung = string.Join("\n",
            [
                "Hàng dễ vỡ như đồ gốm sứ, thủy tinh, thiết bị điện tử cần được đóng gói kỹ trước khi giao cho nhân viên lấy hàng:",
                "- Dùng thùng carton cứng, còn nguyên vẹn, kích thước vừa với hàng.",
                "- Bọc từng món bằng màng xốp hơi, chèn kín khoảng trống để hàng không xê dịch.",
                "- Dán băng keo chữ H ở mặt trên và mặt đáy thùng.",
                "- Chọn đúng loại hàng khi tạo đơn và ghi chú \"Hàng dễ vỡ, nhẹ tay\" để nhân viên lưu ý.",
                "Khai báo đúng khối lượng giúp hệ thống xếp xe phù hợp với tải trọng, tránh chèn ép hàng."
            ])
        },
        new()
        {
            MaTinTuc = 5, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.HuongDan, NgayDang = new DateTime(2026, 8, 20), BieuTuong = "bi-arrow-repeat",
            TieuDe = "Đơn giao không thành công được xử lý thế nào?",
            TomTat = $"Mỗi đơn được giao tối đa {DonHangXuLy.SoLanGiaoToiDa} lần. Lý do không giao được luôn được ghi lại để quý khách theo dõi.",
            NoiDung = string.Join("\n",
            [
                "Khi không liên lạc được người nhận hoặc người nhận hẹn lại, nhân viên sẽ cập nhật trạng thái Giao không thành công kèm lý do cụ thể.",
                "Sau đó bộ phận điều phối sẽ sắp xếp giao lại:",
                $"- Mỗi đơn được giao tối đa {DonHangXuLy.SoLanGiaoToiDa} lần (lần đầu và các lần giao lại).",
                "- Mỗi lần giao lại đều được ghi vào lịch sử giao nhận: ai giao, lúc nào, kết quả ra sao.",
                "- Quá số lần giao, công ty sẽ liên hệ người gửi để hủy hoặc chuyển hoàn đơn.",
                "Để đơn được giao thành công ngay lần đầu, quý khách vui lòng nhập đúng số điện thoại và địa chỉ người nhận."
            ])
        },
        new()
        {
            MaTinTuc = 6, NguoiDang = "Quản trị hệ thống", ChuyenMuc = ChuyenMucTinTuc.HuongDan, NgayDang = new DateTime(2026, 8, 12), BieuTuong = "bi-pencil-square",
            TieuDe = "Sửa hoặc hủy đơn: khi nào vẫn còn kịp?",
            TomTat = "Quý khách tự sửa thông tin hoặc hủy đơn ngay trên tài khoản khi nhân viên chưa đến lấy hàng.",
            NoiDung = string.Join("\n",
            [
                "Sau khi tạo đơn, quý khách có thể tự thay đổi thông tin người nhận, khối lượng, khu vực giao hoặc hủy đơn tại trang Đơn hàng của tôi.",
                "- Được sửa / hủy: khi đơn đang Chờ phân công hoặc Đã phân công (nhân viên chưa lấy hàng).",
                "- Không được sửa / hủy: khi nhân viên đã nhận hàng – lúc này vui lòng liên hệ tổng đài 1900 1515 để được hỗ trợ.",
                "Khi sửa khối lượng, khu vực hoặc loại hàng, phí vận chuyển được hệ thống tính lại và ghi vào lịch sử đơn."
            ])
        }
    ];

    }
