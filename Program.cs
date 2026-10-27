using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Data;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

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

builder.Services.AddControllersWithViews(ThongBaoKiemTra.CauHinh)   // thông báo Validation tiếng Việt
    .AddJsonOptions(o => o.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
var chuoiKetNoi = builder.Configuration.GetConnectionString("QuanLyGiaoNhanDb");
builder.Services.AddDbContext<QuanLyGiaoNhanDbContext>(o =>
{
    o.UseSqlServer(chuoiKetNoi,
                   sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));
    o.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Session lưu thông tin đăng nhập (cookie .QuanLyGiaoNhan.Session, hết hạn sau 60 phút không thao tác)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();   // đếm số lần đăng nhập sai (DangNhapController)
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
builder.Services.AddHostedService<TuDongHuyDonService>();


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

app.MapControllerRoute(name: "dangnhap", pattern: "dang-nhap", defaults: new { controller = "DangNhap", action = "Index" });
app.MapControllerRoute(name: "dangky", pattern: "dang-ky", defaults: new { controller = "DangNhap", action = "DangKy" });
app.MapControllerRoute(name: "dangxuat", pattern: "dang-xuat", defaults: new { controller = "DangNhap", action = "DangXuat" });
app.MapControllerRoute(name: "tracuu", pattern: "tra-cuu", defaults: new { controller = "TrangChu", action = "TraCuu" });
app.MapControllerRoute(name: "tintuc", pattern: "tin-tuc", defaults: new { controller = "TrangChu", action = "TinTuc" });
app.MapControllerRoute(name: "lienhe", pattern: "lien-he", defaults: new { controller = "TrangChu", action = "LienHe" });
app.MapControllerRoute(name: "buucuc", pattern: "buu-cuc", defaults: new { controller = "BuuCuc", action = "Index" });
app.MapControllerRoute(name: "thongbao", pattern: "thong-bao", defaults: new { controller = "ThongBao", action = "Index" });

app.MapControllerRoute(name: "default", pattern: "{controller=TrangChu}/{action=Index}/{id?}")
   .WithStaticAssets();

using (var phamVi = app.Services.CreateScope())
{
    var db = phamVi.ServiceProvider.GetRequiredService<QuanLyGiaoNhanDbContext>();
    db.Database.Migrate();
    await QuanLyGiaoNhanDbContext.DamBaoCauTrucBangVaCotAsync(db);
    await QuanLyGiaoNhanDbContext.NapDuLieuMauAsync(db);
    var donHangXuLy = phamVi.ServiceProvider.GetRequiredService<DonHangXuLy>();
    await donHangXuLy.TuDongHuyDonGuiTaiBuuCucQuaHanAsync(7);
}

app.Run();
