using System.Collections.Concurrent;
using System.IO;

namespace HEICAutoConverter.Core;

public class Logger : IDisposable
{
    private readonly string _logFilePath;
    private readonly BlockingCollection<string> _logQueue = new(1000);
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Action<string>> _listeners = new();
    private readonly object _listenerLock = new();

    public Logger()
    {
        var logDir = Path.Combine(Settings.AppData, "logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"heicconverter_{DateTime.Now:yyyyMMdd}.log");

        _writerTask = Task.Run(WriteLoop);
    }

    public void AddListener(Action<string> listener)
    {
        lock (_listenerLock) _listeners.Add(listener);
    }

    public void RemoveListener(Action<string> listener)
    {
        lock (_listenerLock) _listeners.Remove(listener);
    }

    public void Log(string message)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        _logQueue.TryAdd(entry);

        lock (_listenerLock)
        {
            foreach (var listener in _listeners)
            {
                try { listener(entry); } catch { }
            }
        }
    }

    private async Task WriteLoop()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_logQueue.TryTake(out var entry, 1000, _cts.Token))
                {
                    await File.AppendAllTextAsync(_logFilePath, entry + Environment.NewLine, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void CleanOldLogs(int keepDays = 30)
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logFilePath)!;
            var cutoff = DateTime.Now.AddDays(-keepDays);
            foreach (var file in Directory.GetFiles(logDir, "heicconverter_*.log"))
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch { }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _logQueue.CompleteAdding();
        try { _writerTask.Wait(TimeSpan.FromSeconds(3)); } catch { }
        _cts.Dispose();
        _logQueue.Dispose();
    }
}
