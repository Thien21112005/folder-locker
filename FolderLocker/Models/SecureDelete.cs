using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace FolderLocker.Models
{
    /// <summary>
    /// Xóa "an toàn": ghi đè nội dung bằng dữ liệu ngẫu nhiên, đổi tên ngẫu nhiên rồi mới xóa.
    /// LƯU Ý: trên SSD/USB (wear-leveling, TRIM) và ổ có snapshot/backup, dữ liệu cũ vẫn có thể còn
    /// ở vùng nhớ khác. Đây là biện pháp giảm rủi ro, không phải bảo đảm tuyệt đối.
    /// </summary>
    internal static class SecureDelete
    {
        private const int BlockSize = 1024 * 1024;

        public static void Run(string path, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            if (File.Exists(path))
            {
                ShredFile(path, ct, null);
                progress?.Report(100);
                return;
            }

            if (!Directory.Exists(path)) return;

            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                ct.ThrowIfCancellationRequested();
                ShredFile(files[i], ct, null);
                progress?.Report((i + 1) * 100 / files.Length);
            }
            Directory.Delete(path, recursive: true);
            progress?.Report(100);
        }

        private static void ShredFile(string file, CancellationToken ct, IProgress<int>? progress)
        {
            var attrs = File.GetAttributes(file);
            if ((attrs & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                long length = fs.Length;
                byte[] block = new byte[BlockSize];
                long written = 0;
                while (written < length)
                {
                    ct.ThrowIfCancellationRequested();
                    int n = (int)Math.Min(block.Length, length - written);
                    RandomNumberGenerator.Fill(block.AsSpan(0, n));
                    fs.Write(block, 0, n);
                    written += n;
                    if (length > 0) progress?.Report((int)(written * 100 / length));
                }
                fs.Flush(flushToDisk: true);
                fs.SetLength(0);
            }

            // Đổi tên để tên gốc không còn trong bảng thư mục
            string dir = Path.GetDirectoryName(file)!;
            string renamed = Path.Combine(dir, Guid.NewGuid().ToString("N"));
            File.Move(file, renamed);
            File.Delete(renamed);
        }
    }
}
