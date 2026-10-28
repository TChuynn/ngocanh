using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC002: Quản lý hạ tầng ký túc xá - chỉ Quản lý KTX được thao tác
    [Authorize(Roles = "QuanLyKTX")]
    public class HaTangController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HaTangController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var toaNhas = await _db.ToaNhas
                .Include(t => t.DanhSachTang)
                    .ThenInclude(t => t.DanhSachPhong)
                        .ThenInclude(p => p.DanhSachGiuong)
                            .ThenInclude(g => g.SinhVien)
                                .ThenInclude(sv => sv!.TaiKhoan)
                .ToListAsync();
            return View(toaNhas);
        }

        // ================= TÒA NHÀ =================

        [HttpGet]
        public IActionResult TaoToaNha() => View(new ToaNha());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoToaNha(ToaNha model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.ToaNhas.Add(model);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã tạo tòa nhà \"{model.TenToaNha}\".";
            return RedirectToAction(nameof(Index));
        }

        // ================= TẦNG =================

        [HttpGet]
        public async Task<IActionResult> TaoTang(int toaNhaId)
        {
            var toaNha = await _db.ToaNhas.FindAsync(toaNhaId);
            if (toaNha == null) return NotFound();
            ViewBag.ToaNha = toaNha;
            return View(new Tang { ToaNhaId = toaNhaId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoTang(Tang model)
        {
            _db.Tangs.Add(model);
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã thêm tầng {model.SoThuTu}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SuaTang(int id)
        {
            var tang = await _db.Tangs.Include(t => t.ToaNha).FirstOrDefaultAsync(t => t.Id == id);
            if (tang == null) return NotFound();
            ViewBag.DanhSachToaNha = await _db.ToaNhas.ToListAsync();
            return View(tang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaTang(Tang model)
        {
            var tang = await _db.Tangs.FindAsync(model.Id);
            if (tang == null) return NotFound();

            if (await _db.Tangs.AnyAsync(t => t.ToaNhaId == model.ToaNhaId && t.SoThuTu == model.SoThuTu && t.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.SoThuTu), $"Tầng {model.SoThuTu} đã tồn tại trong tòa nhà này.");
                ViewBag.DanhSachToaNha = await _db.ToaNhas.ToListAsync();
                return View(model);
            }

            tang.SoThuTu = model.SoThuTu;
            tang.ToaNhaId = model.ToaNhaId;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật thông tin tầng {tang.SoThuTu}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTang(int id)
        {
            var tang = await _db.Tangs
                .Include(t => t.DanhSachPhong)
                    .ThenInclude(p => p.DanhSachGiuong)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tang == null) return NotFound();

            bool coSvDangO = tang.DanhSachPhong.Any(p => p.DanhSachGiuong.Any(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null));
            if (coSvDangO)
            {
                TempData["Loi"] = $"Không thể xóa tầng {tang.SoThuTu} vì có phòng đang có sinh viên cư trú. Vui lòng chuyển hoặc trả phòng cho sinh viên trước.";
                return RedirectToAction(nameof(Index));
            }

            var allGiuongs = tang.DanhSachPhong.SelectMany(p => p.DanhSachGiuong).ToList();
            _db.Giuongs.RemoveRange(allGiuongs);
            _db.Phongs.RemoveRange(tang.DanhSachPhong);
            _db.Tangs.Remove(tang);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã xóa tầng {tang.SoThuTu} cùng các phòng và giường trống liên quan.";
            return RedirectToAction(nameof(Index));
        }

        // ================= PHÒNG =================

        [HttpGet]
        public async Task<IActionResult> TaoPhong(int tangId)
        {
            var tang = await _db.Tangs.Include(t => t.ToaNha).FirstOrDefaultAsync(t => t.Id == tangId);
            if (tang == null) return NotFound();
            ViewBag.Tang = tang;
            return View(new Phong { TangId = tangId, SucChua = 4, DonGiaThang = 700_000 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoPhong(Phong model, int soGiuongTao)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Phongs.Add(model);
            await _db.SaveChangesAsync();

            // Tự động sinh danh sách giường theo sức chứa nhập vào (tối đa = sức chứa phòng)
            var soLuong = Math.Min(soGiuongTao <= 0 ? model.SucChua : soGiuongTao, model.SucChua);
            for (int i = 1; i <= soLuong; i++)
            {
                _db.Giuongs.Add(new Giuong { PhongId = model.Id, SoGiuong = $"{model.SoPhong}-{i:00}", TrangThai = TrangThaiGiuong.Trong });
            }
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã tạo phòng {model.SoPhong} với {soLuong} giường.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SuaPhong(int id)
        {
            var phong = await _db.Phongs
                .Include(p => p.Tang).ThenInclude(t => t!.ToaNha)
                .Include(p => p.DanhSachGiuong)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (phong == null) return NotFound();

            ViewBag.DanhSachTang = await _db.Tangs
                .Include(t => t.ToaNha)
                .OrderBy(t => t.ToaNha != null ? t.ToaNha.TenToaNha : "").ThenBy(t => t.SoThuTu)
                .ToListAsync();
            return View(phong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaPhong(Phong model, bool tuDongDieuChinhGiuong = true)
        {
            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == model.Id);
            if (phong == null) return NotFound();

            int soSvDangO = phong.DanhSachGiuong.Count(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null);
            if (model.SucChua < soSvDangO)
            {
                ModelState.AddModelError(nameof(model.SucChua), $"Không thể giảm sức chứa xuống {model.SucChua} vì hiện tại phòng đang có {soSvDangO} sinh viên đang ở.");
                ViewBag.DanhSachTang = await _db.Tangs.Include(t => t.ToaNha).OrderBy(t => t.ToaNha!.TenToaNha).ThenBy(t => t.SoThuTu).ToListAsync();
                return View(model);
            }

            phong.SoPhong = model.SoPhong;
            phong.TangId = model.TangId;
            phong.DonGiaThang = model.DonGiaThang;
            phong.SucChua = model.SucChua;

            if (tuDongDieuChinhGiuong)
            {
                // Thêm giường nếu số giường hiện có < sức chứa mới
                while (phong.DanhSachGiuong.Count < model.SucChua)
                {
                    var stt = phong.DanhSachGiuong.Count + 1;
                    var g = new Giuong
                    {
                        PhongId = phong.Id,
                        SoGiuong = $"{model.SoPhong}-{stt:00}",
                        TrangThai = TrangThaiGiuong.Trong
                    };
                    phong.DanhSachGiuong.Add(g);
                    _db.Giuongs.Add(g);
                }
                // Giảm giường trống nếu số giường hiện có > sức chứa mới
                if (phong.DanhSachGiuong.Count > model.SucChua)
                {
                    var giuongTrongs = phong.DanhSachGiuong
                        .Where(g => g.TrangThai == TrangThaiGiuong.Trong && g.SinhVienId == null)
                        .OrderByDescending(g => g.Id)
                        .ToList();
                    int canXoa = phong.DanhSachGiuong.Count - model.SucChua;
                    foreach (var g in giuongTrongs.Take(canXoa))
                    {
                        _db.Giuongs.Remove(g);
                    }
                }
            }

            // Cập nhật trạng thái
            if (model.TrangThai == TrangThaiPhong.BaoTri)
            {
                phong.TrangThai = TrangThaiPhong.BaoTri;
            }
            else
            {
                phong.TrangThai = soSvDangO >= phong.SucChua ? TrangThaiPhong.Day : (soSvDangO > 0 ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật thông tin phòng {phong.SoPhong}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaPhong(int id)
        {
            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == id);
            if (phong == null) return NotFound();

            if (phong.DanhSachGiuong.Any(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null))
            {
                TempData["Loi"] = $"Không thể xóa phòng {phong.SoPhong} vì đang có sinh viên cư trú. Vui lòng chuyển hoặc trả phòng cho sinh viên trước.";
                return RedirectToAction(nameof(Index));
            }

            _db.Giuongs.RemoveRange(phong.DanhSachGiuong);
            _db.Phongs.Remove(phong);
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = $"Đã xóa phòng {phong.SoPhong} và các giường trống thuộc phòng.";
            return RedirectToAction(nameof(Index));
        }

        // Chức năng: Cập nhật số sinh viên trong phòng (sức chứa + tự động đồng bộ giường & trạng thái)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatSoSinhVien(int phongId, int sucChuaMoi, bool dongBoGiuong = true)
        {
            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == phongId);
            if (phong == null) return NotFound();

            int soSvDangO = phong.DanhSachGiuong.Count(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null);
            if (sucChuaMoi < soSvDangO)
            {
                TempData["Loi"] = $"Không thể cập nhật số sinh viên phòng {phong.SoPhong} thành {sucChuaMoi} vì hiện đang có {soSvDangO} sinh viên đang ở.";
                return RedirectToAction(nameof(Index));
            }

            phong.SucChua = sucChuaMoi;

            if (dongBoGiuong)
            {
                // Thêm giường nếu số giường < sức chứa mới
                while (phong.DanhSachGiuong.Count < sucChuaMoi)
                {
                    var stt = phong.DanhSachGiuong.Count + 1;
                    var g = new Giuong
                    {
                        PhongId = phong.Id,
                        SoGiuong = $"{phong.SoPhong}-{stt:00}",
                        TrangThai = TrangThaiGiuong.Trong
                    };
                    phong.DanhSachGiuong.Add(g);
                    _db.Giuongs.Add(g);
                }
                // Giảm bớt giường trống thừa nếu số giường > sức chứa mới
                if (phong.DanhSachGiuong.Count > sucChuaMoi)
                {
                    var giuongTrongs = phong.DanhSachGiuong
                        .Where(g => g.TrangThai == TrangThaiGiuong.Trong && g.SinhVienId == null)
                        .OrderByDescending(g => g.Id)
                        .ToList();
                    int canXoa = phong.DanhSachGiuong.Count - sucChuaMoi;
                    foreach (var g in giuongTrongs.Take(canXoa))
                    {
                        _db.Giuongs.Remove(g);
                    }
                }
            }

            if (phong.TrangThai != TrangThaiPhong.BaoTri)
            {
                phong.TrangThai = soSvDangO >= phong.SucChua ? TrangThaiPhong.Day : (soSvDangO > 0 ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật số sinh viên phòng {phong.SoPhong}: Sức chứa tối đa {sucChuaMoi} SV (hiện có {soSvDangO} SV, tổng số {phong.DanhSachGiuong.Count} giường).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiTrangThaiPhong(int phongId, TrangThaiPhong trangThai)
        {
            var phong = await _db.Phongs.FindAsync(phongId);
            if (phong == null) return NotFound();
            phong.TrangThai = trangThai;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật trạng thái phòng {phong.SoPhong}.";
            return RedirectToAction(nameof(Index));
        }

        // ================= GIƯỜNG =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemGiuong(int phongId, string? soGiuong)
        {
            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == phongId);
            if (phong == null) return NotFound();

            if (string.IsNullOrWhiteSpace(soGiuong))
            {
                soGiuong = $"{phong.SoPhong}-{(phong.DanhSachGiuong.Count + 1):00}";
            }

            if (phong.DanhSachGiuong.Any(g => g.SoGiuong.Equals(soGiuong, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["Loi"] = $"Giường {soGiuong} đã tồn tại trong phòng {phong.SoPhong}.";
                return RedirectToAction(nameof(Index));
            }

            var giuongMoi = new Giuong
            {
                PhongId = phong.Id,
                SoGiuong = soGiuong,
                TrangThai = TrangThaiGiuong.Trong
            };
            _db.Giuongs.Add(giuongMoi);

            // Nếu số giường vượt quá sức chứa, tự động cập nhật sức chứa phòng
            if (phong.DanhSachGiuong.Count + 1 > phong.SucChua)
            {
                phong.SucChua = phong.DanhSachGiuong.Count + 1;
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã thêm giường {soGiuong} vào phòng {phong.SoPhong}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SuaGiuong(int id)
        {
            var giuong = await _db.Giuongs
                .Include(g => g.Phong)
                .Include(g => g.SinhVien).ThenInclude(sv => sv!.TaiKhoan)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (giuong == null) return NotFound();
            return View(giuong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaGiuong(int id, string soGiuong, TrangThaiGiuong trangThai)
        {
            var giuong = await _db.Giuongs
                .Include(g => g.Phong)
                .Include(g => g.SinhVien).ThenInclude(sv => sv!.TaiKhoan)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (giuong == null) return NotFound();

            if (giuong.SinhVienId != null && trangThai == TrangThaiGiuong.Trong)
            {
                TempData["Loi"] = $"Giường {giuong.SoGiuong} đang có sinh viên cư trú (MSSV: {giuong.SinhVien?.MSSV}). Vui lòng trả phòng hoặc chuyển phòng cho sinh viên trước khi đổi trạng thái giường về Trống.";
                return RedirectToAction(nameof(Index));
            }

            giuong.SoGiuong = soGiuong;
            giuong.TrangThai = trangThai;

            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == giuong.PhongId);
            if (phong != null && phong.TrangThai != TrangThaiPhong.BaoTri)
            {
                int soSvDangO = phong.DanhSachGiuong.Count(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null);
                phong.TrangThai = soSvDangO >= phong.SucChua ? TrangThaiPhong.Day : (soSvDangO > 0 ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã cập nhật giường {giuong.SoGiuong}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaGiuong(int id)
        {
            var giuong = await _db.Giuongs
                .Include(g => g.Phong)
                .Include(g => g.SinhVien)
                .FirstOrDefaultAsync(g => g.Id == id);
            if (giuong == null) return NotFound();

            if (giuong.TrangThai == TrangThaiGiuong.DaCoNguoi || giuong.SinhVienId != null)
            {
                TempData["Loi"] = $"Không thể xóa giường {giuong.SoGiuong} vì đang có sinh viên cư trú (MSSV: {giuong.SinhVien?.MSSV}).";
                return RedirectToAction(nameof(Index));
            }

            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstOrDefaultAsync(p => p.Id == giuong.PhongId);
            _db.Giuongs.Remove(giuong);

            if (phong != null && phong.TrangThai != TrangThaiPhong.BaoTri)
            {
                int soSvDangO = phong.DanhSachGiuong.Count(g => g.Id != id && (g.TrangThai == TrangThaiGiuong.DaCoNguoi || g.SinhVienId != null));
                phong.TrangThai = soSvDangO >= phong.SucChua ? TrangThaiPhong.Day : (soSvDangO > 0 ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong);
            }

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xóa giường {giuong.SoGiuong} thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
