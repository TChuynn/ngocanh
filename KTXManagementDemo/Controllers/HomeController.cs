using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vaiTro = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            ViewBag.VaiTro = vaiTro;
            ViewBag.HoTen = User.GetHoTen();

            if (vaiTro == VaiTro.QuanLyKTX.ToString())
            {
                ViewBag.SoPhong = await _db.Phongs.CountAsync();
                ViewBag.SoGiuongTrong = await _db.Giuongs.CountAsync(g => g.TrangThai == TrangThaiGiuong.Trong);
                ViewBag.ChoDuyet = await _db.DangKyOs.CountAsync(d => d.TrangThai == TrangThaiDangKyO.ChoDuyet);
                ViewBag.PhanAnhMoi = await _db.PhanAnhSuCos.CountAsync(p => p.TrangThai == TrangThaiPhanAnh.Moi);
            }
            else if (vaiTro == VaiTro.KeToan.ToString())
            {
                ViewBag.TongCongNo = await _db.CongNos.SumAsync(c => c.TongNoHienTai);
                ViewBag.SoSinhVienConNo = await _db.CongNos.CountAsync(c => c.TongNoHienTai > 0);
                ViewBag.PhieuChuaThanhToan = await _db.PhieuPhis.CountAsync(p => p.TrangThai != TrangThaiPhieuPhi.DaThanhToan);
            }
            else if (vaiTro == VaiTro.SinhVien.ToString())
            {
                var taiKhoanId = User.GetTaiKhoanId();
                var sv = await _db.SinhViens.Include(s => s.CongNo).FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
                ViewBag.SinhVien = sv;
                if (sv != null)
                {
                    ViewBag.PhongHienTai = await _db.DangKyOs.Include(d => d.Phong).Include(d => d.Giuong)
                        .Where(d => d.SinhVienId == sv.Id && d.TrangThai == TrangThaiDangKyO.DangO)
                        .FirstOrDefaultAsync();
                }
            }

            return View();
        }

        public IActionResult Error() => View();
    }
}
