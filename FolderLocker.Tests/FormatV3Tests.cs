using FolderLocker.Controllers;
using FolderLocker.Models;
using FolderLocker.Models.Crypto;

namespace FolderLocker.Tests;

public class FormatV3Tests
{
    private const int Chunk = 1024 * 1024;
    private readonly LockerController _controller = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(Chunk - 1)]
    [InlineData(Chunk)]          // đúng 1 chunk → cần chunk cuối rỗng
    [InlineData(2 * Chunk)]
    [InlineData(2 * Chunk + 777)]
    public void File_round_trips(int size)
    {
        using var t = new TempDir();
        byte[] data = TempDir.RandomBytes(size);
        string src = t.WriteFile("data.bin", data);
        string enc = t.Path_("data.bin.enc");

        var r = _controller.Encrypt(src, enc, "mật khẩu 🔐");
        Assert.True(r.Success, r.Message);
        Assert.True(RecoveryCode.IsWellFormed(r.RecoveryCode!));
        Assert.Equal(EncryptedFormat.V3, LockerModel.DetectFormat(enc));

        string outDir = t.NewDir("out");
        var d = _controller.Decrypt(enc, outDir, "mật khẩu 🔐");
        Assert.True(d.Success, d.Message);
        Assert.Equal(Path.Combine(outDir, "data.bin"), d.OutputPath);
        Assert.Equal(data, File.ReadAllBytes(d.OutputPath!));
    }

    [Fact]
    public void Folder_round_trips_with_empty_dirs_and_original_name()
    {
        using var t = new TempDir();
        string src = t.NewDir("Ảnh gia đình");
        t.NewDir("Ảnh gia đình", "2024", "trống");
        t.NewDir("Ảnh gia đình", "rỗng");
        byte[] big = TempDir.RandomBytes(Chunk + 5);
        t.WriteFile(Path.Combine("Ảnh gia đình", "2024", "a.jpg"), big);
        t.WriteFile(Path.Combine("Ảnh gia đình", "note.txt"), "xin chào"u8.ToArray());

        string enc = t.Path_("renamed-by-user.enc");
        Assert.True(_controller.Encrypt(src, enc, "pw").Success);

        var d = _controller.Decrypt(enc, t.NewDir("out"), "pw");
        Assert.True(d.Success, d.Message);
        Assert.Equal("Ảnh gia đình", Path.GetFileName(d.OutputPath));
        Assert.Equal(big, File.ReadAllBytes(Path.Combine(d.OutputPath!, "2024", "a.jpg")));
        Assert.Equal("xin chào", File.ReadAllText(Path.Combine(d.OutputPath!, "note.txt")));
        Assert.True(Directory.Exists(Path.Combine(d.OutputPath!, "2024", "trống")));
        Assert.True(Directory.Exists(Path.Combine(d.OutputPath!, "rỗng")));
    }

    [Fact]
    public void Wrong_password_fails_without_output()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "secret"u8.ToArray());
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "right");

        string outDir = t.NewDir("out");
        var d = _controller.Decrypt(enc, outDir, "wrong");
        Assert.False(d.Success);
        Assert.Contains("Sai mật khẩu", d.Message);
        Assert.Empty(Directory.GetFileSystemEntries(outDir));
    }

    [Theory]
    [InlineData("{0}")]
    [InlineData("  {0}  ")]
    [InlineData("lower")]
    [InlineData("nodash")]
    public void Recovery_code_unlocks_in_any_reasonable_format(string shape)
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "data"u8.ToArray());
        string enc = t.Path_("a.enc");
        string code = _controller.Encrypt(src, enc, "pw").RecoveryCode!;

        string input = shape switch
        {
            "lower" => code.ToLowerInvariant(),
            "nodash" => code.Replace("-", " "),
            _ => string.Format(shape, code),
        };

        var d = _controller.Decrypt(enc, t.NewDir("out"), input);
        Assert.True(d.Success, d.Message);
    }

    [Fact]
    public void Password_is_not_normalized()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "data"u8.ToArray());
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "PassWord");

        Assert.False(_controller.Decrypt(enc, t.NewDir("out"), "password").Success);
    }

    [Theory]
    [InlineData(6)]      // chunk size trong header
    [InlineData(12)]     // nonce prefix trong header
    [InlineData(30)]     // salt của slot mật khẩu
    [InlineData(400)]    // giữa chunk dữ liệu đầu tiên
    [InlineData(-1)]     // tag của chunk cuối
    public void Any_tampered_byte_is_rejected_without_output(int position)
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.bin", TempDir.RandomBytes(Chunk + 1000));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "pw");

        byte[] bytes = File.ReadAllBytes(enc);
        int i = position >= 0 ? position : bytes.Length + position;
        bytes[i] ^= 0x01;
        File.WriteAllBytes(enc, bytes);

        string outDir = t.NewDir("out");
        var d = _controller.Decrypt(enc, outDir, "pw");
        Assert.False(d.Success);
        Assert.Empty(Directory.GetFileSystemEntries(outDir));
    }

    [Theory]
    [InlineData(1)]                  // cắt 1 byte cuối
    [InlineData(1000 + 16)]          // cắt mất nguyên chunk cuối
    public void Truncated_file_is_rejected(int cut)
    {
        using var t = new TempDir();
        // payload = tên (4 + 5 byte) + thời gian (16) + dữ liệu; chọn kích thước để chunk cuối đúng 1000 byte plaintext
        string src = t.WriteFile("a.bin", TempDir.RandomBytes(Chunk + 1000 - 9 - 16));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "pw");

        byte[] bytes = File.ReadAllBytes(enc);
        File.WriteAllBytes(enc, bytes[..^cut]);

        string outDir = t.NewDir("out");
        Assert.False(_controller.Decrypt(enc, outDir, "pw").Success);
        Assert.Empty(Directory.GetFileSystemEntries(outDir));
    }

    [Fact]
    public void Reordered_chunks_are_rejected()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.bin", TempDir.RandomBytes(3 * Chunk));
        string enc = t.Path_("a.enc");
        _controller.Encrypt(src, enc, "pw");

        byte[] bytes = File.ReadAllBytes(enc);
        const int dataStart = 14 + 1 + 2 * 81;
        const int block = Chunk + AesGcmChunk.TagSize;
        byte[] first = bytes[dataStart..(dataStart + block)];
        Array.Copy(bytes, dataStart + block, bytes, dataStart, block);
        first.CopyTo(bytes, dataStart + block);
        File.WriteAllBytes(enc, bytes);

        Assert.False(_controller.Decrypt(enc, t.NewDir("out"), "pw").Success);
    }

    [Fact]
    public void Each_encryption_uses_fresh_salt_and_key()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "same"u8.ToArray());
        _controller.Encrypt(src, t.Path_("1.enc"), "pw");
        _controller.Encrypt(src, t.Path_("2.enc"), "pw");
        Assert.NotEqual(File.ReadAllBytes(t.Path_("1.enc")), File.ReadAllBytes(t.Path_("2.enc")));
    }

    [Fact]
    public void Default_kdf_iterations_follow_owasp()
    {
        Assert.Equal(600_000, KeyDerivation.DefaultIterations);
    }
}
