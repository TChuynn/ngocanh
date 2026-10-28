using System.Security.Claims;

namespace KTXManagement.Demo.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetTaiKhoanId(this ClaimsPrincipal user)
        {
            var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idStr, out var id) ? id : 0;
        }

        public static string GetHoTen(this ClaimsPrincipal user) => user.FindFirstValue("HoTen") ?? user.Identity?.Name ?? string.Empty;
    }
}
