using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models
{
    // UC002: Quản lý hạ tầng ký túc xá (tòa, tầng, phòng, giường)
    public class ToaNha
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TenToaNha { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? MoTa { get; set; }

        public ICollection<Tang> DanhSachTang { get; set; } = new List<Tang>();
    }

    public class Tang
    {
        public int Id { get; set; }

        public int ToaNhaId { get; set; }
        public ToaNha? ToaNha { get; set; }

        [Required]
        public int SoThuTu { get; set; }

        public ICollection<Phong> DanhSachPhong { get; set; } = new List<Phong>();
    }

    public class Phong
    {
        public int Id { get; set; }

        public int TangId { get; set; }
        public Tang? Tang { get; set; }

        [Required, MaxLength(20)]
        public string SoPhong { get; set; } = string.Empty;

        [Range(1, 20)]
        public int SucChua { get; set; } = 4;

        public decimal DonGiaThang { get; set; }

        public TrangThaiPhong TrangThai { get; set; } = TrangThaiPhong.Trong;

        public ICollection<Giuong> DanhSachGiuong { get; set; } = new List<Giuong>();

        // Thuộc tính tiện ích không lưu DB (dùng để hiển thị)
        public int SoGiuongTrong => DanhSachGiuong.Count(g => g.TrangThai == TrangThaiGiuong.Trong);

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int SoSinhVienHienTai => DanhSachGiuong.Count(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null);
    }

    public class Giuong
    {
        public int Id { get; set; }

        public int PhongId { get; set; }
        public Phong? Phong { get; set; }

        [Required, MaxLength(10)]
        public string SoGiuong { get; set; } = string.Empty;

        public TrangThaiGiuong TrangThai { get; set; } = TrangThaiGiuong.Trong;

        // Sinh viên hiện đang ở giường này (nếu có)
        public int? SinhVienId { get; set; }
        public SinhVien? SinhVien { get; set; }
    }
}
