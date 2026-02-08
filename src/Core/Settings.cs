using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HEICAutoConverter");

    private static readonly string SettingsPath = Path.Combine(AppDataFolder, "settings.json");

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
    public static string AppData => AppDataFolder;

    public void Save()
    {
        Directory.CreateDirectory(AppDataFolder);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, options));
    }

    public static Settings Load()
    {
        if (!File.Exists(SettingsPath))
            return new Settings();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
        }
        catch
        {
            return new Settings();
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
