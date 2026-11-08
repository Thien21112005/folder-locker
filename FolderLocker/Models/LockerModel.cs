using System;
using System.IO;
using System.Threading;
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
        private const string NotOurs = "Đây không phải file do FolderLocker mã hóa.";

        public string Encrypt(string sourcePath, string destPath, string password, IProgress<int>? progress, CancellationToken ct = default) =>
            FormatV3.Encrypt(sourcePath, destPath, password, progress, ct);

        public OperationResult Decrypt(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite, IProgress<int>? progress, CancellationToken ct = default) =>
            DetectFormat(encPath) switch
            {
                EncryptedFormat.V3 => FormatV3.Decrypt(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress, ct),
                EncryptedFormat.V2 => LegacyFormats.DecryptV2(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress, ct),
                EncryptedFormat.V1 => LegacyFormats.DecryptV1(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite),
                _ => OperationResult.Fail(NotOurs),
            };

        /// <summary>Kiểm tra mật khẩu / mã khôi phục mà không ghi gì ra đĩa.</summary>
        public OperationResult CheckSecret(string encPath, string passwordOrRecoveryCode) =>
            DetectFormat(encPath) switch
            {
                EncryptedFormat.V3 => FormatV3.CheckSecret(encPath, passwordOrRecoveryCode),
                EncryptedFormat.V2 => LegacyFormats.CheckSecretV2(encPath, passwordOrRecoveryCode),
                EncryptedFormat.V1 => LegacyFormats.CheckSecretV1(encPath, passwordOrRecoveryCode),
                _ => OperationResult.Fail(NotOurs),
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

        /// <summary>Đọc lại và xác thực toàn bộ file .enc (chỉ hỗ trợ V3, định dạng mà app đang tạo).</summary>
        public OperationResult Verify(string encPath, string password, IProgress<int>? progress, CancellationToken ct = default) =>
            DetectFormat(encPath) == EncryptedFormat.V3
                ? FormatV3.Verify(encPath, password, progress, ct)
                : OperationResult.Fail("Chỉ kiểm tra được file định dạng mới (V3).");

        /// <summary>Ghi đè ngẫu nhiên rồi xóa vĩnh viễn (không qua Thùng rác).</summary>
        public void ShredPermanently(string path, IProgress<int>? progress, CancellationToken ct = default) =>
            SecureDelete.Run(path, progress, ct);

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
