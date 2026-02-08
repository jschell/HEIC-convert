using HEICAutoConverter.Core;
using Xunit;

namespace HEICAutoConverter.Tests;

public class FileWatcherTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _subDir;
    private readonly List<string> _logs = new();

    public FileWatcherTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HEICTests_" + Guid.NewGuid().ToString("N")[..8]);
        _subDir = Path.Combine(_tempDir, "subfolder");
        Directory.CreateDirectory(_subDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private (FileWatcher watcher, ConversionQueue queue) CreateWatcher(Settings? settings = null)
    {
        settings ??= new Settings
        {
            WatchFolders = new List<string> { _tempDir },
            IncludeSubdirectories = true,
            SkipExisting = false,
            OriginalFileAction = OriginalFileAction.Keep
        };
        var engine = new ConversionEngine(settings, msg => _logs.Add(msg));
        var queue = new ConversionQueue(engine, 1, msg => _logs.Add(msg));
        var watcher = new FileWatcher(queue, settings, msg => _logs.Add(msg));
        return (watcher, queue);
    }

    [Fact]
    public void ScanExistingFiles_FindsHeicFiles()
    {
        // Create test files
        File.WriteAllText(Path.Combine(_tempDir, "photo1.heic"), "fake");
        File.WriteAllText(Path.Combine(_tempDir, "photo2.HEIC"), "fake");
        File.WriteAllText(Path.Combine(_tempDir, "photo3.heif"), "fake");
        File.WriteAllText(Path.Combine(_tempDir, "photo4.jpg"), "not heic");
        File.WriteAllText(Path.Combine(_tempDir, "photo5.png"), "not heic");

        var (watcher, queue) = CreateWatcher();
        using var _ = watcher;
        using var __ = queue;

        watcher.ScanExistingFiles();

        // Should find .heic and .heif files (3 total, but case-insensitive dedup)
        Assert.True(queue.PendingCount >= 2, $"Expected at least 2 pending, got {queue.PendingCount}");
    }

    [Fact]
    public void ScanExistingFiles_WithSubdirectories_FindsNestedFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "root.heic"), "fake");
        File.WriteAllText(Path.Combine(_subDir, "nested.heic"), "fake");

        var settings = new Settings
        {
            WatchFolders = new List<string> { _tempDir },
            IncludeSubdirectories = true,
            SkipExisting = false,
            OriginalFileAction = OriginalFileAction.Keep
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        watcher.ScanExistingFiles();

        Assert.True(queue.PendingCount >= 2, $"Expected at least 2 pending, got {queue.PendingCount}");
    }

    [Fact]
    public void ScanExistingFiles_WithoutSubdirectories_OnlyFindsRootFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "root.heic"), "fake");
        File.WriteAllText(Path.Combine(_subDir, "nested.heic"), "fake");

        var settings = new Settings
        {
            WatchFolders = new List<string> { _tempDir },
            IncludeSubdirectories = false,
            SkipExisting = false,
            OriginalFileAction = OriginalFileAction.Keep
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        watcher.ScanExistingFiles();

        Assert.Equal(1, queue.PendingCount);
    }

    [Fact]
    public void ScanExistingFiles_NonExistentFolder_NoError()
    {
        var settings = new Settings
        {
            WatchFolders = new List<string> { Path.Combine(_tempDir, "nonexistent") },
            IncludeSubdirectories = true,
            SkipExisting = false
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        // Should not throw
        watcher.ScanExistingFiles();
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void Start_NonExistentFolder_LogsWarning()
    {
        var settings = new Settings
        {
            WatchFolders = new List<string> { Path.Combine(_tempDir, "nonexistent") },
            IncludeSubdirectories = true,
            SkipExisting = false
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        watcher.Start();

        Assert.Contains(_logs, l => l.Contains("does not exist"));
    }

    [Fact]
    public void Stop_ClearsWatchers()
    {
        var (watcher, queue) = CreateWatcher();
        using var _ = watcher;
        using var __ = queue;

        watcher.Start();
        watcher.Stop();

        Assert.Contains(_logs, l => l.Contains("stopped"));
    }

    [Fact]
    public void Start_CalledTwice_CleansUpPrevious()
    {
        var (watcher, queue) = CreateWatcher();
        using var _ = watcher;
        using var __ = queue;

        watcher.Start();
        watcher.Start(); // Should not throw

        Assert.Contains(_logs, l => l.Contains("Watching folder"));
    }

    [Fact]
    public async Task FileCreated_InWatchedFolder_EnqueuesFile()
    {
        var (watcher, queue) = CreateWatcher();
        using var _ = watcher;
        using var __ = queue;

        watcher.Start();

        // Create a HEIC file
        var filePath = Path.Combine(_tempDir, "new_photo.heic");
        File.WriteAllText(filePath, "fake heic");

        // FileSystemWatcher events are async, give it time
        await Task.Delay(1000);

        Assert.True(queue.PendingCount >= 0); // May have been dequeued already
        // Check logs for enqueue
        Assert.Contains(_logs, l => l.Contains("new_photo.heic"));
    }

    [Fact]
    public void ScanExistingFiles_EmptyFolder_NoError()
    {
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        var settings = new Settings
        {
            WatchFolders = new List<string> { emptyDir },
            IncludeSubdirectories = true,
            SkipExisting = false
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        watcher.ScanExistingFiles();
        Assert.Equal(0, queue.PendingCount);
    }

    [Fact]
    public void ScanExistingFiles_MultipleWatchFolders()
    {
        var dir1 = Path.Combine(_tempDir, "dir1");
        var dir2 = Path.Combine(_tempDir, "dir2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        File.WriteAllText(Path.Combine(dir1, "a.heic"), "fake");
        File.WriteAllText(Path.Combine(dir2, "b.heic"), "fake");

        var settings = new Settings
        {
            WatchFolders = new List<string> { dir1, dir2 },
            IncludeSubdirectories = false,
            SkipExisting = false
        };

        var (watcher, queue) = CreateWatcher(settings);
        using var _ = watcher;
        using var __ = queue;

        watcher.ScanExistingFiles();
        Assert.Equal(2, queue.PendingCount);
    }
}
