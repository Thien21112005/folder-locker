using System;
using System.IO;
using FolderLocker.Models.Formats;
using Microsoft.VisualBasic.FileIO;

namespace FolderLocker.Models
{
    public enum EncryptedFormat
    {
        /// <summary>Không phải file do FolderLocker tạo.</summary>
        None,
        V1,
        V2,
        V3,
    }

    /// <summary>
    /// Facade cho tầng Model: mã hóa luôn dùng định dạng mới nhất (V3),
    /// giải mã tự nhận diện V1 / V2 / V3.
    /// </summary>
    public class LockerModel
    {
        /// <summary>Mã hóa file/folder thành file .enc; trả về mã khôi phục.</summary>
        public string Encrypt(string sourcePath, string destPath, string password, IProgress<int>? progress) =>
            FormatV3.Encrypt(sourcePath, destPath, password, progress);

        public OperationResult Decrypt(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite, IProgress<int>? progress) =>
            DetectFormat(encPath) switch
            {
                EncryptedFormat.V3 => FormatV3.Decrypt(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress),
                EncryptedFormat.V2 => LegacyFormats.DecryptV2(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress),
                EncryptedFormat.V1 => LegacyFormats.DecryptV1(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite),
                _ => OperationResult.Fail("Đây không phải file do FolderLocker mã hóa."),
            };

        /// <summary>
        /// Nhận diện định dạng qua header. File cũ (V1/V2) không có magic nên chỉ được nhận khi có đuôi .enc.
        /// </summary>
        public static EncryptedFormat DetectFormat(string path)
        {
            try
            {
                if (!File.Exists(path)) return EncryptedFormat.None;

                Span<byte> head = stackalloc byte[4];
                int read;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    read = fs.ReadAtLeast(head, head.Length, throwOnEndOfStream: false);

                if (read == 0) return EncryptedFormat.None;
                if (FormatV3.HasMagic(head[..read])) return EncryptedFormat.V3;

                if (!path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)) return EncryptedFormat.None;
                return head[0] switch
                {
                    LegacyFormats.VersionV2 => EncryptedFormat.V2,
                    0 or 1 => EncryptedFormat.V1,
                    _ => EncryptedFormat.None,
                };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return EncryptedFormat.None;
            }
        }

        /// <summary>Chuyển file/folder vào Thùng rác (có thể khôi phục), không xóa vĩnh viễn.</summary>
        public void MoveToRecycleBin(string path)
        {
            if (Directory.Exists(path))
                FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            else if (File.Exists(path))
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }
    }
}
