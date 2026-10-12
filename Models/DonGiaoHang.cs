using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyGiaoNhan_UNETI5_DHTI17A4.Models;

/*
 * Bảng DonGiaoHang
 * - MaDon: khóa chính tự tăng; hiển thị dạng DH000012 (MaHienThi).
 * - PhiVanChuyen = PhiCoBan + PhuPhiKhoiLuong + PhuPhiLoaiHang (do TinhPhiXuLy tính).
 * - Thu hộ (COD): TienThuHo = tiền hàng shipper thu của người nhận để trả lại người gửi.
 */
public class DonGiaoHang
{
    [Key]
    [Display(Name = "Mã đơn")]
    public int MaDon { get; set; }

    [Display(Name = "Khách hàng")]
    public int MaKhachHang { get; set; }
    public KhachHang? KhachHang { get; set; }

    [Display(Name = "Loại hàng")]
    public int MaLoaiHang { get; set; }
    public LoaiHang? LoaiHang { get; set; }

    [Display(Name = "Khu vực giao")]
    public int MaKhuVuc { get; set; }
    public KhuVuc? KhuVuc { get; set; }

    /// <summary>Bưu cục khách mang hàng tới gửi.</summary>
    [Display(Name = "Bưu cục gửi")]
    public int? MaBuuCucGui { get; set; }
    public BuuCuc? BuuCucGui { get; set; }

    /// <summary>Bưu cục phụ trách khu vực giao (KhuVuc.MaBuuCuc lúc tạo / sửa đơn).</summary>
    [Display(Name = "Bưu cục nhận")]
    public int? MaBuuCucNhan { get; set; }
    public BuuCuc? BuuCucNhan { get; set; }

    /// <summary>Bưu cục đang giữ hàng (null khi hàng ở chỗ khách / shipper / đã giao).</summary>
    [Display(Name = "Đang ở bưu cục")]
    public int? MaBuuCucHienTai { get; set; }
    public BuuCuc? BuuCucHienTai { get; set; }

    /// <summary>Shipper đang giữ hàng (null khi hàng ở bưu cục / chỗ khách / đã giao).</summary>
    [Display(Name = "Shipper đang giữ")]
    public int? MaNhanVienDangGiu { get; set; }
    public NhanVienGiaoHang? NhanVienDangGiu { get; set; }

    [Required(ErrorMessage = "Nhập tên người nhận")]
    [StringLength(100)]
    [Display(Name = "Người nhận")]
    public string TenNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập số điện thoại người nhận")]
    [StringLength(15)]
    [Display(Name = "Số điện thoại người nhận")]
    public string SoDienThoaiNguoiNhan { get; set; } = "";

    [Required(ErrorMessage = "Nhập địa chỉ nhận")]
    [StringLength(255)]
    [Display(Name = "Địa chỉ nhận")]
    public string DiaChiNhan { get; set; } = "";

    [Range(0.01, 100_000, ErrorMessage = "Khối lượng phải lớn hơn 0")]
    [Display(Name = "Khối lượng (kg)")]
    public decimal KhoiLuong { get; set; }

    [Range(0.0001, 1000, ErrorMessage = "Kích thước phải lớn hơn 0 m³")]
    [Display(Name = "Kích thước (m³)")]
    public decimal KichThuocM3 { get; set; } = 0.005m;

    [Display(Name = "Ngày tạo")]
    public DateTime NgayTao { get; set; } = DateTime.Now;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày giao dự kiến")]
    public DateTime NgayGiaoDuKien { get; set; }

    // ----- Thành phần phí -----
    [Display(Name = "Phí cơ bản")] public decimal PhiCoBan { get; set; }
    [Display(Name = "Phụ phí khối lượng")] public decimal PhuPhiKhoiLuong { get; set; }
    [Display(Name = "Phụ phí kích thước")] public decimal PhuPhiKichThuoc { get; set; }
    [Display(Name = "Phụ phí loại hàng")] public decimal PhuPhiLoaiHang { get; set; }
    [Display(Name = "Phí vận chuyển")] public decimal PhiVanChuyen { get; set; }

    // ----- Thu hộ (COD) và người trả phí -----
    [Range(typeof(decimal), "0", "50000000", ErrorMessage = "Tiền thu hộ từ 0 đến 50.000.000 đ")]
    [Display(Name = "Tiền thu hộ")]
    public decimal TienThuHo { get; set; }

