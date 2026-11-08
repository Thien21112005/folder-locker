using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using FolderLocker.Models.Crypto;

namespace FolderLocker.Models.Formats
{
    /// <summary>
    /// Định dạng V3 (xác thực, có salt):
    /// <code>
    /// [Header bất biến – 14 byte, dùng làm AAD]
    ///   magic "FLCK" (4) | version = 3 (1) | flags (1, bit0 = folder) | chunkSize (int32 LE) | noncePrefix (4)
    /// [Key slots – có thể thay khi đổi mật khẩu]
    ///   slotCount (1)
    ///   mỗi slot: type (1: 1 = mật khẩu, 2 = mã khôi phục) | iterations (int32 LE) | salt (16)
    ///             | nonce (12) | fileKey đã bọc AES-GCM (32) | tag (16)
    /// [Dữ liệu] các chunk AES-256-GCM (xem AesGcmChunk); plaintext = tên gốc + nội dung file / zip folder
    /// </code>
    /// </summary>
    internal static class FormatV3
    {
        public static ReadOnlySpan<byte> Magic => "FLCK"u8;
        public const byte Version = 3;
        public const int DefaultChunkSize = 1024 * 1024;

        private const int HeaderSize = 4 + 1 + 1 + 4 + AesGcmChunk.NoncePrefixSize;
        private const int MinChunkSize = 4 * 1024;
        private const int MaxChunkSize = 64 * 1024 * 1024;
        private const int MaxSlots = 8;
        private const byte FlagFolder = 0x01;

        private const byte SlotPassword = 1;
        private const byte SlotRecovery = 2;

        /// <summary>Có thể giảm trong unit test cho nhanh; file vẫn lưu số vòng nên đọc được.</summary>
        internal static int Iterations { get; set; } = KeyDerivation.DefaultIterations;

        private sealed record KeySlot(byte Type, int Iterations, byte[] Salt, byte[] Nonce, byte[] WrappedKey, byte[] Tag);

        public static bool HasMagic(ReadOnlySpan<byte> firstBytes) =>
            firstBytes.Length >= Magic.Length && firstBytes[..Magic.Length].SequenceEqual(Magic);

        // ============================================
        // MÃ HÓA
        // ============================================

