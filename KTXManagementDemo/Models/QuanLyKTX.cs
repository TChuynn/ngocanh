namespace KTXManagement.Demo.Models
{
    // UC002, UC003, UC004, UC006, UC007: tác nhân Quản lý KTX
    public class QuanLyKTX
    {
        public int Id { get; set; }
        public int TaiKhoanId { get; set; }
        public TaiKhoan? TaiKhoan { get; set; }
        public string? ChucVu { get; set; }
    }
}
