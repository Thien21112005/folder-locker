using FolderLocker.Controllers;
using FolderLocker.Models.Formats;
using FolderLocker.Services;

namespace FolderLocker.Tests;

/// <summary>IProgress đồng bộ: hủy ngay khi nhận báo cáo tiến độ đầu tiên.</summary>
internal sealed class CancelOnFirstReport(CancellationTokenSource cts) : IProgress<int>
{
    public void Report(int value) => cts.Cancel();
}

public class CancellationTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void Cancelled_encryption_leaves_nothing_behind()
    {
        using var t = new TempDir();
        string src = t.WriteFile("big.bin", TempDir.RandomBytes(3 * 1024 * 1024));
        string enc = t.Path_("big.enc");
        using var cts = new CancellationTokenSource();

        var r = _controller.Encrypt(src, enc, "password-1", new CancelOnFirstReport(cts), cts.Token);

        Assert.True(r.Cancelled);
        Assert.False(r.Success);
        Assert.Equal(["big.bin"], Directory.GetFileSystemEntries(t.Root).Select(Path.GetFileName));
    }

    [Fact]
    public void Cancelled_folder_encryption_leaves_nothing_behind()
    {
        using var t = new TempDir();
        string src = t.NewDir("data");
        for (int i = 0; i < 3; i++) t.WriteFile(Path.Combine("data", $"f{i}.bin"), TempDir.RandomBytes(2 * 1024 * 1024));
        using var cts = new CancellationTokenSource();

        var r = _controller.Encrypt(src, t.Path_("data.enc"), "password-1", new CancelOnFirstReport(cts), cts.Token);

        Assert.True(r.Cancelled);
        Assert.Equal(["data"], Directory.GetFileSystemEntries(t.Root).Select(Path.GetFileName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancelled_decryption_leaves_no_output(bool folder)
    {
        using var t = new TempDir();
        string src = folder ? t.NewDir("src") : t.WriteFile("src.bin", TempDir.RandomBytes(3 * 1024 * 1024));
        if (folder) t.WriteFile(Path.Combine("src", "a.bin"), TempDir.RandomBytes(3 * 1024 * 1024));
        string enc = t.Path_("x.enc");
        Assert.True(_controller.Encrypt(src, enc, "password-1").Success);

        string outDir = t.NewDir("out");
        using var cts = new CancellationTokenSource();
        var r = _controller.Decrypt(enc, outDir, "password-1", true, new CancelOnFirstReport(cts), cts.Token);

        Assert.True(r.Cancelled);
        Assert.Empty(Directory.GetFileSystemEntries(outDir));
    }
}

public class CheckSecretTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void V3_accepts_password_and_recovery_code_without_writing()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "x"u8.ToArray());
        string enc = t.Path_("a.enc");
        string code = _controller.Encrypt(src, enc, "password-1").RecoveryCode!;
        var before = Directory.GetFileSystemEntries(t.Root).Length;

        Assert.True(_controller.CheckSecret(enc, "password-1").Success);
        Assert.True(_controller.CheckSecret(enc, code.ToLowerInvariant()).Success);
        Assert.False(_controller.CheckSecret(enc, "wrong").Success);
        Assert.Equal(before, Directory.GetFileSystemEntries(t.Root).Length);
    }

    [Fact]
    public void V2_check_works()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "x"u8.ToArray());
        string enc = t.Path_("a.enc");
        string code = LegacyFormats.EncryptV2ForTests(src, enc, "pw");

        Assert.True(_controller.CheckSecret(enc, "pw").Success);
        Assert.True(_controller.CheckSecret(enc, code).Success);
        Assert.False(_controller.CheckSecret(enc, "nope").Success);
    }

    [Fact]
    public void Unknown_file_is_rejected()
    {
        using var t = new TempDir();
        string fake = t.WriteFile("fake.enc", "hello"u8.ToArray());
        Assert.False(_controller.CheckSecret(fake, "pw").Success);
    }
}

[Collection("AppSettings")]
public class AppSettingsTests
{
    [Fact]
    public void Round_trips_and_survives_corrupt_file()
    {
        using var t = new TempDir();
        string original = AppSettings.DataFolder;
        AppSettings.DataFolder = t.Root;
        try
        {
            Assert.Null(AppSettings.Load().LastOutputFolder);

            var s = new AppSettings { LastOutputFolder = @"D:\Out", SecureDelete = true };
            s.Save();
            var loaded = AppSettings.Load();
            Assert.Equal(@"D:\Out", loaded.LastOutputFolder);
            Assert.True(loaded.SecureDelete);

            File.WriteAllText(Path.Combine(t.Root, "settings.json"), "{ not json");
            Assert.False(AppSettings.Load().SecureDelete);
        }
        finally
        {
            AppSettings.DataFolder = original;
        }
    }
}
