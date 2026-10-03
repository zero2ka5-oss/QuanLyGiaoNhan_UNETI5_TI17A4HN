// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Lưu / xóa ảnh tải lên (ảnh đại diện, ảnh bìa tin tức, banner, ảnh phương tiện, ảnh giao hàng).

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/*
 * AnhXuLy — đăng ký AddScoped trong Program.cs
 * - Chỉ nhận JPG, PNG, WEBP, GIF; kiểm tra bằng chữ ký byte đầu tệp (không tin đuôi tên tệp hay Content-Type).
 *   Không nhận SVG vì SVG có thể chứa mã script.
 * - Tối đa 5 MB; tên tệp mới là chuỗi ngẫu nhiên nên không trùng và không đoán được.
 * - Tệp lưu tại wwwroot/uploads/{thuMuc}/, CSDL lưu đường dẫn "/uploads/{thuMuc}/{ten}".
 * - Xoa() chỉ xóa tệp nằm trong /uploads/ – không đụng ảnh mẫu trong /img/.
 */
public class AnhXuLy(IWebHostEnvironment moiTruong)
{
    public const long DungLuongToiDa = 5 * 1024 * 1024;
    public const string ChapNhan = "image/jpeg,image/png,image/webp,image/gif";

    public const string ThuMucAnhDaiDien = "anh-dai-dien";
    public const string ThuMucTinTuc = "tin-tuc";
    public const string ThuMucBanner = "banner";
    public const string ThuMucPhuongTien = "phuong-tien";
    public const string ThuMucGiaoHang = "giao-hang";

    /// <summary>Lưu ảnh; trả về (đường dẫn, null) khi thành công hoặc (null, thông báo lỗi).</summary>
    public async Task<(string? DuongDan, string? Loi)> LuuAsync(IFormFile? tep, string thuMuc)
    {
        if (tep is null || tep.Length == 0) return (null, "Chưa chọn ảnh");
        if (tep.Length > DungLuongToiDa) return (null, "Ảnh vượt quá 5 MB");

        var dau = new byte[12];
        await using (var doc = tep.OpenReadStream())
        {
            int daDoc = 0, n;
            while (daDoc < dau.Length && (n = await doc.ReadAsync(dau.AsMemory(daDoc))) > 0) daDoc += n;
            if (daDoc < dau.Length) return (null, "Tệp không phải ảnh hợp lệ");
        }
        string? duoi = DoanDuoi(dau);
        if (duoi is null) return (null, "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF");

        string thuMucLuu = Path.Combine(moiTruong.WebRootPath, "uploads", thuMuc);
        Directory.CreateDirectory(thuMucLuu);
        string ten = $"{Guid.NewGuid():N}{duoi}";
        await using (var ghi = new FileStream(Path.Combine(thuMucLuu, ten), FileMode.CreateNew))
            await tep.CopyToAsync(ghi);
        return ($"/uploads/{thuMuc}/{ten}", null);
    }

    /// <summary>Xóa ảnh cũ đã tải lên (bỏ qua đường dẫn rỗng hoặc ngoài thư mục /uploads/).</summary>
    public void Xoa(string? duongDan)
    {
        if (string.IsNullOrWhiteSpace(duongDan) || !duongDan.StartsWith("/uploads/") || duongDan.Contains("..")) return;
        string tep = Path.Combine(moiTruong.WebRootPath, duongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        try { if (File.Exists(tep)) File.Delete(tep); } catch (IOException) { }
    }

    private static string? DoanDuoi(byte[] b)
    {
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return ".gif";
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return ".webp";
        return null;
    }
}
