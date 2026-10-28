using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models
{
    /// <summary>
    /// UC004 (bản revised): Sinh viên chủ động gửi yêu cầu đăng ký ở,
    /// Quản lý KTX tiếp nhận/xếp phòng (có AI gợi ý), xử lý chuyển phòng (A4)
    /// và trả phòng (A5 - có kiểm tra công nợ liên kết UC005).
    /// </summary>
    public class DangKyO
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien? SinhVien { get; set; }

        public int? PhongId { get; set; }
        public Phong? Phong { get; set; }

        public int? GiuongId { get; set; }
        public Giuong? Giuong { get; set; }

        public DateTime NgayDangKy { get; set; } = DateTime.Now;
        public DateTime? NgayNhanPhong { get; set; }
        public DateTime? NgayTraPhong { get; set; }

        public TrangThaiDangKyO TrangThai { get; set; } = TrangThaiDangKyO.ChoDuyet;

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        // Gợi ý phòng do AIService sinh ra khi QL bắt đầu xử lý yêu cầu (UC004 - AI gợi ý phòng)
        [MaxLength(1000)]
        public string? GoiYAI { get; set; }
    }
}
