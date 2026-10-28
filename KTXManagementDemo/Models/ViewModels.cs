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

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Reply { get; set; } = string.Empty;
    }

    // ViewModel cho trang xử lý xếp phòng (UC004) - hiển thị gợi ý AI + danh sách phòng còn trống
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
