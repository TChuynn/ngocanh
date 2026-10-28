using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KTXManagement.Demo.Models
{
    // UC005: Theo dõi phí phòng và công nợ
    public class PhieuPhi
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien? SinhVien { get; set; }

        [Required, MaxLength(10)]
        public string ThangNam { get; set; } = string.Empty; // vd "10/2026"

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienPhaiDong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTienDaDong { get; set; } = 0;

        public DateTime HanThanhToan { get; set; }

        public TrangThaiPhieuPhi TrangThai { get; set; } = TrangThaiPhieuPhi.ChuaThanhToan;

        public ICollection<ThanhToan> DanhSachThanhToan { get; set; } = new List<ThanhToan>();

        [NotMapped]
        public decimal SoTienConLai => SoTienPhaiDong - SoTienDaDong;
    }

    public class ThanhToan
    {
        public int Id { get; set; }

        public int PhieuPhiId { get; set; }
        public PhieuPhi? PhieuPhi { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        public DateTime NgayThanhToan { get; set; } = DateTime.Now;

        public int? NguoiThuId { get; set; } // KeToan.Id
        public KeToan? NguoiThu { get; set; }

        [MaxLength(200)]
        public string? GhiChu { get; set; }
    }

    // Bảng tổng hợp công nợ hiện tại của từng sinh viên (được AIService/PhiCongNoController cập nhật lại)
    public class CongNo
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien? SinhVien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TongNoHienTai { get; set; } = 0;

        public DateTime CapNhatLanCuoi { get; set; } = DateTime.Now;
    }
}
