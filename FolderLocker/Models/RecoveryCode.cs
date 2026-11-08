using System.Security.Cryptography;
using System.Text;

namespace FolderLocker.Models
{
    /// <summary>Sinh và chuẩn hóa mã khôi phục dạng FL-XXXX-XXXX-XXXX.</summary>
    public static class RecoveryCode
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>Sinh mã bằng CSPRNG (≈ 62 bit entropy).</summary>
        public static string Generate()
        {
            static string Segment() => RandomNumberGenerator.GetString(Alphabet, 4);
            return $"FL-{Segment()}-{Segment()}-{Segment()}";
        }

        /// <summary>true nếu chuỗi (đã chuẩn hóa) có dạng FL-XXXX-XXXX-XXXX.</summary>
        public static bool IsWellFormed(string code)
        {
            if (code.Length != 17 || !code.StartsWith("FL-")) return false;
            for (int i = 3; i < 17; i++)
            {
                bool dash = i == 7 || i == 12;
                if (dash ? code[i] != '-' : Alphabet.IndexOf(code[i]) < 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Chuẩn hóa mã người dùng nhập: bỏ khoảng trắng, chữ hoa, tự thêm "FL-" và dấu gạch nếu thiếu.
        /// " fl-abcd efgh-ijkl " → "FL-ABCD-EFGH-IJKL". Trả về chuỗi đã trim/upper nếu không khớp định dạng.
        /// </summary>
        public static string Normalize(string input)
        {
            var sb = new StringBuilder();
            foreach (char c in input.ToUpperInvariant())
                if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);

            string raw = sb.ToString();
            if (raw.StartsWith("FL") && raw.Length == 14) raw = raw[2..];
            if (raw.Length == 12)
                return $"FL-{raw[..4]}-{raw[4..8]}-{raw[8..]}";

            return input.Trim().ToUpperInvariant();
        }
    }
}
