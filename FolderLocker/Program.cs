using FolderLocker.Services;
using FolderLocker.Views;

namespace FolderLocker
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            InstallCrashHandlers();
            AppLog.Prune();
            Views.Theming.Theme.Initialize();
            Application.Run(new MainForm());
        }

        /// <summary>Lỗi không lường trước: ghi nhật ký và báo nhẹ nhàng thay vì sập app.</summary>
        private static void InstallCrashHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                AppLog.Error("ui-unhandled", e.Exception);
                MessageBox.Show(
                    "Đã xảy ra lỗi không mong muốn. Dữ liệu của bạn không bị thay đổi bởi lỗi này.\n\n" +
                    "Chi tiết đã được ghi vào nhật ký (menu ⚙ → Mở thư mục nhật ký).",
                    "Folder Locker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex) AppLog.Error("fatal", ex);
            };
        }
    }
}
