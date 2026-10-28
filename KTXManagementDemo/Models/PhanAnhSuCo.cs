using System.ComponentModel.DataAnnotations;

namespace KTXManagement.Demo.Models
{
    // UC006: Quản lý phản ánh/sự cố và vi phạm nội quy
    public class PhanAnhSuCo
    {
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        public SinhVien? SinhVien { get; set; }

        public int? PhongId { get; set; }
        public Phong? Phong { get; set; }

        [Required, MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string NoiDung { get; set; } = string.Empty;

        public LoaiPhanAnh LoaiPhanAnh { get; set; } = LoaiPhanAnh.SuCoCoSoVatChat;

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public TrangThaiPhanAnh TrangThai { get; set; } = TrangThaiPhanAnh.Moi;

        [MaxLength(1000)]
        public string? PhanHoi { get; set; }
    }
}
