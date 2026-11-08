using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace FolderLocker.Models
{
    /// <summary>Đọc/ghi phần payload (đã giải mã) và ghi kết quả ra đĩa một cách an toàn.</summary>
    internal static class PayloadIO
    {
        public const int BufferSize = 1024 * 1024;

        /// <summary>Sao chép có báo tiến độ (theo vị trí của <paramref name="progressSource"/>) và hỗ trợ hủy.</summary>
        public static void CopyWithProgress(Stream from, Stream to, Stream progressSource, IProgress<int>? progress, CancellationToken ct = default)
        {
            long total = progressSource.CanSeek ? progressSource.Length : 0;
            Copy(from, to, ct, _ =>
            {
                if (total > 0)
                    progress?.Report((int)Math.Min(100, progressSource.Position * 100 / total));
            });
        }

        private static void Copy(Stream from, Stream to, CancellationToken ct, Action<int> onChunk)
        {
            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            while ((bytesRead = from.Read(buffer, 0, buffer.Length)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                to.Write(buffer, 0, bytesRead);
                onChunk(bytesRead);
            }
        }

        // ---------- Tên gốc (độ dài int32 LE + UTF-8) ----------

        public static void WriteName(Stream s, string name)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            Span<byte> len = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(len, nameBytes.Length);
            s.Write(len);
            s.Write(nameBytes);
        }

        /// <summary>Đọc tên gốc và làm sạch; null nếu không hợp lệ (độ dài lạ, path traversal...).</summary>
        public static string? ReadName(Stream s)
        {
            Span<byte> len = stackalloc byte[4];
            s.ReadExactly(len);
            int nameLen = BinaryPrimitives.ReadInt32LittleEndian(len);
            if (nameLen <= 0 || nameLen > PathSafety.MaxNameBytes) return null;

            byte[] nameBytes = new byte[nameLen];
            s.ReadExactly(nameBytes);
            return PathSafety.SafeFileName(Encoding.UTF8.GetString(nameBytes));
        }

        // ---------- Folder ⇄ zip ----------

        /// <summary>Nén cả cây thư mục vào stream, giữ thư mục rỗng và thời gian sửa; tiến độ tính theo byte.</summary>
        public static void WriteFolderZip(Stream destination, string sourceFolder, IProgress<int>? progress, CancellationToken ct = default)
        {
            using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);

            var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            var emptyDirs = Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories)
                .Where(d => !Directory.EnumerateFileSystemEntries(d).Any());

            foreach (var dir in emptyDirs)
                zip.CreateEntry(ToEntryName(sourceFolder, dir) + "/");

            long total = Math.Max(1, files.Sum(f => new FileInfo(f).Length));
            long done = 0;
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var entry = zip.CreateEntry(ToEntryName(sourceFolder, file), CompressionLevel.Optimal);
                entry.LastWriteTime = File.GetLastWriteTime(file);

                using var src = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var es = entry.Open();
                Copy(src, es, ct, n =>
                {
                    done += n;
                    progress?.Report((int)Math.Min(100, done * 100 / total));
                });
            }
            progress?.Report(100);
        }

        private static string ToEntryName(string root, string path) =>
            Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

        /// <summary>
        /// Giải mã zip ra file tạm (ẩn), giải nén vào thư mục tạm rồi mới thay thế thư mục đích.
        /// File tạm chứa dữ liệu đã giải mã luôn được xóa, kể cả khi lỗi hoặc bị hủy.
        /// </summary>
        public static void ExtractZipStream(Stream plain, Stream progressSource, string outputPath, IProgress<int>? progress, CancellationToken ct = default)
        {
            string tempZip = PathSafety.TempSibling(outputPath + ".zip");
            string tempDir = PathSafety.TempSibling(outputPath);
            try
            {
                using (var fsTemp = new FileStream(tempZip, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    File.SetAttributes(tempZip, FileAttributes.Hidden | FileAttributes.Temporary);
                    CopyWithProgress(plain, fsTemp, progressSource, progress, ct);
                    fsTemp.Position = 0;

                    using var archive = new ZipArchive(fsTemp, ZipArchiveMode.Read);
                    Directory.CreateDirectory(tempDir);
                    ct.ThrowIfCancellationRequested();
                    // ExtractToDirectory tự chặn entry trỏ ra ngoài thư mục đích (zip slip)
                    archive.ExtractToDirectory(tempDir, true);
                }

                ct.ThrowIfCancellationRequested();
                PathSafety.CommitDirectory(tempDir, outputPath);
            }
            catch
            {
                PathSafety.TryDeleteDirectory(tempDir);
                throw;
            }
            finally
            {
                PathSafety.TryDeleteFile(tempZip);
            }
        }

        /// <summary>Ghi file qua file tạm cùng thư mục; chỉ thay thế file đích khi ghi xong không lỗi.</summary>
        public static void WriteFileAtomically(string outputPath, Action<Stream> write)
        {
            string temp = PathSafety.TempSibling(outputPath);
            try
            {
                using (var fsDest = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    write(fsDest);
                PathSafety.CommitFile(temp, outputPath);
            }
            catch
            {
                PathSafety.TryDeleteFile(temp);
                throw;
            }
        }
    }
}
