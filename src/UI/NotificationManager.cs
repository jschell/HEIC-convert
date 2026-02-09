using System.IO;
using System.Windows.Forms;
using HEICAutoConverter.Core;

namespace HEICAutoConverter.UI;

public class NotificationManager
{
    private readonly NotifyIcon _trayIcon;
    private readonly Settings _settings;
    private int _batchCount;
    private DateTime _lastNotification = DateTime.MinValue;
    private readonly object _batchLock = new();
    private System.Threading.Timer? _batchTimer;
    private static readonly TimeSpan BatchWindow = TimeSpan.FromSeconds(3);

    public NotificationManager(NotifyIcon trayIcon, Settings settings)
    {
        _trayIcon = trayIcon;
        _settings = settings;
    }

    public void NotifyConversion(ConversionResult result)
    {
        if (!_settings.ShowNotifications) return;

        if (result.Success)
        {
            lock (_batchLock)
            {
                _batchCount++;
                // Reset or start batch timer
                _batchTimer?.Dispose();
                _batchTimer = new System.Threading.Timer(
                    FlushBatchNotification, null, BatchWindow, System.Threading.Timeout.InfiniteTimeSpan);
            }
        }
        else
        {
            ShowBalloon("Conversion Failed",
                $"Failed to convert {Path.GetFileName(result.SourcePath)}: {result.ErrorMessage}",
                ToolTipIcon.Error);
        }
    }

    private void FlushBatchNotification(object? state)
    {
        int count;
        lock (_batchLock)
        {
            count = _batchCount;
            _batchCount = 0;
        }

        if (count == 1)
        {
            ShowBalloon("Conversion Complete",
                "1 HEIC file converted to JPG",
                ToolTipIcon.Info);
        }
        else if (count > 1)
        {
            ShowBalloon("Conversion Complete",
                $"{count} HEIC files converted to JPG",
                ToolTipIcon.Info);
        }
    }

    public void NotifyError(string message)
    {
        if (!_settings.ShowNotifications) return;
        ShowBalloon("HEIC Auto Converter", message, ToolTipIcon.Error);
    }

    public void NotifyInfo(string message)
    {
        if (!_settings.ShowNotifications) return;
        ShowBalloon("HEIC Auto Converter", message, ToolTipIcon.Info);
    }

    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        try
        {
            _trayIcon.ShowBalloonTip(3000, title, text, icon);
        }
        catch
        {
            // Tray icon may be disposed
        }
    }
}
