// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Module 1 - Băm và kiểm tra mật khẩu (PBKDF2 của ASP.NET Core Identity).

using Microsoft.AspNetCore.Identity;
using QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>Mật khẩu không lưu dạng rõ trong CSDL – cột TaiKhoan.MatKhau chứa chuỗi đã băm có salt.</summary>
public static class MatKhauHelper
{
    private static readonly PasswordHasher<TaiKhoan> BoBam = new();

    public static string Bam(string matKhau) => BoBam.HashPassword(null!, matKhau);

    public static bool KiemTra(string matKhauDaBam, string matKhauNhap) =>
        !string.IsNullOrEmpty(matKhauDaBam) &&
        BoBam.VerifyHashedPassword(null!, matKhauDaBam, matKhauNhap) != PasswordVerificationResult.Failed;
}
