using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC005: Theo dõi phí phòng và công nợ - chủ yếu Kế toán, Quản lý KTX xem báo cáo
    [Authorize]
    public class PhiCongNoController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PhiCongNoController(ApplicationDbContext db) => _db = db;

        [Authorize(Roles = "KeToan,QuanLyKTX")]
        public async Task<IActionResult> Index()
        {
            var phieuPhis = await _db.PhieuPhis.Include(p => p.SinhVien).ThenInclude(sv => sv!.TaiKhoan).OrderByDescending(p => p.HanThanhToan).ToListAsync();
            return View(phieuPhis);
        }

        [Authorize(Roles = "KeToan,QuanLyKTX")]
        public async Task<IActionResult> CongNo()
        {
            var congNos = await _db.CongNos.Include(c => c.SinhVien).ThenInclude(sv => sv!.TaiKhoan).OrderByDescending(c => c.TongNoHienTai).ToListAsync();
            return View(congNos);
        }

        [Authorize(Roles = "KeToan")]
        [HttpGet]
        public async Task<IActionResult> TaoPhieuPhi()
        {
            ViewBag.DanhSachSinhVien = await _db.SinhViens.Include(s => s.TaiKhoan).Where(s => s.TrangThaiO == TrangThaiOKTX.DangO).ToListAsync();
            return View(new PhieuPhi { ThangNam = DateTime.Now.ToString("MM/yyyy"), HanThanhToan = DateTime.Now.AddDays(10) });
        }

        [Authorize(Roles = "KeToan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhieuPhi(PhieuPhi model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachSinhVien = await _db.SinhViens.Include(s => s.TaiKhoan).Where(s => s.TrangThaiO == TrangThaiOKTX.DangO).ToListAsync();
                return View(model);
            }

            model.TrangThai = TrangThaiPhieuPhi.ChuaThanhToan;
            model.SoTienDaDong = 0;
            _db.PhieuPhis.Add(model);
            await _db.SaveChangesAsync();

            await CapNhatCongNoAsync(model.SinhVienId);

            TempData["ThongBao"] = "Đã tạo phiếu phí phòng.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "KeToan")]
        [HttpGet]
        public async Task<IActionResult> GhiNhanThanhToan(int id)
        {
            var phieu = await _db.PhieuPhis.Include(p => p.SinhVien).ThenInclude(sv => sv!.TaiKhoan).FirstOrDefaultAsync(p => p.Id == id);
            if (phieu == null) return NotFound();
            return View(phieu);
        }

        [Authorize(Roles = "KeToan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GhiNhanThanhToan(int id, decimal soTien, string? ghiChu)
        {
            var phieu = await _db.PhieuPhis.Include(p => p.SinhVien).FirstOrDefaultAsync(p => p.Id == id);
            if (phieu == null) return NotFound();

            var taiKhoanId = User.GetTaiKhoanId();
            var keToan = await _db.KeToans.FirstOrDefaultAsync(k => k.TaiKhoanId == taiKhoanId);

            if (soTien <= 0 || soTien > phieu.SoTienConLai)
            {
                TempData["Loi"] = "Số tiền thanh toán không hợp lệ (phải > 0 và không vượt quá số tiền còn lại).";
                return RedirectToAction(nameof(GhiNhanThanhToan), new { id });
            }

            _db.ThanhToans.Add(new ThanhToan
            {
                PhieuPhiId = phieu.Id,
                SoTien = soTien,
                NgayThanhToan = DateTime.Now,
                NguoiThuId = keToan?.Id,
                GhiChu = ghiChu
            });

            phieu.SoTienDaDong += soTien;
            phieu.TrangThai = phieu.SoTienConLai <= 0 ? TrangThaiPhieuPhi.DaThanhToan
                : phieu.SoTienDaDong > 0 ? TrangThaiPhieuPhi.DaThanhToanMotPhan
                : TrangThaiPhieuPhi.ChuaThanhToan;

            await _db.SaveChangesAsync();
            await CapNhatCongNoAsync(phieu.SinhVienId);

            TempData["ThongBao"] = $"Đã ghi nhận thanh toán {soTien:N0}đ cho sinh viên {phieu.SinhVien?.MSSV}.";
            return RedirectToAction(nameof(Index));
        }

        // Sinh viên xem phiếu phí / công nợ của chính mình
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CuaToi()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.Include(s => s.CongNo).FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            var phieuPhis = await _db.PhieuPhis.Where(p => p.SinhVienId == sv.Id).OrderByDescending(p => p.HanThanhToan).ToListAsync();
            ViewBag.CongNo = sv.CongNo;
            return View(phieuPhis);
        }

        private async Task CapNhatCongNoAsync(int sinhVienId)
        {
            var tongNo = await _db.PhieuPhis.Where(p => p.SinhVienId == sinhVienId && p.TrangThai != TrangThaiPhieuPhi.DaThanhToan)
                .SumAsync(p => p.SoTienPhaiDong - p.SoTienDaDong);

            var congNo = await _db.CongNos.FirstOrDefaultAsync(c => c.SinhVienId == sinhVienId);
            if (congNo == null)
            {
                _db.CongNos.Add(new CongNo { SinhVienId = sinhVienId, TongNoHienTai = tongNo });
            }
            else
            {
                congNo.TongNoHienTai = tongNo;
                congNo.CapNhatLanCuoi = DateTime.Now;
            }
            await _db.SaveChangesAsync();
        }
    }
}
