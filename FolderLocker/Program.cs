using FolderLocker.Services;
using FolderLocker.Views;

namespace FolderLocker
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        ///  Tham số dòng lệnh là đường dẫn file/thư mục (từ Explorer) sẽ được chọn sẵn.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0 && Cli.CliRunner.IsCommand(args[0]))
                return RunCli(args);

            var paths = args.Where(a => File.Exists(a) || Directory.Exists(a)).Select(Path.GetFullPath).ToArray();

            using var instance = new SingleInstance("FolderLocker");
            if (!instance.IsPrimary && instance.ForwardToPrimary(paths))
                return 0; // Đã giao cho cửa sổ đang mở

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            InstallCrashHandlers();
            AppLog.Prune();
            Views.Theming.Theme.Initialize();

            var form = new MainForm(paths);
            if (instance.IsPrimary) instance.Listen(form.ReceiveExternalPaths);
            Application.Run(form);
            return 0;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        /// <summary>
        /// App là WinExe nên không có console riêng: gắn vào console của shell gọi nó
        /// (trừ khi stdout đã được chuyển hướng vào file / pipe).
        /// </summary>
        private static int RunCli(string[] args)
        {
            const int AttachParentProcess = -1;
            if (!Console.IsOutputRedirected || !Console.IsErrorRedirected)
                AttachConsole(AttachParentProcess);
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (IOException) { }

            var runner = new Cli.CliRunner(Console.In, Console.IsInputRedirected, Console.Out, Console.Error);
            int code = runner.Run(args);
            Console.Out.Flush();
            return code;
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
