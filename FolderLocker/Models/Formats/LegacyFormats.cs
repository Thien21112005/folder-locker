using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FolderLocker.Models.Crypto;

namespace FolderLocker.Models.Formats
{
    /// <summary>
    /// Đọc file định dạng cũ để tương thích ngược. Không còn dùng để tạo file mới.
    /// <list type="bullet">
    /// <item>V1: [isFolder (1)] [IV (16)] [AES-CBC(SHA256(pass))(payload)]: toàn bộ trong RAM, không có mã khôi phục.</item>
    /// <item>V2: [2] [isFolder] [len|IV+CBC(fileKey) theo pass] [len|IV+CBC(fileKey) theo mã khôi phục] [IV] [AES-CBC(fileKey)(payload)].</item>
    /// </list>
    /// Cả hai đều không có xác thực (MAC) và dùng SHA-256 một vòng làm khóa.
    /// </summary>
    internal static class LegacyFormats
    {
        public const byte VersionV2 = 2;
        private const string BadFormat = "File bị lỗi hoặc không đúng định dạng!";

        // ============================================
        // V2
        // ============================================

        private sealed record V2Header(bool IsFolder, byte[] PassEnc, byte[] RecEnc, byte[] Iv);

        private static V2Header? ReadV2Header(Stream fs)
        {
            try
            {
                if (ReadByteOrThrow(fs) != VersionV2) return null;
                bool isFolder = ReadByteOrThrow(fs) == 1;
                var passEnc = new byte[ReadByteOrThrow(fs)];
                fs.ReadExactly(passEnc);
                var recEnc = new byte[ReadByteOrThrow(fs)];
                fs.ReadExactly(recEnc);
                var iv = new byte[16];
                fs.ReadExactly(iv);
                return new V2Header(isFolder, passEnc, recEnc, iv);
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        /// <summary>Mật khẩu dùng nguyên văn; mã khôi phục được chuẩn hóa (khoảng trắng, chữ thường...).</summary>
        private static byte[]? UnlockV2(V2Header h, string secret) =>
            TryUnwrapKey(h.PassEnc, KeyDerivation.LegacySha256(secret))
            ?? TryUnwrapKey(h.RecEnc, KeyDerivation.LegacySha256(RecoveryCode.Normalize(secret)));

        public static OperationResult CheckSecretV2(string encPath, string secret)
        {
            using var fs = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var h = ReadV2Header(fs);
            if (h == null) return OperationResult.Fail(BadFormat);
            byte[]? key = UnlockV2(h, secret);
            if (key == null) return OperationResult.Fail("Sai mật khẩu hoặc mã khôi phục!");
            CryptographicOperations.ZeroMemory(key);
            return new OperationResult(true, "OK");
        }

        public static OperationResult DecryptV2(string encPath, string outputFolder, string secret, bool checkOverwrite, IProgress<int>? progress, CancellationToken ct = default)
        {
            using var fs = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var h = ReadV2Header(fs);
            if (h == null) return OperationResult.Fail(BadFormat);
            bool isFolder = h.IsFolder;

            byte[]? fileKey = UnlockV2(h, secret);
            if (fileKey == null) return OperationResult.Fail("Sai mật khẩu hoặc mã khôi phục!");

            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.IV = h.Iv;
            CryptographicOperations.ZeroMemory(fileKey);

            using var cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read);

            if (isFolder)
            {
                // V2 không lưu tên folder → lấy theo tên file .enc
                string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(encPath));
                if (checkOverwrite && Directory.Exists(outputPath))
                    return OperationResult.OverwriteNeeded(outputPath);

                PayloadIO.ExtractZipStream(cs, fs, outputPath, progress, ct);
                return new OperationResult(true, "Giải mã thành công!", outputPath);
            }
            else
            {
                string? originalName = PayloadIO.ReadName(cs);
                if (originalName == null) return OperationResult.Fail("Tên file trong dữ liệu không hợp lệ!");

                string outputPath = Path.Combine(outputFolder, originalName);
                if (checkOverwrite && File.Exists(outputPath))
                    return OperationResult.OverwriteNeeded(outputPath);

                PayloadIO.WriteFileAtomically(outputPath, dest => PayloadIO.CopyWithProgress(cs, dest, fs, progress, ct));
                return new OperationResult(true, "Giải mã thành công!", outputPath);
            }
        }

