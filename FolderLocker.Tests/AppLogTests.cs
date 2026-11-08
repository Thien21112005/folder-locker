using FolderLocker.Controllers;
using FolderLocker.Services;

namespace FolderLocker.Tests;

[Collection("AppSettings")]
public class AppLogTests
{
    [Fact]
    public void Logs_operations_without_secrets_or_paths_and_prunes_old_files()
    {
        using var t = new TempDir();
        string original = AppSettings.DataFolder;
        AppSettings.DataFolder = t.Path_("data");
        try
        {
            var controller = new LockerController();
            string src = t.WriteFile("BiMat-TaiLieu.txt", "x"u8.ToArray());
            string enc = t.Path_("BiMat-TaiLieu.enc");
            var r = controller.Encrypt(src, enc, "SuperSecret-Pass1");
            controller.Decrypt(enc, t.NewDir("out"), "wrong-password");

            string log = string.Join("\n", Directory.GetFiles(AppLog.Folder, "*.log").Select(File.ReadAllText));
            Assert.Contains("encrypt-file\tOK", log);
            Assert.Contains("decrypt-v3", log);
            Assert.Contains("FAILED", log);
            Assert.DoesNotContain("SuperSecret", log);
            Assert.DoesNotContain(r.RecoveryCode!, log);
            Assert.DoesNotContain("BiMat", log);
            Assert.DoesNotContain(t.Root, log);

            string old = Path.Combine(AppLog.Folder, DateTime.Now.AddDays(-AppLog.KeepDays - 5).ToString("yyyy-MM-dd") + ".log");
            File.WriteAllText(old, "old");
            AppLog.Prune();
            Assert.False(File.Exists(old));
            Assert.NotEmpty(Directory.GetFiles(AppLog.Folder, "*.log"));
        }
        finally
        {
            AppSettings.DataFolder = original;
        }
    }
}
