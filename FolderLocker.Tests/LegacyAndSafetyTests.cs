using System.Security.Cryptography;
using System.Text;
using FolderLocker.Controllers;
using FolderLocker.Models;
using FolderLocker.Models.Formats;

namespace FolderLocker.Tests;

public class LegacyFormatTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void V2_file_opens_with_password_and_recovery_code()
    {
        using var t = new TempDir();
        byte[] data = TempDir.RandomBytes(5 * 1024 * 1024 + 3);
        string src = t.WriteFile("old.bin", data);
        string enc = t.Path_("old.bin.enc");
        string code = LegacyFormats.EncryptV2ForTests(src, enc, "pw");

        Assert.Equal(EncryptedFormat.V2, LockerModel.DetectFormat(enc));

        var byPass = _controller.Decrypt(enc, t.NewDir("a"), "pw");
        Assert.True(byPass.Success, byPass.Message);
        Assert.Equal(data, File.ReadAllBytes(byPass.OutputPath!));

        var byCode = _controller.Decrypt(enc, t.NewDir("b"), " " + code.ToLowerInvariant());
        Assert.True(byCode.Success, byCode.Message);
    }

    [Fact]
    public void V2_folder_opens_using_enc_file_name()
    {
        using var t = new TempDir();
        string src = t.NewDir("src");
        t.WriteFile(Path.Combine("src", "sub", "x.txt"), "x"u8.ToArray());
        string enc = t.Path_("Docs.enc");
        LegacyFormats.EncryptV2ForTests(src, enc, "pw");

        var d = _controller.Decrypt(enc, t.NewDir("out"), "pw");
        Assert.True(d.Success, d.Message);
        Assert.Equal("Docs", Path.GetFileName(d.OutputPath));
        Assert.True(File.Exists(Path.Combine(d.OutputPath!, "sub", "x.txt")));
    }

    [Fact]
    public void V1_file_opens_and_wrong_password_is_reported()
    {
        using var t = new TempDir();
        string enc = WriteV1(t, "v1.enc", "hello.txt", "xin chào V1"u8.ToArray(), "pw");

        Assert.Equal(EncryptedFormat.V1, LockerModel.DetectFormat(enc));

        var ok = _controller.Decrypt(enc, t.NewDir("a"), "pw");
        Assert.True(ok.Success, ok.Message);
        Assert.Equal("xin chào V1", File.ReadAllText(ok.OutputPath!));

        string outB = t.NewDir("b");
        Assert.False(_controller.Decrypt(enc, outB, "nope").Success);
        Assert.Empty(Directory.GetFileSystemEntries(outB));
    }

    [Theory]
    [InlineData(@"..\..\evil.txt")]
    [InlineData(@"C:\Windows\evil.txt")]
    [InlineData("../evil.txt")]
    public void Malicious_names_in_payload_cannot_escape_output_folder(string name)
    {
        using var t = new TempDir();
        string enc = WriteV1(t, "bad.enc", name, "x"u8.ToArray(), "pw");
        string outDir = t.NewDir("deep", "out");

        var d = _controller.Decrypt(enc, outDir, "pw");
        Assert.True(d.Success, d.Message);
        Assert.Equal(Path.Combine(outDir, "evil.txt"), d.OutputPath);
        Assert.False(File.Exists(t.Path_("evil.txt")));
        Assert.False(File.Exists(t.Path_("deep", "evil.txt")));
    }

    /// <summary>Tạo file V1: [isFolder=0][IV][AES-CBC(SHA256(pass))(nameLen|name|data)].</summary>
    private static string WriteV1(TempDir t, string encName, string originalName, byte[] data, string password)
    {
        byte[] name = Encoding.UTF8.GetBytes(originalName);
        byte[] payload = [.. BitConverter.GetBytes(name.Length), .. name, .. data];

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        aes.GenerateIV();
        byte[] cipher = aes.EncryptCbc(payload, aes.IV);
        return t.WriteFile(encName, [0, .. aes.IV, .. cipher]);
    }
}

public class ControllerSafetyTests
{
    private readonly LockerController _controller = new();

