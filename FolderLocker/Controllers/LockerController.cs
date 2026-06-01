using System;
using System.IO;
using FolderLocker.Models;

namespace FolderLocker.Controllers
{
    public class LockerController
    {
        private readonly LockerModel _model = new LockerModel();

        public (bool success, string message, string recoveryCode) Encrypt(string sourcePath, string destPath, string password, IProgress<int> progress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath)) return (false, "Chưa chọn file/folder!", null);
                if (string.IsNullOrWhiteSpace(password)) return (false, "Chưa nhập password!", null);
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath)) return (false, "File/Folder không tồn tại!", null);

                bool isFolder = Directory.Exists(sourcePath);
                
                string recCode = _model.EncryptV2(sourcePath, destPath, password, isFolder, progress);
                return (true, "Mã hóa thành công!", recCode);
            }
            catch(Exception ex)
            {
                return (false, "Lỗi: " + ex.Message, null);
            }
        }

        public (bool success, string message, string outPath) Decrypt(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite = true, IProgress<int> progress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(encPath)) return (false, "Chưa chọn file .enc!", null);
                if (string.IsNullOrWhiteSpace(passwordOrRecoveryCode)) return (false, "Chưa nhập password/mã khôi phục!", null);
                if (!File.Exists(encPath)) return (false, "File .enc không tồn tại!", null);

                return _model.DecryptV2(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress);
            }
            catch(Exception ex)
            {
                return (false, "Lỗi: " + ex.Message, null);
            }
        }

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
