using System.Collections.Concurrent;
using System.IO;
using System.Threading.Channels;

namespace HEICAutoConverter.Core;

public class ConversionQueue : IDisposable
{
    private readonly ConversionEngine _engine;
    private readonly Channel<string> _channel;
    private readonly ConcurrentDictionary<string, byte> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Task> _workers = new();
    private CancellationTokenSource _cts = new();
    private bool _isPaused;
    private readonly SemaphoreSlim _pauseLock = new(1, 1);
    private readonly Action<string> _log;
    private int _totalProcessed;
    private int _totalFailed;

    public event Action<ConversionResult>? ConversionCompleted;
    public event Action<int>? QueueCountChanged;

    public int PendingCount => _pendingFiles.Count;
    public int TotalProcessed => _totalProcessed;
    public int TotalFailed => _totalFailed;
    public bool IsPaused => _isPaused;

    public ConversionQueue(ConversionEngine engine, int maxConcurrency, Action<string> log)
    {
        _engine = engine;
        _log = log;
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

        // Start worker tasks
        for (int i = 0; i < maxConcurrency; i++)
        {
            _workers.Add(Task.Run(() => ProcessQueueAsync(_cts.Token)));
        }
    }

    public void Enqueue(string filePath)
    {
        if (_pendingFiles.TryAdd(filePath, 0))
        {
            _channel.Writer.TryWrite(filePath);
            QueueCountChanged?.Invoke(_pendingFiles.Count);
            _log($"Queued: {filePath}");
        }
    }

    public void EnqueueBatch(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            Enqueue(path);
        }
    }

    public void Pause()
    {
        _isPaused = true;
        _log("Conversion queue paused");
    }

    public void Resume()
    {
        _isPaused = false;
        _pauseLock.Release();
        _log("Conversion queue resumed");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        await foreach (var filePath in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // Wait if paused
                while (_isPaused && !cancellationToken.IsCancellationRequested)
                {
                    await _pauseLock.WaitAsync(cancellationToken);
                    if (!_isPaused) _pauseLock.Release();
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Debounce: wait briefly for file to settle
                await Task.Delay(500, cancellationToken);

                if (!File.Exists(filePath))
                {
                    _log($"File no longer exists, skipping: {filePath}");
                    continue;
                }

                var result = await _engine.ConvertAsync(filePath, cancellationToken);

                if (result.Success)
                    Interlocked.Increment(ref _totalProcessed);
                else
                    Interlocked.Increment(ref _totalFailed);

                ConversionCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Queue processing error for {filePath}: {ex.Message}");
                Interlocked.Increment(ref _totalFailed);
            }
            finally
            {
                _pendingFiles.TryRemove(filePath, out _);
                QueueCountChanged?.Invoke(_pendingFiles.Count);
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.Complete();

        try { Task.WhenAll(_workers).Wait(TimeSpan.FromSeconds(5)); }
        catch { }

        _cts.Dispose();
        _pauseLock.Dispose();
    }
}
