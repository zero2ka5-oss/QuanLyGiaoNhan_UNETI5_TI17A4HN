namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Services;

/*
 * AnhXuLy — đăng ký AddScoped trong Program.cs
 * - Chỉ nhận JPG, PNG, WEBP, GIF; kiểm tra bằng chữ ký byte đầu tệp (không tin đuôi tên tệp hay Content-Type).
 *   Không nhận SVG vì SVG có thể chứa mã script.
 * - Tối đa 5 MB; tên tệp mới là chuỗi ngẫu nhiên nên không trùng và không đoán được.
 * - Tệp lưu tại wwwroot/uploads/{thuMuc}/, CSDL lưu đường dẫn "/uploads/{thuMuc}/{ten}".
 * - Xoa() chỉ xóa tệp nằm trong /uploads/ – không đụng ảnh mẫu trong /img/.
 */
public class AnhXuLy(IWebHostEnvironment moiTruong, ILogger<AnhXuLy>? logger = null)
{
    public const long DungLuongToiDa = 5 * 1024 * 1024;
    public const string ChapNhan = "image/jpeg,image/png,image/webp,image/gif";

    public const string ThuMucAnhDaiDien = "anh-dai-dien";
    public const string ThuMucTinTuc = "tin-tuc";
    public const string ThuMucBanner = "banner";
    public const string ThuMucPhuongTien = "phuong-tien";
    public const string ThuMucGiaoHang = "giao-hang";

    private static readonly HashSet<string> DuoiChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly HashSet<string> DuoiNguyHiem = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".php", ".phtml", ".asp", ".aspx", ".jsp", ".ps1", ".vbs", ".config", ".htm", ".html"
    };

    /// <summary>Lưu ảnh; trả về (đường dẫn, null) khi thành công hoặc (null, thông báo lỗi).</summary>
    public async Task<(string? DuongDan, string? Loi)> LuuAsync(IFormFile? tep, string thuMuc)
    {
        if (tep is null || tep.Length == 0) return (null, "Chưa chọn ảnh");
        if (tep.Length > DungLuongToiDa) return (null, "Ảnh vượt quá dung lượng tối đa 5 MB");

        // Kiểm tra phần mở rộng tệp gửi lên
        string extGoc = Path.GetExtension(tep.FileName);
        if (string.IsNullOrEmpty(extGoc) || !DuoiChoPhep.Contains(extGoc) || DuoiNguyHiem.Contains(extGoc))
            return (null, "Định dạng tệp không được hỗ trợ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP, GIF");

        // Kiểm tra Content-Type
        if (!string.IsNullOrEmpty(tep.ContentType) && !ChapNhan.Contains(tep.ContentType.ToLowerInvariant()) && tep.ContentType != "image/pjpeg" && tep.ContentType != "image/x-png")
            return (null, "Loại tệp không khớp định dạng ảnh hợp lệ");

        var dau = new byte[12];
        await using (var doc = tep.OpenReadStream())
        {
            int daDoc = 0, n;
            while (daDoc < dau.Length && (n = await doc.ReadAsync(dau.AsMemory(daDoc))) > 0) daDoc += n;
            if (daDoc < dau.Length) return (null, "Tệp không phải ảnh hợp lệ");
        }
        string? duoi = DoanDuoi(dau);
        if (duoi is null) return (null, "Chữ ký tệp không hợp lệ. Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF");

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
        try
        {
            if (File.Exists(tep)) File.Delete(tep);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Không thể xóa tệp ảnh {DuongDan}", tep);
        }
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
