using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using FolderLocker.Models;
namespace FolderLocker.Controllers
{
    public class LockerController
    {
        private readonly LockerModel _model = new LockerModel();

        //Mã hóa 
        public (bool success, string message) Encrypt(string sourcePath, string destPath, string password)
        {
            try
            {
                //Validate 
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    return (false, "Chưa chọn file/folder!");
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Chưa nhập password!");
                }
                if(!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    return (false, "File/Folder không tồn tại! ");
                }


                bool isFolder = Directory.Exists(sourcePath);

                byte[] data;

                //Nén nếu là folder, nếu là file thì lưu thêm tên gốc vào data
                if (isFolder)
                {
                    data = _model.CompressFolder(sourcePath);
                }
                else
                {
                    byte[] fileBytes = File.ReadAllBytes(sourcePath);
                    string fileName = Path.GetFileName(sourcePath);
                    byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                    byte[] nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);

                    data = new byte[4 + nameBytes.Length + fileBytes.Length];
                    Array.Copy(nameLengthBytes, 0, data, 0, 4);
                    Array.Copy(nameBytes, 0, data, 4, nameBytes.Length);
                    Array.Copy(fileBytes, 0, data, 4 + nameBytes.Length, fileBytes.Length);
                }

                byte[] encrypted = _model.Encrypt(data, password);
                // Đánh dấu folder(1) hay file(0) ở byte đầu

                byte[] final = new byte[encrypted.Length + 1];
                final[0] = isFolder ? (byte)1 : (byte)0;
                Array.Copy(encrypted, 0, final, 1, encrypted.Length);

                //Lưu file .enc 
                File.WriteAllBytes(destPath, final);
                return (true, "Mã hóa thành công! ");
            }
            catch(Exception ex)
            {
                return (false, "Lỗi: " + ex.Message);
            }
        }

        //Giải mã 
        public (bool success, string message) Decrypt(string encPath, string outputFolder, string password)
        {
            try
            {
                //validate
                if (string.IsNullOrWhiteSpace(encPath))
                    return (false, "Chưa chọn file .enc!");
                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Chưa nhập password!");
                if (!File.Exists(encPath))
                    return (false, "File .enc không tồn tại!");

                byte[] final = File.ReadAllBytes(encPath);

                bool isFolder = final[0] == 1;
                byte[] encrypted = new byte[final.Length - 1];
                Array.Copy(final, 1, encrypted, 0, encrypted.Length);

                //Giải mã 
                byte[] data = _model.Decrypt(encrypted, password);
                
                //Giải nén nếu là folder, ghi đè nếu là file 
                if (isFolder)
                {
                    //Tên output = tên file .enc bỏ đuôi .enc 
                    string outputName = Path.GetFileNameWithoutExtension(encPath);
                    string outputPath = Path.Combine(outputFolder, outputName);
                    _model.DecompressFolder(data, outputPath);
                    return (true, "Giải mã thành công!\nXuất tại: " + outputPath);
                }
                else 
                {
                    int nameLength = BitConverter.ToInt32(data, 0);
                    string originalFileName = Encoding.UTF8.GetString(data, 4, nameLength);
                    byte[] fileBytes = new byte[data.Length - 4 - nameLength];
                    Array.Copy(data, 4 + nameLength, fileBytes, 0, fileBytes.Length);
                    
                    string outputPath = Path.Combine(outputFolder, originalFileName);
                    File.WriteAllBytes(outputPath, fileBytes);
                    return (true, "Giải mã thành công!\nXuất tại: " + outputPath);
                }
            }
            catch
            {
                return (false, "Sai password hoặc file bị lỗi!");
            }
        }


        // ── Xóa file/folder gốc ──
        public (bool success, string message) DeleteSource(string path)
        {
            try
            {
                _model.Delete(path);
                return (true, "Đã xóa file/folder gốc!");
            }
            catch (Exception ex)
            {
                return (false, "Xóa thất bại: " + ex.Message);
            }
        }
    }
}
