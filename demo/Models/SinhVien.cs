using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models
{
    // UC003: Quản lý hồ sơ sinh viên nội trú
    public class SinhVien
    {
        public int Id { get; set; }

        public int TaiKhoanId { get; set; }
        public TaiKhoan? TaiKhoan { get; set; }

        [Required, MaxLength(20)]
        public string MSSV { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Lop { get; set; }

        [MaxLength(100)]
        public string? Khoa { get; set; }

        public GioiTinh GioiTinh { get; set; }

        public DateTime? NgaySinh { get; set; }

        [MaxLength(200)]
        public string? QueQuan { get; set; }

        public TrangThaiOKTX TrangThaiO { get; set; } = TrangThaiOKTX.ChuaDangKy;

        // Navigation
        public ICollection<DangKyO> DanhSachDangKyO { get; set; } = new List<DangKyO>();
        public ICollection<PhieuPhi> DanhSachPhieuPhi { get; set; } = new List<PhieuPhi>();
        public ICollection<PhanAnhSuCo> DanhSachPhanAnh { get; set; } = new List<PhanAnhSuCo>();
        public CongNo? CongNo { get; set; }
    }
}
