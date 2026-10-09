using Microsoft.EntityFrameworkCore;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * DanhSachTrang<T> — kết quả một trang dữ liệu.
 * TaoAsync nhận IQueryable ĐÃ lọc + sắp xếp, rồi mới Count() và Skip().Take() ngay trên SQL Server.
 * Không gọi ToList() trước khi phân trang để tránh tải toàn bộ bảng về bộ nhớ.
 */
/// <summary>Thông tin phân trang dùng cho partial _PhanTrang.</summary>
public interface IPhanTrang
{
    int Trang { get; }
    int KichThuocTrang { get; }
    int TongSoDong { get; }
    int TongSoTrang { get; }
}

public class DanhSachTrang<T> : IPhanTrang
{
    public List<T> DanhSach { get; init; } = [];
    public int Trang { get; init; }
    public int KichThuocTrang { get; init; }
    public int TongSoDong { get; init; }
    public int TongSoTrang => Math.Max(1, (int)Math.Ceiling(TongSoDong / (double)KichThuocTrang));

    public static async Task<DanhSachTrang<T>> TaoAsync(IQueryable<T> truyVan, int trang, int kichThuocTrang)
    {
        int tong = await truyVan.CountAsync();
        int tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)kichThuocTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        var ds = await truyVan.Skip((trang - 1) * kichThuocTrang).Take(kichThuocTrang).ToListAsync();
        return new DanhSachTrang<T> { DanhSach = ds, Trang = trang, KichThuocTrang = kichThuocTrang, TongSoDong = tong };
    }
}
