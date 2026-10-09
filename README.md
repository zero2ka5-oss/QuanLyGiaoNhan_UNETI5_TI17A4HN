# Hệ thống quản lý vận chuyển và giao nhận hàng hóa

Khối cơ bản cho bài tập lớn môn Thực hành lập trình .NET.

## Công nghệ

- .NET SDK 10.0.302 / ASP.NET Core MVC `net10.0`
- Entity Framework Core 10.0.0 với SQL Server Provider
- Razor View, Bootstrap, LINQ, Session

## Cấu trúc MVC

- `Models`: entity, `ApplicationDbContext`, dữ liệu mẫu, ViewModel và migration.
- `Views`: giao diện Razor.
- `Controllers`: xử lý request, phân quyền và điều hướng.

Ứng dụng không dùng thư mục `Data`, `Services` hoặc `ViewModels` riêng; các thành phần đó đã được đưa về đúng lớp MVC tương ứng.

## Chạy project

1. Cài .NET 10 SDK và SQL Server LocalDB hoặc SQL Server Developer.
2. Mở `appsettings.json` và sửa `ConnectionStrings:DefaultConnection` nếu không dùng LocalDB.
3. Tạo database bằng migration:

```powershell
dotnet ef database update
```

Nếu máy chưa có `dotnet-ef`:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.0
```

4. Chạy ứng dụng:

```powershell
dotnet run
```

## Xử lý lỗi không kết nối được SQL Server

Lỗi `Cannot create an automatic instance` hoặc thông báo không thể kết nối cơ sở dữ liệu nghĩa là SQL Server/LocalDB chưa chạy, không phải lỗi ở Controller login.

Với LocalDB, kiểm tra và khởi động:

```powershell
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
dotnet ef database update
```

Nếu LocalDB không khởi động được, cài lại thành phần **SQL Server Express LocalDB** trong Visual Studio Installer hoặc dùng SQL Server Developer/Express và sửa connection string, ví dụ:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuanLyGiaoNhanDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Ứng dụng cũng tự gọi `MigrateAsync` và seed khi khởi động. Nếu SQL Server chưa chạy, ứng dụng vẫn khởi động nhưng cần xem log và cấu hình lại connection string.

## Database và migration

- Database mặc định: `QuanLyGiaoNhanDb`
- DbContext: `Models/ApplicationDbContext.cs`
- Connection string: `appsettings.json`
- Migration ban đầu: `Migrations/InitialCreate`

## Entity và quan hệ

`TaiKhoan`, `KhachHang`, `LoaiHang`, `KhuVuc`, `DonGiaoHang`, `NhanVienGiaoHang`, `PhuongTien`, `PhanCongGiaoHang`, `LichSuGiaoNhan`.

- `TaiKhoan` có quan hệ một-một tùy chọn với `KhachHang` và `NhanVienGiaoHang`.
- `KhachHang`, `LoaiHang`, `KhuVuc` có nhiều `DonGiaoHang`.
- `DonGiaoHang` có nhiều `PhanCongGiaoHang` và `LichSuGiaoNhan`.
- `NhanVienGiaoHang` và `PhuongTien` có nhiều `PhanCongGiaoHang`.

## Controller và view

Đã tạo controller/view cơ bản cho: `TaiKhoan`, `KhachHang`, `LoaiHang`, `KhuVuc`, `DonGiaoHang`, `NhanVienGiaoHang`, `PhuongTien`, `PhanCongGiaoHang`, `LichSuGiaoNhan`, `ThongKe` và `Home`.

Session lưu `MaTaiKhoan`, `HoTen`, `VaiTro`. Quyền được kiểm tra tại controller/filter, không chỉ ẩn nút trên view:

- `Admin` và `Nhân viên điều phối`: quản lý dữ liệu, đơn, nhân viên, phương tiện, phân công.
- `Nhân viên giao hàng`: xem các phân công của mình.
- `Khách hàng`: xem/tạo/sửa/xóa đơn của tài khoản mình trong phạm vi khung cơ bản.

## Seed data và tài khoản mẫu

Seed tạo 1 admin, 1 khách hàng, 2 nhân viên giao hàng, 4 loại hàng, 4 khu vực, 2 phương tiện và 2 đơn giao hàng.

| Vai trò | Tài khoản | Mật khẩu |
| --- | --- | --- |
| Admin | `admin` | `123456` |
| Nhân viên | `nv01` | `123456` |
| Khách hàng | `kh01` | `123456` |
| Shipper | `sp01` | `123456` |

Mật khẩu hiện đang lưu dạng đơn giản để phù hợp phạm vi học tập; trước khi triển khai thật cần thay bằng ASP.NET Core Identity hoặc cơ chế hash mật khẩu.

## Chưa thực hiện trong khối cơ bản

Chưa có bản đồ, QR, email, AJAX, tối ưu tuyến đường, phân trang nâng cao, tính phí theo nghiệp vụ đầy đủ, workflow giao hàng hoàn chỉnh và báo cáo thống kê chuyên sâu.

Không tạo commit Git tự động; nhóm có thể tự chia module và commit theo quy ước của mình.
