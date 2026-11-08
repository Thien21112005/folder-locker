using System;
using System.IO;
using System.Security.Cryptography;
using FolderLocker.Models;

namespace FolderLocker.Controllers
{
    public class LockerController
    {
        private readonly LockerModel _model = new LockerModel();

        public OperationResult Encrypt(string sourcePath, string destPath, string password, IProgress<int>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) return OperationResult.Fail("Chưa chọn file/thư mục!");
            if (string.IsNullOrWhiteSpace(destPath)) return OperationResult.Fail("Chưa chọn nơi lưu file mã hóa!");
            if (string.IsNullOrWhiteSpace(password)) return OperationResult.Fail("Chưa nhập mật khẩu!");
            if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath)) return OperationResult.Fail("File/thư mục không tồn tại!");

            bool isFolder = Directory.Exists(sourcePath);

            if (isFolder && PathSafety.IsInside(destPath, sourcePath))
                return OperationResult.Fail("Không thể lưu file mã hóa bên trong chính thư mục đang mã hóa. Hãy chọn nơi lưu khác.");
            if (!isFolder && string.Equals(Path.GetFullPath(destPath), Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
                return OperationResult.Fail("File mã hóa không được trùng với file gốc.");

            try
            {
                string recCode = _model.EncryptV2(sourcePath, destPath, password, isFolder, progress!);
                return new OperationResult(true, "Mã hóa thành công!", destPath, recCode);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        public OperationResult Decrypt(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite = true, IProgress<int>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(encPath)) return OperationResult.Fail("Chưa chọn file .enc!");
            if (string.IsNullOrWhiteSpace(passwordOrRecoveryCode)) return OperationResult.Fail("Chưa nhập mật khẩu hoặc mã khôi phục!");
            if (!File.Exists(encPath)) return OperationResult.Fail("File .enc không tồn tại!");
            if (!Directory.Exists(outputFolder)) return OperationResult.Fail("Thư mục xuất không tồn tại!");

            try
            {
                var (success, message, outPath) = _model.DecryptV2(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress);
                if (message == "OVERWRITE_WARNING" && outPath != null) return OperationResult.OverwriteNeeded(outPath);
                return new OperationResult(success, message, outPath);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        /// <summary>Chuyển bản gốc vào Thùng rác. Trả về kết quả để View báo cho người dùng.</summary>
        public OperationResult DeleteSource(string path)
        {
            try
            {
                _model.MoveToRecycleBin(path);
                if (File.Exists(path) || Directory.Exists(path))
                    return OperationResult.Fail("Không xóa được bản gốc (có thể đang được mở bởi chương trình khác).");
                return new OperationResult(true, "Đã chuyển bản gốc vào Thùng rác.");
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Fail("Đã hủy xóa bản gốc.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("Xóa bản gốc thất bại: " + Describe(ex));
            }
        }

        /// <summary>Thông báo lỗi thân thiện thay cho exception thô.</summary>
        private static string Describe(Exception ex) => ex switch
        {
            CryptographicException => "Dữ liệu mã hóa bị hỏng hoặc đã bị chỉnh sửa.",
            UnauthorizedAccessException => "Không có quyền truy cập file/thư mục.",
            PathTooLongException => "Đường dẫn quá dài.",
            DirectoryNotFoundException => "Không tìm thấy thư mục.",
            EndOfStreamException or InvalidDataException => "File bị lỗi hoặc không đúng định dạng.",
            IOException io => "Lỗi đọc/ghi file: " + io.Message,
            _ => "Lỗi không xác định: " + ex.Message,
        };
    }
}
