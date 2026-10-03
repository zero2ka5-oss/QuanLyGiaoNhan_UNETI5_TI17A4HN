// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Cấu hình ứng dụng – DbContext, Session, dịch vụ nghiệp vụ, route, Migration và dữ liệu mẫu.

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;

/*
 * Program.cs — Entity → DbContext → EF Core 10 → Migration → SQL Server → LINQ → Controller → Razor View
 *  1) Đăng ký MVC, DbContext (chuỗi kết nối "QuanLyGiaoNhanDb" trong appsettings.json), Session, DonHangXuLy
 *  2) Pipeline: StaticFiles → Routing → Session → Controller
 *  3) Khởi động: tự áp dụng Migration + nạp dữ liệu mẫu khi CSDL còn trống
 */

// Số thập phân nhập trên form (khối lượng 1.5) dùng dấu chấm, không phụ thuộc cấu hình vùng của máy
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(ThongBaoKiemTra.CauHinh);   // thông báo Validation tiếng Việt
// Tự chọn SQL Server có trên máy (SQL Server / SQLEXPRESS / LocalDB – thứ tự trong appsettings.json);
// tự thử lại khi gặp lỗi kết nối tạm thời.
var chuoiKetNoi = QuanLyGiaoNhanDbContext.ChonChuoiKetNoi(
    builder.Configuration.GetConnectionString("QuanLyGiaoNhanDb")!,
    builder.Configuration.GetSection("MayChuSqlThuLanLuot").Get<string[]>() ?? [],
    Console.WriteLine);
builder.Services.AddDbContext<QuanLyGiaoNhanDbContext>(o =>
    o.UseSqlServer(chuoiKetNoi,
                   sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)));

// Session lưu thông tin đăng nhập (cookie .QuanLyGiaoNhan.Session, hết hạn sau 60 phút không thao tác)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".QuanLyGiaoNhan.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromMinutes(60);
});

builder.Services.AddScoped<DonHangXuLy>();
builder.Services.AddScoped<ViXuLy>();
builder.Services.AddScoped<AnhXuLy>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/TrangChu/Loi");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/TrangChu/Loi", "?ma={0}");

// Ảnh người dùng tải lên lúc chạy (wwwroot/uploads) không nằm trong danh sách tệp tĩnh lúc build → cần UseStaticFiles
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapStaticAssets();

app.MapControllerRoute(name: "default", pattern: "{controller=TrangChu}/{action=Index}/{id?}")
   .WithStaticAssets();

using (var phamVi = app.Services.CreateScope())
{
    var db = phamVi.ServiceProvider.GetRequiredService<QuanLyGiaoNhanDbContext>();
    db.Database.Migrate();
    await QuanLyGiaoNhanDbContext.NapDuLieuMauAsync(db);
}

app.Run();
