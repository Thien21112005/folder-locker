using System.Security.Cryptography;
using System.Text;

namespace FolderLocker.Models.Crypto
{
    /// <summary>Dẫn xuất khóa từ mật khẩu / mã khôi phục.</summary>
    internal static class KeyDerivation
    {
        /// <summary>Theo khuyến nghị OWASP 2023 cho PBKDF2-HMAC-SHA256.</summary>
        public const int DefaultIterations = 600_000;
        public const int MaxIterations = 10_000_000;
        public const int SaltSize = 16;
        public const int KeySize = 32;

        public static byte[] Pbkdf2(string secret, byte[] salt, int iterations) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(secret), salt, iterations, HashAlgorithmName.SHA256, KeySize);

        /// <summary>
        /// Chỉ dùng để đọc file V1/V2 cũ: SHA-256 một vòng, không salt. KHÔNG dùng cho dữ liệu mới.
        /// </summary>
        public static byte[] LegacySha256(string secret) => SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }
}
