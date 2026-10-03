using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemTinTuc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TinTucs",
                columns: table => new
                {
                    MaTinTuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChuyenMuc = table.Column<byte>(type: "tinyint", nullable: false),
                    BieuTuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayDang = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HienThi = table.Column<bool>(type: "bit", nullable: false),
                    NguoiDang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinTucs", x => x.MaTinTuc);
                });

            migrationBuilder.InsertData(
                table: "TinTucs",
                columns: new[] { "MaTinTuc", "BieuTuong", "ChuyenMuc", "HienThi", "NgayCapNhat", "NgayDang", "NguoiDang", "NoiDung", "TieuDe", "TomTat" },
                values: new object[,]
                {
                    { 1, "bi-cloud-lightning-rain", (byte)0, true, null, new DateTime(2026, 9, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Do ảnh hưởng của mưa lớn kéo dài, một số tuyến giao hàng ngoại thành và liên tỉnh có thể chậm hơn ngày giao dự kiến từ 1 đến 2 ngày.\nTrong thời gian này, GiaoNhanh áp dụng các biện pháp sau:\n- Hàng hóa được bọc chống nước trước khi đưa lên xe.\n- Nhân viên giao hàng gọi điện cho người nhận trước khi đến để hẹn thời gian phù hợp.\n- Đơn chưa giao được vì thời tiết sẽ được ghi rõ lý do trong lịch sử giao nhận và sắp xếp giao lại sớm nhất.\nQuý khách có thể theo dõi trạng thái đơn bất cứ lúc nào tại mục Tra cứu đơn bằng mã đơn và số điện thoại người nhận.", "Mùa mưa bão: một số đơn có thể giao chậm 1 – 2 ngày", "Tại các khu vực chịu ảnh hưởng của mưa lớn, thời gian giao hàng có thể kéo dài hơn dự kiến. Hàng hóa vẫn được bảo quản an toàn." },
                    { 2, "bi-cash-coin", (byte)1, true, null, new DateTime(2026, 9, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Khi tạo đơn, người gửi có thể nhập số tiền thu hộ – đây là tiền hàng mà nhân viên giao hàng sẽ thu của người nhận.\nQuy trình đối soát tiền thu hộ tại GiaoNhanh:\n- Giao thành công: nhân viên thu tiền thu hộ (cộng phí ship nếu người nhận trả phí).\n- Nhân viên nộp số tiền đã thu về công ty qua ví trên ứng dụng.\n- Công ty xác nhận đã nhận tiền, đơn chuyển sang Hoàn tất và được đối soát.\n- Người gửi nhận lại tiền thu hộ, trừ phí ship nếu người gửi là người trả phí.\nNhân viên giao hàng chỉ được hưởng tiền ship của các đơn giao thành công, không được hưởng tiền thu hộ.", "Thu hộ tiền hàng: người gửi nhận lại tiền khi nào?", "Tiền thu hộ là tiền hàng của người gửi. Nhân viên giao hàng chỉ giữ hộ và nộp lại công ty để đối soát trả người gửi." },
                    { 3, "bi-calculator", (byte)2, true, null, new DateTime(2026, 9, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Phí vận chuyển của mỗi đơn gồm ba phần và được hiển thị trước khi quý khách xác nhận tạo đơn:\n- Phí cơ bản theo khu vực giao (xem Bảng giá trên trang chủ).\n- Phụ phí khối lượng: phần vượt 2 kg tính 5.000 đ cho mỗi kg.\n- Phụ phí loại hàng: hàng dễ vỡ, hàng giá trị cao… được cộng thêm theo hệ số của từng loại.\nQuý khách có thể chọn người gửi hoặc người nhận trả phí ship. Nếu người nhận trả, nhân viên sẽ thu phí ship cùng tiền thu hộ khi giao.", "Cách tính phí vận chuyển minh bạch", "Phí được hệ thống tự tính ngay khi tạo đơn theo khu vực giao, khối lượng và loại hàng – không phát sinh thêm khi giao." },
                    { 4, "bi-box-seam", (byte)1, true, null, new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Hàng dễ vỡ như đồ gốm sứ, thủy tinh, thiết bị điện tử cần được đóng gói kỹ trước khi giao cho nhân viên lấy hàng:\n- Dùng thùng carton cứng, còn nguyên vẹn, kích thước vừa với hàng.\n- Bọc từng món bằng màng xốp hơi, chèn kín khoảng trống để hàng không xê dịch.\n- Dán băng keo chữ H ở mặt trên và mặt đáy thùng.\n- Chọn đúng loại hàng khi tạo đơn và ghi chú \"Hàng dễ vỡ, nhẹ tay\" để nhân viên lưu ý.\nKhai báo đúng khối lượng giúp hệ thống xếp xe phù hợp với tải trọng, tránh chèn ép hàng.", "Đóng gói hàng dễ vỡ đúng cách để giao an toàn", "Một vài lưu ý đơn giản khi đóng gói giúp hàng dễ vỡ đến tay người nhận nguyên vẹn." },
                    { 5, "bi-arrow-repeat", (byte)2, true, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Khi không liên lạc được người nhận hoặc người nhận hẹn lại, nhân viên sẽ cập nhật trạng thái Giao không thành công kèm lý do cụ thể.\nSau đó bộ phận điều phối sẽ sắp xếp giao lại:\n- Mỗi đơn được giao tối đa 3 lần (lần đầu và các lần giao lại).\n- Mỗi lần giao lại đều được ghi vào lịch sử giao nhận: ai giao, lúc nào, kết quả ra sao.\n- Quá số lần giao, công ty sẽ liên hệ người gửi để hủy hoặc chuyển hoàn đơn.\nĐể đơn được giao thành công ngay lần đầu, quý khách vui lòng nhập đúng số điện thoại và địa chỉ người nhận.", "Đơn giao không thành công được xử lý thế nào?", "Mỗi đơn được giao tối đa 3 lần. Lý do không giao được luôn được ghi lại để quý khách theo dõi." },
                    { 6, "bi-pencil-square", (byte)2, true, null, new DateTime(2026, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quản trị hệ thống", "Sau khi tạo đơn, quý khách có thể tự thay đổi thông tin người nhận, khối lượng, khu vực giao hoặc hủy đơn tại trang Đơn hàng của tôi.\n- Được sửa / hủy: khi đơn đang Chờ phân công hoặc Đã phân công (nhân viên chưa lấy hàng).\n- Không được sửa / hủy: khi nhân viên đã nhận hàng – lúc này vui lòng liên hệ tổng đài 1900 1515 để được hỗ trợ.\nKhi sửa khối lượng, khu vực hoặc loại hàng, phí vận chuyển được hệ thống tính lại và ghi vào lịch sử đơn.", "Sửa hoặc hủy đơn: khi nào vẫn còn kịp?", "Quý khách tự sửa thông tin hoặc hủy đơn ngay trên tài khoản khi nhân viên chưa đến lấy hàng." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TinTucs_HienThi_NgayDang",
                table: "TinTucs",
                columns: new[] { "HienThi", "NgayDang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TinTucs");
        }
    }
}
