using FolderLocker.Services;
using Microsoft.Win32;

namespace FolderLocker.Tests;

public class ShellIntegrationTests : IDisposable
{
    // Khóa tạm trong HKCU, KHÔNG phải Software\Classes thật
    private readonly string _keyPath = @"Software\FolderLockerTests\" + Guid.NewGuid().ToString("N");
    private readonly RegistryKey _classes;

    public ShellIntegrationTests() => _classes = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true);

    public void Dispose()
    {
        _classes.Dispose();
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
        using var parent = Registry.CurrentUser.OpenSubKey(@"Software\FolderLockerTests");
        if (parent is { SubKeyCount: 0 })
        {
            parent.Close();
            Registry.CurrentUser.DeleteSubKey(@"Software\FolderLockerTests", throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void Register_creates_verbs_and_association_then_unregister_cleans_up()
    {
        const string exe = @"C:\Apps\FolderLocker\FolderLocker.exe";
        Assert.False(ShellIntegration.IsRegistered(_classes));

        ShellIntegration.Register(exe, _classes);

        Assert.True(ShellIntegration.IsRegistered(_classes));
        using (var cmd = _classes.OpenSubKey(@"*\shell\FolderLocker.Encrypt\command"))
            Assert.Equal($"\"{exe}\" \"%1\"", cmd!.GetValue(""));
        using (var dirCmd = _classes.OpenSubKey(@"Directory\shell\FolderLocker.Encrypt\command"))
            Assert.NotNull(dirCmd);
        using (var verb = _classes.OpenSubKey(@"*\shell\FolderLocker.Encrypt"))
            Assert.Equal("NOT System.FileExtension:=.enc", verb!.GetValue("AppliesTo"));
        using (var ext = _classes.OpenSubKey(".enc"))
            Assert.Equal(ShellIntegration.ProgId, ext!.GetValue(""));

        ShellIntegration.Unregister(_classes);

        Assert.False(ShellIntegration.IsRegistered(_classes));
        Assert.Null(_classes.OpenSubKey(ShellIntegration.ProgId));
        using (var ext = _classes.OpenSubKey(".enc"))
            Assert.Null(ext?.GetValue(""));
    }

    [Fact]
    public void Does_not_steal_enc_from_another_app()
    {
        using (var ext = _classes.CreateSubKey(".enc")) ext.SetValue("", "OtherApp.Document");

        ShellIntegration.Register(@"C:\x\FolderLocker.exe", _classes);
        using (var ext = _classes.OpenSubKey(".enc"))
        {
            Assert.Equal("OtherApp.Document", ext!.GetValue(""));
            using var openWith = ext.OpenSubKey("OpenWithProgids");
            Assert.Contains(ShellIntegration.ProgId, openWith!.GetValueNames());
        }

        ShellIntegration.Unregister(_classes);
        using (var ext = _classes.OpenSubKey(".enc"))
            Assert.Equal("OtherApp.Document", ext!.GetValue(""));
    }
}

public class SingleInstanceTests
{
    [Fact]
    public async Task Second_instance_forwards_paths_to_primary()
    {
        string appId = "FolderLockerTest." + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstance(appId);
        Assert.True(primary.IsPrimary);

        var received = new TaskCompletionSource<string[]>();
        primary.Listen(p => received.TrySetResult(p));

        using var secondary = new SingleInstance(appId);
        Assert.False(secondary.IsPrimary);
        Assert.True(secondary.ForwardToPrimary([@"C:\a.txt", @"D:\b c\d.enc"]));

        var paths = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal([@"C:\a.txt", @"D:\b c\d.enc"], paths);
    }
}
