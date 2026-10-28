using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC006: Quản lý phản ánh/sự cố và vi phạm nội quy
    [Authorize]
    public class PhanAnhController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PhanAnhController(ApplicationDbContext db) => _db = db;

        // Quản lý KTX: xem tất cả phản ánh
        [Authorize(Roles = "QuanLyKTX")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.PhanAnhSuCos.Include(p => p.SinhVien).Include(p => p.Phong)
                .OrderByDescending(p => p.NgayGui).ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpGet]
        public async Task<IActionResult> XuLy(int id)
        {
            var pa = await _db.PhanAnhSuCos.Include(p => p.SinhVien).Include(p => p.Phong).FirstOrDefaultAsync(p => p.Id == id);
            if (pa == null) return NotFound();
            return View(pa);
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XuLy(int id, string phanHoi, TrangThaiPhanAnh trangThai)
        {
            var pa = await _db.PhanAnhSuCos.FirstOrDefaultAsync(p => p.Id == id);
            if (pa == null) return NotFound();

            pa.PhanHoi = phanHoi;
            pa.TrangThai = trangThai;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật phản hồi/xử lý phản ánh.";
            return RedirectToAction(nameof(Index));
        }

        // Sinh viên: gửi phản ánh mới + xem danh sách của mình
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CuaToi()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            var list = await _db.PhanAnhSuCos.Where(p => p.SinhVienId == sv.Id).OrderByDescending(p => p.NgayGui).ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "SinhVien")]
        [HttpGet]
        public async Task<IActionResult> Gui()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.Include(s => s.DanhSachDangKyO).ThenInclude(d => d.Phong)
                .FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            var phongHienTai = sv?.DanhSachDangKyO.FirstOrDefault(d => d.TrangThai == TrangThaiDangKyO.DangO)?.Phong;
            ViewBag.PhongHienTai = phongHienTai;
            return View(new PhanAnhSuCo());
        }

        [Authorize(Roles = "SinhVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Gui(PhanAnhSuCo model)
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.Include(s => s.DanhSachDangKyO).ThenInclude(d => d.Phong)
                .FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.TieuDe) || string.IsNullOrWhiteSpace(model.NoiDung))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ tiêu đề và nội dung phản ánh.");
                ViewBag.PhongHienTai = sv.DanhSachDangKyO.FirstOrDefault(d => d.TrangThai == TrangThaiDangKyO.DangO)?.Phong;
                return View(model);
            }

            var phongHienTai = sv.DanhSachDangKyO.FirstOrDefault(d => d.TrangThai == TrangThaiDangKyO.DangO)?.Phong;

            _db.PhanAnhSuCos.Add(new PhanAnhSuCo
            {
                SinhVienId = sv.Id,
                PhongId = phongHienTai?.Id,
                TieuDe = model.TieuDe,
                NoiDung = model.NoiDung,
                LoaiPhanAnh = model.LoaiPhanAnh,
                NgayGui = DateTime.Now,
                TrangThai = TrangThaiPhanAnh.Moi
            });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi phản ánh/sự cố tới Quản lý KTX.";
            return RedirectToAction(nameof(CuaToi));
        }
    }
}
