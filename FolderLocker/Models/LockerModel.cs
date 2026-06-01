using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

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
            byte[] fileKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(fileKey);

            byte[] passHash = HashPassword(password);
            byte[] recHash = HashPassword(recoveryCode);

            // Mã hóa FileKey bằng Pass và RecCode
            var passEnc = EncryptKey(fileKey, passHash);
            var recEnc = EncryptKey(fileKey, recHash);

            using var fsDest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            // Write Header V2
            fsDest.WriteByte(VERSION_2);
            fsDest.WriteByte(isFolder ? (byte)1 : (byte)0);
            
            fsDest.WriteByte((byte)passEnc.Length);
            fsDest.Write(passEnc, 0, passEnc.Length);
            
            fsDest.WriteByte((byte)recEnc.Length);
            fsDest.Write(recEnc, 0, recEnc.Length);

            // Setup FileKey encryption for the actual file data
            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.GenerateIV();
            
            fsDest.Write(aes.IV, 0, 16); // FileData IV

            using var cs = new CryptoStream(fsDest, aes.CreateEncryptor(), CryptoStreamMode.Write);

            if (isFolder)
            {
                using (var zip = new ZipArchive(cs, ZipArchiveMode.Create, true))
                {
                    var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    int totalFiles = files.Length;
                    int processed = 0;
                    
                    foreach(var file in files)
                    {
                        var entryName = Path.GetRelativePath(sourcePath, file);
                        zip.CreateEntryFromFile(file, entryName);
                        processed++;
                        progress?.Report((int)((processed / (float)totalFiles) * 100));
                    }
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
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                long totalBytes = fsSource.Length;
                long totalRead = 0;

                while ((bytesRead = fsSource.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cs.Write(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    if (totalBytes > 0)
                    {
                        progress?.Report((int)((totalRead / (float)totalBytes) * 100));
                    }
                }
            }
            return recoveryCode;
        }

        // Giải mã file (Hỗ trợ cả V1 cũ và V2 mới)
        public (bool success, string message, string outPath) DecryptV2(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite, IProgress<int> progress)
        {
            using var fsSource = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int firstByte = fsSource.ReadByte();
            if (firstByte == -1) return (false, "File rỗng!", null);

            if (firstByte != VERSION_2)
            {
                fsSource.Close();
                return DecryptLegacy(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite);
            }

            bool isFolder = fsSource.ReadByte() == 1;

            int passEncLen = fsSource.ReadByte();
            byte[] passEnc = new byte[passEncLen];
            fsSource.Read(passEnc, 0, passEncLen);

            int recEncLen = fsSource.ReadByte();
            byte[] recEnc = new byte[recEncLen];
            fsSource.Read(recEnc, 0, recEncLen);

            byte[] providedHash = HashPassword(passwordOrRecoveryCode);
            
            byte[] fileKey = null;
            try { fileKey = DecryptKey(passEnc, providedHash); } catch { }
            if (fileKey == null)
            {
                try { fileKey = DecryptKey(recEnc, providedHash); } catch { }
            }

            if (fileKey == null)
            {
                return (false, "Sai mật khẩu hoặc mã khôi phục!", null);
            }

            byte[] iv = new byte[16];
            fsSource.Read(iv, 0, 16);

            using var aes = Aes.Create();
            aes.Key = fileKey;
            aes.IV = iv;

            using var cs = new CryptoStream(fsSource, aes.CreateDecryptor(), CryptoStreamMode.Read);

            if (isFolder)
            {
                string outputName = Path.GetFileNameWithoutExtension(encPath);
                string outputPath = Path.Combine(outputFolder, outputName);
                
                if (checkOverwrite && Directory.Exists(outputPath))
                    return (false, "OVERWRITE_WARNING", outputPath);

                Directory.CreateDirectory(outputPath);
                
                string tempZip = Path.Combine(outputFolder, Guid.NewGuid().ToString() + ".tmp");
                using (var fsTemp = new FileStream(tempZip, FileMode.Create))
                {
                    byte[] buffer = new byte[BUFFER_SIZE];
                    int bytesRead;
                    long totalEncBytes = fsSource.Length;
                    
                    while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fsTemp.Write(buffer, 0, bytesRead);
                        progress?.Report((int)((fsSource.Position / (float)totalEncBytes) * 100));
                    }
                }
                
                using (var archive = ZipFile.OpenRead(tempZip))
                {
                    archive.ExtractToDirectory(outputPath, true);
                }
                File.Delete(tempZip);
                return (true, "Giải mã thành công!", outputPath);
            }
            else
            {
                byte[] lenBuffer = new byte[4];
                int read = cs.Read(lenBuffer, 0, 4);
                if (read < 4) return (false, "File bị lỗi!", null);
                
                int nameLen = BitConverter.ToInt32(lenBuffer, 0);
                byte[] nameBytes = new byte[nameLen];
                cs.Read(nameBytes, 0, nameLen);
                string originalName = Encoding.UTF8.GetString(nameBytes);

                string outputPath = Path.Combine(outputFolder, originalName);
                
                if (checkOverwrite && File.Exists(outputPath))
                    return (false, "OVERWRITE_WARNING", outputPath);
                
                using var fsDest = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                long totalEncBytes = fsSource.Length;

                while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsDest.Write(buffer, 0, bytesRead);
                    progress?.Report((int)((fsSource.Position / (float)totalEncBytes) * 100));
                }
                return (true, "Giải mã thành công!", outputPath);
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

        private string GenerateRecoveryCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string GenerateSegment(int length) => new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            return $"FL-{GenerateSegment(4)}-{GenerateSegment(4)}-{GenerateSegment(4)}";
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
        
        private (bool success, string message, string outPath) DecryptLegacy(string encPath, string outputFolder, string password, bool checkOverwrite)
        {
            try
            {
                byte[] final = File.ReadAllBytes(encPath);
                bool isFolder = final[0] == 1;
                byte[] encrypted = new byte[final.Length - 1];
                Array.Copy(final, 1, encrypted, 0, encrypted.Length);

                byte[] data = DecryptLegacyData(encrypted, password);
                
                if (isFolder)
                {
                    string outputName = Path.GetFileNameWithoutExtension(encPath);
                    string outputPath = Path.Combine(outputFolder, outputName);
                    
                    if (checkOverwrite && Directory.Exists(outputPath))
                        return (false, "OVERWRITE_WARNING", outputPath);

                    Directory.CreateDirectory(outputPath);
                    using var ms = new MemoryStream(data);
                    using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
                    zip.ExtractToDirectory(outputPath, true);
                    return (true, "Giải mã thành công (V1)!", outputPath);
                }
                else 
                {
                    int nameLength = BitConverter.ToInt32(data, 0);
                    string originalFileName = Encoding.UTF8.GetString(data, 4, nameLength);
                    byte[] fileBytes = new byte[data.Length - 4 - nameLength];
                    Array.Copy(data, 4 + nameLength, fileBytes, 0, fileBytes.Length);
                    
                    string outputPath = Path.Combine(outputFolder, originalFileName);
                    
                    if (checkOverwrite && File.Exists(outputPath))
                        return (false, "OVERWRITE_WARNING", outputPath);

                    File.WriteAllBytes(outputPath, fileBytes);
                    return (true, "Giải mã thành công (V1)!", outputPath);
                }
            }
            catch
            {
                return (false, "Sai mật khẩu (V1 không hỗ trợ mã khôi phục)!", null);
            }
        }

        private byte[] DecryptLegacyData(byte[] data, string password)
        {
            using var aes = Aes.Create();
            aes.Key = HashPassword(password);
            var iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);
            aes.IV = iv;
            var encrypted = new byte[data.Length - 16];
            Array.Copy(data, 16, encrypted, 0, encrypted.Length);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                cs.Write(encrypted, 0, encrypted.Length);
            return ms.ToArray();
        }
    }
}
