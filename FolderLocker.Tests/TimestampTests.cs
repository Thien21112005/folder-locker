using FolderLocker.Controllers;

namespace FolderLocker.Tests;

public class TimestampTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void File_and_folder_times_are_restored()
    {
        using var t = new TempDir();
        var written = new DateTime(2021, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        var created = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        string file = t.WriteFile("old.txt", "x"u8.ToArray());
        File.SetCreationTimeUtc(file, created);
        File.SetLastWriteTimeUtc(file, written);

        string dir = t.NewDir("Docs");
        string inner = t.WriteFile(Path.Combine("Docs", "inner.txt"), "y"u8.ToArray());
        File.SetLastWriteTimeUtc(inner, written);
        Directory.SetCreationTimeUtc(dir, created);
        Directory.SetLastWriteTimeUtc(dir, written);

        _controller.Encrypt(file, t.Path_("old.enc"), "password-1");
        _controller.Encrypt(dir, t.Path_("Docs.enc"), "password-1");

        string outDir = t.NewDir("out");
        var f = _controller.Decrypt(t.Path_("old.enc"), outDir, "password-1");
        var d = _controller.Decrypt(t.Path_("Docs.enc"), outDir, "password-1");
        Assert.True(f.Success && d.Success);

        Assert.Equal(written, File.GetLastWriteTimeUtc(f.OutputPath!));
        Assert.Equal(created, File.GetCreationTimeUtc(f.OutputPath!));
        Assert.Equal(created, Directory.GetCreationTimeUtc(d.OutputPath!));
        Assert.Equal(written, Directory.GetLastWriteTimeUtc(d.OutputPath!));
        // zip lưu thời gian với độ chính xác 2 giây (định dạng DOS)
        Assert.True((File.GetLastWriteTimeUtc(Path.Combine(d.OutputPath!, "inner.txt")) - written).Duration() <= TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Verify_and_change_password_handle_timestamp_payload()
    {
        using var t = new TempDir();
        string file = t.WriteFile("a.bin", TempDir.RandomBytes(1500));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(file, enc, "password-1");

        Assert.True(_controller.Verify(enc, "password-1").Success);
        Assert.True(_controller.ChangePassword(enc, "password-1", "password-2", false).Success);
        Assert.True(_controller.Verify(enc, "password-2").Success);
    }
}
