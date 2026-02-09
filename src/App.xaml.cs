using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using HEICAutoConverter.Core;
using HEICAutoConverter.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HEICAutoConverter;

public partial class App : Application
{
    private Settings _settings = null!;
    private Logger _logger = null!;
    private ConversionEngine _engine = null!;
    private ConversionQueue _queue = null!;
    private FileWatcher _fileWatcher = null!;
    private SystemTrayIcon _trayIcon = null!;
    private NotificationManager _notifications = null!;
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure single instance
        _singleInstanceMutex = new Mutex(true, "HEICAutoConverter_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("HEIC Auto Converter is already running.\nCheck the system tray.",
                "Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Initialize components
        _logger = new Logger();
        _logger.CleanOldLogs();
        _logger.Log("Application starting");

        _settings = Settings.Load();

        _engine = new ConversionEngine(_settings, _logger.Log);
        _queue = new ConversionQueue(_engine, _settings.MaxConcurrentConversions, _logger.Log);

        var icon = LoadAppIcon();
        _trayIcon = new SystemTrayIcon(_queue, _settings, icon);

        _notifications = new NotificationManager(_trayIcon.TrayIcon, _settings);
        _queue.ConversionCompleted += _notifications.NotifyConversion;

        _fileWatcher = new FileWatcher(_queue, _settings, _logger.Log);

        _trayIcon.SettingsRequested += OnSettingsRequested;
        _trayIcon.ExitRequested += OnExitRequested;
        _trayIcon.ScanExistingRequested += OnScanExisting;

        // Start watching if folders are configured
        if (_settings.WatchFolders.Count > 0)
        {
            _fileWatcher.Start();
            _logger.Log($"Monitoring {_settings.WatchFolders.Count} folder(s)");
        }
        else
        {
            _logger.Log("No watch folders configured - opening settings");
            OnSettingsRequested();
        }
    }

    private void OnSettingsRequested()
    {
        var window = new SettingsWindow(_settings);
        if (window.ShowDialog() == true && window.SettingsSaved)
        {
            var newSettings = window.GetSettings();
            ApplySettings(newSettings);
        }
    }

    private void ApplySettings(Settings newSettings)
    {
        var needsRestart = _settings.MaxConcurrentConversions != newSettings.MaxConcurrentConversions;

        // Update settings reference
        _settings.WatchFolders = newSettings.WatchFolders;
        _settings.OutputStrategy = newSettings.OutputStrategy;
        _settings.CustomOutputFolder = newSettings.CustomOutputFolder;
        _settings.ArchiveFolder = newSettings.ArchiveFolder;
        _settings.OriginalFileAction = newSettings.OriginalFileAction;
        _settings.JpegQuality = newSettings.JpegQuality;
        _settings.IncludeSubdirectories = newSettings.IncludeSubdirectories;
        _settings.MaxConcurrentConversions = newSettings.MaxConcurrentConversions;
        _settings.SkipExisting = newSettings.SkipExisting;
        _settings.StartWithWindows = newSettings.StartWithWindows;
        _settings.StartMinimized = newSettings.StartMinimized;
        _settings.ShowNotifications = newSettings.ShowNotifications;
        _settings.FileNamingPattern = newSettings.FileNamingPattern;

        // Restart file watcher with new settings
        _fileWatcher.Stop();

        if (needsRestart)
        {
            // Recreate queue with new concurrency
            _queue.ConversionCompleted -= _notifications.NotifyConversion;
            _queue.Dispose();
            _queue = new ConversionQueue(_engine, _settings.MaxConcurrentConversions, _logger.Log);
            _queue.ConversionCompleted += _notifications.NotifyConversion;
            _fileWatcher = new FileWatcher(_queue, _settings, _logger.Log);
        }

        if (_settings.WatchFolders.Count > 0)
        {
            _fileWatcher.Start();
            _logger.Log("Settings updated, watchers restarted");
        }
    }

    private void OnScanExisting()
    {
        _logger.Log("Scanning for existing HEIC files");
        _fileWatcher.ScanExistingFiles();
    }

    private void OnExitRequested()
    {
        _logger.Log("Application shutting down");
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _fileWatcher?.Dispose();
        _queue?.Dispose();
        _trayIcon?.Dispose();
        _logger?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("icon.ico"));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null) return new Icon(stream);
            }
        }
        catch { }

        // Fallback: create a simple icon programmatically
        return CreateDefaultIcon();
    }

    private static Icon CreateDefaultIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);

        // Draw a simple camera/image icon
        g.Clear(Color.FromArgb(52, 152, 219)); // Blue background
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // White arrow (conversion symbol)
        using var pen = new Pen(Color.White, 2.5f);
        g.DrawLine(pen, 8, 16, 24, 16);
        g.DrawLine(pen, 18, 10, 24, 16);
        g.DrawLine(pen, 18, 22, 24, 16);

        // "H" letter top-left
        using var font = new Font("Segoe UI", 7, System.Drawing.FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        g.DrawString("H", font, brush, 2, 1);

        // "J" letter bottom-right
        g.DrawString("J", font, brush, 20, 20);

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }
}