    [Fact]
    public void Rejects_saving_enc_inside_source_folder()
    {
        using var t = new TempDir();
        string src = t.NewDir("folder");
        t.WriteFile(Path.Combine("folder", "a.txt"), "a"u8.ToArray());

        var r = _controller.Encrypt(src, Path.Combine(src, "folder.enc"), "pw");
        Assert.False(r.Success);
        Assert.False(File.Exists(Path.Combine(src, "folder.enc")));
    }

    [Fact]
    public void Overwrite_requires_confirmation_then_replaces_folder()
    {
        using var t = new TempDir();
        string src = t.NewDir("data");
        t.WriteFile(Path.Combine("data", "new.txt"), "new"u8.ToArray());
        string enc = t.Path_("data.enc");
        _controller.Encrypt(src, enc, "pw");

        string outDir = t.NewDir("out");
        t.WriteFile(Path.Combine("out", "data", "stale.txt"), "old"u8.ToArray());

        var first = _controller.Decrypt(enc, outDir, "pw");
        Assert.True(first.NeedsOverwriteConfirm);
        Assert.True(File.Exists(Path.Combine(outDir, "data", "stale.txt")));

        var second = _controller.Decrypt(enc, outDir, "pw", checkOverwrite: false);
        Assert.True(second.Success, second.Message);
        Assert.True(File.Exists(Path.Combine(outDir, "data", "new.txt")));
        Assert.False(File.Exists(Path.Combine(outDir, "data", "stale.txt")));
        Assert.Single(Directory.GetFileSystemEntries(outDir)); // không còn thư mục tạm/backup
    }

    [Fact]
    public void Failed_encryption_leaves_no_partial_file()
    {
        using var t = new TempDir();
        string src = t.WriteFile("locked.bin", TempDir.RandomBytes(10));
        string enc = t.Path_("locked.enc");

        using (new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.None)) // khóa file nguồn
        {
            Assert.False(_controller.Encrypt(src, enc, "pw").Success);
        }
        Assert.Equal(["locked.bin"], Directory.GetFileSystemEntries(t.Root).Select(Path.GetFileName));
    }

    [Fact]
    public void Detects_format_by_header_not_extension()
    {
        using var t = new TempDir();
        string fake = t.WriteFile("fake.enc", "just text"u8.ToArray());
        string empty = t.WriteFile("empty.enc", []);
        Assert.Equal(EncryptedFormat.None, LockerModel.DetectFormat(fake));
        Assert.Equal(EncryptedFormat.None, LockerModel.DetectFormat(empty));
        Assert.False(_controller.Decrypt(fake, t.NewDir("o"), "pw").Success);

        string src = t.WriteFile("a.txt", "a"u8.ToArray());
        string noExt = t.Path_("a.locked");
        _controller.Encrypt(src, noExt, "pw");
        Assert.True(_controller.IsEncryptedFile(noExt));
    }
}

public class HelperTests
{
    [Theory]
    [InlineData("fl-abcd-efgh-ijkl", "FL-ABCD-EFGH-IJKL")]
    [InlineData(" FL ABCD EFGH IJKL ", "FL-ABCD-EFGH-IJKL")]
    [InlineData("abcdefghijkl", "FL-ABCD-EFGH-IJKL")]
    [InlineData("my password", "MY PASSWORD")]
    public void Normalizes_recovery_codes(string input, string expected)
    {
        Assert.Equal(expected, RecoveryCode.Normalize(input));
    }

    [Fact]
    public void Generated_codes_are_well_formed_and_unique()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => RecoveryCode.Generate()).ToList();
        Assert.All(codes, c => Assert.True(RecoveryCode.IsWellFormed(c), c));
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Theory]
    [InlineData("a.txt", "a.txt")]
    [InlineData(@"..\x.txt", "x.txt")]
    [InlineData("dir/x.txt", "x.txt")]
    [InlineData("..", null)]
    [InlineData("   ", null)]
    [InlineData("a|b.txt", null)]
    public void Sanitizes_file_names(string input, string? expected)
    {
        Assert.Equal(expected, PathSafety.SafeFileName(input));
    }

    [Fact]
    public void IsInside_matches_nested_paths_only()
    {
        Assert.True(PathSafety.IsInside(@"C:\a\b\c.enc", @"C:\a\b"));
        Assert.True(PathSafety.IsInside(@"C:\a\b", @"C:\a\b\"));
        Assert.False(PathSafety.IsInside(@"C:\a\bc.enc", @"C:\a\b"));
    }
}
