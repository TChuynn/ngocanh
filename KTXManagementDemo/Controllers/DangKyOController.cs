using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Models.ViewModels;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC004 (bản revised): Đăng ký ở, xếp phòng, chuyển phòng, trả phòng - có AI gợi ý phòng
    [Authorize]
    public class DangKyOController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAIService _aiService;

        public DangKyOController(ApplicationDbContext db, IAIService aiService)
        {
            _db = db;
            _aiService = aiService;
        }

        // ============ SINH VIÊN: gửi yêu cầu đăng ký ở ============

        [Authorize(Roles = "SinhVien")]
        public async Task<IActionResult> CuaToi()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            var list = await _db.DangKyOs.Include(d => d.Phong).Include(d => d.Giuong)
                .Where(d => d.SinhVienId == sv.Id)
                .OrderByDescending(d => d.NgayDangKy)
                .ToListAsync();
            return View(list);
        }

        [Authorize(Roles = "SinhVien")]
        [HttpGet]
        public async Task<IActionResult> GuiYeuCau()
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            var dangCoYeuCauMo = await _db.DangKyOs.AnyAsync(d => d.SinhVienId == sv.Id &&
                (d.TrangThai == TrangThaiDangKyO.ChoDuyet || d.TrangThai == TrangThaiDangKyO.DangO || d.TrangThai == TrangThaiDangKyO.DaDuyet));
            if (dangCoYeuCauMo)
            {
                TempData["Loi"] = "Bạn đang có một yêu cầu đăng ký ở / đang ở KTX, không thể gửi thêm yêu cầu mới.";
                return RedirectToAction(nameof(CuaToi));
            }

            return View();
        }

        [Authorize(Roles = "SinhVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuiYeuCau(string? ghiChu)
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            if (sv == null) return NotFound();

            var yeuCau = new DangKyO
            {
                SinhVienId = sv.Id,
                TrangThai = TrangThaiDangKyO.ChoDuyet,
                GhiChu = ghiChu,
                NgayDangKy = DateTime.Now
            };
            _db.DangKyOs.Add(yeuCau);

            sv.TrangThaiO = TrangThaiOKTX.DaDangKy;
            await _db.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi yêu cầu đăng ký ở KTX. Vui lòng chờ Quản lý KTX xử lý.";
            return RedirectToAction(nameof(CuaToi));
        }

        [Authorize(Roles = "SinhVien")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeuCauTraPhong(int id)
        {
            var taiKhoanId = User.GetTaiKhoanId();
            var sv = await _db.SinhViens.FirstOrDefaultAsync(s => s.TaiKhoanId == taiKhoanId);
            var dk = await _db.DangKyOs.FirstOrDefaultAsync(d => d.Id == id && d.SinhVienId == sv!.Id);
            if (dk == null || dk.TrangThai != TrangThaiDangKyO.DangO) return NotFound();

            dk.TrangThai = TrangThaiDangKyO.DangChoTraPhong;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã gửi yêu cầu trả phòng, chờ Quản lý KTX xác nhận (hệ thống sẽ kiểm tra công nợ trước khi duyệt).";
            return RedirectToAction(nameof(CuaToi));
        }

        // ============ QUẢN LÝ KTX: xử lý yêu cầu ============

        [Authorize(Roles = "QuanLyKTX")]
        public async Task<IActionResult> Index()
        {
            var list = await _db.DangKyOs
                .Include(d => d.SinhVien).ThenInclude(sv => sv!.TaiKhoan)
                .Include(d => d.Phong)
                .Include(d => d.Giuong)
                .OrderByDescending(d => d.NgayDangKy)
                .ToListAsync();
            return View(list);
        }

        // Bước 1: QL mở màn hình xếp phòng -> hệ thống gọi AI gợi ý phòng phù hợp
        [Authorize(Roles = "QuanLyKTX")]
        [HttpGet]
        public async Task<IActionResult> XepPhong(int id)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).FirstOrDefaultAsync(d => d.Id == id);
            if (yeuCau == null) return NotFound();
            if (yeuCau.TrangThai != TrangThaiDangKyO.ChoDuyet)
            {
                TempData["Loi"] = "Yêu cầu này không ở trạng thái chờ duyệt.";
                return RedirectToAction(nameof(Index));
            }

            var goiY = await _aiService.GoiYPhongAsync(yeuCau);
            yeuCau.GoiYAI = goiY;
            await _db.SaveChangesAsync();

            var phongTrong = await _db.Phongs.Include(p => p.DanhSachGiuong)
                .Where(p => p.TrangThai != TrangThaiPhong.BaoTri)
                .ToListAsync();
            phongTrong = phongTrong.Where(p => p.SoGiuongTrong > 0).ToList();

            var vm = new XepPhongViewModel { YeuCau = yeuCau, GoiYAI = goiY, DanhSachPhongTrong = phongTrong };
            return View(vm);
        }

        // Bước 2: QL xác nhận xếp giường cụ thể cho sinh viên
        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanXepPhong(int yeuCauId, int giuongId)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).FirstOrDefaultAsync(d => d.Id == yeuCauId);
            var giuong = await _db.Giuongs.Include(g => g.Phong).FirstOrDefaultAsync(g => g.Id == giuongId);
            if (yeuCau == null || giuong == null) return NotFound();

            if (giuong.TrangThai != TrangThaiGiuong.Trong)
            {
                TempData["Loi"] = "Giường này vừa được xếp cho sinh viên khác, vui lòng chọn giường khác.";
                return RedirectToAction(nameof(XepPhong), new { id = yeuCauId });
            }

            giuong.TrangThai = TrangThaiGiuong.DaCoNguoi;
            giuong.SinhVienId = yeuCau.SinhVienId;

            yeuCau.PhongId = giuong.PhongId;
            yeuCau.GiuongId = giuong.Id;
            yeuCau.TrangThai = TrangThaiDangKyO.DangO;
            yeuCau.NgayNhanPhong = DateTime.Now;

            if (yeuCau.SinhVien != null) yeuCau.SinhVien.TrangThaiO = TrangThaiOKTX.DangO;

            // Cập nhật trạng thái phòng nếu đã hết giường trống
            var phong = await _db.Phongs.Include(p => p.DanhSachGiuong).FirstAsync(p => p.Id == giuong.PhongId);
            phong.TrangThai = phong.DanhSachGiuong.All(g => g.TrangThai == TrangThaiGiuong.DaCoNguoi)
                ? TrangThaiPhong.Day
                : TrangThaiPhong.DangSuDung;

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = $"Đã xếp sinh viên {yeuCau.SinhVien?.MSSV} vào phòng {giuong.Phong?.SoPhong}, giường {giuong.SoGiuong}.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoi(int id, string? lyDo)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).FirstOrDefaultAsync(d => d.Id == id);
            if (yeuCau == null) return NotFound();
            yeuCau.TrangThai = TrangThaiDangKyO.TuChoi;
            yeuCau.GhiChu = (yeuCau.GhiChu ?? "") + $" | Từ chối: {lyDo}";
            if (yeuCau.SinhVien != null) yeuCau.SinhVien.TrangThaiO = TrangThaiOKTX.ChuaDangKy;
            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã từ chối yêu cầu.";
            return RedirectToAction(nameof(Index));
        }

        // A4: Chuyển phòng - có tính lại phí nếu chênh lệch đơn giá phòng cũ/mới
        [Authorize(Roles = "QuanLyKTX")]
        [HttpGet]
        public async Task<IActionResult> ChuyenPhong(int id)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).Include(d => d.Phong).FirstOrDefaultAsync(d => d.Id == id);
            if (yeuCau == null || yeuCau.TrangThai != TrangThaiDangKyO.DangO) return NotFound();

            var phongKhac = await _db.Phongs.Include(p => p.DanhSachGiuong)
                .Where(p => p.Id != yeuCau.PhongId && p.TrangThai != TrangThaiPhong.BaoTri)
                .ToListAsync();
            phongKhac = phongKhac.Where(p => p.SoGiuongTrong > 0).ToList();

            ViewBag.YeuCau = yeuCau;
            return View(phongKhac);
        }

        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanChuyenPhong(int yeuCauId, int giuongMoiId)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).Include(d => d.Giuong).Include(d => d.Phong)
                .FirstOrDefaultAsync(d => d.Id == yeuCauId);
            var giuongMoi = await _db.Giuongs.Include(g => g.Phong).FirstOrDefaultAsync(g => g.Id == giuongMoiId);
            if (yeuCau == null || giuongMoi == null) return NotFound();

            if (giuongMoi.TrangThai != TrangThaiGiuong.Trong)
            {
                TempData["Loi"] = "Giường này vừa có người khác chọn, vui lòng thử giường khác.";
                return RedirectToAction(nameof(ChuyenPhong), new { id = yeuCauId });
            }

            var phongCu = yeuCau.Phong;
            var giuongCu = yeuCau.Giuong;

            // Trả giường cũ
            if (giuongCu != null)
            {
                giuongCu.TrangThai = TrangThaiGiuong.Trong;
                giuongCu.SinhVienId = null;
            }
            if (phongCu != null)
            {
                var conAiOPhongCu = await _db.Giuongs.AnyAsync(g => g.PhongId == phongCu.Id && g.TrangThai == TrangThaiGiuong.DaCoNguoi);
                phongCu.TrangThai = conAiOPhongCu ? TrangThaiPhong.DangSuDung : TrangThaiPhong.Trong;
            }

            // Nhận giường mới
            giuongMoi.TrangThai = TrangThaiGiuong.DaCoNguoi;
            giuongMoi.SinhVienId = yeuCau.SinhVienId;

            decimal giaCu = phongCu?.DonGiaThang ?? 0;
            decimal giaMoi = giuongMoi.Phong?.DonGiaThang ?? 0;
            decimal chenhLech = giaMoi - giaCu;

            yeuCau.PhongId = giuongMoi.PhongId;
            yeuCau.GiuongId = giuongMoi.Id;
            await _db.SaveChangesAsync();

            // Nếu có chênh lệch phí, tạo phiếu phí bổ sung/hoàn trả cho tháng hiện tại (UC005)
            if (chenhLech != 0)
            {
                var phieu = new PhieuPhi
                {
                    SinhVienId = yeuCau.SinhVienId,
                    ThangNam = DateTime.Now.ToString("MM/yyyy"),
                    SoTienPhaiDong = Math.Abs(chenhLech),
                    HanThanhToan = DateTime.Now.AddDays(10),
                    TrangThai = TrangThaiPhieuPhi.ChuaThanhToan
                };
                _db.PhieuPhis.Add(phieu);
                await _db.SaveChangesAsync();

                await CapNhatCongNoAsync(yeuCau.SinhVienId);
            }

            TempData["ThongBao"] = chenhLech == 0
                ? $"Đã chuyển sinh viên sang phòng {giuongMoi.Phong?.SoPhong}, giường {giuongMoi.SoGiuong}."
                : $"Đã chuyển phòng. Phát sinh phiếu phí chênh lệch {Math.Abs(chenhLech):N0}đ cho tháng {DateTime.Now:MM/yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        // A5: Trả phòng - kiểm tra công nợ trước khi xác nhận (liên kết UC005)
        [Authorize(Roles = "QuanLyKTX")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanTraPhong(int id)
        {
            var yeuCau = await _db.DangKyOs.Include(d => d.SinhVien).Include(d => d.Giuong).Include(d => d.Phong)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (yeuCau == null) return NotFound();

            var congNo = await _db.CongNos.FirstOrDefaultAsync(c => c.SinhVienId == yeuCau.SinhVienId);
            if (congNo != null && congNo.TongNoHienTai > 0)
            {
                TempData["Loi"] = $"Sinh viên còn nợ {congNo.TongNoHienTai:N0}đ tiền phòng. Cần thanh toán hết công nợ trước khi được trả phòng.";
                return RedirectToAction(nameof(Index));
            }

            if (yeuCau.Giuong != null)
            {
                yeuCau.Giuong.TrangThai = TrangThaiGiuong.Trong;
                yeuCau.Giuong.SinhVienId = null;
            }
            if (yeuCau.Phong != null)
            {
                yeuCau.Phong.TrangThai = TrangThaiPhong.DangSuDung;
                var conAi = await _db.Giuongs.AnyAsync(g => g.PhongId == yeuCau.Phong.Id && g.TrangThai == TrangThaiGiuong.DaCoNguoi);
                if (!conAi) yeuCau.Phong.TrangThai = TrangThaiPhong.Trong;
            }

            yeuCau.TrangThai = TrangThaiDangKyO.DaTraPhong;
            yeuCau.NgayTraPhong = DateTime.Now;
            if (yeuCau.SinhVien != null) yeuCau.SinhVien.TrangThaiO = TrangThaiOKTX.DaTraPhong;

            await _db.SaveChangesAsync();
            TempData["ThongBao"] = "Đã xác nhận trả phòng thành công.";
            return RedirectToAction(nameof(Index));
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
