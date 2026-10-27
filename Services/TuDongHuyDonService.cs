using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

/// <summary>
/// Background Service chạy nền định kỳ:
/// Tự động quét và hủy các đơn hàng tạo hình thức gửi tại bưu cục (GuiTaiBuuCuc)
/// sau 7 ngày mà khách hàng chưa mang hàng tới bưu cục (vẫn ở trạng thái DaTao).
/// Tránh làm gia tăng tồn đọng dữ liệu rác cho hệ thống và tự động gửi thông báo cho khách hàng.
/// </summary>
public class TuDongHuyDonService(IServiceScopeFactory scopeFactory, ILogger<TuDongHuyDonService> logger) : BackgroundService
{
    // Chu kỳ quét nền: mỗi 15 phút quét một lần
    private static readonly TimeSpan ChuKyQuet = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Chờ 10 giây khi web server hoàn tất khởi động
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var donHangXuLy = scope.ServiceProvider.GetRequiredService<DonHangXuLy>();
                    int soDonHuy = await donHangXuLy.TuDongHuyDonGuiTaiBuuCucQuaHanAsync(7);
                    if (soDonHuy > 0)
                    {
                        logger.LogInformation("TuDongHuyDonService: Đã tự động hủy {SoLuong} đơn hàng gửi tại bưu cục quá hạn 7 ngày.", soDonHuy);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "TuDongHuyDonService: Có lỗi khi quét tự động hủy đơn hàng quá hạn 7 ngày.");
                }

                await Task.Delay(ChuKyQuet, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // BackgroundService kết thúc êm ái khi ứng dụng dừng hoặc hủy
        }
    }
}