        public static string Encrypt(string sourcePath, string destPath, string password, IProgress<int>? progress)
        {
            bool isFolder = Directory.Exists(sourcePath);
            string recoveryCode = RecoveryCode.Generate();
            byte[] fileKey = RandomNumberGenerator.GetBytes(KeyDerivation.KeySize);
            byte[] noncePrefix = RandomNumberGenerator.GetBytes(AesGcmChunk.NoncePrefixSize);
            byte[] header = BuildHeader(isFolder, DefaultChunkSize, noncePrefix);

            string tempDest = PathSafety.TempSibling(destPath);
            try
            {
                using (var fs = new FileStream(tempDest, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    fs.Write(header);
                    fs.WriteByte(2);
                    WriteSlot(fs, CreateSlot(SlotPassword, password, fileKey, header));
                    WriteSlot(fs, CreateSlot(SlotRecovery, recoveryCode, fileKey, header));

                    using var enc = new AesGcmChunkWriter(fs, fileKey, header, noncePrefix, DefaultChunkSize);
                    PayloadIO.WriteName(enc, OriginalName(sourcePath, destPath, isFolder));

                    if (isFolder)
                    {
                        PayloadIO.WriteFolderZip(enc, sourcePath, progress);
                    }
                    else
                    {
                        using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        PayloadIO.CopyWithProgress(src, enc, src, progress);
                    }
                }

                PathSafety.CommitFile(tempDest, destPath);
            }
            catch
            {
                PathSafety.TryDeleteFile(tempDest);
                throw;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(fileKey);
            }
            return recoveryCode;
        }

        private static string OriginalName(string sourcePath, string destPath, bool isFolder)
        {
            string name = isFolder
                ? Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath)))
                : Path.GetFileName(sourcePath);
            // Thư mục gốc ổ đĩa (C:\) không có tên → dùng tên file .enc
            return string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(destPath) : name;
        }

        private static byte[] BuildHeader(bool isFolder, int chunkSize, byte[] noncePrefix)
        {
            var header = new byte[HeaderSize];
            Magic.CopyTo(header);
            header[4] = Version;
            header[5] = isFolder ? FlagFolder : (byte)0;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(6), chunkSize);
            noncePrefix.CopyTo(header, 10);
            return header;
        }

        private static KeySlot CreateSlot(byte type, string secret, byte[] fileKey, byte[] header)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(KeyDerivation.SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(AesGcmChunk.NonceSize);
            byte[] wrapped = new byte[fileKey.Length];
            byte[] tag = new byte[AesGcmChunk.TagSize];

            byte[] kek = KeyDerivation.Pbkdf2(secret, salt, Iterations);
            try
            {
                using var gcm = new AesGcm(kek, AesGcmChunk.TagSize);
                gcm.Encrypt(nonce, fileKey, wrapped, tag, SlotAad(header, type));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(kek);
            }
            return new KeySlot(type, Iterations, salt, nonce, wrapped, tag);
        }

        private static byte[] SlotAad(byte[] header, byte type)
        {
            var aad = new byte[header.Length + 1];
            header.CopyTo(aad, 0);
            aad[^1] = type;
            return aad;
        }

        private static void WriteSlot(Stream s, KeySlot slot)
        {
            Span<byte> iter = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(iter, slot.Iterations);
            s.WriteByte(slot.Type);
            s.Write(iter);
            s.Write(slot.Salt);
            s.Write(slot.Nonce);
            s.Write(slot.WrappedKey);
            s.Write(slot.Tag);
        }

        // ============================================
        // GIẢI MÃ
        // ============================================

        public static OperationResult Decrypt(string encPath, string outputFolder, string secret, bool checkOverwrite, IProgress<int>? progress)
        {
            const string BadFormat = "File bị lỗi hoặc không đúng định dạng!";

            using var fs = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] header = new byte[HeaderSize];
            List<KeySlot> slots;
            try
            {
                fs.ReadExactly(header);
                if (!HasMagic(header)) return OperationResult.Fail(BadFormat);
                if (header[4] > Version) return OperationResult.Fail("File được tạo bởi phiên bản FolderLocker mới hơn. Hãy cập nhật ứng dụng.");
                if (header[4] != Version) return OperationResult.Fail(BadFormat);

                slots = ReadSlots(fs);
            }
            catch (EndOfStreamException)
            {
                return OperationResult.Fail(BadFormat);
            }

            bool isFolder = (header[5] & FlagFolder) != 0;
            int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(6));
            byte[] noncePrefix = header.AsSpan(10, AesGcmChunk.NoncePrefixSize).ToArray();
            if (chunkSize < MinChunkSize || chunkSize > MaxChunkSize || slots.Count == 0)
                return OperationResult.Fail(BadFormat);

            byte[]? fileKey = Unlock(slots, header, secret);
            if (fileKey == null) return OperationResult.Fail("Sai mật khẩu hoặc mã khôi phục!");

            AesGcmChunkReader dec;
            try
            {
                dec = new AesGcmChunkReader(fs, fileKey, header, noncePrefix, chunkSize);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(fileKey);
            }

            using (dec)
            {
                string? name = PayloadIO.ReadName(dec);
                if (name == null) return OperationResult.Fail("Tên file trong dữ liệu không hợp lệ!");

                string outputPath = Path.Combine(outputFolder, name);
                if (checkOverwrite && (File.Exists(outputPath) || Directory.Exists(outputPath)))
                    return OperationResult.OverwriteNeeded(outputPath);

                if (isFolder)
                    PayloadIO.ExtractZipStream(dec, fs, outputPath, progress);
                else
                    PayloadIO.WriteFileAtomically(outputPath, dest => PayloadIO.CopyWithProgress(dec, dest, fs, progress));

                return new OperationResult(true, "Giải mã thành công!", outputPath);
            }
        }

        private static List<KeySlot> ReadSlots(Stream s)
        {
            int count = s.ReadByte();
            if (count < 0) throw new EndOfStreamException();

            var slots = new List<KeySlot>();
            Span<byte> iter = stackalloc byte[4];
            for (int i = 0; i < Math.Min(count, MaxSlots); i++)
            {
                int type = s.ReadByte();
                if (type < 0) throw new EndOfStreamException();
                s.ReadExactly(iter);
                var salt = new byte[KeyDerivation.SaltSize];
                var nonce = new byte[AesGcmChunk.NonceSize];
                var wrapped = new byte[KeyDerivation.KeySize];
                var tag = new byte[AesGcmChunk.TagSize];
                s.ReadExactly(salt);
                s.ReadExactly(nonce);
                s.ReadExactly(wrapped);
                s.ReadExactly(tag);
                slots.Add(new KeySlot((byte)type, BinaryPrimitives.ReadInt32LittleEndian(iter), salt, nonce, wrapped, tag));
            }
            if (count > MaxSlots) throw new EndOfStreamException();
            return slots;
        }

        /// <summary>
        /// Thử mở khóa: mật khẩu dùng nguyên văn, mã khôi phục được chuẩn hóa.
        /// Nếu chuỗi nhập trông giống mã khôi phục thì thử slot khôi phục trước (đỡ một lần PBKDF2).
        /// </summary>
        private static byte[]? Unlock(List<KeySlot> slots, byte[] header, string secret)
        {
            string normalized = RecoveryCode.Normalize(secret);
            bool looksLikeCode = RecoveryCode.IsWellFormed(normalized);

            var attempts = slots.Select(slot => (slot, secret: slot.Type == SlotRecovery ? normalized : secret))
                                .Where(a => a.slot.Type is SlotPassword or SlotRecovery)
                                .OrderBy(a => looksLikeCode == (a.slot.Type == SlotRecovery) ? 0 : 1);

            foreach (var (slot, candidate) in attempts)
            {
                if (slot.Iterations < 1 || slot.Iterations > KeyDerivation.MaxIterations) continue;

                byte[] kek = KeyDerivation.Pbkdf2(candidate, slot.Salt, slot.Iterations);
                byte[] fileKey = new byte[KeyDerivation.KeySize];
                try
                {
                    using var gcm = new AesGcm(kek, AesGcmChunk.TagSize);
                    gcm.Decrypt(slot.Nonce, slot.WrappedKey, slot.Tag, fileKey, SlotAad(header, slot.Type));
                    return fileKey;
                }
                catch (AuthenticationTagMismatchException)
                {
                    // Sai bí mật cho slot này → thử slot kế tiếp
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(kek);
                }
            }
            return null;
        }
    }
}
