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

                //Nén nếu là data, đọc là file 
                if (isFolder)
                {
                    data = _model.CompressFolder(sourcePath);
                }
                else data = File.ReadAllBytes(sourcePath);

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
    }
}
