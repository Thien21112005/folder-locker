using System;
using System.IO;
using System.Text.Json;

namespace FolderLocker.Services
{
    /// <summary>
    /// Cài đặt người dùng, lưu ở %LOCALAPPDATA%\FolderLocker\settings.json.
    /// Không bao giờ lưu mật khẩu, mã khôi phục hay danh sách file đã mã hóa.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>Thư mục xuất lần trước khi giải mã (để mở sẵn hộp thoại).</summary>
        public string? LastOutputFolder { get; set; }

        /// <summary>Xóa bản gốc bằng cách ghi đè rồi xóa vĩnh viễn, thay vì chuyển vào Thùng rác.</summary>
        public bool SecureDelete { get; set; }

        /// <summary>Thư mục dữ liệu của app; test có thể đổi để không đụng dữ liệu thật.</summary>
        internal static string DataFolder { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FolderLocker");

        private static string FilePath => Path.Combine(DataFolder, "settings.json");

        private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // File cài đặt hỏng → dùng mặc định
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Không lưu được cài đặt không ảnh hưởng chức năng chính
            }
        }
    }
}
