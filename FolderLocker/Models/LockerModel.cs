using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FolderLocker.Models
{
    public class LockerModel
    {
        //Mã hóa 
        public byte[] Encrypt(byte[] data, string password)
        {
            using var aes = Aes.Create();
            aes.Key = HashPassword(password);
            aes.GenerateIV(); //tạp IV ngẫu nhiên
            using var ms = new MemoryStream(); //bộ nhớ tạm trên RAM, không tạo trực tiếp trên disk
            ms.Write(aes.IV, 0, 16); //ghi IV vào đầu file 


            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        //Giải mã
        public byte[] Decrypt(byte[] data, string passwrod)
        {
            using var aes = Aes.Create();
            aes.Key = HashPassword(passwrod);

            var iv = new byte[16];
            Array.Copy(data, 0, iv, 0, 16);
            aes.IV = iv;

            var encrypted = new byte[data.Length - 16];
            Array.Copy(data, 0, encrypted, 0, encrypted.Length);

            using var ms = new MemoryStream();

            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                cs.Write(encrypted, 0, encrypted.Length);

            return ms.ToArray();
        }


        //Compress Folder
        public byte[] CompressFolder(string folderPath)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach(var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(folderPath, file);
                    zip.CreateEntryFromFile(file, entryName);
                }
            }
            return ms.ToArray();
        }

        //Decompress

        public void DecompressFolder(byte[] data, string outputPath)
        {
            Directory.CreateDirectory(outputPath);
            using var ms = new MemoryStream();
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            zip.ExtractToDirectory(outputPath, true);
        }


        //xóa file hoặc folder 

        public void Delete(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }


        //Hass
        public static byte[] HashPassword(string password)
            => SHA256.HashData(Encoding.UTF8.GetBytes(password));
    }
}
