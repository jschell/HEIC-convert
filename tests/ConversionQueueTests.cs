using HEICAutoConverter.Core;

namespace HEICAutoConverter.Tests;

public class ConversionQueueTests : IDisposable
{
    private readonly string _tempDir;
    private readonly List<string> _logs = new();

    public ConversionQueueTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HEICTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private ConversionQueue CreateQueue(Settings? settings = null, int concurrency = 1)
    {
        settings ??= new Settings
        {
            WatchFolders = new List<string> { _tempDir },
            SkipExisting = false,
            OriginalFileAction = OriginalFileAction.Keep
        };
        var engine = new ConversionEngine(settings, msg => _logs.Add(msg));
        return new ConversionQueue(engine, concurrency, msg => _logs.Add(msg));
    }

    [Fact]
    public void Enqueue_IncrementsCount()
    {
        using var queue = CreateQueue();

        queue.Enqueue(Path.Combine(_tempDir, "file1.heic"));

        Assert.Equal(1, queue.PendingCount);
    }

    [Fact]
    public void Enqueue_DuplicatePath_IgnoresSecond()
    {
        using var queue = CreateQueue();
        var path = Path.Combine(_tempDir, "file1.heic");

        queue.Enqueue(path);
        queue.Enqueue(path);

        Assert.Equal(1, queue.PendingCount);
    }

    [Fact]
    public void Enqueue_DuplicatePath_CaseInsensitive()
    {
        using var queue = CreateQueue();

        queue.Enqueue(Path.Combine(_tempDir, "FILE.HEIC"));
        queue.Enqueue(Path.Combine(_tempDir, "file.heic"));

        Assert.Equal(1, queue.PendingCount);
    }

    [Fact]
    public void EnqueueBatch_AddsAllFiles()
    {
        using var queue = CreateQueue();
        var files = Enumerable.Range(1, 5)
            .Select(i => Path.Combine(_tempDir, $"file{i}.heic"))
            .ToList();

        queue.EnqueueBatch(files);

        Assert.Equal(5, queue.PendingCount);
    }

    [Fact]
    public void InitialState_NotPaused()
    {
        using var queue = CreateQueue();

        Assert.False(queue.IsPaused);
        Assert.Equal(0, queue.TotalProcessed);
        Assert.Equal(0, queue.TotalFailed);
    }

    [Fact]
    public void Pause_SetsIsPaused()
    {
        using var queue = CreateQueue();

        queue.Pause();

        Assert.True(queue.IsPaused);
    }

    [Fact]
    public void PauseAndResume_TogglesState()
    {
        using var queue = CreateQueue();

        queue.Pause();
        Assert.True(queue.IsPaused);

        queue.Resume();
        Assert.False(queue.IsPaused);
    }

    [Fact]
    public async Task QueueCountChanged_FiresOnEnqueue()
    {
        using var queue = CreateQueue();
        var counts = new List<int>();
        queue.QueueCountChanged += count => counts.Add(count);

        queue.Enqueue(Path.Combine(_tempDir, "file1.heic"));
        queue.Enqueue(Path.Combine(_tempDir, "file2.heic"));

        // Give queue a moment to fire events
        await Task.Delay(100);

        Assert.Equal(2, counts.Count);
        Assert.Equal(1, counts[0]);
        Assert.Equal(2, counts[1]);
    }

    [Fact]
    public async Task ConversionCompleted_FiresAfterProcessing()
    {
        using var queue = CreateQueue();
        var results = new List<ConversionResult>();
        queue.ConversionCompleted += result => results.Add(result);

        // Create a file that will fail conversion (not valid HEIC)
        var filePath = Path.Combine(_tempDir, "test.heic");
        File.WriteAllText(filePath, "not valid heic");

        queue.Enqueue(filePath);

        // Wait for processing (debounce is 500ms + processing)
        await Task.Delay(3000);

        Assert.Single(results);
        Assert.False(results[0].Success); // Invalid file should fail
    }

    [Fact]
    public async Task NonExistentFile_SkippedGracefully()
    {
        using var queue = CreateQueue();
        var results = new List<ConversionResult>();
        queue.ConversionCompleted += result => results.Add(result);

        var filePath = Path.Combine(_tempDir, "ghost.heic");
        queue.Enqueue(filePath);

        // Wait for processing
        await Task.Delay(2000);

        // File doesn't exist, so it should be skipped (no result fired)
        Assert.Empty(results);
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public async Task Dispose_StopsProcessing()
    {
        var queue = CreateQueue();

        // Enqueue many files
        for (int i = 0; i < 100; i++)
            queue.Enqueue(Path.Combine(_tempDir, $"file{i}.heic"));

        // Dispose should complete within timeout
        var disposeTask = Task.Run(() => queue.Dispose());
        var completed = await Task.WhenAny(disposeTask, Task.Delay(10000));

        Assert.Equal(disposeTask, completed);
    }
}
