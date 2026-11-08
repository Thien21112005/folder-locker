using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FolderLocker.Controllers;
using FolderLocker.Models;

namespace FolderLocker.Cli
{
    /// <summary>
    /// Giao diện dòng lệnh cho script / sao lưu tự động.
    /// Mật khẩu KHÔNG BAO GIỜ nhận qua tham số (lộ trong lịch sử shell và danh sách tiến trình):
    /// chỉ đọc từ biến môi trường FOLDERLOCKER_PASSWORD hoặc dòng đầu của stdin.
    /// </summary>
    public sealed class CliRunner
    {
        public const string PasswordEnv = "FOLDERLOCKER_PASSWORD";

        public const int ExitOk = 0;
        public const int ExitFailed = 1;
        public const int ExitUsage = 2;

        private static readonly string[] Commands = { "encrypt", "decrypt", "verify", "help", "--help", "-h", "/?" };

        private readonly TextReader _stdin;
        private readonly bool _stdinRedirected;
        private readonly TextWriter _out;
        private readonly TextWriter _err;
        private readonly Func<string, string?> _env;
        private readonly LockerController _controller = new();

        public CliRunner(TextReader stdin, bool stdinRedirected, TextWriter stdout, TextWriter stderr, Func<string, string?>? env = null)
        {
            _stdin = stdin;
            _stdinRedirected = stdinRedirected;
            _out = stdout;
            _err = stderr;
            _env = env ?? Environment.GetEnvironmentVariable;
        }

        public static bool IsCommand(string arg) => Commands.Contains(arg, StringComparer.OrdinalIgnoreCase);

        public int Run(string[] args)
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h" or "/?")
            {
                PrintHelp();
                return args.Length == 0 ? ExitUsage : ExitOk;
            }

            if (!TryParse(args.Skip(1).ToArray(), out var inputs, out var options, out string? error))
                return Usage(error!);

            return args[0].ToLowerInvariant() switch
            {
                "encrypt" => Encrypt(inputs, options),
                "decrypt" => Decrypt(inputs, options),
                "verify" => Verify(inputs),
                _ => Usage($"Lệnh không hợp lệ: {args[0]}"),
            };
        }

        private sealed record Options(string? Out, bool Overwrite, bool DeleteOriginal);

