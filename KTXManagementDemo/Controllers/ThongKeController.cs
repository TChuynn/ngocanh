using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Models.ViewModels;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC007: Thống kê công suất, công nợ, phản ánh và tóm tắt phản ánh bằng AI
    [Authorize(Roles = "QuanLyKTX")]
    public class ThongKeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAIService _aiService;

        public ThongKeController(ApplicationDbContext db, IAIService aiService)
        {
            _db = db;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new ThongKeViewModel
            {
                TongSoPhong = await _db.Phongs.CountAsync(),
                TongSoGiuong = await _db.Giuongs.CountAsync(),
                SoGiuongDaCoNguoi = await _db.Giuongs.CountAsync(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi),
                TongCongNo = await _db.CongNos.SumAsync(c => c.TongNoHienTai),
                SoSinhVienConNo = await _db.CongNos.CountAsync(c => c.TongNoHienTai > 0),
                SoPhanAnhMoi = await _db.PhanAnhSuCos.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.Moi),
                SoPhanAnhDangXuLy = await _db.PhanAnhSuCos.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.DangXuLy),
                SoPhanAnhDaXuLy = await _db.PhanAnhSuCos.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.DaXuLy)
            };

            var danhSachPhanAnh = await _db.PhanAnhSuCos.ToListAsync();
            vm.TomTatAI = await _aiService.TomTatPhanAnhAsync(danhSachPhanAnh);

            return View(vm);
        }
    }
}
