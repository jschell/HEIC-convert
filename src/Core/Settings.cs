using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("HEICAutoConverter.Tests")]

namespace HEICAutoConverter.Core;

public enum OutputStrategy
{
    SameFolder,
    CustomFolder,
    MirrorStructure
}

public enum OriginalFileAction
{
    Keep,
    Delete,
    MoveToArchive
}

public class Settings
{
    private static readonly string DefaultAppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HEICAutoConverter");

    private readonly string _appDataFolder;
    private readonly string _settingsPath;

    public List<string> WatchFolders { get; set; } = new();
    public OutputStrategy OutputStrategy { get; set; } = OutputStrategy.SameFolder;
    public string CustomOutputFolder { get; set; } = string.Empty;
    public string ArchiveFolder { get; set; } = string.Empty;
    public OriginalFileAction OriginalFileAction { get; set; } = OriginalFileAction.Keep;
    public int JpegQuality { get; set; } = 95;
    public bool IncludeSubdirectories { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public bool StartMinimized { get; set; } = true;
    public int MaxConcurrentConversions { get; set; } = 2;
    public string FileNamingPattern { get; set; } = "{name}";
    public bool SkipExisting { get; set; } = true;

    [JsonIgnore]
    public static string AppData => DefaultAppDataFolder;

    public Settings() : this(null) { }

    internal Settings(string? customAppDataFolder)
    {
        _appDataFolder = customAppDataFolder ?? DefaultAppDataFolder;
        _settingsPath = Path.Combine(_appDataFolder, "settings.json");
    }

    public void Save()
    {
        Directory.CreateDirectory(_appDataFolder);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(this, options));
    }

    public static Settings Load() => Load(null);

    internal static Settings Load(string? customAppDataFolder)
    {
        var folder = customAppDataFolder ?? DefaultAppDataFolder;
        var path = Path.Combine(folder, "settings.json");

        if (!File.Exists(path))
            return new Settings(customAppDataFolder);

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings(customAppDataFolder);
            // Re-initialize the paths since they aren't serialized
            return new Settings(customAppDataFolder)
            {
                WatchFolders = settings.WatchFolders,
                OutputStrategy = settings.OutputStrategy,
                CustomOutputFolder = settings.CustomOutputFolder,
                ArchiveFolder = settings.ArchiveFolder,
                OriginalFileAction = settings.OriginalFileAction,
                JpegQuality = settings.JpegQuality,
                IncludeSubdirectories = settings.IncludeSubdirectories,
                StartWithWindows = settings.StartWithWindows,
                ShowNotifications = settings.ShowNotifications,
                StartMinimized = settings.StartMinimized,
                MaxConcurrentConversions = settings.MaxConcurrentConversions,
                FileNamingPattern = settings.FileNamingPattern,
                SkipExisting = settings.SkipExisting
            };
        }
        catch
        {
            return new Settings(customAppDataFolder);
        }
    }

    public Settings Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
    }

    public void UpdateStartWithWindows()
    {
        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startupFolder, "HEICAutoConverter.lnk");

        if (StartWithWindows)
        {
            CreateShortcut(shortcutPath);
        }
        else if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    private static void CreateShortcut(string shortcutPath)
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        // Use Windows Script Host to create shortcut
        var script = $@"
Set ws = CreateObject(""WScript.Shell"")
Set shortcut = ws.CreateShortcut(""{shortcutPath}"")
shortcut.TargetPath = ""{exePath}""
shortcut.Arguments = ""--minimized""
shortcut.WorkingDirectory = ""{Path.GetDirectoryName(exePath)}""
shortcut.Description = ""HEIC Auto Converter""
shortcut.Save";

        var vbsPath = Path.Combine(Path.GetTempPath(), "create_shortcut.vbs");
        File.WriteAllText(vbsPath, script);
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cscript",
                Arguments = $"//nologo \"{vbsPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit(5000);
        }
        finally
        {
            try { File.Delete(vbsPath); } catch { }
        }
    }
}
