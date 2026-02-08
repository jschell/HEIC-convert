using HEICAutoConverter.Core;
using Xunit;

namespace HEICAutoConverter.Tests;

public class ConversionResultTests
{
    [Fact]
    public void ConversionResult_DefaultValues()
    {
        var result = new ConversionResult();

        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.SourcePath);
        Assert.Equal(string.Empty, result.OutputPath);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(0, result.OriginalSize);
        Assert.Equal(0, result.ConvertedSize);
        Assert.Equal(TimeSpan.Zero, result.Duration);
    }

    [Fact]
    public void ConversionResult_SuccessResult()
    {
        var result = new ConversionResult
        {
            Success = true,
            SourcePath = @"C:\Photos\img.heic",
            OutputPath = @"C:\Photos\img.jpg",
            OriginalSize = 5_000_000,
            ConvertedSize = 2_000_000,
            Duration = TimeSpan.FromMilliseconds(450)
        };

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(5_000_000, result.OriginalSize);
        Assert.Equal(2_000_000, result.ConvertedSize);
        Assert.Equal(450, result.Duration.TotalMilliseconds);
    }

    [Fact]
    public void ConversionResult_FailureResult()
    {
        var result = new ConversionResult
        {
            Success = false,
            SourcePath = @"C:\Photos\bad.heic",
            OutputPath = @"C:\Photos\bad.jpg",
            ErrorMessage = "Invalid HEIC format"
        };

        Assert.False(result.Success);
        Assert.Equal("Invalid HEIC format", result.ErrorMessage);
    }
}

public class OutputStrategyEnumTests
{
    [Fact]
    public void OutputStrategy_HasExpectedValues()
    {
        Assert.Equal(0, (int)OutputStrategy.SameFolder);
        Assert.Equal(1, (int)OutputStrategy.CustomFolder);
        Assert.Equal(2, (int)OutputStrategy.MirrorStructure);
    }

    [Fact]
    public void OriginalFileAction_HasExpectedValues()
    {
        Assert.Equal(0, (int)OriginalFileAction.Keep);
        Assert.Equal(1, (int)OriginalFileAction.Delete);
        Assert.Equal(2, (int)OriginalFileAction.MoveToArchive);
    }
}
