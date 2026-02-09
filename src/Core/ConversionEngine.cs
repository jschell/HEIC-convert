using ImageMagick;
using System.IO;

namespace HEICAutoConverter.Core;

public class ConversionResult
{
    public bool Success { get; init; }
    public string SourcePath { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public long OriginalSize { get; init; }
    public long ConvertedSize { get; init; }
    public TimeSpan Duration { get; init; }
}

public class ConversionEngine
{
    private readonly Settings _settings;
    private readonly Action<string> _log;

    public ConversionEngine(Settings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
    }

    public async Task<ConversionResult> ConvertAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var outputPath = GetOutputPath(sourcePath);

        try
        {
            if (_settings.SkipExisting && File.Exists(outputPath))
            {
                _log($"Skipping (already exists): {outputPath}");
                return new ConversionResult
                {
                    Success = true,
                    SourcePath = sourcePath,
                    OutputPath = outputPath,
                    Duration = stopwatch.Elapsed
                };
            }

            // Wait for file to be fully written / unlocked
            await WaitForFileReady(sourcePath, cancellationToken);

            var originalSize = new FileInfo(sourcePath).Length;

            // Check disk space (need at least 2x source file size)
            var outputDir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(outputDir);

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var image = new MagickImage(sourcePath);
                image.Format = MagickFormat.Jpeg;
                image.Quality = _settings.JpegQuality;

                // Preserve EXIF orientation
                image.AutoOrient();

                cancellationToken.ThrowIfCancellationRequested();
                image.Write(outputPath);
            }, cancellationToken);

            var convertedSize = new FileInfo(outputPath).Length;
            stopwatch.Stop();

            _log($"Converted: {sourcePath} -> {outputPath} ({stopwatch.ElapsedMilliseconds}ms)");

            // Handle original file based on settings
            await HandleOriginalFile(sourcePath, cancellationToken);

            return new ConversionResult
            {
                Success = true,
                SourcePath = sourcePath,
                OutputPath = outputPath,
                OriginalSize = originalSize,
                ConvertedSize = convertedSize,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            // Clean up partial output
            TryDeleteFile(outputPath);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _log($"Error converting {sourcePath}: {ex.Message}");

            // Clean up partial output
            TryDeleteFile(outputPath);

            return new ConversionResult
            {
                Success = false,
                SourcePath = sourcePath,
                OutputPath = outputPath,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    internal string GetOutputPath(string sourcePath)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
        var outputName = _settings.FileNamingPattern.Replace("{name}", nameWithoutExt) + ".jpg";

        return _settings.OutputStrategy switch
        {
            OutputStrategy.CustomFolder => Path.Combine(_settings.CustomOutputFolder, outputName),
            OutputStrategy.MirrorStructure => GetMirroredPath(sourcePath, outputName),
            _ => Path.Combine(Path.GetDirectoryName(sourcePath)!, outputName)
        };
    }

    internal string GetMirroredPath(string sourcePath, string outputName)
    {
        var sourceDir = Path.GetDirectoryName(sourcePath)!;

        // Find which watch folder this file belongs to
        foreach (var watchFolder in _settings.WatchFolders)
        {
            if (sourceDir.StartsWith(watchFolder, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = Path.GetRelativePath(watchFolder, sourceDir);
                var mirroredDir = Path.Combine(_settings.CustomOutputFolder, relativePath);
                return Path.Combine(mirroredDir, outputName);
            }
        }

        return Path.Combine(_settings.CustomOutputFolder, outputName);
    }

    private async Task HandleOriginalFile(string sourcePath, CancellationToken cancellationToken)
    {
        switch (_settings.OriginalFileAction)
        {
            case OriginalFileAction.Delete:
                await Task.Run(() => File.Delete(sourcePath), cancellationToken);
                _log($"Deleted original: {sourcePath}");
                break;

            case OriginalFileAction.MoveToArchive:
                if (!string.IsNullOrEmpty(_settings.ArchiveFolder))
                {
                    var archivePath = Path.Combine(_settings.ArchiveFolder, Path.GetFileName(sourcePath));
                    Directory.CreateDirectory(_settings.ArchiveFolder);
                    await Task.Run(() => File.Move(sourcePath, archivePath, overwrite: true), cancellationToken);
                    _log($"Archived original: {sourcePath} -> {archivePath}");
                }
                break;

            case OriginalFileAction.Keep:
            default:
                break;
        }
    }

    private static async Task WaitForFileReady(string path, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        const int baseDelayMs = 200;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return; // File is ready
            }
            catch (IOException)
            {
                if (i == maxAttempts - 1) throw;
                var delay = baseDelayMs * (int)Math.Pow(2, i);
                await Task.Delay(Math.Min(delay, 5000), cancellationToken);
            }
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
