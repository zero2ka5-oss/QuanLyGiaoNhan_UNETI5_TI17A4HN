using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

public static class ChatBotXuLy
{
    public static async Task<ChatBotPhanHoiVM> XuLyCauHoiAsync(QuanLyGiaoNhanDbContext db, string? cauHoi, bool daDangNhap = false)
    {
        var phanHoi = new ChatBotPhanHoiVM();
        if (string.IsNullOrWhiteSpace(cauHoi))
        {
            phanHoi.CauTraLoi = "Xin chào bạn! Em là **Trợ lý ảo GiaoNhanh**. Em có thể giúp gì cho bạn hôm nay?";
            phanHoi.GoiY = ["Tra cứu đơn hàng", "Bảng giá & Kích thước", "Thời gian giao hàng", "Danh sách 34 bưu cục", "Quy định COD", "Liên hệ tổng đài"];
            return phanHoi;
        }

        var text = cauHoi.Trim();
        var lower = text.ToLowerInvariant();
        var khongDau = BoDau(lower);

        // 1. KIỂM TRA TRA CỨU ĐƠN HÀNG (MÃ ĐƠN)
        var maDonTimDuoc = DinhDang.TachMaDon(text);
        if (!maDonTimDuoc.HasValue)
        {
            // Kiểm tra regex dạng DH2026... hoặc số
            var matchDH = Regex.Match(text, @"\b(?:DH)?(\d{1,14})\b", RegexOptions.IgnoreCase);
            if (matchDH.Success)
            {
                maDonTimDuoc = DinhDang.TachMaDon(matchDH.Value);
            }
        }

        if (maDonTimDuoc.HasValue)
        {
            var don = await db.DonGiaoHangs.AsNoTracking()
                .Include(d => d.BuuCucGui)
                .Include(d => d.BuuCucNhan)
                .Include(d => d.BuuCucHienTai)
                .Include(d => d.PhanCongs).ThenInclude(p => p.NhanVien)
                .FirstOrDefaultAsync(d => d.MaDon == maDonTimDuoc.Value);

            if (don != null)
            {
                var matchSdt = Regex.Match(text, @"(?:0\d{9}|\b\d{4}\b)");
                string? sdtNhap = matchSdt.Success ? matchSdt.Value : null;
                bool hopLeSdt = !string.IsNullOrEmpty(sdtNhap) && 
                    (don.SoDienThoaiNguoiNhan.EndsWith(sdtNhap) || don.SoDienThoaiNguoiNhan == sdtNhap);

                if (!daDangNhap && !hopLeSdt)
                {
                    phanHoi.CauTraLoi = $"📦 **Thông tin vận đơn {don.MaHienThi}:**\n"
                        + $"- **Trạng thái:** `{don.TrangThai.TenHienThi()}`\n"
                        + $"- **Lộ trình:** {don.BuuCucGui?.TenBuuCuc ?? "Bưu cục gửi"} ➔ {don.BuuCucNhan?.TenBuuCuc ?? "Bưu cục nhận"}\n"
                        + (don.BuuCucHienTai != null ? $"- **Vị trí hiện tại:** {don.BuuCucHienTai.TenBuuCuc}\n" : "")
                        + $"🔒 *Để bảo mật thông tin đơn hàng, vui lòng nhập mã đơn kèm 4 số cuối SĐT người nhận (VD: `{don.MaHienThi} 1234`) hoặc tra cứu tại trang Tra Cứu Đơn.*";
                    phanHoi.HanhDong.Add(new ChatHanhDongVM
                    {
                        TieuDe = "Mở trang Tra cứu đơn",
                        Url = $"/TrangChu/TraCuu?ma={don.MaHienThi}",
                        Icon = "bi-shield-lock",
                        Kieu = "btn-cam"
                    });
                    phanHoi.GoiY = ["Thời gian giao hàng mất bao lâu?", "Cách tính cước phí", "Gặp nhân viên hỗ trợ"];
                    phanHoi.LaTraCuuDon = true;
                    return phanHoi;
                }

                var pcDangGiao = don.PhanCongs.FirstOrDefault(p => p.TrangThai == TrangThaiPhanCong.DangGiao && p.LoaiChang == LoaiShipper.KhuVuc);
                var shipperText = pcDangGiao?.NhanVien != null ? $"\n- **Shipper phụ trách:** {pcDangGiao.NhanVien.HoTen} ({DinhDang.CheSoDienThoai(pcDangGiao.NhanVien.SoDienThoai)})" : "";
                var buuCucHienTaiText = don.BuuCucHienTai != null ? $"\n- **Vị trí hiện tại:** {don.BuuCucHienTai.TenBuuCuc}" : "";

                phanHoi.CauTraLoi = $"📦 **Thông tin vận đơn {don.MaHienThi}:**\n"
                    + $"- **Trạng thái:** `{don.TrangThai.TenHienThi()}`\n"
                    + $"- **Lộ trình:** {don.BuuCucGui?.TenBuuCuc ?? "Bưu cục gửi"} ➔ {don.BuuCucNhan?.TenBuuCuc ?? "Bưu cục nhận"}{buuCucHienTaiText}{shipperText}\n"
                    + $"- **Người nhận:** {don.TenNguoiNhan} ({DinhDang.CheSoDienThoai(don.SoDienThoaiNguoiNhan)})\n"
                    + $"- **Cước vận chuyển:** {DinhDang.Tien(don.PhiVanChuyen)}" + (don.TienThuHo > 0 ? $" · **Thu hộ COD:** {DinhDang.Tien(don.TienThuHo)}" : "") + "\n"
                    + $"- **Ngày gửi:** {don.NgayTao:dd/MM/yyyy} · **Giao dự kiến:** {don.NgayGiaoDuKien:dd/MM/yyyy}";

                phanHoi.HanhDong.Add(new ChatHanhDongVM
                {
                    TieuDe = "Xem hành trình chi tiết",
                    Url = $"/TrangChu/TraCuu?ma={don.MaHienThi}",
                    Icon = "bi-truck",
                    Kieu = "btn-cam"
                });

                if (daDangNhap)
                {
                    phanHoi.HanhDong.Add(new ChatHanhDongVM
                    {
                        TieuDe = "Gửi yêu cầu hỗ trợ đơn này",
                        Url = $"/KhachHangLienHe/TaoMoi?maDon={don.MaDon}",
                        Icon = "bi-headset",
                        Kieu = "btn-outline-secondary"
                    });
                }

                phanHoi.GoiY = ["Thời gian giao hàng mất bao lâu?", "Cách tính cước phí", "Gặp nhân viên hỗ trợ"];
                phanHoi.LaTraCuuDon = true;
                return phanHoi;
            }
            else
            {
                phanHoi.CauTraLoi = $"🔍 Rất tiếc, hệ thống không tìm thấy đơn hàng nào có mã **{text}**. Bạn vui lòng kiểm tra lại mã vận đơn in trên nhãn (dạng `DH...`) hoặc vào trang Tra cứu đơn để kiểm tra kèm số điện thoại người nhận nhé!";
                phanHoi.HanhDong.Add(new ChatHanhDongVM
                {
                    TieuDe = "Mở trang Tra cứu đơn",
                    Url = "/TrangChu/TraCuu",
                    Icon = "bi-search",
                    Kieu = "btn-cam"
                });
                phanHoi.GoiY = ["Bảng giá cước", "Thời gian giao hàng", "Danh sách 34 bưu cục"];
                return phanHoi;
            }
        }

        // 2. TỪ KHÓA TRA CỨU ĐƠN (chưa có mã)
        if (ChuaTuKhoa(khongDau, "tra cuu", "van don", "kiem tra don", "don hang o dau", "theo doi don", "ma van don"))
        {
            phanHoi.CauTraLoi = "Bạn vui lòng **nhập mã vận đơn** (ví dụ: `DH202610060041` hoặc số đơn) ngay tại đây để em tra cứu tình trạng hành trình trực tiếp cho bạn nhé! Hoặc bạn có thể tra cứu theo số điện thoại người nhận:";
            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Tra cứu bằng SĐT",
                Url = "/TrangChu/TraCuu",
                Icon = "bi-search",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Thời gian giao bao lâu?", "Bảng giá cước", "Liên hệ tổng đài"];
            return phanHoi;
        }

        // 3. BẢNG GIÁ, CƯỚC PHÍ, KÍCH THƯỚC, PHỤ PHÍ
        if (ChuaTuKhoa(khongDau, "gia cuoc", "bang gia", "cuoc phi", "phi ship", "bao nhieu tien", "tinh phi", "phu phi", "cong kenh", "kich thuoc", "m3", "khoi luong", "can nang", "kg"))
        {
            phanHoi.CauTraLoi = "💰 **Bảng giá cước & Phụ phí vận chuyển GiaoNhanh:**\n\n"
                + "1. **Cước cơ bản theo cự ly GPS giữa các tỉnh thành:**\n"
                + "   - **Nội tỉnh / < 50 km:** `18.000 đ` (giao trong ngày)\n"
                + "   - **Lân cận (50 – 150 km):** `28.000 đ` (1–2 ngày)\n"
                + "   - **Khu vực gần (150 – 300 km):** `35.000 đ` (1–2 ngày)\n"
                + "   - **Trung bình (300 – 600 km):** `40.000 đ` (2–3 ngày)\n"
                + "   - **Xa (600 – 1.200 km):** `46.000 đ` (2–3 ngày)\n"
                + "   - **Rất xa (> 1.200 km):** `52.000 đ` (3–4 ngày)\n\n"
                + "2. **Phụ phí khối lượng:** Miễn phí cho 2 kg đầu tiên; vượt 2 kg tính `2.500 đ / kg`.\n\n"
                + "3. **Phụ phí kích thước thể tích (m³):**\n"
                + "   - Nhỏ gọn (≤ 0.01 m³): `0 đ`\n"
                + "   - Kiện vừa (0.01 – 0.05 m³): `+10.000 đ`\n"
                + "   - Cồng kềnh (0.05 – 0.15 m³): `+25.000 đ`\n"
                + "   - Rất cồng kềnh (> 0.15 m³): `+50.000 đ`";

            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Ước lượng phí online",
                Url = "/#bang-gia",
                Icon = "bi-calculator",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Thời gian giao hàng", "Lấy hàng tận nơi thế nào?", "Quy định COD"];
            return phanHoi;
        }

        // 4. THỜI GIAN GIAO HÀNG
        if (ChuaTuKhoa(khongDau, "thoi gian", "mat bao lau", "may ngay", "khi nao nhan", "giao nhanh khong", "hoa toc", "giao trong ngay"))
        {
            phanHoi.CauTraLoi = "⏱️ **Thời gian vận chuyển cam kết:**\n\n"
                + "- **Nội tỉnh / Nội thành:** Giao trong ngày hoặc tối đa `24 giờ`.\n"
                + "- **Liên tỉnh lân cận (< 300 km):** Từ `1 – 2 ngày` làm việc.\n"
                + "- **Liên tỉnh cự ly trung bình (300 – 600 km):** Từ `2 – 3 ngày` làm việc.\n"
                + "- **Liên tỉnh xa Bắc – Nam (> 1.200 km):** Từ `3 – 4 ngày` làm việc.\n\n"
                + "*Lưu ý: Hệ thống tính toán thời gian tự động theo khoảng cách thực tế giữa các bưu cục trên bản đồ.*";

            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Xem bảng cước & thời gian",
                Url = "/#bang-gia",
                Icon = "bi-clock-history",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Bảng giá cước", "Danh sách 34 bưu cục", "Quy trình gửi hàng"];
            return phanHoi;
        }

        // 5. BƯU CỤC & TỈNH THÀNH
        if (ChuaTuKhoa(khongDau, "buu cuc", "chi nhanh", "dia chi", "o dau", "tinh thanh", "ha noi", "bac ninh", "da nang", "sai gon", "ho chi minh", "hai phong", "can tho"))
        {
            // Kiểm tra xem có hỏi tỉnh cụ thể không
            var dsBc = await db.BuuCucs.AsNoTracking().Where(b => b.TrangThai == TrangThaiHoatDong.HoatDong).ToListAsync();
            var buuCucKhop = dsBc.FirstOrDefault(b => khongDau.Contains(BoDau(b.TinhThanh.ToLowerInvariant())) || khongDau.Contains(BoDau(b.TenBuuCuc.ToLowerInvariant())));

            if (buuCucKhop != null)
            {
                phanHoi.CauTraLoi = $"🏢 **Thông tin bưu cục tại {buuCucKhop.TinhThanh}:**\n\n"
                    + $"- **Tên bưu cục:** {buuCucKhop.TenBuuCuc}\n"
                    + $"- **Địa chỉ:** {buuCucKhop.DiaChi}, {buuCucKhop.TinhThanh}\n"
                    + $"- **Hotline:** {(string.IsNullOrEmpty(buuCucKhop.SoDienThoai) ? "1900 1515" : buuCucKhop.SoDienThoai)}\n"
                    + "- **Giờ phục vụ:** `7:00 – 21:00` tất cả các ngày trong tuần.";

                phanHoi.HanhDong.Add(new ChatHanhDongVM
                {
                    TieuDe = "Xem toàn bộ 34 bưu cục",
                    Url = "/#buu-cuc",
                    Icon = "bi-buildings",
                    Kieu = "btn-cam"
                });
                phanHoi.GoiY = ["Giá cước gửi hàng", "Thời gian giao hàng", "Lấy hàng tận nơi"];
                return phanHoi;
            }

            phanHoi.CauTraLoi = $"🏢 GiaoNhanh hiện có mạng lưới **{dsBc.Count} bưu cục** hiện đại tại các tỉnh thành trọng điểm trên toàn quốc. Tất cả bưu cục đều mở cửa từ `7:00 – 21:00` mỗi ngày để tiếp nhận hàng và hỗ trợ khách hàng.";
            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Tra cứu mạng lưới 34 bưu cục",
                Url = "/#buu-cuc",
                Icon = "bi-geo-alt-fill",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Bảng giá cước", "Lấy hàng tận nơi thế nào?", "Liên hệ tổng đài"];
            return phanHoi;
        }

        // 6. QUY TRÌNH GỬI HÀNG & LẤY HÀNG TẬN NƠI
        if (ChuaTuKhoa(khongDau, "gui hang", "cach gui", "lay tan noi", "lay tai nha", "shipper den lay", "tao don"))
        {
            phanHoi.CauTraLoi = "📦 **Bạn có thể gửi hàng theo 2 cách rất thuận tiện:**\n\n"
                + "1. **Gửi tại bưu cục (Drop-off):** Tự mang kiện hàng tới bưu cục GiaoNhanh gần nhất để gửi.\n\n"
                + "2. **Lấy hàng tận nơi (Pick-up):** Đăng ký tài khoản và tạo đơn trực tuyến, chọn hình thức *'Lấy hàng tận nơi'*. Shipper khu vực sẽ đến tận nhà hoặc kho của bạn để lấy kiện hàng và in nhãn!";

            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Tạo đơn gửi hàng ngay",
                Url = daDangNhap ? "/KhachHangDonHang/TaoMoi" : "/DangNhap/DangKy",
                Icon = "bi-plus-circle",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Bảng giá cước", "Khối lượng tối đa", "Quy định COD"];
            return phanHoi;
        }

        // 7. TIỀN THU HỘ COD & ĐỐI SOÁT
        if (ChuaTuKhoa(khongDau, "cod", "thu ho", "doi soat", "tien hang", "rut tien", "phi cod"))
        {
            phanHoi.CauTraLoi = "💵 **Dịch vụ Thu hộ tiền hàng (COD) & Đối soát ví:**\n\n"
                + "- GiaoNhanh hỗ trợ thu hộ COD toàn quốc khi giao kiện hàng cho người nhận.\n"
                + "- Ngay khi giao thành công, tiền thu hộ sẽ được tự động kết toán vào **Ví tài khoản** của bạn.\n"
                + "- Bạn có thể rút tiền về tài khoản ngân hàng bất kỳ lúc nào, hệ thống duyệt chi nhanh chóng và minh bạch.";

            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Mở tài khoản gửi hàng",
                Url = "/DangNhap/DangKy",
                Icon = "bi-person-plus",
                Kieu = "btn-cam"
            });
            phanHoi.GoiY = ["Bảng giá cước", "Thời gian giao hàng", "Liên hệ tổng đài"];
            return phanHoi;
        }

        // 8. SỬA / HỦY ĐƠN HÀNG
        if (ChuaTuKhoa(khongDau, "huy don", "sua don", "doi dia chi", "doi so dien thoai", "doi sdt"))
        {
            phanHoi.CauTraLoi = "📝 **Quy định Sửa hoặc Hủy đơn hàng:**\n\n"
                + "- **Khi đơn chưa bàn giao:** Bạn có thể tự chỉnh sửa thông tin hoặc bấm *'Hủy đơn'* trực tiếp trong mục Quản lý đơn hàng.\n"
                + "- **Khi đơn đã tiếp nhận hoặc đang vận chuyển:** Vui lòng gọi ngay hotline **1900 1515** hoặc gửi *'Yêu cầu hỗ trợ'* trực tuyến để nhân viên điều phối hỗ trợ can thiệp kịp thời.";

            if (daDangNhap)
            {
                phanHoi.HanhDong.Add(new ChatHanhDongVM
                {
                    TieuDe = "Gửi yêu cầu hỗ trợ",
                    Url = "/KhachHangLienHe/TaoMoi",
                    Icon = "bi-headset",
                    Kieu = "btn-cam"
                });
            }
            phanHoi.GoiY = ["Tra cứu đơn hàng", "Liên hệ tổng đài", "Bảng giá cước"];
            return phanHoi;
        }

        // 9. LIÊN HỆ TỔNG ĐÀI / NGƯỜI THẬT / KHIẾU NẠI
        if (ChuaTuKhoa(khongDau, "tong dai", "hotline", "nhan vien", "nguoi that", "so dien thoai", "sdt", "khieu nai", "gap nguoi", "ho tro", "lien he"))
        {
            phanHoi.CauTraLoi = "📞 **Các kênh hỗ trợ chính thức của GiaoNhanh:**\n\n"
                + "- **Tổng đài CSKH miễn phí:** **1900 1515** (7:00 – 21:00 hàng ngày)\n"
                + "- **Email tiếp nhận phản ánh:** `hotro@giaonhan.vn`\n"
                + "- **Hỗ trợ trực tuyến dạng Ticket:** Khách hàng có thể tạo yêu cầu trên hệ thống để được bưu cục và CSKH phản hồi, giải quyết mọi thắc mắc.";

            phanHoi.HanhDong.Add(new ChatHanhDongVM
            {
                TieuDe = "Gọi tổng đài 1900 1515",
                Url = "tel:19001515",
                Icon = "bi-telephone-fill",
                Kieu = "btn-success"
            });

            if (daDangNhap)
            {
                phanHoi.HanhDong.Add(new ChatHanhDongVM
                {
                    TieuDe = "Gửi yêu cầu hỗ trợ online",
                    Url = "/KhachHangLienHe/TaoMoi",
                    Icon = "bi-chat-dots-fill",
                    Kieu = "btn-cam"
                });
            }

            phanHoi.GoiY = ["Tra cứu đơn hàng", "Bảng giá cước", "Danh sách 34 bưu cục"];
            return phanHoi;
        }

        // 10. HÀNG CẤM & QUY ĐỊNH ĐÓNG GÓI
        if (ChuaTuKhoa(khongDau, "hang cam", "dong goi", "de vo", "chat long", "quy dinh"))
        {
            phanHoi.CauTraLoi = "🛡️ **Quy định hàng hóa & đóng gói an toàn:**\n\n"
                + "- **Hàng không nhận vận chuyển:** Chất dễ cháy nổ, vũ khí, động vật sống, tiền mặt, kim loại quý và các loại hàng hóa bị cấm theo pháp luật.\n"
                + "- **Kiện hàng dễ vỡ:** Cần bọc tối thiểu 2–3 lớp xốp khí (bubble wrap), dán kín thùng carton và dán nhãn cảnh báo *'Hàng dễ vỡ'* để shipper lưu ý nhẹ tay.";

            phanHoi.GoiY = ["Bảng giá cước", "Thời gian giao hàng", "Quy trình gửi hàng"];
            return phanHoi;
        }

        // 11. CHÀO HỎI / CẢM ƠN
        if (ChuaTuKhoa(khongDau, "xin chao", "hello", "hi", "chao em", "chao ban", "alo", "hey"))
        {
            phanHoi.CauTraLoi = "Dạ chào bạn! Em là **Trợ lý ảo GiaoNhanh**. Em luôn sẵn sàng giải đáp thắc mắc về cước phí, tra cứu hành trình đơn và bưu cục 24/7. Bạn cần em hỗ trợ gì ạ?";
            phanHoi.GoiY = ["Tra cứu đơn hàng", "Bảng giá cước", "Thời gian giao hàng", "Danh sách 34 bưu cục"];
            return phanHoi;
        }

        if (ChuaTuKhoa(khongDau, "cam on", "thanks", "thank you", "ok", "tot qua"))
        {
            phanHoi.CauTraLoi = "Dạ không có gì ạ! Rất vui được hỗ trợ bạn. Nếu cần thêm thông tin gì, bạn cứ nhắn cho em nhé. Chúc bạn một ngày tốt lành cùng GiaoNhanh! 😊";
            phanHoi.GoiY = ["Tra cứu đơn hàng", "Bảng giá cước", "Danh sách 34 bưu cục"];
            return phanHoi;
        }

        // 12. FALLBACK DEFAULT
        phanHoi.CauTraLoi = "Em chưa hiểu rõ câu hỏi của bạn lắm. Bạn có thể:\n\n"
            + "1. Nhập **mã vận đơn** (VD: `DH202610060041`) để tra cứu đơn hàng trực tiếp.\n"
            + "2. Chọn một trong các chủ đề hỗ trợ nhanh bên dưới.\n"
            + "3. Hoặc gọi trực tiếp tổng đài **1900 1515** để gặp nhân viên CSKH hỗ trợ nhé!";

        phanHoi.HanhDong.Add(new ChatHanhDongVM
        {
            TieuDe = "Gọi tổng đài 1900 1515",
            Url = "tel:19001515",
            Icon = "bi-telephone",
            Kieu = "btn-outline-primary"
        });

        phanHoi.GoiY = ["Tra cứu đơn hàng", "Bảng giá & Kích thước", "Thời gian giao hàng", "Danh sách 34 bưu cục", "Quy định COD"];
        return phanHoi;
    }

    private static bool ChuaTuKhoa(string text, params string[] tuKhoas)
    {
        return tuKhoas.Any(tk => text.Contains(tk, StringComparison.OrdinalIgnoreCase));
    }

    private static string BoDau(string str)
    {
        string[] arr1 = ["á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                         "đ",
                         "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ",
                         "í", "ì", "ỉ", "ĩ", "ị",
                         "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ",
                         "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự",
                         "ý", "ỳ", "ỷ", "ỹ", "ỵ"];
        string[] arr2 = ["a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                         "d",
                         "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e",
                         "i", "i", "i", "i", "i",
                         "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                         "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u",
                         "y", "y", "y", "y", "y"];
        for (int i = 0; i < arr1.Length; i++)
        {
            str = str.Replace(arr1[i], arr2[i]);
            str = str.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
        }
        return str;
    }
}
