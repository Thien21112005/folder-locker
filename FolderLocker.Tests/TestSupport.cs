using System.Runtime.CompilerServices;
using FolderLocker.Models.Formats;

namespace FolderLocker.Tests;

internal static class TestSetup
{
    /// <summary>Giảm số vòng PBKDF2 cho test chạy nhanh; số vòng được lưu trong file nên vẫn giải mã đúng.</summary>
    [ModuleInitializer]
    internal static void Init() => FormatV3.Iterations = 1_000;
}

/// <summary>Thư mục tạm tự xóa sau mỗi test.</summary>
internal sealed class TempDir : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "fl-tests-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Root);

    public string Path_(params string[] parts) => Path.Combine([Root, .. parts]);

    public string NewDir(params string[] parts)
    {
        string p = Path_(parts);
        Directory.CreateDirectory(p);
        return p;
    }

    public string WriteFile(string name, byte[] content)
    {
        string p = Path_(name);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllBytes(p, content);
        return p;
    }

    public static byte[] RandomBytes(int length)
    {
        var data = new byte[length];
        Random.Shared.NextBytes(data);
        return data;
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, true); } catch { /* best effort */ }
    }
}
