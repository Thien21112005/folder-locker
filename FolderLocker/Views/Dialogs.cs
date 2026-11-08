using System.Diagnostics;
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
