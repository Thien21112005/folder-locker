using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderLocker.Services
{
    /// <summary>
    /// Chỉ chạy một cửa sổ cho mỗi phiên đăng nhập. Khi Explorer mở nhiều file cùng lúc
    /// (mỗi file một tiến trình), các tiến trình phụ gửi đường dẫn sang cửa sổ chính qua named pipe rồi thoát.
    /// </summary>
    public sealed class SingleInstance : IDisposable
    {
        private readonly Mutex _mutex;
        private readonly string _pipeName;
        private readonly CancellationTokenSource _cts = new();

        public bool IsPrimary { get; }

        public SingleInstance(string appId)
        {
            string scope = $"{appId}.{Environment.UserName}.{System.Diagnostics.Process.GetCurrentProcess().SessionId}";
            _pipeName = scope + ".open";
            _mutex = new Mutex(initiallyOwned: true, @"Local\" + scope, out bool created);
            IsPrimary = created;
        }

        /// <summary>Tiến trình phụ: gửi đường dẫn cho cửa sổ chính. Trả về false nếu không liên lạc được.</summary>
        public bool ForwardToPrimary(IEnumerable<string> paths, int timeoutMs = 3000)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                client.Connect(timeoutMs);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                foreach (var p in paths) writer.WriteLine(p);
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException)
            {
                return false;
            }
        }

        /// <summary>Cửa sổ chính: lắng nghe và gọi <paramref name="onPaths"/> (trên thread nền) cho mỗi lần nhận.</summary>
        public void Listen(Action<string[]> onPaths)
        {
            var token = _cts.Token;
            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                        await server.WaitForConnectionAsync(token);
                        using var reader = new StreamReader(server);
                        var lines = new List<string>();
                        while (await reader.ReadLineAsync(token) is { } line)
                            if (line.Length > 0) lines.Add(line);
                        // Danh sách rỗng = chỉ yêu cầu đưa cửa sổ lên trước
                        onPaths(lines.ToArray());
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (IOException)
                    {
                        // Client ngắt giữa chừng → chờ kết nối tiếp
                    }
                }
            }, token);
        }

        public void Dispose()
        {
            _cts.Cancel();
            if (IsPrimary)
            {
                try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
            }
            _mutex.Dispose();
            _cts.Dispose();
        }
    }
}
