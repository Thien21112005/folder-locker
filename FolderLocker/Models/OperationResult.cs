namespace FolderLocker.Models
{
    /// <summary>Kết quả một thao tác mã hóa / giải mã / xóa trả về cho View.</summary>
    public sealed record OperationResult(
        bool Success,
        string Message,
        string? OutputPath = null,
        string? RecoveryCode = null,
        bool NeedsOverwriteConfirm = false,
        bool Cancelled = false)
    {
        public static OperationResult Fail(string message) => new(false, message);

        public static OperationResult OverwriteNeeded(string path) =>
            new(false, "Đích đã tồn tại.", path, NeedsOverwriteConfirm: true);

        public static OperationResult Canceled() =>
            new(false, "Đã hủy. Không có file nào được tạo hay thay đổi.", Cancelled: true);
    }
}
