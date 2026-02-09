using System.IO;
using HEICAutoConverter.Core;
using Xunit;

namespace HEICAutoConverter.Tests;

public class SettingsTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HEICTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void DefaultSettings_HasExpectedValues()
    {
        var settings = new Settings();

        Assert.Empty(settings.WatchFolders);
        Assert.Equal(OutputStrategy.SameFolder, settings.OutputStrategy);
        Assert.Equal(string.Empty, settings.CustomOutputFolder);
        Assert.Equal(string.Empty, settings.ArchiveFolder);
        Assert.Equal(OriginalFileAction.Keep, settings.OriginalFileAction);
        Assert.Equal(95, settings.JpegQuality);
        Assert.True(settings.IncludeSubdirectories);
        Assert.False(settings.StartWithWindows);
        Assert.True(settings.ShowNotifications);
        Assert.True(settings.StartMinimized);
        Assert.Equal(2, settings.MaxConcurrentConversions);
        Assert.Equal("{name}", settings.FileNamingPattern);
        Assert.True(settings.SkipExisting);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllProperties()
    {
        var original = new Settings(_tempDir)
        {
            WatchFolders = new List<string> { @"C:\Photos", @"C:\Downloads" },
            OutputStrategy = OutputStrategy.CustomFolder,
            CustomOutputFolder = @"C:\Converted",
            ArchiveFolder = @"C:\Archive",
            OriginalFileAction = OriginalFileAction.MoveToArchive,
            JpegQuality = 80,
            IncludeSubdirectories = false,
            StartWithWindows = true,
            ShowNotifications = false,
            StartMinimized = false,
            MaxConcurrentConversions = 4,
            FileNamingPattern = "{name}_converted",
            SkipExisting = false
        };

        original.Save();

        var loaded = Settings.Load(_tempDir);

        Assert.Equal(original.WatchFolders, loaded.WatchFolders);
        Assert.Equal(original.OutputStrategy, loaded.OutputStrategy);
        Assert.Equal(original.CustomOutputFolder, loaded.CustomOutputFolder);
        Assert.Equal(original.ArchiveFolder, loaded.ArchiveFolder);
        Assert.Equal(original.OriginalFileAction, loaded.OriginalFileAction);
        Assert.Equal(original.JpegQuality, loaded.JpegQuality);
        Assert.Equal(original.IncludeSubdirectories, loaded.IncludeSubdirectories);
        Assert.Equal(original.StartWithWindows, loaded.StartWithWindows);
        Assert.Equal(original.ShowNotifications, loaded.ShowNotifications);
        Assert.Equal(original.StartMinimized, loaded.StartMinimized);
        Assert.Equal(original.MaxConcurrentConversions, loaded.MaxConcurrentConversions);
        Assert.Equal(original.FileNamingPattern, loaded.FileNamingPattern);
        Assert.Equal(original.SkipExisting, loaded.SkipExisting);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var nonExistentDir = Path.Combine(_tempDir, "nonexistent");
        var settings = Settings.Load(nonExistentDir);

        Assert.NotNull(settings);
        Assert.Empty(settings.WatchFolders);
        Assert.Equal(95, settings.JpegQuality);
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "NOT VALID JSON {{{");

        var settings = Settings.Load(_tempDir);

        Assert.NotNull(settings);
        Assert.Empty(settings.WatchFolders);
        Assert.Equal(95, settings.JpegQuality);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new Settings
        {
            WatchFolders = new List<string> { @"C:\Photos" },
            JpegQuality = 80,
            OutputStrategy = OutputStrategy.CustomFolder,
            CustomOutputFolder = @"C:\Output"
        };

        var clone = original.Clone();

        // Modify original
        original.WatchFolders.Add(@"C:\More");
        original.JpegQuality = 50;

        // Clone should be unaffected
        Assert.Single(clone.WatchFolders);
        Assert.Equal(80, clone.JpegQuality);
        Assert.Equal(OutputStrategy.CustomFolder, clone.OutputStrategy);
        Assert.Equal(@"C:\Output", clone.CustomOutputFolder);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        var nestedDir = Path.Combine(_tempDir, "sub", "folder");
        // The Settings constructor doesn't create the directory,
        // but Save() should via Directory.CreateDirectory
        var settings = new Settings(nestedDir);
        settings.WatchFolders.Add(@"C:\Test");

        settings.Save();

        Assert.True(File.Exists(Path.Combine(nestedDir, "settings.json")));
    }

    [Fact]
    public void Save_OverwritesPreviousSettings()
    {
        var settings = new Settings(_tempDir) { JpegQuality = 90 };
        settings.Save();

        settings.JpegQuality = 50;
        settings.Save();

        var loaded = Settings.Load(_tempDir);
        Assert.Equal(50, loaded.JpegQuality);
    }

    [Fact]
    public void Load_PartialJson_FillsDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        // JSON with only some properties set
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"),
            """{"JpegQuality": 42, "WatchFolders": ["C:\\Test"]}""");

        var settings = Settings.Load(_tempDir);

        Assert.Equal(42, settings.JpegQuality);
        Assert.Single(settings.WatchFolders);
        Assert.Equal(@"C:\Test", settings.WatchFolders[0]);
        // Other properties should have defaults
        Assert.Equal(OutputStrategy.SameFolder, settings.OutputStrategy);
        Assert.True(settings.IncludeSubdirectories);
    }
}
