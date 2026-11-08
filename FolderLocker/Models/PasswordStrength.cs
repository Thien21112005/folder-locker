using System;
using System.Collections.Generic;
using System.Linq;

namespace FolderLocker.Models
{
    public enum StrengthLevel
    {
        Empty,
        TooShort,
        Weak,
        Fair,
        Strong,
        VeryStrong,
    }

    /// <summary>Ước lượng độ mạnh mật khẩu (heuristic, offline). Chỉ để gợi ý, không phải bảo đảm.</summary>
    public static class PasswordStrength
    {
        public const int MinLength = 8;

        private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "password1", "password123", "passw0rd", "12345678", "123456789", "1234567890",
            "qwerty123", "qwertyuiop", "11111111", "00000000", "88888888", "iloveyou", "admin123",
            "abc12345", "abcd1234", "letmein1", "welcome1", "football", "baseball", "sunshine",
            "matkhau", "matkhau123", "anhyeuem", "emyeuanh", "123123123", "zxcvbnm1", "asdfghjk",
            "dragon12", "monkey12", "princess", "superman", "trustno1", "1q2w3e4r", "q1w2e3r4",
        };

        public static StrengthLevel Evaluate(string? password)
        {
            if (string.IsNullOrEmpty(password)) return StrengthLevel.Empty;
            if (password.Length < MinLength) return StrengthLevel.TooShort;
            if (Common.Contains(password) || IsRepetitiveOrSequential(password)) return StrengthLevel.Weak;

            int classes = 0;
            if (password.Any(char.IsLower)) classes++;
            if (password.Any(char.IsUpper)) classes++;
            if (password.Any(char.IsDigit)) classes++;
            if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;

            int distinct = password.Distinct().Count();
            int score = password.Length switch
            {
                >= 20 => 3,
                >= 16 => 2,
                >= 12 => 1,
                _ => 0,
            };
            score += classes - 1;
            if (distinct < password.Length / 2) score--;

            return score switch
            {
                <= 0 => StrengthLevel.Weak,
                1 or 2 => StrengthLevel.Fair,
                3 or 4 => StrengthLevel.Strong,
                _ => StrengthLevel.VeryStrong,
            };
        }

        public static string Describe(StrengthLevel level) => level switch
        {
            StrengthLevel.TooShort => $"Quá ngắn (tối thiểu {MinLength} ký tự)",
            StrengthLevel.Weak => "Yếu",
            StrengthLevel.Fair => "Trung bình",
            StrengthLevel.Strong => "Mạnh",
            StrengthLevel.VeryStrong => "Rất mạnh",
            _ => "",
        };

        /// <summary>Phần trăm để vẽ thanh độ mạnh.</summary>
        public static int Percent(StrengthLevel level) => level switch
        {
            StrengthLevel.TooShort => 10,
            StrengthLevel.Weak => 25,
            StrengthLevel.Fair => 50,
            StrengthLevel.Strong => 75,
            StrengthLevel.VeryStrong => 100,
            _ => 0,
        };

        private static bool IsRepetitiveOrSequential(string s)
        {
            if (s.Distinct().Count() <= 2) return true;
            bool ascending = true, descending = true;
            for (int i = 1; i < s.Length; i++)
            {
                if (s[i] != s[i - 1] + 1) ascending = false;
                if (s[i] != s[i - 1] - 1) descending = false;
            }
            return ascending || descending;
        }
    }
}
