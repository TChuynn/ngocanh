using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;
    }

    // ==================== CÁC VIEWMODEL CHO TÍNH NĂNG AI ====================

    // 1. AI Tóm tắt phản ánh/sự cố theo nhóm vấn đề (UC006 / UC007)
    public class PhanAnhNhomVanDe
    {
        public string TenNhom { get; set; } = string.Empty;
        public string Icon { get; set; } = "⚡";
        public string MauSac { get; set; } = "warning";
        public string MucDoUuTien { get; set; } = "Trung bình";
        public string MucDoUuTienBadge { get; set; } = "warning";
        public int SoLuong { get; set; }
        public double TyLePhanTram { get; set; }
        public int SoLuongMoi { get; set; }
        public int SoLuongDangXuLy { get; set; }
        public int SoLuongDaXuLy { get; set; }
        public List<string> DanhSachPhongLienQuan { get; set; } = new();
        public string TomTatAI { get; set; } = string.Empty;
        public string DeXuatHanhDongAI { get; set; } = string.Empty;
        public List<PhanAnhSuCo> DanhSachPhanAnh { get; set; } = new();
    }

    public class AITomTatPhanAnhViewModel
    {
        public int TongSoPhanAnh { get; set; }
        public int TongPhanAnhMoi { get; set; }
        public int TongDangXuLy { get; set; }
        public int TongDaXuLy { get; set; }
        public string NhanXetTongQuanAI { get; set; } = string.Empty;
        public List<PhanAnhNhomVanDe> CacNhomVanDe { get; set; } = new();
        public List<string> KhuyenNghiUuTienAI { get; set; } = new();
        public DateTime ThoiGianPhanTich { get; set; } = DateTime.Now;
    }

    // 2. AI Gợi ý xếp phòng dựa trên chỗ trống và tiêu chí cho sinh viên (UC004)
    public class TieuChiGoiYPhongViewModel
    {
        public int SinhVienId { get; set; }
        public string MSSV { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public GioiTinh GioiTinh { get; set; }
        public string? Lop { get; set; }
        public string? Khoa { get; set; }

        // Các tiêu chí cơ bản sinh viên lựa chọn
        public string UuTienBanCungPhong { get; set; } = "CungKhoa"; // "CungKhoa", "CungLop", "KhongYeuCau"
        public string MucGiaMongMuon { get; set; } = "TatCa";        // "TietKiem", "TieuChuan", "TatCa"
        public string ViTriTang { get; set; } = "TatCa";             // "TangThap", "TangCao", "TatCa"
        public string PhongCachSong { get; set; } = "YenTinh";       // "YenTinh", "DongVui", "CanBang"
        public string? GhiChuNguyenVong { get; set; }
    }

    public class GiuongGoiYInfo
    {
        public int GiuongId { get; set; }
        public string SoGiuong { get; set; } = string.Empty;
    }

    public class PhongGoiYItem
    {
        public int PhongId { get; set; }
        public string SoPhong { get; set; } = string.Empty;
        public string TenToaNha { get; set; } = string.Empty;
        public int TangSo { get; set; }
        public int SucChua { get; set; }
        public int SoGiuongTrong { get; set; }
        public decimal DonGiaThang { get; set; }
        public int DiemPhuHop { get; set; } // % 0 - 100
        public string DanhGiaPhuHop { get; set; } = "Rất phù hợp";
        public string MauBadge { get; set; } = "success";
        public List<string> DiemNoiBat { get; set; } = new();
        public string NhanXetAI { get; set; } = string.Empty;
        public List<GiuongGoiYInfo> GiuongTrong { get; set; } = new();
        public List<string> BanCungPhong { get; set; } = new();
    }

    public class KetQuaGoiYPhongViewModel
    {
        public TieuChiGoiYPhongViewModel TieuChi { get; set; } = new();
        public List<PhongGoiYItem> DanhSachGoiY { get; set; } = new();
        public string NhanXetTongQuatAI { get; set; } = string.Empty;
    }

    // ViewModel cho trang xử lý xếp phòng của Quản lý KTX (UC004)
    public class XepPhongViewModel
    {
        public DangKyO? YeuCau { get; set; }
        public string GoiYAI { get; set; } = string.Empty;
        public List<Phong> DanhSachPhongTrong { get; set; } = new();
    }

    // ViewModel trang thống kê UC007
    public class ThongKeViewModel
    {
        public int TongSoPhong { get; set; }
        public int TongSoGiuong { get; set; }
        public int SoGiuongDaCoNguoi { get; set; }
        public double TyLeLapDay => TongSoGiuong == 0 ? 0 : Math.Round(SoGiuongDaCoNguoi * 100.0 / TongSoGiuong, 1);

        public decimal TongCongNo { get; set; }
        public int SoSinhVienConNo { get; set; }

        public int SoPhanAnhMoi { get; set; }
        public int SoPhanAnhDangXuLy { get; set; }
        public int SoPhanAnhDaXuLy { get; set; }

        public string TomTatAI { get; set; } = string.Empty;
    }
}
