using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models
{
    /// <summary>
    /// Lớp NguoiDung/TaiKhoan gộp (đơn giản hoá cho bản demo):
    /// chứa thông tin đăng nhập + thông tin cá nhân dùng chung cho 3 tác nhân.
    /// UC001: kiểm tra trạng thái tài khoản trước, rồi mới đối chiếu mật khẩu (A1 -> A2 -> A3).
    /// </summary>
    public class TaiKhoan
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        // Demo: lưu hash SHA256 đơn giản, KHÔNG dùng cho production
        [Required]
        public string MatKhauHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? SoDienThoai { get; set; }

        [Required]
        public VaiTro VaiTro { get; set; }

        [Required]
        public TrangThaiTaiKhoan TrangThai { get; set; } = TrangThaiTaiKhoan.HoatDong;

        // UC001-A2/A3: đếm số lần đăng nhập sai liên tiếp, khóa khi vượt ngưỡng
        public int SoLanDangNhapSai { get; set; } = 0;
        public const int SoLanSaiToiDa = 5;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation 1-1 tuỳ vai trò
        public SinhVien? HoSoSinhVien { get; set; }
        public KeToan? HoSoKeToan { get; set; }
        public QuanLyKTX? HoSoQuanLy { get; set; }
    }
}
