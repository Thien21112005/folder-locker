using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FolderLocker.Models
{
    public class LockerModel
    {
        public const byte VERSION_2 = 2;
        public const int BUFFER_SIZE = 4096 * 1024; // 4MB chunks

        // Mã hóa file V2 (Hỗ trợ File Chunking và Recovery Code)
        public string EncryptV2(string sourcePath, string destPath, string password, bool isFolder, IProgress<int> progress)
        {
            // Sinh Recovery Code
            string recoveryCode = GenerateRecoveryCode();
            byte[] fileKey = RandomNumberGenerator.GetBytes(32);

            byte[] passHash = HashPassword(password);
            byte[] recHash = HashPassword(recoveryCode);

            // Mã hóa FileKey bằng Pass và RecCode
            var passEnc = EncryptKey(fileKey, passHash);
            var recEnc = EncryptKey(fileKey, recHash);

            // Ghi ra file tạm, thành công mới đổi tên → lỗi giữa chừng không để lại file .enc hỏng
            string tempDest = PathSafety.TempSibling(destPath);
            try
            {
                using (var fsDest = new FileStream(tempDest, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    // Write Header V2
                    fsDest.WriteByte(VERSION_2);
                    fsDest.WriteByte(isFolder ? (byte)1 : (byte)0);

                    fsDest.WriteByte((byte)passEnc.Length);
                    fsDest.Write(passEnc, 0, passEnc.Length);

                    fsDest.WriteByte((byte)recEnc.Length);
                    fsDest.Write(recEnc, 0, recEnc.Length);

                    using var aes = Aes.Create();
                    aes.Key = fileKey;
                    aes.GenerateIV();
                    fsDest.Write(aes.IV, 0, 16); // FileData IV

                    using var cs = new CryptoStream(fsDest, aes.CreateEncryptor(), CryptoStreamMode.Write);

                    if (isFolder)
                    {
                        using var zip = new ZipArchive(cs, ZipArchiveMode.Create, true);
                        var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                        int totalFiles = files.Length;
                        int processed = 0;

                        foreach (var file in files)
                        {
                            var entryName = Path.GetRelativePath(sourcePath, file);
                            zip.CreateEntryFromFile(file, entryName);
                            processed++;
                            progress?.Report((int)((processed / (float)totalFiles) * 100));
                        }
                    }
                    else
                    {
                        string fileName = Path.GetFileName(sourcePath);
                        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                        byte[] nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

                        cs.Write(nameLengthBytes, 0, 4);
                        cs.Write(nameBytes, 0, nameBytes.Length);

                        using var fsSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        CopyWithProgress(fsSource, cs, fsSource, progress);
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

        // Giải mã file (Hỗ trợ cả V1 cũ và V2 mới)
        public (bool success, string message, string? outPath) DecryptV2(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite, IProgress<int>? progress)
        {
            using var fsSource = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int firstByte = fsSource.ReadByte();
            if (firstByte == -1) return (false, "File rỗng!", null);

            if (firstByte != VERSION_2)
            {
                fsSource.Close();
                return DecryptLegacy(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite);
            }

            bool isFolder;
            byte[] passEnc, recEnc, iv;
            try
            {
                isFolder = ReadByteOrThrow(fsSource) == 1;
                passEnc = new byte[ReadByteOrThrow(fsSource)];
                fsSource.ReadExactly(passEnc);
                recEnc = new byte[ReadByteOrThrow(fsSource)];
                fsSource.ReadExactly(recEnc);
                iv = new byte[16];
                fsSource.ReadExactly(iv);
            }
            catch (EndOfStreamException)
            {
                return (false, "File bị lỗi hoặc không đúng định dạng!", null);
            }

            byte[] providedHash = HashPassword(passwordOrRecoveryCode);
            byte[]? fileKey = TryUnwrapKey(passEnc, providedHash) ?? TryUnwrapKey(recEnc, providedHash);

            if (fileKey == null)
            {
                return (false, "Sai mật khẩu hoặc mã khôi phục!", null);
            }

            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.IV = iv;
            CryptographicOperations.ZeroMemory(fileKey);

            using var cs = new CryptoStream(fsSource, aes.CreateDecryptor(), CryptoStreamMode.Read);

            if (isFolder)
            {
                string outputName = Path.GetFileNameWithoutExtension(encPath);
                string outputPath = Path.Combine(outputFolder, outputName);

                if (checkOverwrite && Directory.Exists(outputPath))
                    return (false, "OVERWRITE_WARNING", outputPath);

                ExtractZipStream(cs, fsSource, outputPath, progress);
                return (true, "Giải mã thành công!", outputPath);
            }
            else
            {
                byte[] lenBuffer = new byte[4];
                cs.ReadExactly(lenBuffer);

                int nameLen = BitConverter.ToInt32(lenBuffer, 0);
                if (nameLen <= 0 || nameLen > PathSafety.MaxNameBytes)
                    return (false, "File bị lỗi hoặc không đúng định dạng!", null);

                byte[] nameBytes = new byte[nameLen];
                cs.ReadExactly(nameBytes);
                string? originalName = PathSafety.SafeFileName(Encoding.UTF8.GetString(nameBytes));
                if (originalName == null)
                    return (false, "Tên file trong dữ liệu không hợp lệ!", null);

                string outputPath = Path.Combine(outputFolder, originalName);

                if (checkOverwrite && File.Exists(outputPath))
                    return (false, "OVERWRITE_WARNING", outputPath);

                WriteFileAtomically(outputPath, dest => CopyWithProgress(cs, dest, fsSource, progress));
                return (true, "Giải mã thành công!", outputPath);
            }
        }

        /// <summary>
        /// Giải mã zip ra file tạm (ẩn), giải nén vào thư mục tạm rồi mới thay thế thư mục đích.
        /// File tạm chứa dữ liệu đã giải mã luôn được xóa, kể cả khi lỗi.
        /// </summary>
        private static void ExtractZipStream(Stream plain, Stream progressSource, string outputPath, IProgress<int>? progress)
        {
            string tempZip = PathSafety.TempSibling(outputPath + ".zip");
            string tempDir = PathSafety.TempSibling(outputPath);
            try
            {
                using (var fsTemp = new FileStream(tempZip, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    File.SetAttributes(tempZip, FileAttributes.Hidden | FileAttributes.Temporary);
                    CopyWithProgress(plain, fsTemp, progressSource, progress);
                    fsTemp.Position = 0;

                    using var archive = new ZipArchive(fsTemp, ZipArchiveMode.Read);
                    Directory.CreateDirectory(tempDir);
                    archive.ExtractToDirectory(tempDir, true);
                }

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
        private static void WriteFileAtomically(string outputPath, Action<Stream> write)
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

        private static void CopyWithProgress(Stream from, Stream to, Stream progressSource, IProgress<int>? progress)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            long total = progressSource.Length;
            int bytesRead;
            while ((bytesRead = from.Read(buffer, 0, buffer.Length)) > 0)
            {
                to.Write(buffer, 0, bytesRead);
                if (total > 0)
                    progress?.Report((int)Math.Min(100, progressSource.Position * 100 / total));
            }
        }

        private static int ReadByteOrThrow(Stream s)
        {
            int b = s.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            return b;
        }

        private byte[]? TryUnwrapKey(byte[] wrapped, byte[] key)
        {
            if (wrapped.Length < 32) return null;
            try
            {
                var temp = DecryptKey(wrapped, key);
                return temp.Length == 32 ? temp : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private byte[] EncryptKey(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, 16);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        private byte[] DecryptKey(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            byte[] iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);
            aes.IV = iv;
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                cs.Write(data, 16, data.Length - 16);
            return ms.ToArray();
        }

        // Dùng bộ sinh số ngẫu nhiên an toàn cho mật mã (CSPRNG), không dùng System.Random
        private static string GenerateRecoveryCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string Segment() => RandomNumberGenerator.GetString(chars, 4);
            return $"FL-{Segment()}-{Segment()}-{Segment()}";
        }

        public void Delete(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            else if (File.Exists(path)) File.Delete(path);
        }

        public static byte[] HashPassword(string password) => SHA256.HashData(Encoding.UTF8.GetBytes(password));

        // ============================================
        // LOGIC LEGACY (V1) ĐỂ TƯƠNG THÍCH NGƯỢC
        // ============================================

        private (bool success, string message, string? outPath) DecryptLegacy(string encPath, string outputFolder, string password, bool checkOverwrite)
        {
            byte[] data;
            bool isFolder;
            try
            {
                byte[] final = File.ReadAllBytes(encPath);
                if (final.Length < 1 + 32) return (false, "File bị lỗi hoặc không đúng định dạng!", null);
                isFolder = final[0] == 1;
                data = DecryptLegacyData(final.AsSpan(1).ToArray(), password);
            }
            catch (CryptographicException)
            {
                return (false, "Sai mật khẩu (V1 không hỗ trợ mã khôi phục)!", null);
            }

            try
            {
                if (isFolder)
                {
                    string outputName = Path.GetFileNameWithoutExtension(encPath);
                    string outputPath = Path.Combine(outputFolder, outputName);

                    if (checkOverwrite && Directory.Exists(outputPath))
                        return (false, "OVERWRITE_WARNING", outputPath);

                    using var ms = new MemoryStream(data);
                    ExtractZipStream(ms, ms, outputPath, null);
                    return (true, "Giải mã thành công (V1)!", outputPath);
                }
                else
                {
                    if (data.Length < 4) return (false, "Sai mật khẩu (V1 không hỗ trợ mã khôi phục)!", null);
                    int nameLength = BitConverter.ToInt32(data, 0);
                    if (nameLength <= 0 || nameLength > PathSafety.MaxNameBytes || 4 + nameLength > data.Length)
                        return (false, "Sai mật khẩu (V1 không hỗ trợ mã khôi phục)!", null);

                    string? originalFileName = PathSafety.SafeFileName(Encoding.UTF8.GetString(data, 4, nameLength));
                    if (originalFileName == null)
                        return (false, "Tên file trong dữ liệu không hợp lệ!", null);

                    string outputPath = Path.Combine(outputFolder, originalFileName);

                    if (checkOverwrite && File.Exists(outputPath))
                        return (false, "OVERWRITE_WARNING", outputPath);

                    WriteFileAtomically(outputPath, dest => dest.Write(data, 4 + nameLength, data.Length - 4 - nameLength));
                    return (true, "Giải mã thành công (V1)!", outputPath);
                }
            }
            catch (InvalidDataException)
            {
                // Padding đúng ngẫu nhiên với mật khẩu sai → dữ liệu rác, zip không đọc được
                return (false, "Sai mật khẩu (V1 không hỗ trợ mã khôi phục)!", null);
            }
        }

        private static byte[] DecryptLegacyData(byte[] data, string password)
        {
            using var aes = Aes.Create();
            aes.Key = HashPassword(password);
            aes.IV = data.AsSpan(0, 16).ToArray();
            return aes.DecryptCbc(data.AsSpan(16), aes.IV);
        }
    }
}
