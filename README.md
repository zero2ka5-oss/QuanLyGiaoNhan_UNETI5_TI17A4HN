# Module 5 – Nguyễn Minh Hướng (23103100268)

Phần việc của em trong đề tài **Website quản lý giao nhận hàng hóa** (ASP.NET Core MVC, .NET 10, EF Core 10, SQL Server).

Dự án này tách từ dự án chung của nhóm
([zero2ka5-oss/QuanLyGiaoNhan_UNETI5_TI17A4HN](https://github.com/zero2ka5-oss/QuanLyGiaoNhan_UNETI5_TI17A4HN))
và **chạy được độc lập** để trình bày 7 chức năng của Module 5.

## Cách chạy

1. Cài .NET SDK 10 và SQL Server (bản Developer / Express, instance mặc định, đăng nhập bằng Windows).
2. Mở `QuanLyGiaoNhan_UNETI5_DHTI17A4.slnx` bằng Visual Studio rồi bấm F5, hoặc chạy:
   ```
   dotnet run
   ```
3. Mở http://localhost:5022. Lần chạy đầu tự tạo CSDL `QuanLyGiaoNhan_Module5` và nạp dữ liệu mẫu.

Muốn dùng LocalDB thay cho SQL Server: trong `appsettings.json` đổi `Server=localhost` thành `Server=(localdb)\\MSSQLLocalDB`.

Tài khoản mẫu (mật khẩu `123456`): `admin` (Quản trị), `kh01` … `kh20` (Khách hàng).

## Bảy chức năng và nơi xem

| Chức năng | Xem ở đâu | File chính |
|---|---|---|
| Tính phí vận chuyển | Tạo đơn (Quản trị / Khách hàng), Bảng giá, trang chủ | `Controllers/TinhPhiXuLy.cs`, `wwwroot/js/tinh-phi.js`, `TrangChuController.Module5.cs` (API `TinhPhi`), `DonHangXuLy.Module5.cs` (`ApDungPhi`, `BangGiaAsync`), `_UocLuongPhi`, `_ThanhPhanPhi`, `KhachHangTienIch/BangGia` |
| Hoàn tất đơn (đối soát) | Chi tiết đơn Giao thành công, Phê duyệt ví Shipper | `DonHangXuLy.Module5.cs` (`HoanTatAsync`, `DonConNoPhi`), `QuanTriDonGiaoHangController.Module5.cs` (`HoanTat`) |
| Lịch sử giao nhận | Menu Lịch sử giao nhận, chi tiết đơn, tra cứu | `Models/LichSuGiaoNhan.cs`, `DonHangXuLy.Module5.cs` (`GhiSuKien`, `TraCuuAsync`), `QuanTriLichSuGiaoNhanController.cs`, `DinhDang.LichSu.cs`, `HienThi.HanhTrinh.cs`, `_LichSuGiaoNhan`, `_HanhTrinhDon`, `_TienDoNgan`, `TrangChu/TraCuu`, `KhachHangTienIch/TraCuu` |
| Dashboard | Trang đầu sau khi đăng nhập `admin` | `QuanTriBangDieuKhienController.cs`, `Models/ThongKeVM.cs` (`DashboardVM`), `Views/QuanTriBangDieuKhien` |
| Thống kê | Menu Báo cáo & Thống kê | `QuanTriThongKeController.cs`, `Models/ThongKeVM.cs` (`ThongKeVM`, `DongThongKe`), `Views/QuanTriThongKe` |
| Sắp xếp | Danh sách đơn (ô Sắp xếp, bấm tiêu đề cột) | `DonHangXuLy.Module5.cs` (`SapXepDonHang`) |
| Phân trang | Cuối mọi danh sách | `Models/DanhSachTrang.cs`, `Views/Shared/_PhanTrang.cshtml` |

Các file `*.Module5.cs`, `DinhDang.LichSu.cs`, `HienThi.HanhTrinh.cs` là phần `partial` chứa đúng các hàm của Module 5,
được tách khỏi các lớp dùng chung (`DonHangXuLy`, `TrangChuController`, `QuanTriDonGiaoHangController`, `DinhDang`, `HienThi`).

## Phần lõi dùng chung (giữ lại để chạy được)

Đăng nhập (`DangNhapController`), CSDL và dữ liệu mẫu (`Data/`), các model, layout, thông báo,
quản lý đơn hàng (`QuanTriDonGiaoHang`, `KhachHangDonHang` – nơi đặt form tính phí, nút hoàn tất, sắp xếp, phân trang)
và phê duyệt ví shipper (`QuanTriViShipper`, `ViXuLy` – đường hoàn tất qua lệnh nộp tiền).

Các trang của module khác (shipper, phương tiện, phân công, bưu cục, chuyến xe, ví/hồ sơ khách hàng, tin tức, liên hệ…)
đã được bỏ; đường dẫn tới chúng hiện trang "không tìm thấy".
