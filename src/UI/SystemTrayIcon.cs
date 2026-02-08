using System.Drawing;
using System.Windows.Forms;
using HEICAutoConverter.Core;

namespace HEICAutoConverter.UI;

public class SystemTrayIcon : IDisposable
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _pauseResumeItem;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ConversionQueue _queue;
    private readonly Settings _settings;
    private bool _isPaused;
    private System.Threading.Timer? _statusTimer;

    public NotifyIcon TrayIcon => _trayIcon;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action? ScanExistingRequested;

    public SystemTrayIcon(ConversionQueue queue, Settings settings, Icon icon)
    {
        _queue = queue;
        _settings = settings;

        _statusItem = new ToolStripMenuItem("Status: Monitoring")
        {
            Enabled = false
        };

        _pauseResumeItem = new ToolStripMenuItem("Pause", null, OnPauseResume);

        _trayIcon = new NotifyIcon
        {
            Icon = icon,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true,
            Text = "HEIC Auto Converter - Monitoring"
        };

        _trayIcon.DoubleClick += (_, _) => SettingsRequested?.Invoke();

        // Update status periodically
        _statusTimer = new System.Threading.Timer(UpdateStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_pauseResumeItem);
        menu.Items.Add("Convert Existing Files", null, (_, _) => ScanExistingRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => SettingsRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        return menu;
    }

    private void OnPauseResume(object? sender, EventArgs e)
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _queue.Pause();
            _pauseResumeItem.Text = "Resume";
            _trayIcon.Text = "HEIC Auto Converter - Paused";
        }
        else
        {
            _queue.Resume();
            _pauseResumeItem.Text = "Pause";
            _trayIcon.Text = "HEIC Auto Converter - Monitoring";
        }
    }

    private void UpdateStatus(object? state)
    {
        try
        {
            var pending = _queue.PendingCount;
            var processed = _queue.TotalProcessed;
            var failed = _queue.TotalFailed;

            var statusText = pending > 0
                ? $"Status: Converting ({pending} pending)"
                : $"Status: Monitoring ({processed} converted, {failed} failed)";

            _statusItem.Text = statusText;

            var trayText = _isPaused
                ? "HEIC Auto Converter - Paused"
                : pending > 0
                    ? $"HEIC Auto Converter - Converting {pending} file(s)"
                    : "HEIC Auto Converter - Monitoring";

            // NotifyIcon.Text max length is 127 characters
            if (trayText.Length > 127) trayText = trayText[..127];
            _trayIcon.Text = trayText;
        }
        catch { }
    }

    public void Dispose()
    {
        _statusTimer?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
