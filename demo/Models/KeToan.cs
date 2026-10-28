namespace KTXManagement.Demo.Models
{
    // UC005: tác nhân Kế toán
    public class KeToan
    {
        public int Id { get; set; }
        public int TaiKhoanId { get; set; }
        public TaiKhoan? TaiKhoan { get; set; }
        public string? ChucVu { get; set; }

        public ICollection<ThanhToan> DanhSachThanhToanDaGhiNhan { get; set; } = new List<ThanhToan>();
    }
}
