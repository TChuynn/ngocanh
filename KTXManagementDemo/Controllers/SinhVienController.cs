using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC003: Quản lý hồ sơ sinh viên nội trú - Quản lý KTX thao tác; Sinh viên chỉ xem hồ sơ của mình
    [Authorize]
    public class SinhVienController : Controller
    {
        private readonly ApplicationDbContext _db;
        public SinhVienController(ApplicationDbContext db) => _db = db;

        [Authorize(Roles = "QuanLyKTX")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.SinhViens.Include(s => s.TaiKhoan).Include(s => s.CongNo).ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "QuanLyKTX")]
        public async Task<IActionResult> ChiTiet(int id)
        {
            var sv = await _db.SinhViens
                .Include(s => s.TaiKhoan)
                .Include(s => s.CongNo)
                .Include(s => s.DanhSachDangKyO).ThenInclude(d => d.Phong)
                .Include(s => s.DanhSachPhieuPhi)
                .Include(s => s.DanhSachPhanAnh)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sv == null) return NotFound();
            return View(sv);
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpGet]
        public IActionResult TaoMoi() => View(new TaoSinhVienInput());

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMoi(TaoSinhVienInput input)
        {
            if (!ModelState.IsValid) return View(input);

            if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == input.TenDangNhap))
            {
                ModelState.AddModelError(nameof(input.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                return View(input);
            }

            if (await _db.SinhViens.AnyAsync(s => s.MSSV == input.MSSV))
            {
                ModelState.AddModelError(nameof(input.MSSV), "MSSV đã tồn tại trên hệ thống.");
                return View(input);
            }

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = input.TenDangNhap,
                MatKhauHash = PasswordHasher.Hash(input.MatKhau),
                HoTen = input.HoTen,
                Email = input.Email,
                VaiTro = VaiTro.SinhVien
            };
            _db.TaiKhoans.Add(taiKhoan);
            await _db.SaveChangesAsync();

            var sv = new SinhVien
            {
                TaiKhoanId = taiKhoan.Id,
                MSSV = input.MSSV,
                Lop = input.Lop,
                Khoa = input.Khoa,
                GioiTinh = input.GioiTinh,
                NgaySinh = input.NgaySinh,
                QueQuan = input.QueQuan,
                TrangThaiO = TrangThaiOKTX.ChuaDangKy
            };
            _db.SinhViens.Add(sv);
            await _db.SaveChangesAsync();

            _db.CongNos.Add(new CongNo { SinhVienId = sv.Id, TongNoHienTai = 0 });
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo hồ sơ sinh viên {sv.MSSV} - tài khoản đăng nhập: {taiKhoan.TenDangNhap}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpGet]
        public async Task<IActionResult> Sua(int id)
        {
            var sv = await _db.SinhViens
                .Include(s => s.TaiKhoan)
                .Include(s => s.CongNo)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sv == null) return NotFound();

            var input = new SuaSinhVienInput
            {
                Id = sv.Id,
                TenDangNhap = sv.TaiKhoan?.TenDangNhap ?? "",
                HoTen = sv.TaiKhoan?.HoTen ?? "",
                Email = sv.TaiKhoan?.Email,
                TrangThaiTaiKhoan = sv.TaiKhoan?.TrangThai ?? TrangThaiTaiKhoan.HoatDong,
                MSSV = sv.MSSV,
                Lop = sv.Lop,
                Khoa = sv.Khoa,
                GioiTinh = sv.GioiTinh,
                NgaySinh = sv.NgaySinh,
                QueQuan = sv.QueQuan,
                TrangThaiO = sv.TrangThaiO
            };
            return View(input);
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sua(SuaSinhVienInput input)
        {
            if (!ModelState.IsValid) return View(input);

            var sv = await _db.SinhViens.Include(s => s.TaiKhoan).FirstOrDefaultAsync(s => s.Id == input.Id);
            if (sv == null) return NotFound();

            // Kiểm tra trùng MSSV nếu thay đổi
            if (await _db.SinhViens.AnyAsync(s => s.MSSV == input.MSSV && s.Id != input.Id))
            {
                ModelState.AddModelError(nameof(input.MSSV), "MSSV đã tồn tại trên hệ thống.");
                return View(input);
            }

            // Kiểm tra trùng Tên đăng nhập nếu thay đổi
            if (sv.TaiKhoan != null && await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == input.TenDangNhap && t.Id != sv.TaiKhoan.Id))
            {
                ModelState.AddModelError(nameof(input.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                return View(input);
            }

            sv.MSSV = input.MSSV;
            sv.Lop = input.Lop;
            sv.Khoa = input.Khoa;
            sv.GioiTinh = input.GioiTinh;
            sv.NgaySinh = input.NgaySinh;
            sv.QueQuan = input.QueQuan;
            sv.TrangThaiO = input.TrangThaiO;

            if (sv.TaiKhoan != null)
            {
                sv.TaiKhoan.TenDangNhap = input.TenDangNhap;
                sv.TaiKhoan.HoTen = input.HoTen;
                sv.TaiKhoan.Email = input.Email;
                sv.TaiKhoan.TrangThai = input.TrangThaiTaiKhoan;

                if (!string.IsNullOrWhiteSpace(input.MatKhauMoi))
                {
                    sv.TaiKhoan.MatKhauHash = PasswordHasher.Hash(input.MatKhauMoi);
                }
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật hồ sơ sinh viên {sv.MSSV} ({input.HoTen}).";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var sv = await _db.SinhViens
                .Include(s => s.TaiKhoan)
                .Include(s => s.CongNo)
                .Include(s => s.DanhSachDangKyO)
                .Include(s => s.DanhSachPhanAnh)
                .Include(s => s.DanhSachPhieuPhi).ThenInclude(p => p.DanhSachThanhToan)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sv == null) return NotFound();

            var mssv = sv.MSSV;
            var hoTen = sv.TaiKhoan?.HoTen ?? mssv;

            // 1. Giải phóng giường nếu sinh viên đang ở
            var giuongs = await _db.Giuongs.Include(g => g.Phong).Where(g => g.SinhVienId == id).ToListAsync();
            foreach (var g in giuongs)
            {
                g.SinhVienId = null;
                g.TrangThai = TrangThaiGiuong.Trong;
                if (g.Phong != null && g.Phong.TrangThai != TrangThaiPhong.BaoTri)
                {
                    var conAi = await _db.Giuongs.AnyAsync(otherG => otherG.PhongId == g.Phong.Id && otherG.Id != g.Id && (otherG.TrangThai == TrangThaiGiuong.DaCoNguoi || otherG.SinhVienId != null));
                    g.Phong.TrangThai = conAi ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong;
                }
            }

            // 2. Dọn dẹp thanh toán và phiếu phí liên quan
            foreach (var p in sv.DanhSachPhieuPhi)
            {
                _db.ThanhToans.RemoveRange(p.DanhSachThanhToan);
            }
            _db.PhieuPhis.RemoveRange(sv.DanhSachPhieuPhi);

            // 3. Dọn dẹp công nợ, đăng ký ở, phản ánh
            if (sv.CongNo != null) _db.CongNos.Remove(sv.CongNo);
            _db.DangKyOs.RemoveRange(sv.DanhSachDangKyO);
            _db.PhanAnhSuCos.RemoveRange(sv.DanhSachPhanAnh);

            // 4. Xóa sinh viên
            _db.SinhViens.Remove(sv);

            // 5. Xóa tài khoản đăng nhập tương ứng
            if (sv.TaiKhoan != null)
            {
                _db.TaiKhoans.Remove(sv.TaiKhoan);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa hồ sơ sinh viên {mssv} ({hoTen}) và tài khoản đăng nhập thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Sinh viên xem hồ sơ của chính mình
        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> HoSoCuaToi()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens
                .Include(s => s.TaiKhoan)
                .Include(s => s.CongNo)
                .Include(s => s.DanhSachDangKyO).ThenInclude(d => d.Phong)
                .FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();
            return View("ChiTiet", sv);
        }
    }

    // Input model đơn giản để tạo tài khoản + hồ sơ sinh viên cùng lúc
    public class TaoSinhVienInput
    {
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhau { get; set; } = "123456";
        public string HoTen { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string MSSV { get; set; } = string.Empty;
        public string? Lop { get; set; }
        public string? Khoa { get; set; }
        public GioiTinh GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? QueQuan { get; set; }
    }

    // Input model để cập nhật tài khoản + hồ sơ sinh viên
    public class SuaSinhVienInput
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string? MatKhauMoi { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? Email { get; set; }
        public TrangThaiTaiKhoan TrangThaiTaiKhoan { get; set; } = TrangThaiTaiKhoan.HoatDong;
        public string MSSV { get; set; } = string.Empty;
        public string? Lop { get; set; }
        public string? Khoa { get; set; }
        public GioiTinh GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? QueQuan { get; set; }
        public TrangThaiOKTX TrangThaiO { get; set; }
    }
}