        private static bool TryParse(string[] args, out List<string> inputs, out Options options, out string? error)
        {
            inputs = new List<string>();
            string? output = null;
            bool overwrite = false, delete = false;
            error = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--out" or "-o":
                        if (i + 1 >= args.Length) { error = "Thiếu giá trị sau --out."; break; }
                        output = args[++i];
                        break;
                    case "--overwrite":
                        overwrite = true;
                        break;
                    case "--delete-original":
                        delete = true;
                        break;
                    case "--password" or "-p":
                        error = "Không nhận mật khẩu qua tham số. Dùng stdin hoặc biến môi trường " + PasswordEnv + ".";
                        break;
                    default:
                        if (args[i].StartsWith("--")) error = $"Tùy chọn không hợp lệ: {args[i]}";
                        else inputs.Add(args[i]);
                        break;
                }
                if (error != null) break;
            }

            if (error == null && inputs.Count == 0) error = "Chưa chỉ định file/thư mục.";
            options = new Options(output, overwrite, delete);
            return error == null;
        }

        private string? ReadPassword()
        {
            string? fromEnv = _env(PasswordEnv);
            if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
            if (!_stdinRedirected) return null;
            string? line = _stdin.ReadLine();
            return string.IsNullOrEmpty(line) ? null : line;
        }

        private int Encrypt(List<string> inputs, Options o)
        {
            var missing = inputs.Where(p => !File.Exists(p) && !Directory.Exists(p)).ToList();
            if (missing.Count > 0) return Usage("Không tìm thấy: " + string.Join(", ", missing));

            bool outIsFolder = o.Out != null && (inputs.Count > 1 || Directory.Exists(o.Out));
            if (inputs.Count > 1 && o.Out != null && !Directory.Exists(o.Out))
                return Usage("Khi mã hóa nhiều mục, --out phải là thư mục đã tồn tại.");

            string? password = ReadPassword();
            if (password == null) return NoPassword();
            if (PasswordStrength.Evaluate(password) is StrengthLevel.TooShort)
                return Usage($"Mật khẩu cần tối thiểu {PasswordStrength.MinLength} ký tự.");

            int failures = 0;
            foreach (var src in inputs.Select(Path.GetFullPath))
            {
                string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(src)) + ".enc";
                string dest = o.Out == null ? Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(src)) ?? ".", name)
                    : outIsFolder ? Path.Combine(o.Out, name)
                    : o.Out;

                if (File.Exists(dest) && !o.Overwrite)
                {
                    _err.WriteLine($"Bỏ qua (đã tồn tại, thêm --overwrite để ghi đè): {dest}");
                    failures++;
                    continue;
                }

                var r = _controller.Encrypt(src, dest, password);
                if (!r.Success)
                {
                    _err.WriteLine($"Lỗi: {src}: {r.Message}");
                    failures++;
                    continue;
                }

                // stdout chỉ chứa dữ liệu máy đọc được: <mã khôi phục><TAB><file .enc>
                _out.WriteLine($"{r.RecoveryCode}\t{dest}");

                if (o.DeleteOriginal)
                {
                    var v = _controller.Verify(dest, password);
                    var d = v.Success ? _controller.DeleteSource(src) : v;
                    if (!d.Success)
                    {
                        _err.WriteLine($"Giữ nguyên bản gốc {src}: {d.Message}");
                        failures++;
                    }
                }
            }
            return failures == 0 ? ExitOk : ExitFailed;
        }

        private int Decrypt(List<string> inputs, Options o)
        {
            var missing = inputs.Where(p => !File.Exists(p)).ToList();
            if (missing.Count > 0) return Usage("Không tìm thấy: " + string.Join(", ", missing));
            if (o.Out != null && !Directory.Exists(o.Out)) return Usage("--out phải là thư mục đã tồn tại.");

            string? secret = ReadPassword();
            if (secret == null) return NoPassword();

            int failures = 0;
            foreach (var enc in inputs.Select(Path.GetFullPath))
            {
                string outDir = Path.GetFullPath(o.Out ?? Path.GetDirectoryName(enc) ?? ".");
                var r = _controller.Decrypt(enc, outDir, secret, checkOverwrite: !o.Overwrite);
                if (r.NeedsOverwriteConfirm)
                {
                    _err.WriteLine($"Bỏ qua (đích đã tồn tại, thêm --overwrite để ghi đè): {r.OutputPath}");
                    failures++;
                }
                else if (!r.Success)
                {
                    _err.WriteLine($"Lỗi: {enc}: {r.Message}");
                    failures++;
                }
                else
                {
                    _out.WriteLine(r.OutputPath);
                }
            }
            return failures == 0 ? ExitOk : ExitFailed;
        }

        private int Verify(List<string> inputs)
        {
            string? secret = ReadPassword();
            if (secret == null) return NoPassword();

            int failures = 0;
            foreach (var enc in inputs.Select(Path.GetFullPath))
            {
                var r = File.Exists(enc) ? _controller.Verify(enc, secret) : OperationResult.Fail("Không tìm thấy file.");
                _out.WriteLine($"{(r.Success ? "OK" : "FAIL")}\t{enc}");
                if (!r.Success)
                {
                    _err.WriteLine($"{enc}: {r.Message}");
                    failures++;
                }
            }
            return failures == 0 ? ExitOk : ExitFailed;
        }

        private int NoPassword()
        {
            _err.WriteLine($"Chưa có mật khẩu. Truyền qua stdin (ví dụ: echo matkhau | FolderLocker.exe ...) hoặc đặt biến môi trường {PasswordEnv}.");
            return ExitUsage;
        }

        private int Usage(string message)
        {
            _err.WriteLine(message);
            _err.WriteLine("Gõ \"FolderLocker.exe help\" để xem hướng dẫn.");
            return ExitUsage;
        }

        private void PrintHelp()
        {
            _out.WriteLine("""
                Folder Locker — dòng lệnh

                  FolderLocker.exe encrypt <file|thư mục>... [--out <file.enc|thư mục>] [--overwrite] [--delete-original]
                  FolderLocker.exe decrypt <file.enc>... [--out <thư mục>] [--overwrite]
                  FolderLocker.exe verify  <file.enc>...
                  FolderLocker.exe <file|thư mục>...        Mở giao diện với các mục được chọn sẵn

                Mật khẩu (hoặc mã khôi phục khi giải mã) đọc từ biến môi trường FOLDERLOCKER_PASSWORD
                hoặc dòng đầu tiên của stdin. Không bao giờ truyền mật khẩu qua tham số.

                encrypt in ra mỗi dòng: <mã khôi phục><TAB><file .enc>. Hãy lưu mã ở nơi an toàn.
                --delete-original chỉ xóa bản gốc (vào Thùng rác) sau khi file .enc đã được kiểm tra lại.

                Mã thoát: 0 = thành công, 1 = có mục thất bại, 2 = sai cú pháp / thiếu mật khẩu.

                Ví dụ (PowerShell):
                  "mat-khau-cua-ban" | .\FolderLocker.exe encrypt "D:\Tai lieu" --out E:\Backup
                  $env:FOLDERLOCKER_PASSWORD = "mat-khau-cua-ban"; .\FolderLocker.exe decrypt E:\Backup\*.enc --out D:\Khoi phuc
                """);
        }
    }
}
