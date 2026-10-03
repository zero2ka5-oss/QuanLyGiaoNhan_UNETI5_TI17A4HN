// Họ và tên: Nguyễn Minh Hướng
// Mã sinh viên: 23103100268
// Nội dung thực hiện: Kiểu kết quả trả về thống nhất từ tầng xử lý nghiệp vụ cho Controller.

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Controllers;

/// <summary>KetQua.Dat("...") khi thành công, KetQua.Loi("...") kèm lý do khi vi phạm quy tắc nghiệp vụ.</summary>
public record KetQua(bool ThanhCong, string ThongBao = "")
{
    public static KetQua Dat(string thongBao = "") => new(true, thongBao);
    public static KetQua Loi(string thongBao) => new(false, thongBao);
}

public record KetQua<T>(bool ThanhCong, string ThongBao = "", T? DuLieu = default)
{
    public static KetQua<T> Dat(T duLieu, string thongBao = "") => new(true, thongBao, duLieu);
    public static KetQua<T> Loi(string thongBao) => new(false, thongBao);
}