    [Display(Name = "Người trả phí")]
    public NguoiTraPhi NguoiTraPhi { get; set; } = NguoiTraPhi.NguoiGui;

    [Display(Name = "Ngày đối soát")]
    public DateTime? NgayDoiSoat { get; set; }

    [Display(Name = "Ngày thu phí người gửi")]
    public DateTime? NgayThuPhiNguoiGui { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Phí chuyển hoàn")]
    public decimal PhiChuyenHoan { get; set; } = 0;

    [Display(Name = "Ngày thu phí hoàn")]
    public DateTime? NgayThuPhiHoan { get; set; }

    [Display(Name = "Ngày yêu cầu thanh toán")]
    public DateTime? NgayYeuCauThanhToan { get; set; }

    [Display(Name = "Giao dịch nộp tiền")]
    public int? MaGiaoDichNop { get; set; }
    public GiaoDichVi? GiaoDichNop { get; set; }

    [Display(Name = "Hình thức gửi")]
    public HinhThucGui HinhThucGui { get; set; } = HinhThucGui.GuiTaiBuuCuc;

    [StringLength(255)]
    [Display(Name = "Địa chỉ lấy hàng")]
    public string? DiaChiLayHang { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiDon TrangThai { get; set; } = TrangThaiDon.DaTao;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Display(Name = "Ngày hoàn tất")]
    public DateTime? NgayHoanTat { get; set; }

    [Display(Name = "Ngày hoàn hàng")]
    public DateTime? NgayHoanHang { get; set; }

    [Timestamp]
    public byte[] PhienBan { get; set; } = [];

    [StringLength(300)]
    [Display(Name = "Ảnh giao hàng")]
    public string? AnhGiaoHang { get; set; }

    public ICollection<PhanCongGiaoHang> PhanCongs { get; set; } = [];
    public ICollection<LichSuGiaoNhan> LichSus { get; set; } = [];

    [NotMapped]
    public decimal TongThuNguoiNhan => TienThuHo + (NguoiTraPhi == NguoiTraPhi.NguoiNhan ? PhiVanChuyen : 0);

    [NotMapped]
    public decimal TienTraNguoiGui => Math.Max(0, TienThuHo - (NguoiTraPhi == NguoiTraPhi.NguoiGui ? PhiVanChuyen : 0));

    [NotMapped]
    public bool LaDonHoan => TrangThai is TrangThaiDon.ChoChuyenHoan or TrangThaiDon.DangChuyenHoan
                          or TrangThaiDon.DaVeBuuCucGui or TrangThaiDon.DangGiaoHoan or TrangThaiDon.DaHoanHang
                          || (TrangThai == TrangThaiDon.DaHuy && NgayHoanHang != null);

    /// <summary>Số tiền phí chuyển hoàn hoặc 2 chiều người gửi chưa thanh toán.</summary>
    [NotMapped]
    public decimal PhiHoanChuaThu
    {
        get
        {
            if (!LaDonHoan) return 0;
            decimal phaiThu = 0;
            if (NgayThuPhiNguoiGui == null) phaiThu += PhiVanChuyen;
            if (NgayThuPhiHoan == null) phaiThu += PhiChuyenHoan;
            return phaiThu;
        }
    }

    [NotMapped]
    public decimal PhiNguoiGuiPhaiTra => LaDonHoan
        ? PhiHoanChuaThu
        : (NguoiTraPhi == NguoiTraPhi.NguoiGui ? Math.Max(0, PhiVanChuyen - TienThuHo) : 0);

    [NotMapped]
    public bool ConNoPhi => ((TrangThai == TrangThaiDon.HoanTat && NguoiTraPhi == NguoiTraPhi.NguoiGui && PhiVanChuyen > TienThuHo && NgayThuPhiNguoiGui == null)
                          || (LaDonHoan && PhiHoanChuaThu > 0));

    [NotMapped]
    public bool DangChoDuyetCuoc => ConNoPhi && NgayYeuCauThanhToan != null;

    [NotMapped]
    public string MaHienThi => $"DH{NgayTao:yyyyMMdd}{MaDon:D4}";
}
