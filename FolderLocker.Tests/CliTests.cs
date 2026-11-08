using FolderLocker.Cli;

namespace FolderLocker.Tests;

public class CliTests
{
    private static (int code, string stdout, string stderr) Run(string[] args, string? stdin = null, string? envPassword = null)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        var runner = new CliRunner(new StringReader(stdin ?? ""), stdin != null, o, e,
            name => name == CliRunner.PasswordEnv ? envPassword : null);
        return (runner.Run(args), o.ToString(), e.ToString());
    }

    [Fact]
    public void Encrypt_then_decrypt_round_trip_via_stdin_and_env()
    {
        using var t = new TempDir();
        string src = t.WriteFile("note.txt", "xin chào"u8.ToArray());

        var enc = Run(["encrypt", src], stdin: "password-123\n");
        Assert.Equal(CliRunner.ExitOk, enc.code);
        var parts = enc.stdout.Trim().Split('\t');
        Assert.Matches("^FL-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", parts[0]);
        Assert.Equal(t.Path_("note.txt.enc"), parts[1]);

        string outDir = t.NewDir("out");
        var dec = Run(["decrypt", parts[1], "--out", outDir], envPassword: "password-123");
        Assert.Equal(CliRunner.ExitOk, dec.code);
        Assert.Equal("xin chào", File.ReadAllText(Path.Combine(outDir, "note.txt")));

        // Mã khôi phục cũng dùng được; đích đã tồn tại thì không ghi đè nếu thiếu --overwrite
        var again = Run(["decrypt", parts[1], "--out", outDir], stdin: parts[0] + "\n");
        Assert.Equal(CliRunner.ExitFailed, again.code);
        Assert.Contains("--overwrite", again.stderr);
        Assert.Equal(CliRunner.ExitOk, Run(["decrypt", parts[1], "--out", outDir, "--overwrite"], stdin: parts[0]).code);

        Assert.Equal(CliRunner.ExitOk, Run(["verify", parts[1]], stdin: "password-123").code);
    }

    [Fact]
    public void Batch_encrypt_into_folder_and_delete_originals_after_verify()
    {
        using var t = new TempDir();
        string a = t.WriteFile(Path.Combine("src", "a.txt"), "a"u8.ToArray());
        string b = t.NewDir("src", "folder-b");
        t.WriteFile(Path.Combine("src", "folder-b", "x.txt"), "x"u8.ToArray());
        string outDir = t.NewDir("vault");

        var r = Run(["encrypt", a, b, "--out", outDir, "--delete-original"], stdin: "password-123");
        Assert.Equal(CliRunner.ExitOk, r.code);
        Assert.Equal(2, r.stdout.Trim().Split('\n').Length);
        Assert.True(File.Exists(Path.Combine(outDir, "a.txt.enc")));
        Assert.True(File.Exists(Path.Combine(outDir, "folder-b.enc")));
        Assert.False(File.Exists(a));
        Assert.False(Directory.Exists(b));
    }

    [Theory]
    [InlineData("encrypt")]                                  // thiếu đường dẫn
    [InlineData("encrypt|C:\\khong-ton-tai-xyz.txt")]
    [InlineData("encrypt|x|--password|abc")]                 // mật khẩu qua tham số bị từ chối
    [InlineData("decrypt|x|--bogus")]
    public void Usage_errors_exit_with_2(string joined)
    {
        Assert.Equal(CliRunner.ExitUsage, Run(joined.Split('|'), stdin: "password-123").code);
    }

    [Fact]
    public void Missing_or_short_password_is_rejected()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "a"u8.ToArray());
        var none = Run(["encrypt", src]);
        Assert.Equal(CliRunner.ExitUsage, none.code);
        Assert.Contains(CliRunner.PasswordEnv, none.stderr);

        Assert.Equal(CliRunner.ExitUsage, Run(["encrypt", src], stdin: "short").code);
        Assert.False(File.Exists(src + ".enc"));
    }

    [Fact]
    public void Wrong_password_exits_with_1()
    {
        using var t = new TempDir();
        string src = t.WriteFile("a.txt", "a"u8.ToArray());
        string enc = Run(["encrypt", src], stdin: "password-123").stdout.Trim().Split('\t')[1];
        var r = Run(["decrypt", enc, "--out", t.NewDir("o")], stdin: "wrong-password");
        Assert.Equal(CliRunner.ExitFailed, r.code);
        Assert.Contains("Sai mật khẩu", r.stderr);
    }

    [Fact]
    public void Help_lists_commands()
    {
        var r = Run(["help"]);
        Assert.Equal(CliRunner.ExitOk, r.code);
        Assert.Contains("encrypt", r.stdout);
        Assert.Contains(CliRunner.PasswordEnv, r.stdout);
        Assert.True(CliRunner.IsCommand("ENCRYPT"));
        Assert.False(CliRunner.IsCommand(@"C:\file.txt"));
    }
}
