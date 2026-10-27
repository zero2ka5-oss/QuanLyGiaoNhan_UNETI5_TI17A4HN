namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Helpers;

/*
 * DiaChiTienIch — Tiện ích chuẩn hóa và nhận diện Tỉnh / Thành phố từ chuỗi địa chỉ
 * ----------------------------------------------------------------------------------
 * Dùng để:
 * 1. Tự động nhận diện Tỉnh/Thành từ địa chỉ lấy hàng của khách hàng.
 * 2. Tự động gợi ý / gán bưu cục tiếp nhận phù hợp cùng tỉnh/thành.
 * 3. Kiểm tra tính hợp lệ (Validation) chống việc chọn bưu cục lấy hàng khác tỉnh (VD: Khách Hà Nội nhưng chọn bưu cục TP.HCM).
 */
public static class DiaChiTienIch
{
    public record ThongTinTinhThanh(string TenChuan, string[] TuKhoa);

    public static readonly ThongTinTinhThanh[] DanhSachTinhThanh =
    [
        // Miền Bắc
        new("Hà Nội", ["hà nội", "ha noi", "hanoi", "thủ đô hà nội"]),
        new("Hải Phòng", ["hải phòng", "hai phong", "haiphong"]),
        new("Quảng Ninh", ["quảng ninh", "quang ninh", "hạ long", "ha long", "cẩm phả", "uông bí"]),
        new("Hải Dương", ["hải dương", "hai duong"]),
        new("Hưng Yên", ["hưng yên", "hung yen"]),
        new("Bắc Ninh", ["bắc ninh", "bac ninh", "bacninh", "từ sơn"]),
        new("Bắc Giang", ["bắc giang", "bac giang", "việt yên"]),
        new("Thái Nguyên", ["thái nguyên", "thai nguyen", "sông công"]),
        new("Vĩnh Phúc", ["vĩnh phúc", "vinh phuc", "vĩnh yên", "phúc yên"]),
        new("Phú Thọ", ["phú thọ", "phu tho", "việt trì", "viet tri"]),
        new("Nam Định", ["nam định", "nam dinh"]),
        new("Thái Bình", ["thái bình", "thai binh"]),
        new("Ninh Bình", ["ninh bình", "ninh binh", "tam điệp"]),
        new("Lạng Sơn", ["lạng sơn", "lang son"]),
        new("Lào Cai", ["lào cai", "lao cai", "sa pa", "sapa"]),

        // Miền Trung & Tây Nguyên
        new("Thanh Hóa", ["thanh hóa", "thanh hoa", "sầm sơn", "bỉm sơn"]),
        new("Nghệ An", ["nghệ an", "nghe an", "tp. vinh", "tp vinh", "cửa lò"]),
        new("Hà Tĩnh", ["hà tĩnh", "ha tinh", "hồng lĩnh"]),
        new("Thừa Thiên Huế", ["thừa thiên huế", "thừa thiên - huế", "thừa thiên", "tp. huế", "tp huế", "huế", "hue"]),
        new("Đà Nẵng", ["đà nẵng", "da nang", "danang"]),
        new("Quảng Nam", ["quảng nam", "quang nam", "tam kỳ", "tam ky", "hội an", "hoi an"]),
        new("Quảng Ngãi", ["quảng ngãi", "quang ngai"]),
        new("Bình Định", ["bình định", "binh dinh", "quy nhơn", "quy nhon"]),
        new("Khánh Hòa", ["khánh hòa", "khanh hoa", "nha trang", "cam ranh"]),
        new("Đắk Lắk", ["đắk lắk", "đắc lắc", "dak lak", "daklak", "buôn ma thuột", "buon ma thuot"]),
        new("Lâm Đồng", ["lâm đồng", "lam dong", "đà lạt", "da lat", "bảo lộc"]),

        // Miền Nam
        new("TP. Hồ Chí Minh", ["tp. hồ chí minh", "tp.hồ chí minh", "tp hồ chí minh", "tp. hcm", "tp.hcm", "tphcm", "hồ chí minh", "ho chi minh", "sài gòn", "sai gon", "hcmc"]),
        new("Bình Dương", ["bình dương", "binh duong", "thủ dầu một", "thu dau mot", "dĩ an", "thuận an", "bến cát", "tân uyên"]),
        new("Đồng Nai", ["đồng nai", "dong nai", "biên hòa", "bien hoa", "long khánh"]),
        new("Bà Rịa - Vũng Tàu", ["bà rịa - vũng tàu", "bà rịa vũng tàu", "bà rịa", "vũng tàu", "vung tau", "ba ria"]),
        new("Long An", ["long an", "tân an", "tan an", "bến lức", "đức hòa"]),
        new("Tiền Giang", ["tiền giang", "tien giang", "mỹ tho", "my tho", "gò công"]),
        new("Cần Thơ", ["cần thơ", "can tho", "ninh kiều", "cái răng"]),
        new("An Giang", ["an giang", "long xuyên", "long xuyen", "châu đốc"])
    ];

    /// <summary>
    /// Nhận diện tên chuẩn của Tỉnh / Thành phố từ chuỗi địa chỉ (không phân biệt hoa thường).
    /// </summary>
    public static string? NhanDienTinhThanh(string? diaChi)
    {
        if (string.IsNullOrWhiteSpace(diaChi)) return null;
        string s = diaChi.Trim().ToLowerInvariant();

        foreach (var tinh in DanhSachTinhThanh)
        {
            if (tinh.TuKhoa.Any(tk => s.Contains(tk)))
                return tinh.TenChuan;
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra xem địa chỉ lấy hàng và tỉnh thành của bưu cục phụ trách có tương thích với nhau không.
    /// Nếu địa chỉ lấy hàng chỉ rõ một tỉnh khác với bưu cục gửi thì trả về false.
    /// Nếu không xác định rõ tỉnh trong địa chỉ thì trả về true để tránh chặn nhầm.
    /// </summary>
    public static bool KiemTraKhopTinhThanh(string? diaChiLay, string? tinhThanhBuuCuc)
    {
        if (string.IsNullOrWhiteSpace(diaChiLay) || string.IsNullOrWhiteSpace(tinhThanhBuuCuc))
            return true;

        string? tinhLay = NhanDienTinhThanh(diaChiLay);
        if (tinhLay == null) return true;

        string? tinhBc = NhanDienTinhThanh(tinhThanhBuuCuc) ?? tinhThanhBuuCuc.Trim();
        return string.Equals(tinhLay, tinhBc, StringComparison.OrdinalIgnoreCase);
    }
}
