using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        public static OperationResult DecryptV2(string encPath, string outputFolder, string secret, bool checkOverwrite, IProgress<int>? progress)
        {
            using var fs = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            bool isFolder;
            byte[] passEnc, recEnc, iv;
            try
            {
                if (ReadByteOrThrow(fs) != VersionV2) return OperationResult.Fail(BadFormat);
                isFolder = ReadByteOrThrow(fs) == 1;
                passEnc = new byte[ReadByteOrThrow(fs)];
                fs.ReadExactly(passEnc);
                recEnc = new byte[ReadByteOrThrow(fs)];
                fs.ReadExactly(recEnc);
                iv = new byte[16];
                fs.ReadExactly(iv);
            }
            catch (EndOfStreamException)
            {
                return OperationResult.Fail(BadFormat);
            }

            // Mật khẩu dùng nguyên văn; mã khôi phục được chuẩn hóa (khoảng trắng, chữ thường...)
            byte[]? fileKey = TryUnwrapKey(passEnc, KeyDerivation.LegacySha256(secret))
                           ?? TryUnwrapKey(recEnc, KeyDerivation.LegacySha256(RecoveryCode.Normalize(secret)));
            if (fileKey == null) return OperationResult.Fail("Sai mật khẩu hoặc mã khôi phục!");

            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.IV = iv;
            CryptographicOperations.ZeroMemory(fileKey);

            using var cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read);

            if (isFolder)
            {
                // V2 không lưu tên folder → lấy theo tên file .enc
                string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(encPath));
                if (checkOverwrite && Directory.Exists(outputPath))
                    return OperationResult.OverwriteNeeded(outputPath);

                PayloadIO.ExtractZipStream(cs, fs, outputPath, progress);
                return new OperationResult(true, "Giải mã thành công!", outputPath);
            }
            else
            {
                string? originalName = PayloadIO.ReadName(cs);
                if (originalName == null) return OperationResult.Fail("Tên file trong dữ liệu không hợp lệ!");

                string outputPath = Path.Combine(outputFolder, originalName);
                if (checkOverwrite && File.Exists(outputPath))
                    return OperationResult.OverwriteNeeded(outputPath);

                PayloadIO.WriteFileAtomically(outputPath, dest => PayloadIO.CopyWithProgress(cs, dest, fs, progress));
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

        public static OperationResult DecryptV1(string encPath, string outputFolder, string password, bool checkOverwrite)
        {
            const string WrongPassword = "Sai mật khẩu! (File định dạng cũ không hỗ trợ mã khôi phục)";

            byte[] data;
            bool isFolder;
            try
            {
                byte[] raw = File.ReadAllBytes(encPath);
                if (raw.Length < 1 + 32) return OperationResult.Fail(BadFormat);
                isFolder = raw[0] == 1;

                using var aes = Aes.Create();
                aes.Key = KeyDerivation.LegacySha256(password);
                data = aes.DecryptCbc(raw.AsSpan(17), raw.AsSpan(1, 16));
            }
            catch (CryptographicException)
            {
                return OperationResult.Fail(WrongPassword);
            }

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
