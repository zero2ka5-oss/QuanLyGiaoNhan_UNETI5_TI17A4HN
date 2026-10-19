namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

public class ChatBotYeuCauVM
{
    public string NoiDung { get; set; } = "";
}

public class ChatHanhDongVM
{
    public string TieuDe { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Icon { get; set; }
    public string? Kieu { get; set; } // "btn-cam", "btn-outline-primary", etc.
}

public class ChatBotPhanHoiVM
{
    public string CauTraLoi { get; set; } = "";
    public List<ChatHanhDongVM> HanhDong { get; set; } = [];
    public List<string> GoiY { get; set; } = [];
    public bool LaTraCuuDon { get; set; }
    public object? DuLieuDon { get; set; }
}
