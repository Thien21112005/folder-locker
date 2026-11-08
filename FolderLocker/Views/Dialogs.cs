using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace FolderLocker.Views
{
    /// <summary>
    /// Hộp thoại hệ thống thống nhất (TaskDialog) cho mọi thông báo của app.
    /// TaskDialog hỗ trợ trình đọc màn hình, Esc, phím tắt và theo DPI của Windows.
    /// </summary>
    internal static class Dialogs
    {
        private const string AppName = "Folder Locker";

        public static void Info(IWin32Window owner, string heading, string? text = null) =>
            Show(owner, heading, text, TaskDialogIcon.Information);

        public static void Warn(IWin32Window owner, string heading, string? text = null) =>
            Show(owner, heading, text, TaskDialogIcon.Warning);

        public static void Error(IWin32Window owner, string heading, string? text = null) =>
            Show(owner, heading, text, TaskDialogIcon.Error);

        private static void Show(IWin32Window owner, string heading, string? text, TaskDialogIcon icon)
        {
            TaskDialog.ShowDialog(owner, new TaskDialogPage
            {
                Caption = AppName,
                Heading = heading,
                Text = text,
                Icon = icon,
                Buttons = { TaskDialogButton.OK },
            });
        }

        /// <summary>Hỏi xác nhận; nút mặc định là nút an toàn (Không) khi <paramref name="dangerous"/>.</summary>
        public static bool Confirm(IWin32Window owner, string heading, string text, string yesText, string noText, bool dangerous = false)
        {
            var yes = new TaskDialogButton(yesText);
            var no = new TaskDialogButton(noText);
            var page = new TaskDialogPage
            {
                Caption = AppName,
                Heading = heading,
                Text = text,
                Icon = dangerous ? TaskDialogIcon.Warning : TaskDialogIcon.Information,
                Buttons = { yes, no },
                DefaultButton = dangerous ? no : yes,
                AllowCancel = true,
            };
            return TaskDialog.ShowDialog(owner, page) == yes;
        }

        /// <summary>Thông báo thành công kèm nút mở thư mục chứa kết quả.</summary>
        public static void SuccessWithOpen(IWin32Window owner, string heading, string path, string? extra = null)
        {
            var open = new TaskDialogButton("Mở thư mục");
            var close = new TaskDialogButton("Đóng");
            var page = new TaskDialogPage
            {
                Caption = AppName,
                Heading = heading,
                Text = path + (extra != null ? "\n\n" + extra : ""),
                Icon = TaskDialogIcon.ShieldSuccessGreenBar,
                Buttons = { open, close },
                DefaultButton = open,
                AllowCancel = true,
            };
            if (TaskDialog.ShowDialog(owner, page) == open) RevealInExplorer(path);
        }

        /// <summary>Hỏi ghi đè; khi còn mục phía sau thì có ô "Áp dụng cho các mục còn lại".</summary>
        public static (bool overwrite, bool applyToAll) AskOverwrite(IWin32Window owner, string path, int remaining)
        {
            var overwrite = new TaskDialogButton("Ghi đè");
            var skip = new TaskDialogButton(remaining > 0 ? "Bỏ qua mục này" : "Giữ nguyên");
            var page = new TaskDialogPage
            {
                Caption = AppName,
                Heading = "Đích đã tồn tại",
                Text = $"'{path}' đã tồn tại. Ghi đè sẽ thay thế hoàn toàn nội dung cũ.",
                Icon = TaskDialogIcon.Warning,
                Buttons = { overwrite, skip },
                DefaultButton = skip,
                AllowCancel = true,
                Verification = remaining > 0 ? new TaskDialogVerificationCheckBox($"Áp dụng cho {remaining} mục còn lại") : null,
            };
            bool yes = TaskDialog.ShowDialog(owner, page) == overwrite;
            return (yes, page.Verification?.Checked == true);
        }

        /// <summary>Tóm tắt kết quả xử lý hàng loạt; danh sách lỗi nằm trong phần mở rộng.</summary>
        public static void BatchSummary(IWin32Window owner, string action, int ok, int total,
            IReadOnlyList<(string Name, string Reason)> failures, bool cancelled, string? outputFolder)
        {
            string heading = cancelled
                ? $"Đã dừng — {ok}/{total} mục đã {action}"
                : failures.Count == 0 ? $"Đã {action} xong {ok} mục" : $"Đã {action} {ok}/{total} mục";

            var open = new TaskDialogButton("Mở thư mục");
            var close = new TaskDialogButton("Đóng");
            var page = new TaskDialogPage
            {
                Caption = AppName,
                Heading = heading,
                Text = failures.Count == 0
                    ? outputFolder
                    : $"{failures.Count} mục chưa xong. Bấm \"Xem chi tiết\" để biết lý do.",
                Icon = failures.Count == 0 && !cancelled ? TaskDialogIcon.ShieldSuccessGreenBar : TaskDialogIcon.Warning,
                AllowCancel = true,
                Expander = failures.Count == 0 ? null : new TaskDialogExpander
                {
                    Text = string.Join("\n", failures.Select(f => $"• {f.Name}: {f.Reason}")),
                    CollapsedButtonText = "Xem chi tiết",
                    ExpandedButtonText = "Ẩn chi tiết",
                    Position = TaskDialogExpanderPosition.AfterFootnote,
                },
            };
            if (ok > 0 && outputFolder != null) page.Buttons.Add(open);
            page.Buttons.Add(close);
            page.DefaultButton = ok > 0 && outputFolder != null ? open : close;

            if (TaskDialog.ShowDialog(owner, page) == open && outputFolder != null)
                RevealInExplorer(outputFolder);
        }

        public static void RevealInExplorer(string path)
        {
            try
            {
                if (File.Exists(path) || Directory.Exists(path))
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                else if (Path.GetDirectoryName(path) is { } dir && Directory.Exists(dir))
                    Process.Start("explorer.exe", $"\"{dir}\"");
            }
            catch
            {
                // Không mở được Explorer cũng không ảnh hưởng dữ liệu
            }
        }
    }
}
