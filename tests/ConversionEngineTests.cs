using System.IO;
using HEICAutoConverter.Core;
using Xunit;

namespace HEICAutoConverter.Tests;

public class ConversionEngineTests : IDisposable
{
    private readonly string _tempDir;
    private readonly List<string> _logs = new();
    private readonly Settings _settings;
    private readonly ConversionEngine _engine;

    public ConversionEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HEICTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        _settings = new Settings
        {
            WatchFolders = new List<string> { _tempDir },
            OutputStrategy = OutputStrategy.SameFolder,
            FileNamingPattern = "{name}",
            JpegQuality = 95,
            SkipExisting = true
        };

        _engine = new ConversionEngine(_settings, msg => _logs.Add(msg));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // --- Output Path Tests ---

    [Fact]
    public void GetOutputPath_SameFolder_ReturnsJpgInSameDirectory()
    {
        var source = Path.Combine(_tempDir, "photo.heic");
        var expected = Path.Combine(_tempDir, "photo.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetOutputPath_CustomFolder_ReturnsJpgInCustomDirectory()
    {
        var customDir = Path.Combine(_tempDir, "output");
        _settings.OutputStrategy = OutputStrategy.CustomFolder;
        _settings.CustomOutputFolder = customDir;

        var source = Path.Combine(_tempDir, "photo.heic");
        var expected = Path.Combine(customDir, "photo.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetOutputPath_MirrorStructure_PreservesSubdirectoryPath()
    {
        var customDir = Path.Combine(_tempDir, "output");
        var subDir = Path.Combine(_tempDir, "vacation", "beach");
        _settings.OutputStrategy = OutputStrategy.MirrorStructure;
        _settings.CustomOutputFolder = customDir;

        var source = Path.Combine(subDir, "photo.heic");
        var expected = Path.Combine(customDir, "vacation", "beach", "photo.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetOutputPath_CustomNamingPattern_AppliesPattern()
    {
        _settings.FileNamingPattern = "{name}_converted";

        var source = Path.Combine(_tempDir, "photo.heic");
        var expected = Path.Combine(_tempDir, "photo_converted.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetOutputPath_HeifExtension_ReturnsJpg()
    {
        var source = Path.Combine(_tempDir, "image.heif");
        var expected = Path.Combine(_tempDir, "image.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetOutputPath_FileNameWithSpaces_HandledCorrectly()
    {
        var source = Path.Combine(_tempDir, "my vacation photo.heic");
        var expected = Path.Combine(_tempDir, "my vacation photo.jpg");

        var result = _engine.GetOutputPath(source);

        Assert.Equal(expected, result);
    }

    // --- Mirror Path Tests ---

    [Fact]
    public void GetMirroredPath_NoMatchingWatchFolder_FallsBackToCustomFolder()
    {
        var customDir = Path.Combine(_tempDir, "output");
        _settings.CustomOutputFolder = customDir;
        _settings.WatchFolders = new List<string> { Path.Combine(_tempDir, "watched") };

        var source = Path.Combine(_tempDir, "unwatched", "photo.heic");
        var result = _engine.GetMirroredPath(source, "photo.jpg");

        Assert.Equal(Path.Combine(customDir, "photo.jpg"), result);
    }

    [Fact]
    public void GetMirroredPath_RootOfWatchFolder_MirrorsToDot()
    {
        var customDir = Path.Combine(_tempDir, "output");
        _settings.CustomOutputFolder = customDir;
        _settings.WatchFolders = new List<string> { _tempDir };

        var source = Path.Combine(_tempDir, "photo.heic");
        var result = _engine.GetMirroredPath(source, "photo.jpg");

        Assert.Equal(Path.Combine(customDir, ".", "photo.jpg"), result);
    }

    // --- Conversion Error Handling ---

    [Fact]
    public async Task ConvertAsync_NonExistentFile_ReturnsFailure()
    {
        var source = Path.Combine(_tempDir, "nonexistent.heic");

        var result = await _engine.ConvertAsync(source);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(source, result.SourcePath);
    }

    [Fact]
    public async Task ConvertAsync_SkipExisting_WhenOutputExists_ReturnsSuccess()
    {
        _settings.SkipExisting = true;
        var source = Path.Combine(_tempDir, "photo.heic");
        var output = Path.Combine(_tempDir, "photo.jpg");

        // Create both files
        File.WriteAllText(source, "fake heic");
        File.WriteAllText(output, "existing jpg");

        var result = await _engine.ConvertAsync(source);

        Assert.True(result.Success);
        Assert.Contains("Skipping", _logs.Last());
    }

    [Fact]
    public async Task ConvertAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        var source = Path.Combine(_tempDir, "photo.heic");
        File.WriteAllText(source, "fake heic");

        _settings.SkipExisting = false;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _engine.ConvertAsync(source, cts.Token));
    }

    [Fact]
    public async Task ConvertAsync_InvalidFile_ReturnsFailureWithError()
    {
        var source = Path.Combine(_tempDir, "bad.heic");
        File.WriteAllText(source, "this is not a valid HEIC file");
        _settings.SkipExisting = false;

        var result = await _engine.ConvertAsync(source);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(source, result.SourcePath);
        // Output file should be cleaned up
        Assert.False(File.Exists(Path.Combine(_tempDir, "bad.jpg")));
    }

    [Fact]
    public async Task ConvertAsync_InvalidFile_DoesNotDeleteOriginal_WhenKeepSet()
    {
        _settings.OriginalFileAction = OriginalFileAction.Keep;
        _settings.SkipExisting = false;
        var source = Path.Combine(_tempDir, "keep_me.heic");
        File.WriteAllText(source, "fake heic data");

        await _engine.ConvertAsync(source);

        // Original should still exist even after failed conversion
        Assert.True(File.Exists(source));
    }
}
