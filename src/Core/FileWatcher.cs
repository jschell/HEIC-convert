using System.IO;

namespace HEICAutoConverter.Core;

public class FileWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly ConversionQueue _queue;
    private readonly Settings _settings;
    private readonly Action<string> _log;
    private readonly HashSet<string> _activeDirectories = new(StringComparer.OrdinalIgnoreCase);

    // Debounce: track recently seen events to avoid duplicates
    private readonly Dictionary<string, DateTime> _recentEvents = new();
    private readonly object _recentEventsLock = new();
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(2);

    private static readonly string[] HeicExtensions = { "*.heic", "*.heif", "*.HEIC", "*.HEIF" };

    public FileWatcher(ConversionQueue queue, Settings settings, Action<string> log)
    {
        _queue = queue;
        _settings = settings;
        _log = log;
    }

    public void Start()
    {
        Stop();

        foreach (var folder in _settings.WatchFolders)
        {
            if (!Directory.Exists(folder))
            {
                _log($"Watch folder does not exist: {folder}");
                continue;
            }

            if (!_activeDirectories.Add(folder))
                continue;

            foreach (var ext in HeicExtensions)
            {
                var watcher = new FileSystemWatcher
                {
                    Path = folder,
                    Filter = ext,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    IncludeSubdirectories = _settings.IncludeSubdirectories,
                    InternalBufferSize = 65536 // 64KB buffer for high volume
                };

                watcher.Created += OnFileDetected;
                watcher.Renamed += OnFileRenamed;
                watcher.Error += OnWatcherError;
                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
            }

            _log($"Watching folder: {folder} (subdirectories: {_settings.IncludeSubdirectories})");
        }
    }

    public void Stop()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
        _activeDirectories.Clear();
        _log("File watchers stopped");
    }

    public void ScanExistingFiles()
    {
        foreach (var folder in _settings.WatchFolders)
        {
            if (!Directory.Exists(folder)) continue;

            var searchOption = _settings.IncludeSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = new List<string>();
            foreach (var ext in HeicExtensions)
            {
                try
                {
                    files.AddRange(Directory.GetFiles(folder, ext, searchOption));
                }
                catch (Exception ex)
                {
                    _log($"Error scanning {folder} for {ext}: {ex.Message}");
                }
            }

            // Deduplicate (case-insensitive)
            var unique = files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (unique.Count > 0)
            {
                _log($"Found {unique.Count} existing HEIC files in {folder}");
                _queue.EnqueueBatch(unique);
            }
        }
    }

    private void OnFileDetected(object sender, FileSystemEventArgs e)
    {
        if (IsHeicFile(e.FullPath) && !IsDuplicate(e.FullPath))
        {
            _queue.Enqueue(e.FullPath);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (IsHeicFile(e.FullPath) && !IsDuplicate(e.FullPath))
        {
            _queue.Enqueue(e.FullPath);
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _log($"FileSystemWatcher error: {ex.Message}");

        // Attempt to restart watchers
        try
        {
            Start();
        }
        catch (Exception restartEx)
        {
            _log($"Failed to restart watchers: {restartEx.Message}");
        }
    }

    private bool IsDuplicate(string path)
    {
        lock (_recentEventsLock)
        {
            var now = DateTime.UtcNow;

            // Clean old entries
            var expired = _recentEvents
                .Where(kvp => now - kvp.Value > DebounceWindow)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expired)
                _recentEvents.Remove(key);

            if (_recentEvents.ContainsKey(path))
                return true;

            _recentEvents[path] = now;
            return false;
        }
    }

    private static bool IsHeicFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".heic", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".heif", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Stop();
    }
}
