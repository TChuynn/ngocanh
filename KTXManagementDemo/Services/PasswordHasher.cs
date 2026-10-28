using System.Security.Cryptography;
using System.Text;

namespace KTXManagement.Demo.Services
{
    /// <summary>
    /// Băm mật khẩu SHA256 đơn giản — CHỈ dùng cho mục đích DEMO.
    /// Không dùng cách này cho môi trường thật (nên dùng ASP.NET Core Identity / BCrypt...).
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string plainText)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToHexString(bytes);
        }

        public static bool Verify(string plainText, string hash)
        {
            return Hash(plainText) == hash;
        }
    }
}
