using KTXManagement.Demo.Models;
using KTXManagement.Demo.Models.ViewModels;

namespace KTXManagement.Demo.Services
{
    public interface IAIService
    {
        /// <summary>AI tóm tắt và phân tích phản ánh/sự cố theo nhóm vấn đề cho Quản lý KTX.</summary>
        Task<AITomTatPhanAnhViewModel> TomTatPhanAnhTheoNhomAsync(List<PhanAnhSuCo> danhSachPhanAnh);

        /// <summary>AI tóm tắt xu hướng phản ánh/sự cố (chuỗi văn bản ngắn) cho báo cáo thống kê (UC007).</summary>
        Task<string> TomTatPhanAnhAsync(List<PhanAnhSuCo> danhSachPhanAnh);

        /// <summary>AI gợi ý xếp phòng dựa trên chỗ trống và các tiêu chí cơ bản cho sinh viên (UC004).</summary>
        Task<KetQuaGoiYPhongViewModel> GoiYPhongChoSinhVienAsync(SinhVien sv, TieuChiGoiYPhongViewModel tieuChi);

        /// <summary>AI gợi ý phòng/giường phù hợp cho một yêu cầu đăng ký ở khi Quản lý duyệt (UC004).</summary>
        Task<string> GoiYPhongAsync(DangKyO yeuCau);
    }
}
