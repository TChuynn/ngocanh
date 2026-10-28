using System.Security.Claims;
using KTXManagement.Demo.Data;
using KTXManagement.Demo.Models;
using KTXManagement.Demo.Models.ViewModels;
using KTXManagement.Demo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KTXManagement.Demo.Controllers
{
    // UC001: Đăng nhập và phân quyền
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == model.TenDangNhap);

            if (taiKhoan == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Luồng UC001 (bản revised):
            // Bước 1: kiểm tra trạng thái tài khoản TRƯỚC khi đối chiếu mật khẩu (A1)
            if (taiKhoan.TrangThai == TrangThaiTaiKhoan.BiKhoa)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa do đăng nhập sai quá số lần cho phép. Vui lòng liên hệ Quản lý KTX để được mở khóa.");
                return View(model);
            }

            // Bước 2: đối chiếu mật khẩu (A2: sai thì tăng biến đếm)
            if (!PasswordHasher.Verify(model.MatKhau, taiKhoan.MatKhauHash))
            {
                taiKhoan.SoLanDangNhapSai++;

                // Bước 3 (A3 - hệ quả của A2): vượt quá số lần sai cho phép -> khóa tài khoản
                if (taiKhoan.SoLanDangNhapSai >= TaiKhoan.SoLanSaiToiDa)
                {
                    taiKhoan.TrangThai = TrangThaiTaiKhoan.BiKhoa;
                    await _db.SaveChangesAsync();
                    ModelState.AddModelError(string.Empty, $"Bạn đã nhập sai mật khẩu {TaiKhoan.SoLanSaiToiDa} lần. Tài khoản đã bị khóa.");
                    return View(model);
                }

                await _db.SaveChangesAsync();
                var conLai = TaiKhoan.SoLanSaiToiDa - taiKhoan.SoLanDangNhapSai;
                ModelState.AddModelError(string.Empty, $"Tên đăng nhập hoặc mật khẩu không đúng. Bạn còn {conLai} lần thử trước khi tài khoản bị khóa.");
                return View(model);
            }

            // Đăng nhập thành công: reset số lần sai, tạo cookie xác thực + claim phân quyền
            taiKhoan.SoLanDangNhapSai = 0;
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, taiKhoan.Id.ToString()),
                new(ClaimTypes.Name, taiKhoan.TenDangNhap),
                new(ClaimTypes.Role, taiKhoan.VaiTro.ToString()),
                new("HoTen", taiKhoan.HoTen)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}
