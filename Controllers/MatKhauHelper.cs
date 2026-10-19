using Microsoft.AspNetCore.Identity;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Mật khẩu không lưu dạng rõ trong CSDL – cột TaiKhoan.MatKhau chứa chuỗi đã băm có salt.</summary>
public static class MatKhauHelper
{
    private static readonly PasswordHasher<TaiKhoan> BoBam = new();

    public static string Bam(string matKhau) => BoBam.HashPassword(null!, matKhau);

    /// <summary>Mật khẩu tạm 10 ký tự ngẫu nhiên (bỏ các ký tự dễ nhầm 0/O, 1/l/I) khi Quản trị đặt lại mật khẩu.</summary>
    public static string TaoMatKhauTam()
    {
        const string KyTu = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(10, 0, (o, _) => { for (int i = 0; i < o.Length; i++) o[i] = KyTu[System.Security.Cryptography.RandomNumberGenerator.GetInt32(KyTu.Length)]; });
    }

    public static bool KiemTra(string matKhauDaBam, string matKhauNhap) =>
        !string.IsNullOrEmpty(matKhauDaBam) &&
        BoBam.VerifyHashedPassword(null!, matKhauDaBam, matKhauNhap) != PasswordVerificationResult.Failed;
}
