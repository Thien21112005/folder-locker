using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FolderLocker.Models;

namespace FolderLocker.Services
{
    /// <summary>
    /// Nhật ký hoạt động để hỗ trợ báo lỗi, lưu ở %LOCALAPPDATA%\FolderLocker\logs\yyyy-MM-dd.log.
    /// QUYỀN RIÊNG TƯ: chỉ ghi loại thao tác, kết quả và loại lỗi. Không bao giờ ghi
    /// mật khẩu, mã khôi phục, đường dẫn hay tên file.
    /// </summary>
    public static class AppLog
    {
        public const int KeepDays = 30;
        private static readonly object Gate = new();

        public static string Folder => Path.Combine(AppSettings.DataFolder, "logs");

        public static void Info(string operation, string message) => Write("INFO", operation, message);

        public static void Error(string operation, Exception ex) =>
            Write("ERROR", operation, ex.GetType().FullName ?? ex.GetType().Name);

        /// <summary>Ghi kết quả một thao tác (thông điệp của OperationResult là chuỗi cố định, không chứa đường dẫn).</summary>
        public static void Result(string operation, OperationResult result)
        {
            string outcome = result.Success ? "OK"
                : result.Cancelled ? "CANCELLED"
                : result.NeedsOverwriteConfirm ? "NEEDS_OVERWRITE"
                : "FAILED";
            Write(result.Success || result.Cancelled ? "INFO" : "WARN", operation,
                result.Success ? outcome : $"{outcome}: {StripPaths(result.Message)}");
        }

        /// <summary>Phòng hờ: cắt mọi thứ sau dấu ':' cuối nếu trông như có đường dẫn.</summary>
        private static string StripPaths(string message) =>
            message.Contains('\\') || message.Contains('/') ? message[..Math.Max(0, message.IndexOf(':'))] + ": <đã ẩn>" : message;

        private static void Write(string level, string operation, string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Folder);
                    string file = Path.Combine(Folder, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{level}\t{operation}\t{message.ReplaceLineEndings(" ")}{Environment.NewLine}";
                    File.AppendAllText(file, line);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Không ghi được log không được làm hỏng thao tác chính
            }
        }

        /// <summary>Xóa log cũ hơn <see cref="KeepDays"/> ngày.</summary>
        public static void Prune()
        {
            try
            {
                if (!Directory.Exists(Folder)) return;
                var cutoff = DateTime.Now.Date.AddDays(-KeepDays);
                foreach (var file in Directory.GetFiles(Folder, "*.log"))
                {
                    if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) && day < cutoff)
                        File.Delete(file);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
