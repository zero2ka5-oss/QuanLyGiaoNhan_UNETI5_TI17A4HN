# Module 1 - Quản lý vận chuyển và giao nhận hàng hóa

**Sinh viên thực hiện:** Vũ Thị Vân Anh  
**Mã sinh viên:** 23103100194  
**Phạm vi:** Tài khoản, đăng nhập/đăng xuất, phân quyền theo Session, quản lý loại hàng và khu vực giao.

## Công nghệ
- ASP.NET Core MVC / .NET 10
- Entity Framework Core 10
- SQL Server
- Razor View, Bootstrap, Data Annotation và LINQ

## Chạy chương trình
1. Cài .NET 10 SDK và SQL Server hoặc SQL Server LocalDB.
2. Mở terminal tại thư mục chứa file `QuanLyGiaoNhan_UNETI5_TI17A4HN.csproj`.
3. Kiểm tra chuỗi kết nối trong `appsettings.json`.
4. Chạy:
   ```powershell
   dotnet restore
   dotnet run
   ```
5. Ứng dụng tự áp dụng migration và tạo dữ liệu mẫu khi kết nối cơ sở dữ liệu thành công. Nếu không tạo được database, hãy kiểm tra SQL Server/LocalDB và quyền truy cập.

## Tài khoản kiểm thử mẫu
- Quản trị: `admin` / `Admin@123`
- Nhân viên giao hàng: `shipper01` / `Shipper@123`
- Khách hàng: `khachhang01` / `Khach@123`

Đổi mật khẩu mẫu trước khi triển khai thực tế. Dữ liệu mẫu thuộc môi trường học tập.

## Chức năng Module 1
- Đăng nhập kiểm tra tài khoản đang hoạt động; lưu mã tài khoản, họ tên và vai trò vào Session.
- Đăng xuất xóa Session.
- Bảo vệ các trang quản trị bằng bộ lọc `SessionAuthorizeAttribute` tại Controller.
- CRUD tài khoản; kiểm tra tên đăng nhập trùng, email, trạng thái và vai trò.
- CRUD loại hàng; tên không trùng, hệ số phụ thu không âm.
- CRUD khu vực giao; tên không trùng, phí cơ bản không âm.
- Không cho xóa loại hàng/khu vực đã được sử dụng trong đơn hoặc tài khoản đang liên kết với hồ sơ khác; hiển thị thông báo để người dùng xử lý.

## Ghi chú
Mã nguồn Module 1 có comment họ tên, mã sinh viên và nội dung thực hiện tại đầu các file phụ trách. Các module khác trong project gốc được giữ lại để tích hợp chung. Cần kiểm thử trên máy có .NET 10 và SQL Server trước khi nộp.
