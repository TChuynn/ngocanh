namespace KTXManagement.Demo.Models
{
    // Vai trò tài khoản - dùng cho phân quyền UC001
    public enum VaiTro
    {
        QuanLyKTX,
        SinhVien,
        KeToan
    }

    // Trạng thái tài khoản - UC001 (A1: tài khoản bị khóa)
    public enum TrangThaiTaiKhoan
    {
        HoatDong,
        BiKhoa
    }

    public enum GioiTinh
    {
        Nam,
        Nu,
        Khac
    }

    // UC003: trạng thái ở KTX của sinh viên
    public enum TrangThaiOKTX
    {
        ChuaDangKy,
        DaDangKy,
        DangO,
        DaTraPhong
    }

    // UC002: trạng thái phòng
    public enum TrangThaiPhong
    {
        Trong,
        DangSuDung,
        Day,
        BaoTri
    }

    // UC002: trạng thái giường
    public enum TrangThaiGiuong
    {
        Trong,
        DaCoNguoi
    }

    // UC004: trạng thái yêu cầu đăng ký ở / chuyển / trả phòng
    public enum TrangThaiDangKyO
    {
        ChoDuyet,       // SV vừa gửi yêu cầu
        DaDuyet,        // QL đã xếp phòng/giường
        TuChoi,         // QL từ chối
        DangO,          // SV đã nhận phòng, đang ở
        DangChoTraPhong, // SV yêu cầu trả phòng, chờ QL xác nhận (kiểm tra công nợ)
        DaTraPhong      // Hoàn tất trả phòng
    }

    // UC005: trạng thái phiếu phí
    public enum TrangThaiPhieuPhi
    {
        ChuaThanhToan,
        DaThanhToanMotPhan,
        DaThanhToan,
        QuaHan
    }

    // UC006: loại phản ánh
    public enum LoaiPhanAnh
    {
        SuCoCoSoVatChat,
        ViPhamNoiQuy,
        Khac
    }

    // UC006: trạng thái xử lý phản ánh
    public enum TrangThaiPhanAnh
    {
        Moi,
        DangXuLy,
        DaXuLy,
        TuChoi
    }
}
