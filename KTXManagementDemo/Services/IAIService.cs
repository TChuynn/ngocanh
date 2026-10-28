using KTXManagement.Demo.Models;

namespace KTXManagement.Demo.Services
{
    public interface IAIService
    {
        /// <summary>Chatbot hỏi-đáp cho người dùng đang đăng nhập (nếu có).</summary>
        Task<string> TraLoiAsync(string cauHoi, TaiKhoan? nguoiDung);

        /// <summary>UC004: AI gợi ý phòng/giường phù hợp cho một yêu cầu đăng ký ở.</summary>
        Task<string> GoiYPhongAsync(DangKyO yeuCau);

        /// <summary>UC007: AI tóm tắt xu hướng phản ánh/sự cố để đưa vào báo cáo thống kê.</summary>
        Task<string> TomTatPhanAnhAsync(List<PhanAnhSuCo> danhSachPhanAnh);
    }
}
