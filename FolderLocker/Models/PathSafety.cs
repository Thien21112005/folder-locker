using System;
using System.IO;

namespace FolderLocker.Models
{
    /// <summary>
    /// Tiện ích an toàn đường dẫn: làm sạch tên file đọc từ dữ liệu giải mã,
    /// ghi ra file/thư mục tạm rồi mới "commit" vào vị trí cuối.
    /// </summary>
    internal static class PathSafety
    {
        public const int MaxNameBytes = 1024;

        /// <summary>
        /// Trả về tên file an toàn (chỉ phần tên, không có thư mục) hoặc null nếu không hợp lệ.
        /// Chặn path traversal kiểu "..\..\evil.exe" hoặc đường dẫn tuyệt đối.
        /// </summary>
        public static string? SafeFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string candidate = Path.GetFileName(name.Replace('/', Path.DirectorySeparatorChar)).Trim();
            if (candidate.Length == 0 || candidate == "." || candidate == "..") return null;
            if (candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
            return candidate;
        }

        /// <summary>Đường dẫn tạm cùng thư mục với <paramref name="finalPath"/> (cùng ổ đĩa → Move nhanh).</summary>
        public static string TempSibling(string finalPath)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(finalPath)) ?? ".";
            string name = Path.GetFileName(finalPath);
            return Path.Combine(dir, $".{name}.{Guid.NewGuid():N}.partial");
        }

        public static void CommitFile(string tempPath, string finalPath)
        {
            File.Move(tempPath, finalPath, overwrite: true);
        }

        /// <summary>
        /// Thay thế <paramref name="finalDir"/> bằng <paramref name="tempDir"/>.
        /// Folder cũ (nếu có) được đổi tên tạm, chỉ bị xóa khi thư mục mới đã vào chỗ.
        /// </summary>
        public static void CommitDirectory(string tempDir, string finalDir)
        {
            string? backup = null;
            if (Directory.Exists(finalDir))
            {
                backup = TempSibling(finalDir);
                Directory.Move(finalDir, backup);
            }

            try
            {
                Directory.Move(tempDir, finalDir);
            }
            catch
            {
                if (backup != null) Directory.Move(backup, finalDir);
                throw;
            }

            if (backup != null) TryDeleteDirectory(backup);
        }

        public static void TryDeleteFile(string? path)
        {
            if (path == null) return;
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
        }

        public static void TryDeleteDirectory(string? path)
        {
            if (path == null) return;
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { /* best effort */ }
        }

        /// <summary>true nếu <paramref name="path"/> nằm bên trong (hoặc trùng) <paramref name="folder"/>.</summary>
        public static bool IsInside(string path, string folder)
        {
            string full = Path.GetFullPath(path);
            string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                || string.Equals(full, root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }
    }
}
