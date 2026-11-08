using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FolderLocker.Services
{
    /// <summary>
    /// Đăng ký menu chuột phải "Mã hóa bằng Folder Locker" và mở file .enc bằng double-click.
    /// Chỉ ghi vào HKCU\Software\Classes (không cần quyền admin, chỉ ảnh hưởng người dùng hiện tại).
    /// Không giành quyền mặc định của .enc nếu một ứng dụng khác đã đăng ký trước.
    /// </summary>
    public static class ShellIntegration
    {
        public const string ProgId = "FolderLocker.EncryptedFile";
        private const string VerbKey = "FolderLocker.Encrypt";
        private const string Extension = ".enc";

        /// <summary>Mặc định HKCU\Software\Classes; test truyền một khóa tạm.</summary>
        private static RegistryKey OpenClasses(RegistryKey? classes) =>
            classes ?? Registry.CurrentUser.CreateSubKey(@"Software\Classes", writable: true);

        public static bool IsRegistered(RegistryKey? classes = null)
        {
            var root = OpenClasses(classes);
            try
            {
                using var verb = root.OpenSubKey($@"*\shell\{VerbKey}\command");
                using var prog = root.OpenSubKey($@"{ProgId}\shell\open\command");
                return verb != null && prog != null;
            }
            finally
            {
                if (classes == null) root.Dispose();
            }
        }

        public static void Register(string exePath, RegistryKey? classes = null)
        {
            var root = OpenClasses(classes);
            try
            {
                string command = $"\"{exePath}\" \"%1\"";
                string icon = $"\"{exePath}\",0";

                // ProgId cho file .enc
                using (var prog = root.CreateSubKey(ProgId))
                {
                    prog.SetValue("", "File mã hóa Folder Locker");
                    using (var di = prog.CreateSubKey("DefaultIcon")) di.SetValue("", icon);
                    using var open = prog.CreateSubKey(@"shell\open");
                    open.SetValue("MUIVerb", "Giải mã bằng Folder Locker");
                    using var cmd = open.CreateSubKey("command");
                    cmd.SetValue("", command);
                }

                using (var ext = root.CreateSubKey(Extension))
                {
                    // Chỉ làm ứng dụng mặc định nếu .enc chưa thuộc về ứng dụng nào khác
                    string? current = ext.GetValue("") as string;
                    if (string.IsNullOrEmpty(current) || current == ProgId)
                        ext.SetValue("", ProgId);
                    using var openWith = ext.CreateSubKey("OpenWithProgids");
                    openWith.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
                }

                // Menu chuột phải cho file (trừ .enc) và thư mục
                foreach (var (path, appliesTo) in new[]
                         {
                             ($@"*\shell\{VerbKey}", "NOT System.FileExtension:=.enc"),
                             ($@"Directory\shell\{VerbKey}", (string?)null),
                         })
                {
                    using var verb = root.CreateSubKey(path);
                    verb.SetValue("MUIVerb", "Mã hóa bằng Folder Locker");
                    verb.SetValue("Icon", icon);
                    if (appliesTo != null) verb.SetValue("AppliesTo", appliesTo);
                    using var cmd = verb.CreateSubKey("command");
                    cmd.SetValue("", command);
                }
            }
            finally
            {
                if (classes == null) root.Dispose();
            }
            NotifyShell();
        }

        public static void Unregister(RegistryKey? classes = null)
        {
            var root = OpenClasses(classes);
            try
            {
                root.DeleteSubKeyTree($@"*\shell\{VerbKey}", throwOnMissingSubKey: false);
                root.DeleteSubKeyTree($@"Directory\shell\{VerbKey}", throwOnMissingSubKey: false);
                root.DeleteSubKeyTree(ProgId, throwOnMissingSubKey: false);

                using var ext = root.OpenSubKey(Extension, writable: true);
                if (ext != null)
                {
                    if (ext.GetValue("") as string == ProgId) ext.DeleteValue("", throwOnMissingValue: false);
                    using (var openWith = ext.OpenSubKey("OpenWithProgids", writable: true))
                        openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
                }
            }
            finally
            {
                if (classes == null) root.Dispose();
            }
            NotifyShell();
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        /// <summary>Báo Explorer làm mới liên kết file / icon.</summary>
        private static void NotifyShell()
        {
            const int SHCNE_ASSOCCHANGED = 0x08000000;
            try { SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero); }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
    }
}
