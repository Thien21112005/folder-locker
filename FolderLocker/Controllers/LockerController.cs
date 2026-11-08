using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using FolderLocker.Models;

namespace FolderLocker.Controllers
{
    public class LockerController
    {
        private readonly LockerModel _model = new LockerModel();

        public OperationResult Encrypt(string sourcePath, string destPath, string password, IProgress<int>? progress = null, CancellationToken ct = default)
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
                string recCode = _model.Encrypt(sourcePath, destPath, password, progress, ct);
                return new OperationResult(true, "Mã hóa thành công!", destPath, recCode);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        public OperationResult Decrypt(string encPath, string outputFolder, string passwordOrRecoveryCode, bool checkOverwrite = true, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(encPath)) return OperationResult.Fail("Chưa chọn file .enc!");
            if (string.IsNullOrWhiteSpace(passwordOrRecoveryCode)) return OperationResult.Fail("Chưa nhập mật khẩu hoặc mã khôi phục!");
            if (!File.Exists(encPath)) return OperationResult.Fail("File .enc không tồn tại!");
            if (!Directory.Exists(outputFolder)) return OperationResult.Fail("Thư mục xuất không tồn tại!");

            try
            {
                return _model.Decrypt(encPath, outputFolder, passwordOrRecoveryCode, checkOverwrite, progress, ct);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        /// <summary>Kiểm tra mật khẩu / mã khôi phục trước khi hỏi nơi xuất (không ghi gì ra đĩa).</summary>
        public OperationResult CheckSecret(string encPath, string passwordOrRecoveryCode)
        {
            if (string.IsNullOrWhiteSpace(passwordOrRecoveryCode)) return OperationResult.Fail("Chưa nhập mật khẩu hoặc mã khôi phục!");
            if (!File.Exists(encPath)) return OperationResult.Fail("File .enc không tồn tại!");
            try
            {
                return _model.CheckSecret(encPath, passwordOrRecoveryCode);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        /// <summary>true nếu là file .enc do FolderLocker tạo (đọc header, không chỉ dựa vào đuôi file).</summary>
        public bool IsEncryptedFile(string path) => LockerModel.DetectFormat(path) != EncryptedFormat.None;

        /// <summary>Đổi mật khẩu của file .enc; trả về mã khôi phục mới nếu được yêu cầu.</summary>
        public OperationResult ChangePassword(string encPath, string currentSecret, string newPassword, bool newRecoveryCode, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(currentSecret)) return OperationResult.Fail("Chưa nhập mật khẩu hoặc mã khôi phục hiện tại!");
            if (PasswordStrength.Evaluate(newPassword) is StrengthLevel.Empty or StrengthLevel.TooShort)
                return OperationResult.Fail($"Mật khẩu mới cần tối thiểu {PasswordStrength.MinLength} ký tự.");
            if (!File.Exists(encPath)) return OperationResult.Fail("File .enc không tồn tại!");
            try
            {
                return _model.ChangePassword(encPath, currentSecret, newPassword, newRecoveryCode, ct);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        /// <summary>Đọc lại toàn bộ file .enc vừa tạo và xác thực, trước khi dám xóa bản gốc.</summary>
        public OperationResult Verify(string encPath, string password, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            try
            {
                return _model.Verify(encPath, password, progress, ct);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(Describe(ex));
            }
        }

        /// <summary>
        /// Xóa bản gốc: mặc định chuyển vào Thùng rác; <paramref name="secure"/> = ghi đè rồi xóa vĩnh viễn.
        /// </summary>
        public OperationResult DeleteSource(string path, bool secure = false, IProgress<int>? progress = null, CancellationToken ct = default)
        {
            try
            {
                if (secure) _model.ShredPermanently(path, progress, ct);
                else _model.MoveToRecycleBin(path);

                if (File.Exists(path) || Directory.Exists(path))
                    return OperationResult.Fail("Không xóa được bản gốc (có thể đang được mở bởi chương trình khác).");
                return new OperationResult(true, secure ? "Đã xóa vĩnh viễn bản gốc." : "Đã chuyển bản gốc vào Thùng rác.");
            }
            catch (OperationCanceledException)
            {
                return new OperationResult(false, "Đã dừng xóa bản gốc. Một phần dữ liệu gốc có thể đã bị xóa.", Cancelled: true);
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
