using FolderLocker.Controllers;
using FolderLocker.Models.Formats;

namespace FolderLocker.Tests;

public class ChangePasswordTests
{
    private readonly LockerController _controller = new();

    private (TempDir t, string enc, byte[] data, string code) Setup()
    {
        var t = new TempDir();
        byte[] data = TempDir.RandomBytes(2 * 1024 * 1024 + 99);
        string src = t.WriteFile("a.bin", data);
        string enc = t.Path_("a.enc");
        string code = _controller.Encrypt(src, enc, "old-password").RecoveryCode!;
        return (t, enc, data, code);
    }

    [Fact]
    public void New_password_works_old_does_not_and_data_is_intact()
    {
        var (t, enc, data, code) = Setup();
        using (t)
        {
            var r = _controller.ChangePassword(enc, "old-password", "new-password-2", newRecoveryCode: false);
            Assert.True(r.Success, r.Message);
            Assert.Null(r.RecoveryCode);

            Assert.False(_controller.CheckSecret(enc, "old-password").Success);
            Assert.True(_controller.CheckSecret(enc, code).Success);   // mã khôi phục cũ vẫn dùng được

            var d = _controller.Decrypt(enc, t.NewDir("out"), "new-password-2");
            Assert.True(d.Success, d.Message);
            Assert.Equal(data, File.ReadAllBytes(d.OutputPath!));
            Assert.Equal(["a.bin", "a.enc", "out"], Directory.GetFileSystemEntries(t.Root).Select(Path.GetFileName).Order());
        }
    }

    [Fact]
    public void Can_unlock_with_recovery_code_and_rotate_it()
    {
        var (t, enc, data, oldCode) = Setup();
        using (t)
        {
            var r = _controller.ChangePassword(enc, oldCode.ToLowerInvariant(), "brand-new-pass", newRecoveryCode: true);
            Assert.True(r.Success, r.Message);
            Assert.NotNull(r.RecoveryCode);
            Assert.NotEqual(oldCode, r.RecoveryCode);

            Assert.False(_controller.CheckSecret(enc, oldCode).Success);
            Assert.True(_controller.CheckSecret(enc, r.RecoveryCode!).Success);
            Assert.True(_controller.Verify(enc, "brand-new-pass").Success);
        }
    }

    [Fact]
    public void Wrong_current_secret_leaves_file_untouched()
    {
        var (t, enc, _, _) = Setup();
        using (t)
        {
            byte[] before = File.ReadAllBytes(enc);
            Assert.False(_controller.ChangePassword(enc, "nope", "whatever-123", false).Success);
            Assert.Equal(before, File.ReadAllBytes(enc));
        }
    }

    [Fact]
    public void Rejects_short_new_password_and_legacy_files()
    {
        var (t, enc, _, _) = Setup();
        using (t)
        {
            Assert.False(_controller.ChangePassword(enc, "old-password", "short", false).Success);

            string src = t.WriteFile("v2.txt", "x"u8.ToArray());
            string v2 = t.Path_("v2.enc");
            LegacyFormats.EncryptV2ForTests(src, v2, "pw");
            var r = _controller.ChangePassword(v2, "pw", "long-enough-1", false);
            Assert.False(r.Success);
            Assert.Contains("định dạng cũ", r.Message);
        }
    }
}