        /// <summary>Chỉ dùng trong unit test để tạo file V2 kiểm tra tương thích ngược.</summary>
        internal static string EncryptV2ForTests(string sourcePath, string destPath, string password)
        {
            bool isFolder = Directory.Exists(sourcePath);
            string recoveryCode = RecoveryCode.Generate();
            byte[] fileKey = RandomNumberGenerator.GetBytes(32);
            byte[] passEnc = WrapKey(fileKey, KeyDerivation.LegacySha256(password));
            byte[] recEnc = WrapKey(fileKey, KeyDerivation.LegacySha256(recoveryCode));

            using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            fs.WriteByte(VersionV2);
            fs.WriteByte(isFolder ? (byte)1 : (byte)0);
            fs.WriteByte((byte)passEnc.Length);
            fs.Write(passEnc);
            fs.WriteByte((byte)recEnc.Length);
            fs.Write(recEnc);

            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.GenerateIV();
            fs.Write(aes.IV);

            using var cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write);
            if (isFolder)
            {
                PayloadIO.WriteFolderZip(cs, sourcePath, null);
            }
            else
            {
                PayloadIO.WriteName(cs, Path.GetFileName(sourcePath));
                using var src = File.OpenRead(sourcePath);
                src.CopyTo(cs);
            }
            return recoveryCode;
        }

        private static byte[] WrapKey(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            byte[] cipher = aes.EncryptCbc(data, aes.IV);
            byte[] result = new byte[16 + cipher.Length];
            aes.IV.CopyTo(result, 0);
            cipher.CopyTo(result, 16);
            return result;
        }

        private static byte[]? TryUnwrapKey(byte[] wrapped, byte[] key)
        {
            if (wrapped.Length < 32) return null;
            try
            {
                using var aes = Aes.Create();
                aes.Key = key;
                byte[] plain = aes.DecryptCbc(wrapped.AsSpan(16), wrapped.AsSpan(0, 16));
                return plain.Length == 32 ? plain : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static int ReadByteOrThrow(Stream s)
        {
            int b = s.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            return b;
        }

        // ============================================
        // V1
        // ============================================

        private const string WrongPasswordV1 = "Sai mật khẩu! (File định dạng cũ không hỗ trợ mã khôi phục)";

        /// <summary>Giải mã V1 trong RAM; null nếu sai mật khẩu.</summary>
        private static (byte[] data, bool isFolder)? DecryptV1Data(string encPath, string password, out string? error)
        {
            error = null;
            byte[] raw = File.ReadAllBytes(encPath);
            if (raw.Length < 1 + 32) { error = BadFormat; return null; }
            try
            {
                using var aes = Aes.Create();
                aes.Key = KeyDerivation.LegacySha256(password);
                return (aes.DecryptCbc(raw.AsSpan(17), raw.AsSpan(1, 16)), raw[0] == 1);
            }
            catch (CryptographicException)
            {
                error = WrongPasswordV1;
                return null;
            }
        }

        /// <summary>V1 không có key slot: phải giải mã thử trong RAM và kiểm tra cấu trúc payload.</summary>
        public static OperationResult CheckSecretV1(string encPath, string password)
        {
            var result = DecryptV1Data(encPath, password, out string? error);
            if (result == null) return OperationResult.Fail(error!);
            try
            {
                using var ms = new MemoryStream(result.Value.data);
                if (result.Value.isFolder)
                {
                    using var _ = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
                }
                else if (PayloadIO.ReadName(ms) == null)
                {
                    return OperationResult.Fail(WrongPasswordV1);
                }
                return new OperationResult(true, "OK");
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
            {
                return OperationResult.Fail(WrongPasswordV1);
            }
        }

        public static OperationResult DecryptV1(string encPath, string outputFolder, string password, bool checkOverwrite)
        {
            const string WrongPassword = WrongPasswordV1;

            var decrypted = DecryptV1Data(encPath, password, out string? error);
            if (decrypted == null) return OperationResult.Fail(error!);
            var (data, isFolder) = decrypted.Value;

            try
            {
                using var ms = new MemoryStream(data);
                if (isFolder)
                {
                    string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(encPath));
                    if (checkOverwrite && Directory.Exists(outputPath))
                        return OperationResult.OverwriteNeeded(outputPath);

                    PayloadIO.ExtractZipStream(ms, ms, outputPath, null);
                    return new OperationResult(true, "Giải mã thành công!", outputPath);
                }
                else
                {
                    // Padding có thể "đúng" ngẫu nhiên với mật khẩu sai → tên rác sẽ bị chặn ở đây
                    string? originalName = PayloadIO.ReadName(ms);
                    if (originalName == null) return OperationResult.Fail(WrongPassword);

                    string outputPath = Path.Combine(outputFolder, originalName);
                    if (checkOverwrite && File.Exists(outputPath))
                        return OperationResult.OverwriteNeeded(outputPath);

                    PayloadIO.WriteFileAtomically(outputPath, dest => ms.CopyTo(dest));
                    return new OperationResult(true, "Giải mã thành công!", outputPath);
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
            {
                return OperationResult.Fail(WrongPassword);
            }
        }
    }
}
