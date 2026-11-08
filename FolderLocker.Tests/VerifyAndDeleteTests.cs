using FolderLocker.Controllers;
using FolderLocker.Models.Formats;

namespace FolderLocker.Tests;

public class VerifyAndDeleteTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void Verify_passes_for_intact_file_and_writes_nothing()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.bin", TempDir.RandomBytes(2 * 1024 * 1024 + 5));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "password-1");
        var before = Directory.GetFileSystemEntries(t.Root).Length;

        Assert.True(_controller.Verify(enc, "password-1").Success);
        Assert.Equal(before, Directory.GetFileSystemEntries(t.Root).Length);
    }

    [Fact]
    public void Verify_fails_for_corrupted_tail()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.bin", TempDir.RandomBytes(2 * 1024 * 1024 + 5));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "password-1");

        byte[] bytes = File.ReadAllBytes(enc);
        bytes[^20] ^= 0xFF; // hỏng chunk cuối: chỉ phát hiện được khi đọc hết file
        File.WriteAllBytes(enc, bytes);

        Assert.True(_controller.CheckSecret(enc, "password-1").Success);   // header vẫn tốt
        Assert.False(_controller.Verify(enc, "password-1").Success);       // nhưng dữ liệu hỏng
    }

    [Fact]
    public void Verify_rejects_legacy_formats()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "x"u8.ToArray());
        string enc = t.Path_("a.enc");
        LegacyFormats.EncryptV2ForTests(src, enc, "pw");
        Assert.False(_controller.Verify(enc, "pw").Success);
    }

    [Fact]
    public void Secure_delete_removes_file_and_folder_trees()
    {
        using var t = new TempDir();
        string file = t.WriteFile("secret.txt", TempDir.RandomBytes(3000));
        File.SetAttributes(file, FileAttributes.ReadOnly);
        Assert.True(_controller.DeleteSource(file, secure: true).Success);
        Assert.False(File.Exists(file));

        string dir = t.NewDir("tree", "sub", "deeper");
        t.WriteFile(Path.Combine("tree", "a.bin"), TempDir.RandomBytes(2 * 1024 * 1024));
        t.WriteFile(Path.Combine("tree", "sub", "deeper", "b.bin"), []);
        Assert.True(_controller.DeleteSource(t.Path_("tree"), secure: true).Success);
        Assert.False(Directory.Exists(t.Path_("tree")));
        Assert.Empty(Directory.GetFileSystemEntries(t.Root));
    }
}
